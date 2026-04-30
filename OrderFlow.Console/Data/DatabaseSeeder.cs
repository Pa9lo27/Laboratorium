using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OrderFlowContext db)
    {
        if (await db.Products.AnyAsync()) return;

        var products = new List<Product>
        {
            new() { Name = "Laptop",       Category = "Electronics", Price = 3500m, IsAvailable = true  },
            new() { Name = "Mouse",        Category = "Electronics", Price = 120m,  IsAvailable = true  },
            new() { Name = "Desk",         Category = "Furniture",   Price = 850m,  IsAvailable = true  },
            new() { Name = "C# in Depth",  Category = "Books",       Price = 180m,  IsAvailable = true  },
            new() { Name = "Coffee Maker", Category = "Appliances",  Price = 450m,  IsAvailable = false },
            new() { Name = "Monitor",      Category = "Electronics", Price = 1200m, IsAvailable = true  },
        };
        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();

        var customers = new List<Customer>
        {
            new() { Name = "Anna Kowalska",   City = "Warsaw", Email = "anna@mail.com",   IsVip = false },
            new() { Name = "Piotr Nowak",     City = "Krakow", Email = "piotr@mail.com",  IsVip = true  },
            new() { Name = "Olena Kovalenko", City = "Warsaw", Email = "olena@mail.com",  IsVip = false },
            new() { Name = "Dmytro Bondar",   City = "Gdansk", Email = "dmytro@mail.com", IsVip = true  },
        };
        await db.Customers.AddRangeAsync(customers);
        await db.SaveChangesAsync();

        var orders = new List<Order>
        {
            new() {
                CustomerId = customers[0].Id, Status = OrderStatus.Completed,
                CreatedAt = DateTime.Now.AddDays(-10),
                Items = new List<OrderItem>
                {
                    new() { ProductId = products[0].Id, Quantity = 1, UnitPrice = products[0].Price },
                    new() { ProductId = products[1].Id, Quantity = 2, UnitPrice = products[1].Price },
                }
            },
            new() {
                CustomerId = customers[1].Id, Status = OrderStatus.Processing,
                CreatedAt = DateTime.Now.AddDays(-5),
                Items = new List<OrderItem>
                {
                    new() { ProductId = products[2].Id, Quantity = 1, UnitPrice = products[2].Price },
                    new() { ProductId = products[5].Id, Quantity = 2, UnitPrice = products[5].Price },
                }
            },
            new() {
                CustomerId = customers[2].Id, Status = OrderStatus.New,
                CreatedAt = DateTime.Now.AddDays(-1),
                Items = new List<OrderItem>
                {
                    new() { ProductId = products[3].Id, Quantity = 3, UnitPrice = products[3].Price },
                }
            },
            new() {
                CustomerId = customers[3].Id, Status = OrderStatus.Validated,
                CreatedAt = DateTime.Now.AddDays(-3),
                Items = new List<OrderItem>
                {
                    new() { ProductId = products[1].Id, Quantity = 1, UnitPrice = products[1].Price },
                    new() { ProductId = products[4].Id, Quantity = 1, UnitPrice = products[4].Price },
                }
            },
            new() {
                CustomerId = customers[1].Id, Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.Now.AddDays(-7),
                Items = new List<OrderItem>
                {
                    new() { ProductId = products[0].Id, Quantity = 2, UnitPrice = products[0].Price },
                }
            },
            new() {
                CustomerId = customers[0].Id, Status = OrderStatus.Completed,
                CreatedAt = DateTime.Now.AddDays(-15),
                Items = new List<OrderItem>
                {
                    new() { ProductId = products[3].Id, Quantity = 1, UnitPrice = products[3].Price },
                    new() { ProductId = products[2].Id, Quantity = 1, UnitPrice = products[2].Price },
                }
            },
        };
        await db.Orders.AddRangeAsync(orders);
        await db.SaveChangesAsync();

        System.Console.WriteLine("Database seeded successfully.");
    }
}