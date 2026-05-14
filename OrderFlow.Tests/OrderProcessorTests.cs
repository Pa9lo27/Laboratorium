using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderProcessorTests
{
    private static List<Order> CreateOrders() => new()
    {
        new Order
        {
            Id = 1,
            Customer = new Customer { Name = "Anna" },
            Status = OrderStatus.Completed,
            CreatedAt = DateTime.Now.AddDays(-10),
            Items = new List<OrderItem>
            {
                new() { Product = new Product { Name = "Laptop", Price = 3500m }, Quantity = 1, UnitPrice = 3500m }
            }
        },
        new Order
        {
            Id = 2,
            Customer = new Customer { Name = "Piotr" },
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now.AddDays(-2),
            Items = new List<OrderItem>
            {
                new() { Product = new Product { Name = "Mouse", Price = 120m }, Quantity = 2, UnitPrice = 120m }
            }
        },
        new Order
        {
            Id = 3,
            Customer = new Customer { Name = "Olena" },
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.Now.AddDays(-5),
            Items = new List<OrderItem>
            {
                new() { Product = new Product { Name = "Desk", Price = 850m }, Quantity = 1, UnitPrice = 850m }
            }
        },
    };

    [Fact]
    public void Filter_ByCompletedStatus_ReturnsOnlyCompletedOrders()
    {
        var processor = new OrderProcessor(CreateOrders());

        var result = processor.Filter(o => o.Status == OrderStatus.Completed);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Aggregate_TotalRevenue_ReturnsSumOfAllOrders()
    {
        var processor = new OrderProcessor(CreateOrders());

        var result = processor.Aggregate(orders => orders.Sum(o => o.TotalAmount));

        Assert.Equal(3500m + 240m + 850m, result);
    }
}