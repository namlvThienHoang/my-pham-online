using Microsoft.AspNetCore.SignalR;

namespace BeautyCommerce.Api.Hubs;

/// <summary>
/// SignalR Hub để push realtime order status updates đến client.
/// </summary>
public class OrderStatusHub : Hub
{
    private readonly ILogger<OrderStatusHub> _logger;

    public OrderStatusHub(ILogger<OrderStatusHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client join vào room của order cụ thể để nhận update.
    /// </summary>
    public async Task SubscribeToOrder(Guid orderId)
    {
        var groupName = $"order-{orderId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} subscribed to order {OrderId}", Context.ConnectionId, orderId);
    }

    /// <summary>
    /// Client rời khỏi room của order.
    /// </summary>
    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        var groupName = $"order-{orderId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from order {OrderId}", Context.ConnectionId, orderId);
    }

    /// <summary>
    /// Server gọi method này để push update trạng thái order đến tất cả client trong room.
    /// </summary>
    public static async Task NotifyOrderUpdate(IHubContext<OrderStatusHub> hubContext, Guid orderId, object data)
    {
        var groupName = $"order-{orderId}";
        await hubContext.Clients.Group(groupName).SendAsync("OrderUpdated", orderId, data);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
