namespace BeautyEcommerce.Application.Features.Cart.Commands;

using MediatR;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;

/// <summary>
/// Add item to cart command
/// </summary>
public record AddToCartCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int Quantity { get; init; }
}

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Unit>
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddToCartCommandHandler> _logger;

    public AddToCartCommandHandler(
        IRepository<Cart> cartRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddToCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get or create cart for user
            var cart = await GetUserCartAsync(request.UserId, cancellationToken);

            // Check if product exists and is published
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null || !product.IsPublished)
            {
                throw new InvalidOperationException("Product not found or not available");
            }

            // Check stock availability
            var availableStock = await _inventoryRepository.GetAvailableStockAsync(
                request.ProductId, 
                request.VariantId, 
                cancellationToken);

            if (availableStock < request.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock. Available: {availableStock}");
            }

            // Get unit price
            decimal unitPrice = request.VariantId.HasValue && product.Variants.Any(v => v.Id == request.VariantId)
                ? product.Variants.First(v => v.Id == request.VariantId).Price
                : product.Price;

            // Check if item already exists in cart
            var existingItem = cart.Items.FirstOrDefault(i => 
                i.ProductId == request.ProductId && 
                i.VariantId == request.VariantId);

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += request.Quantity;
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                // Add new item
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    VariantId = request.VariantId,
                    Quantity = request.Quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = 0,
                    TotalPrice = request.Quantity * unitPrice
                };
                cart.Items.Add(cartItem);
            }

            // Recalculate cart totals
            RecalculateCart(cart);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Added {Quantity} of product {ProductId} to cart for user {UserId}", 
                request.Quantity, request.ProductId, request.UserId);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to add item to cart for user {UserId}", request.UserId);
            throw;
        }
    }

    private async Task<Cart> GetUserCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var carts = await _cartRepository.GetAllAsync(cancellationToken);
        var cart = carts.FirstOrDefault(c => c.UserId == userId && c.ExpiresAt > DateTime.UtcNow);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Subtotal = 0,
                DiscountAmount = 0,
                TaxAmount = 0,
                ShippingAmount = 0,
                Total = 0,
                CurrencyCode = "VND",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            await _cartRepository.AddAsync(cart, cancellationToken);
        }

        return cart;
    }

    private void RecalculateCart(Cart cart)
    {
        cart.Subtotal = cart.Items.Sum(i => i.TotalPrice);
        cart.DiscountAmount = cart.Vouchers.Sum(v => v.DiscountAmount);
        cart.Total = cart.Subtotal - cart.DiscountAmount + cart.TaxAmount + cart.ShippingAmount;
    }
}

/// <summary>
/// Update cart item quantity command
/// </summary>
public record UpdateCartItemQuantityCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public Guid CartItemId { get; init; }
    public int Quantity { get; init; }
}

public class UpdateCartItemQuantityCommandHandler : IRequestHandler<UpdateCartItemQuantityCommand, Unit>
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCartItemQuantityCommandHandler> _logger;

    public UpdateCartItemQuantityCommandHandler(
        IRepository<Cart> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCartItemQuantityCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var carts = await _cartRepository.GetAllAsync(cancellationToken);
            var cart = carts.FirstOrDefault(c => c.UserId == request.UserId);

            if (cart == null)
            {
                throw new InvalidOperationException("Cart not found");
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
            if (item == null)
            {
                throw new InvalidOperationException("Cart item not found");
            }

            if (request.Quantity <= 0)
            {
                // Remove item
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
                item.TotalPrice = item.Quantity * item.UnitPrice;
            }

            // Recalculate cart totals
            cart.Subtotal = cart.Items.Sum(i => i.TotalPrice);
            cart.DiscountAmount = cart.Vouchers.Sum(v => v.DiscountAmount);
            cart.Total = cart.Subtotal - cart.DiscountAmount + cart.TaxAmount + cart.ShippingAmount;

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Updated cart item {CartItemId} quantity to {Quantity}", 
                request.CartItemId, request.Quantity);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update cart item quantity");
            throw;
        }
    }
}

/// <summary>
/// Remove item from cart command
/// </summary>
public record RemoveFromCartCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public Guid CartItemId { get; init; }
}

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Unit>
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveFromCartCommandHandler> _logger;

    public RemoveFromCartCommandHandler(
        IRepository<Cart> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveFromCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var carts = await _cartRepository.GetAllAsync(cancellationToken);
            var cart = carts.FirstOrDefault(c => c.UserId == request.UserId);

            if (cart == null)
            {
                throw new InvalidOperationException("Cart not found");
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
            if (item != null)
            {
                cart.Items.Remove(item);

                // Recalculate cart totals
                cart.Subtotal = cart.Items.Sum(i => i.TotalPrice);
                cart.Total = cart.Subtotal - cart.DiscountAmount + cart.TaxAmount + cart.ShippingAmount;

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Removed cart item {CartItemId} from cart", request.CartItemId);
            }

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to remove item from cart");
            throw;
        }
    }
}

/// <summary>
/// Clear cart command
/// </summary>
public record ClearCartCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
}

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Unit>
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClearCartCommandHandler> _logger;

    public ClearCartCommandHandler(
        IRepository<Cart> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClearCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var carts = await _cartRepository.GetAllAsync(cancellationToken);
            var cart = carts.FirstOrDefault(c => c.UserId == request.UserId);

            if (cart != null)
            {
                cart.Items.Clear();
                cart.Vouchers.Clear();
                cart.Subtotal = 0;
                cart.DiscountAmount = 0;
                cart.TaxAmount = 0;
                cart.ShippingAmount = 0;
                cart.Total = 0;

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Cleared cart for user {UserId}", request.UserId);
            }

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to clear cart for user {UserId}", request.UserId);
            throw;
        }
    }
}
