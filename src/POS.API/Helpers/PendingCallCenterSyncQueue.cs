using System.Collections.Concurrent;
using System.Text.Json;
using POS.Contract.Dtos.OrderDtos;
using Serilog;

namespace POS.API.Helpers;

public static class PendingCallCenterSyncQueue
{
    private static readonly ConcurrentDictionary<int, OrderDto> _pendingOrders = new();
    private static readonly string _storageFile = Path.Combine(AppContext.BaseDirectory, "pending_cc_sync.json");
    private static readonly object _fileLock = new();
    private static bool _initialized = false;

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_fileLock)
        {
            if (_initialized) return;
            try
            {
                if (File.Exists(_storageFile))
                {
                    var json = File.ReadAllText(_storageFile);
                    var list = JsonSerializer.Deserialize<List<OrderDto>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (item.Id > 0)
                                _pendingOrders[item.Id] = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PendingSyncQueue] Could not load persisted pending sync orders from file.");
            }
            _initialized = true;
        }
    }

    private static void SaveToFile()
    {
        lock (_fileLock)
        {
            try
            {
                var list = _pendingOrders.Values.ToList();
                var json = JsonSerializer.Serialize(list);
                File.WriteAllText(_storageFile, json);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PendingSyncQueue] Could not save pending sync orders to file.");
            }
        }
    }

    public static void Enqueue(OrderDto order)
    {
        if (order == null || order.Id <= 0) return;
        EnsureInitialized();
        _pendingOrders[order.Id] = order;
        SaveToFile();
        Log.Information("[PendingSyncQueue] Order #{OrderId} (LocalId={Id}, State={State}) queued for Call Center sync retry.", 
            order.OrderId, order.Id, order.OrderState);
    }

    public static bool TryRemove(int orderId)
    {
        EnsureInitialized();
        if (_pendingOrders.TryRemove(orderId, out _))
        {
            SaveToFile();
            return true;
        }
        return false;
    }

    public static IReadOnlyCollection<OrderDto> GetAll()
    {
        EnsureInitialized();
        return _pendingOrders.Values.ToList();
    }

    public static bool IsEmpty
    {
        get
        {
            EnsureInitialized();
            return _pendingOrders.IsEmpty;
        }
    }
}
