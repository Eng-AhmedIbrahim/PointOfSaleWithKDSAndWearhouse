using Microsoft.AspNetCore.SignalR;
using POS.Services.CashService;
namespace POS.API.Hubs;

public class PrinterHub : Hub
{

    private readonly RedisService _redisService;

    // إذا كان لديك خدمة RedisService مدمجة في DI
    public PrinterHub(RedisService redisService)
    {
        _redisService = redisService;
    }
    private static readonly Dictionary<string, string> _clients = new Dictionary<string, string>(); // لتخزين الـ ConnectionId واسم الطابعة

    public override async Task OnConnectedAsync()
    {
        string connectionId = Context.ConnectionId;
        string machineName = Environment.MachineName;  // أو Dns.GetHostName()

        // تخزين ConnectionId في Redis مع اسم الجهاز
        await _redisService.SetConnectionIdAsync(machineName, connectionId);

        Console.WriteLine($"Connected: {machineName} - ConnectionId: {connectionId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string connectionId = Context.ConnectionId;
        var db = _redisService.GetDatabase();

        await db.KeyDeleteAsync($"connection:{connectionId}");

        await base.OnDisconnectedAsync(exception);
    }

    // استدعاء الوظيفة الخاصة بالطباعة إلى عميل معين
    public async Task PrintReceipt(string connectionId, string printerName, string receiptContent)
    {
        // إذا كان الـ connectionId موجودًا في القائمة، نقوم بإرسال الرسالة للعميل
        if (_clients.ContainsKey(connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceivePrintJob", printerName, receiptContent);
        }
        else
        {
            // إذا لم يتم العثور على الـ connectionId
            Console.WriteLine($"Client with connectionId {connectionId} not found.");
        }
    }
}
