using System.Collections.Concurrent;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderStatistics
{

    public int TotalProcessedUnsafe;
    public decimal TotalRevenueUnsafe;


    public int TotalProcessed;
    public decimal TotalRevenue;
    public ConcurrentDictionary<OrderStatus, int> OrdersPerStatus = new();
    public List<string> ProcessingErrors = new();
    private readonly object _lock = new();

    public void Reset()
    {
        TotalProcessedUnsafe = 0;
        TotalRevenueUnsafe = 0;
        TotalProcessed = 0;
        TotalRevenue = 0;
        OrdersPerStatus.Clear();
        ProcessingErrors.Clear();
    }


    public void UpdateUnsafe(Order order)
    {
        TotalProcessedUnsafe++;
        TotalRevenueUnsafe += order.TotalAmount;
    }


    public void UpdateSafe(Order order)
    {
        Interlocked.Increment(ref TotalProcessed);

        lock (_lock)
        {
            TotalRevenue += order.TotalAmount;
        }

        OrdersPerStatus.AddOrUpdate(
            order.Status,
            1,
            (_, count) => count + 1
        );
    }

    public void AddError(string error)
    {
        lock (_lock)
        {
            ProcessingErrors.Add(error);
        }
    }

    public void PrintSafe()
    {
        System.Console.WriteLine($"  TotalProcessed : {TotalProcessed}");
        System.Console.WriteLine($"  TotalRevenue   : {TotalRevenue:C}");
        foreach (var kv in OrdersPerStatus)
            System.Console.WriteLine($"  {kv.Key,-12}: {kv.Value} orders");
        if (ProcessingErrors.Count > 0)
            ProcessingErrors.ForEach(e => System.Console.WriteLine($"  [ERROR] {e}"));
    }

    public void PrintUnsafe()
    {
        System.Console.WriteLine($"  TotalProcessed : {TotalProcessedUnsafe}");
        System.Console.WriteLine($"  TotalRevenue   : {TotalRevenueUnsafe:C}");
    }
}