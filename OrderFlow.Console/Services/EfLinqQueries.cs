using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Data;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public static class EfLinqQueries
{
    public static async Task RunAllAsync(OrderFlowContext db)
    {
        System.Console.WriteLine("\n========== EF LINQ QUERIES ==========\n");

        decimal threshold = 500m;

        System.Console.WriteLine($"--- 1. VIP orders above {threshold:C} ---");

        var vipOrders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.Customer.IsVip)
            .OrderBy(o => o.Id)
            .ToListAsync();

        var vipFiltered = vipOrders
            .Where(o => o.Items.Sum(i => i.UnitPrice * i.Quantity) > threshold)
            .ToList();

        foreach (var o in vipFiltered)
        {
            var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);

            System.Console.WriteLine(
                $"  Order #{o.Id} | {o.Customer.Name} [VIP] | {total:C}");
        }

        System.Console.WriteLine("\n--- 2. Customer ranking by total order value ---");

        // FIX: SQLite + decimal SUM problem → evaluate in memory
        var ordersForRanking = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ToListAsync();

        var ranking = ordersForRanking
            .GroupBy(o => new { o.Customer.Id, o.Customer.Name })
            .Select(g => new
            {
                Name = g.Key.Name,
                Total = g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity)),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        foreach (var r in ranking)
        {
            System.Console.WriteLine(
                $"  {r.Name}: {r.Total:C} ({r.OrderCount} orders)");
        }

        System.Console.WriteLine("\n--- 3. Average order value per city ---");

        var ordersForCity = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ToListAsync();

        var avgByCity = ordersForCity
            .GroupBy(o => o.Customer.City)
            .Select(g => new
            {
                City = g.Key,
                AvgValue = g.Average(o =>
                    o.Items.Sum(i => i.UnitPrice * i.Quantity))
            })
            .ToList();

        foreach (var c in avgByCity)
        {
            System.Console.WriteLine(
                $"  {c.City}: avg {c.AvgValue:C}");
        }

        System.Console.WriteLine("\n--- 4. Products never ordered ---");

        var neverOrdered = await db.Products
            .Where(p => !db.OrderItems.Any(oi => oi.ProductId == p.Id))
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (!neverOrdered.Any())
        {
            System.Console.WriteLine("  All products have been ordered.");
        }
        else
        {
            foreach (var p in neverOrdered)
            {
                System.Console.WriteLine(
                    $"  {p.Name} ({p.Category})");
            }
        }

        System.Console.WriteLine(
            "\n--- 5. Dynamic filter (Status=New, MinAmount=100) ---");

        OrderStatus? statusFilter = OrderStatus.New;
        decimal minAmount = 100m;

        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == statusFilter.Value);
        }

        var dynamicResult = await query
            .OrderBy(o => o.Id)
            .ToListAsync();

        var filtered = dynamicResult
            .Where(o =>
                o.Items.Sum(i => i.UnitPrice * i.Quantity) >= minAmount);

        foreach (var o in filtered)
        {
            var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);

            System.Console.WriteLine(
                $"  Order #{o.Id} | {o.Customer.Name} | {o.Status} | {total:C}");
        }
    }

    public static async Task ProcessOrderAsync(
        OrderFlowContext db,
        int orderId)
    {
        System.Console.WriteLine($"\n--- Processing Order #{orderId} ---");

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        try
        {
            var order = await db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .OrderBy(o => o.Id)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null)
            {
                throw new Exception($"Order #{orderId} not found.");
            }

            if (order.Status != OrderStatus.New)
            {
                throw new Exception(
                    $"Order #{orderId} is not in New status.");
            }

            order.Status = OrderStatus.Processing;
            await db.SaveChangesAsync();

            System.Console.WriteLine("  Status → Processing");

            foreach (var item in order.Items)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    throw new Exception(
                        $"Insufficient stock for '{item.Product.Name}': has {item.Product.Stock}, needs {item.Quantity}.");
                }

                var previousStock = item.Product.Stock;

                item.Product.Stock -= item.Quantity;

                System.Console.WriteLine(
                    $"  Stock '{item.Product.Name}': {previousStock} → {item.Product.Stock}");
            }

            order.Status = OrderStatus.Completed;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            System.Console.WriteLine(
                $"  Order #{orderId} completed successfully. ✓");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            db.ChangeTracker.Clear();

            System.Console.WriteLine(
                $"  ROLLBACK: {ex.Message}");

            throw;
        }
    }
}