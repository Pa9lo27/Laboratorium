using OrderFlow.Console.Models;
using System.Diagnostics;

namespace OrderFlow.Console.Services;

public class ExternalServiceSimulator
{
    public async Task<bool> CheckInventoryAsync(Product product)
    {
        var delay = Random.Shared.Next(500, 1500);
        await Task.Delay(delay);
        System.Console.WriteLine($"  [Inventory] {product.Name} — OK ({delay}ms)");
        return true;
    }

    public async Task<bool> ValidatePaymentAsync(Order order)
    {
        var delay = Random.Shared.Next(1000, 2000);
        await Task.Delay(delay);
        System.Console.WriteLine($"  [Payment] Order #{order.Id} — OK ({delay}ms)");
        return true;
    }

    public async Task<decimal> CalculateShippingAsync(Order order)
    {
        var delay = Random.Shared.Next(300, 800);
        await Task.Delay(delay);
        var shipping = Math.Round((decimal)Random.Shared.NextDouble() * 50 + 10, 2);
        System.Console.WriteLine($"  [Shipping] Order #{order.Id} — {shipping:C} ({delay}ms)");
        return shipping;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        System.Console.WriteLine($"\n  Processing Order #{order.Id} in parallel...");
        var sw = Stopwatch.StartNew();

        var inventoryTasks = order.Items
            .Select(i => CheckInventoryAsync(i.Product))
            .ToList();

        await Task.WhenAll(
            Task.WhenAll(inventoryTasks),
            ValidatePaymentAsync(order),
            CalculateShippingAsync(order)
        );

        sw.Stop();
        System.Console.WriteLine($"  Order #{order.Id} done in {sw.ElapsedMilliseconds}ms");
    }

    public async Task ProcessMultipleOrdersAsync(List<Order> orders)
    {
        var semaphore = new SemaphoreSlim(3);
        int processed = 0;
        int total = orders.Count;

        var tasks = orders.Select(async order =>
        {
            await semaphore.WaitAsync();
            try
            {
                await ProcessOrderAsync(order);
                int count = Interlocked.Increment(ref processed);
                System.Console.WriteLine($"  Progress: {count}/{total} orders processed");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}