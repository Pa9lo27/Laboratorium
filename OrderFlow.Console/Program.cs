using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using System.Diagnostics;

var orders = SampleData.Orders;
var customers = SampleData.Customers;

System.Console.WriteLine("========== TASK 1: Events ==========");

var pipeline = new OrderPipeline();

pipeline.StatusChanged += (_, e) =>
    System.Console.WriteLine($"  [LOG]   Order #{e.Order.Id}: {e.OldStatus} → {e.NewStatus} at {e.Timestamp:HH:mm:ss}");

pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus == OrderStatus.Completed)
        System.Console.WriteLine($"  [EMAIL] Sending confirmation to {e.Order.Customer.Email}");
};

int completedCount = 0;
pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus == OrderStatus.Completed)
        completedCount++;
};

pipeline.ValidationCompleted += (_, e) =>
{
    if (e.IsValid)
        System.Console.WriteLine($"  [VALID] Order #{e.Order.Id} passed validation");
    else
    {
        System.Console.WriteLine($"  [VALID] Order #{e.Order.Id} FAILED validation:");
        e.Errors.ForEach(err => System.Console.WriteLine($"    - {err}"));
    }
};

pipeline.ProcessOrder(orders[0]);
pipeline.ProcessOrder(orders[2]);

var badOrder = new Order
{
    Id = 99,
    Customer = customers[0],
    Status = OrderStatus.New,
    CreatedAt = DateTime.Now.AddDays(5),
    Items = new List<OrderItem>()
};
pipeline.ProcessOrder(badOrder);

System.Console.WriteLine($"\n  Total completed: {completedCount}");

System.Console.WriteLine("\n========== TASK 2: Async ==========");

var simulator = new ExternalServiceSimulator();

System.Console.WriteLine("\n-- Sequential processing --");
var swSeq = Stopwatch.StartNew();
foreach (var order in orders.Take(3))
    await simulator.ProcessOrderAsync(order);
swSeq.Stop();
System.Console.WriteLine($"\nSequential total: {swSeq.ElapsedMilliseconds}ms");

System.Console.WriteLine("\n-- Parallel processing (max 3 concurrent) --");
var swPar = Stopwatch.StartNew();
await simulator.ProcessMultipleOrdersAsync(orders);
swPar.Stop();
System.Console.WriteLine($"\nParallel total: {swPar.ElapsedMilliseconds}ms");

System.Console.WriteLine($"\nSpeedup: {swSeq.ElapsedMilliseconds}ms → {swPar.ElapsedMilliseconds}ms");

System.Console.WriteLine("\n========== TASK 3: Thread Safety ==========");

var stats = new OrderStatistics();

System.Console.WriteLine("\n-- Unsafe (no synchronization) --");
for (int run = 1; run <= 3; run++)
{
    stats.Reset();
    Parallel.ForEach(orders, order => stats.UpdateUnsafe(order));
    System.Console.WriteLine($"  Run {run}: Processed={stats.TotalProcessedUnsafe}, Revenue={stats.TotalRevenueUnsafe:C}");
}

System.Console.WriteLine("\n-- Safe (with synchronization) --");
for (int run = 1; run <= 3; run++)
{
    stats.Reset();
    Parallel.ForEach(orders, order => stats.UpdateSafe(order));
    System.Console.WriteLine($"  Run {run}: Processed={stats.TotalProcessed}, Revenue={stats.TotalRevenue:C}");
}

System.Console.WriteLine("\n-- Safe detailed stats --");
stats.Reset();
Parallel.ForEach(orders, order => stats.UpdateSafe(order));
stats.PrintSafe();