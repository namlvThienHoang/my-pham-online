using Microsoft.AspNetCore.Mvc;
using BeautyCommerce.Infrastructure.Saga;

namespace BeautyCommerce.Api.Controllers;

/// <summary>
/// Product API Controller với CRUD, Elasticsearch sync, và autocomplete.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm với filter, sort, facet (cursor pagination).
    /// GET /api/v1/products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? brand,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 20)
    {
        // TODO: Implement with CQRS query handler
        // Query sử dụng Dapper + read model với cursor pagination
        
        var products = new List<ProductDto>(); // Placeholder
        
        return Ok(new PagedResult<ProductDto>
        {
            Items = products,
            NextCursor = null,
            HasMore = false
        });
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm theo ID.
    /// GET /api/v1/products/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        // TODO: Implement with query handler
        var product = new ProductDto { Id = id, Name = "Sample Product" }; // Placeholder
        return Ok(product);
    }

    /// <summary>
    /// Search autocomplete/suggest.
    /// GET /api/v1/products/suggest?q=
    /// </summary>
    [HttpGet("suggest")]
    public async Task<ActionResult<List<SuggestionDto>>> GetSuggestions([FromQuery] string q)
    {
        // TODO: Implement with Elasticsearch suggester
        var suggestions = new List<SuggestionDto>
        {
            new SuggestionDto { Text = q + " sample 1" },
            new SuggestionDto { Text = q + " sample 2" }
        };
        
        return Ok(suggestions);
    }
}

/// <summary>
/// Cart API Controller với atomic reservation, voucher, wallet.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CartsController : ControllerBase
{
    private readonly ILogger<CartsController> _logger;

    public CartsController(ILogger<CartsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lấy cart hiện tại của user.
    /// GET /api/v1/carts/me
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<CartDto>> GetMyCart()
    {
        // TODO: Implement with query handler
        return Ok(new CartDto { Id = Guid.NewGuid(), Items = new List<CartItemDto>() });
    }

    /// <summary>
    /// Thêm item vào cart (với atomic inventory reservation).
    /// POST /api/v1/carts/me/items
    /// </summary>
    [HttpPost("me/items")]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartRequest request)
    {
        // TODO: Implement with command handler
        // Sử dụng SELECT FOR UPDATE SKIP LOCKED để reserve inventory
        return Ok(new CartDto { Id = Guid.NewGuid() });
    }

    /// <summary>
    /// Checkout cart (idempotent, trigger Order Saga).
    /// POST /api/v1/carts/me/checkout
    /// </summary>
    [HttpPost("me/checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout([FromBody] CheckoutRequest request)
    {
        // TODO: Implement with command handler
        // Trigger OrderSaga, trả về orderId và sagaId
        
        var orderId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();
        
        return Ok(new CheckoutResponse
        {
            OrderId = orderId,
            SagaId = sagaId,
            Status = "Created"
        });
    }
}

/// <summary>
/// Orders API Controller.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderSaga _orderSaga;

    public OrdersController(ILogger<OrdersController> logger, IOrderSaga orderSaga)
    {
        _logger = logger;
        _orderSaga = orderSaga;
    }

    /// <summary>
    /// Lấy danh sách orders của user (cursor pagination).
    /// GET /api/v1/orders/me
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetMyOrders(
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 20)
    {
        // TODO: Implement with query handler
        return Ok(new PagedResult<OrderDto>
        {
            Items = new List<OrderDto>(),
            NextCursor = null,
            HasMore = false
        });
    }

    /// <summary>
    /// Lấy chi tiết order.
    /// GET /api/v1/orders/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        // TODO: Implement with query handler
        return Ok(new OrderDto { Id = id });
    }

    /// <summary>
    /// Partial cancel order.
    /// POST /api/v1/orders/{id}/partial-cancel
    /// </summary>
    [HttpPost("{id:guid}/partial-cancel")]
    public async Task<ActionResult> PartialCancel(Guid id, [FromBody] PartialCancelRequest request)
    {
        // TODO: Implement with command handler
        // Compensate chỉ các items được cancel
        return NoContent();
    }

    /// <summary>
    /// Admin: Confirm COD order.
    /// POST /api/v1/admin/orders/{id}/cod-confirm
    /// </summary>
    [HttpPost("admin/{id:guid}/cod-confirm")]
    public async Task<ActionResult> ConfirmCod(Guid id)
    {
        // TODO: Implement with command handler
        return NoContent();
    }
}

/// <summary>
/// Users API Controller.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Lấy recently viewed products của user.
    /// GET /api/v1/users/me/recently-viewed
    /// </summary>
    [HttpGet("me/recently-viewed")]
    public async Task<ActionResult<List<ProductDto>>> GetRecentlyViewed()
    {
        // TODO: Implement with query handler
        return Ok(new List<ProductDto>());
    }

    /// <summary>
    /// Refund to wallet.
    /// POST /api/v1/users/me/wallet/credit
    /// </summary>
    [HttpPost("me/wallet/credit")]
    public async Task<ActionResult<WalletDto>> CreditWallet([FromBody] CreditWalletRequest request)
    {
        // TODO: Implement with command handler
        return Ok(new WalletDto { Balance = request.Amount });
    }
}

// DTOs
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
}

public class SuggestionDto
{
    public string Text { get; set; } = string.Empty;
}

public class CartDto
{
    public Guid Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class CartItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid? VariantId { get; set; }
}

public class CheckoutRequest
{
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? AddressId { get; set; }
    public string? VoucherCode { get; set; }
}

public class CheckoutResponse
{
    public Guid OrderId { get; set; }
    public Guid SagaId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PartialCancelRequest
{
    public List<Guid> ItemIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

public class CreditWalletRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class WalletDto
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
