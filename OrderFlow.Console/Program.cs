using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

await using var db = new OrderFlowContext();
await db.Database.MigrateAsync();
await DatabaseSeeder.SeedAsync(db);

System.Console.WriteLine("\n========== TASK 2: CRUD ==========\n");

var firstCustomer = await db.Customers.FirstAsync();
var products = await db.Products.Take(2).ToListAsync();

var newOrder = new Order
{
    CustomerId = firstCustomer.Id,
    Status = OrderStatus.New,
    CreatedAt = DateTime.Now,
    Notes = "Test order",
    Items = new List<OrderItem>
    {
        new() { ProductId = products[0].Id, Quantity = 1, UnitPrice = products[0].Price },
        new() { ProductId = products[1].Id, Quantity = 2, UnitPrice = products[1].Price },
    }
};
db.Orders.Add(newOrder);
await db.SaveChangesAsync();
System.Console.WriteLine($"CREATE: Added order #{newOrder.Id} with 2 items for {firstCustomer.Name}");

var allOrders = await db.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .ToListAsync();

System.Console.WriteLine($"\nREAD: {allOrders.Count} orders found:");
foreach (var o in allOrders)
{
    var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);
    System.Console.WriteLine($"  #{o.Id} | {o.Customer.Name} | {o.Status} | {total:C} | {o.Items.Count} items");
}

var orderToUpdate = await db.Orders.FirstAsync(o => o.Status == OrderStatus.New);
orderToUpdate.Status = OrderStatus.Processing;
orderToUpdate.Notes = "Updated via EF Core";
await db.SaveChangesAsync();
System.Console.WriteLine($"\nUPDATE: Order #{orderToUpdate.Id} → Processing, Notes updated");

var cancelledOrder = await db.Orders
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.Cancelled);

if (cancelledOrder is not null)
{
    db.Orders.Remove(cancelledOrder);
    await db.SaveChangesAsync();
    System.Console.WriteLine($"\nDELETE: Removed cancelled order #{cancelledOrder.Id}");
}

System.Console.WriteLine("\nDELETE (Restrict demo): Trying to delete customer with orders...");
try
{
    var customerWithOrders = await db.Customers
        .Include(c => c.Orders)
        .FirstAsync(c => c.Orders.Any());
    db.Customers.Remove(customerWithOrders);
    await db.SaveChangesAsync();
}
catch (Exception ex)
{
    System.Console.WriteLine($"  Exception caught (expected): {ex.InnerException?.Message ?? ex.Message}");
}

await EfLinqQueries.RunAllAsync(db);

var newOrderForTx = await db.Orders
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.New);

if (newOrderForTx is not null)
{
    try { await EfLinqQueries.ProcessOrderAsync(db, newOrderForTx.Id); }
    catch { }
}

System.Console.WriteLine("\n--- Transaction failure scenario ---");
var productLowStock = await db.Products.FirstAsync();
productLowStock.Stock = 0;
await db.SaveChangesAsync();

var orderForFailure = new Order
{
    CustomerId = firstCustomer.Id,
    Status = OrderStatus.New,
    CreatedAt = DateTime.Now,
    Items = new List<OrderItem> { new() { ProductId = productLowStock.Id, Quantity = 5, UnitPrice = productLowStock.Price } }
};
db.Orders.Add(orderForFailure);
await db.SaveChangesAsync();

try { await EfLinqQueries.ProcessOrderAsync(db, orderForFailure.Id); }
catch { }

System.Console.WriteLine("\n========== TASK 1: Events ==========");

var orders = SampleData.Orders;
var customers = SampleData.Customers;

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