namespace BeautyEcommerce.Application.Features.Inventory.Commands;

using MediatR;
using BeautyEcommerce.Domain.Interfaces;

/// <summary>
/// Command to add stock to inventory (inbound)
/// </summary>
public record AddStockCommand : IRequest<Guid>
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int Quantity { get; init; }
    public DateTime ManufactureDate { get; init; }
    public DateTime ExpiryDate { get; init; }
    public decimal UnitCost { get; init; }
    public string? SupplierName { get; init; }
    public string? LotNumber { get; init; }
    public Guid PerformedBy { get; init; }
    public string? Reason { get; init; }
}

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, Guid>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddStockCommandHandler> _logger;

    public AddStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddStockCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var lot = new Domain.Entities.InventoryLot
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                LotNumber = request.LotNumber ?? $"LOT-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}",
                Quantity = request.Quantity,
                AvailableQuantity = request.Quantity,
                ReservedQuantity = 0,
                ManufactureDate = request.ManufactureDate,
                ExpiryDate = request.ExpiryDate,
                UnitCost = request.UnitCost,
                SupplierName = request.SupplierName
            };

            await _inventoryRepository.AddStockAsync(lot, cancellationToken);

            // Record stock movement
            // This would be handled by the repository or a separate service

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Added {Quantity} stock for product {ProductId}, lot {LotNumber}", 
                request.Quantity, request.ProductId, lot.LotNumber);

            return lot.Id;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to add stock for product {ProductId}", request.ProductId);
            throw;
        }
    }
}

/// <summary>
/// Command to adjust inventory (for corrections, damages, etc.)
/// </summary>
public record AdjustStockCommand : IRequest<Unit>
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int QuantityChange { get; init; } // Positive for increase, negative for decrease
    public string Reason { get; init; } = string.Empty;
    public Guid PerformedBy { get; init; }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Unit>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdjustStockCommandHandler> _logger;

    public AdjustStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<AdjustStockCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get available lots using FEFO
            var lots = await _inventoryRepository.GetAvailableLotsAsync(
                request.ProductId, 
                request.VariantId, 
                Math.Abs(request.QuantityChange), 
                cancellationToken);

            if (lots.Count == 0 && request.QuantityChange < 0)
            {
                throw new InvalidOperationException("Insufficient stock for adjustment");
            }

            // Adjust stock from lots (FEFO - First Expired, First Out)
            var remainingAdjustment = request.QuantityChange;
            foreach (var lot in lots)
            {
                if (remainingAdjustment == 0) break;

                var adjustmentForLot = request.QuantityChange < 0 
                    ? Math.Min(Math.Abs(remainingAdjustment), lot.AvailableQuantity) * -1
                    : Math.Abs(remainingAdjustment);

                // Update lot quantities
                lot.Quantity += (int)adjustmentForLot;
                lot.AvailableQuantity += (int)adjustmentForLot;
                
                remainingAdjustment -= adjustmentForLot;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Adjusted stock by {QuantityChange} for product {ProductId}", 
                request.QuantityChange, request.ProductId);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to adjust stock for product {ProductId}", request.ProductId);
            throw;
        }
    }
}
