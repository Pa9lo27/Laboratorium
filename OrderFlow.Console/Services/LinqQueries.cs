using OrderFlow.Console.Models;
using SysConsole = System.Console;

namespace OrderFlow.Console.Services;

public static class LinqQueries
{
    public static void RunAll(List<Order> orders, List<Customer> customers)
    {
        SysConsole.WriteLine("\n========== LINQ QUERIES ==========\n");

        SysConsole.WriteLine("--- 1. Orders grouped by customer city (query syntax) ---");
        var byCity =
            from o in orders
            join c in customers on o.Customer.Id equals c.Id
            group o by c.City into cityGroup
            select new
            {
                City = cityGroup.Key,
                OrderCount = cityGroup.Count(),
                TotalAmount = cityGroup.Sum(o => o.TotalAmount)
            };

        foreach (var g in byCity)
            SysConsole.WriteLine($"  {g.City}: {g.OrderCount} orders, total {g.TotalAmount:C}");

        SysConsole.WriteLine("\n--- 2. All order items flattened (method syntax) ---");
        var allItems = orders
            .SelectMany(o => o.Items, (o, item) => new
            {
                OrderId = o.Id,
                CustomerName = o.Customer.Name,
                ProductName = item.Product.Name,
                item.Quantity,
                item.TotalPrice
            });

        foreach (var item in allItems)
            SysConsole.WriteLine($"  Order #{item.OrderId} | {item.CustomerName} | {item.ProductName} x{item.Quantity} = {item.TotalPrice:C}");

        SysConsole.WriteLine("\n--- 3. Top customers by total amount (method syntax) ---");
        var topCustomers = orders
            .GroupBy(o => o.Customer.Name)
            .Select(g => new
            {
                CustomerName = g.Key,
                TotalSpent = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent);

        foreach (var c in topCustomers)
            SysConsole.WriteLine($"  {c.CustomerName}: {c.TotalSpent:C} ({c.OrderCount} orders)");

        SysConsole.WriteLine("\n--- 4. Average order value per product category (method syntax) ---");
        var avgByCategory = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Product.Category)
            .Select(g => new
            {
                Category = g.Key,
                AveragePrice = g.Average(i => i.TotalPrice),
                TotalSold = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(x => x.AveragePrice);

        foreach (var cat in avgByCategory)
            SysConsole.WriteLine($"  {cat.Category}: avg {cat.AveragePrice:C}, sold {cat.TotalSold} units");

        SysConsole.WriteLine("\n--- 5. All customers with their orders (GroupJoin / left join, query syntax) ---");
        var customerOrders =
            from c in customers
            join o in orders on c.Id equals o.Customer.Id into customerGroup
            select new
            {
                CustomerName = c.Name,
                c.IsVip,
                OrderCount = customerGroup.Count(),
                TotalSpent = customerGroup.Sum(o => o.TotalAmount)
            };

        foreach (var co in customerOrders)
            SysConsole.WriteLine($"  {co.CustomerName} {(co.IsVip ? "[VIP]" : "     ")}: {co.OrderCount} orders, {co.TotalSpent:C}");

        SysConsole.WriteLine("\n--- 6. Customer report with favourite product category (mixed syntax) ---");
        var report =
            (from o in orders
             group o by o.Customer into customerGroup
             select new
             {
                 CustomerName = customerGroup.Key.Name,
                 IsVip = customerGroup.Key.IsVip,
                 TotalSpent = customerGroup.Sum(o => o.TotalAmount),
                 FavouriteCategory = customerGroup
                     .SelectMany(o => o.Items)
                     .GroupBy(i => i.Product.Category)
                     .OrderByDescending(g => g.Sum(i => i.TotalPrice))
                     .Select(g => g.Key)
                     .FirstOrDefault() ?? "N/A"
             })
            .OrderByDescending(x => x.TotalSpent);

        foreach (var r in report)
            SysConsole.WriteLine($"  {r.CustomerName} {(r.IsVip ? "[VIP]" : "     ")}: {r.TotalSpent:C}, favourite: {r.FavouriteCategory}");
    }
}