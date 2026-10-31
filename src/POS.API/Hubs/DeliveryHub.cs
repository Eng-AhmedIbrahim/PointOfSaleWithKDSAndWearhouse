namespace POS.API.Hubs;

public class DeliveryHub : Hub
{
    public async Task RegisterDeviceGroup(string machineName, bool isCallCenter)
    {
        if (isCallCenter && !string.IsNullOrEmpty(machineName))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, machineName);
        }
    }
    public async Task SendDeliveryOrderSentSuccess(string machineName, OrderDto orderDto)
    {
        if (!string.IsNullOrEmpty(machineName))
            await Clients.Group(machineName).SendAsync("ReceiveDeliveryOrderSendSuccess", orderDto);
        else
            await Clients.Caller.SendAsync("ReceiveDeliveryOrderSendSuccess", orderDto);
    }

    public async Task SendDeliveryOrderUnsend(string machineName, OrderDto orderDto)
    {
        if (!string.IsNullOrEmpty(machineName))
            await Clients.Group(machineName).SendAsync("ReceiveDeliveryOrderUnsend", orderDto);
        else
            await Clients.Caller.SendAsync("ReceiveDeliveryOrderUnsend", orderDto);
    }

    public async Task SendNewDeliveryOrder(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveNewDeliveryOrder", orderDto);

    public async Task SendOrderVoided(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveOrderVoided", orderDto);


    public async Task SendOrderDispatched(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveOrderDispatched", orderDto);

    public async Task SendOrderCollected(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveOrderCollected", orderDto);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            Console.WriteLine($"Client disconnected with error: {exception.Message}");
        else
            Console.WriteLine("Client disconnected gracefully.");

        await base.OnDisconnectedAsync(exception);
    }
}
