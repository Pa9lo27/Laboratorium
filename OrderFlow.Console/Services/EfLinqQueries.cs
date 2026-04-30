using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Data;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public static class EfLinqQueries
{
    public static async Task RunAllAsync(OrderFlowContext db)
    {
        System.Console.WriteLine("\n========== EF LINQ QUERIES ==========\n");

        // 1. Замовлення VIP клієнтів з сумою > порогу
        decimal threshold = 500m;
        System.Console.WriteLine($"--- 1. VIP orders above {threshold:C} ---");
        var vipOrders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.Customer.IsVip)
            .ToListAsync();

        var vipFiltered = vipOrders
            .Where(o => o.Items.Sum(i => i.UnitPrice * i.Quantity) > threshold)
            .ToList();

        foreach (var o in vipFiltered)
        {
            var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);
            System.Console.WriteLine($"  Order #{o.Id} | {o.Customer.Name} [VIP] | {total:C}");
        }

        // 2. Ranking klientów wg łącznej wartości
        System.Console.WriteLine("\n--- 2. Customer ranking by total order value ---");
        var ranking = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .GroupBy(o => new { o.Customer.Id, o.Customer.Name })
            .Select(g => new
            {
                g.Key.Name,
                Total = g.SelectMany(o => o.Items).Sum(i => i.UnitPrice * i.Quantity),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        foreach (var r in ranking)
            System.Console.WriteLine($"  {r.Name}: {r.Total:C} ({r.OrderCount} orders)");

        // 3. Середня вартість замовлення per місто
        System.Console.WriteLine("\n--- 3. Average order value per city ---");
        var avgByCity = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .GroupBy(o => o.Customer.City)
            .Select(g => new
            {
                City = g.Key,
                AvgValue = g.Average(o => o.Items.Sum(i => i.UnitPrice * i.Quantity))
            })
            .ToListAsync();

        foreach (var c in avgByCity)
            System.Console.WriteLine($"  {c.City}: avg {c.AvgValue:C}");

        // 4. Продукти які ніколи не замовляли (anti-join)
        System.Console.WriteLine("\n--- 4. Products never ordered ---");
        var neverOrdered = await db.Products
            .Where(p => !db.OrderItems.Any(oi => oi.ProductId == p.Id))
            .ToListAsync();

        foreach (var p in neverOrdered)
            System.Console.WriteLine($"  {p.Name} ({p.Category})");

        if (!neverOrdered.Any())
            System.Console.WriteLine("  All products have been ordered.");

        // 5. Динамічний фільтр
        System.Console.WriteLine("\n--- 5. Dynamic filter (Status=New, MinAmount=100) ---");
        OrderStatus? statusFilter = OrderStatus.New;
        decimal minAmount = 100m;

        IQueryable<Order> query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product);

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        var dynamicResult = await query.ToListAsync();
        var filtered = dynamicResult.Where(o => o.Items.Sum(i => i.UnitPrice * i.Quantity) >= minAmount);

        foreach (var o in filtered)
        {
            var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);
            System.Console.WriteLine($"  Order #{o.Id} | {o.Customer.Name} | {o.Status} | {total:C}");
        }
    }

    public static async Task ProcessOrderAsync(OrderFlowContext db, int orderId)
    {
        System.Console.WriteLine($"\n--- Processing Order #{orderId} ---");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var order = await db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null)
                throw new Exception($"Order #{orderId} not found.");

            if (order.Status != OrderStatus.New)
                throw new Exception($"Order #{orderId} is not in New status.");

            order.Status = OrderStatus.Processing;
            await db.SaveChangesAsync();
            System.Console.WriteLine($"  Status → Processing");

            foreach (var item in order.Items)
            {
                if (item.Product.Stock < item.Quantity)
                    throw new Exception($"Insufficient stock for '{item.Product.Name}': has {item.Product.Stock}, needs {item.Quantity}.");

                item.Product.Stock -= item.Quantity;
                System.Console.WriteLine($"  Stock '{item.Product.Name}': {item.Product.Stock + item.Quantity} → {item.Product.Stock}");
            }

            order.Status = OrderStatus.Completed;
            await db.SaveChangesAsync();

            await transaction.CommitAsync();
            System.Console.WriteLine($"  Order #{orderId} completed successfully. ✓");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Console.WriteLine($"  ROLLBACK: {ex.Message}");
            throw;
        }
    }
}