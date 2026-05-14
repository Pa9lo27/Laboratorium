using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    private static Order CreateValidOrder() => new()
    {
        Id = 1,
        Customer = new Customer { Id = 1, Name = "Test" },
        Status = OrderStatus.New,
        CreatedAt = DateTime.Now.AddDays(-1),
        Items = new List<OrderItem>
        {
            new() { Product = new Product { Name = "P1", Price = 100m }, Quantity = 2, UnitPrice = 100m }
        }
    };

    // ===== Named method rules =====

    [Fact]
    public void ValidateAll_OrderWithNoItems_ReturnsHasItemsError()
    {
        var order = CreateValidOrder();
        order.Items.Clear();

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains("Order must contain at least one item", errors);
    }

    [Fact]
    public void ValidateAll_OrderTotalExceedsLimit_ReturnsAmountError()
    {
        var order = CreateValidOrder();
        order.Items.Add(new OrderItem
        {
            Product = new Product { Name = "Expensive", Price = 60000m },
            Quantity = 1,
            UnitPrice = 60000m
        });

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains("Order total cannot exceed 50000", errors);
    }

    [Fact]
    public void ValidateAll_ItemWithZeroQuantity_ReturnsQuantityError()
    {
        var order = CreateValidOrder();
        order.Items[0].Quantity = 0;

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains("All item quantities must be greater than zero", errors);
    }

    // ===== Func lambda rules =====

    [Fact]
    public void ValidateAll_OrderCreatedInFuture_ReturnsDateError()
    {
        var order = CreateValidOrder();
        order.CreatedAt = DateTime.Now.AddDays(5);

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains("Order date cannot be in the future", errors);
    }

    [Fact]
    public void ValidateAll_CancelledOrder_ReturnsCancelledError()
    {
        var order = CreateValidOrder();
        order.Status = OrderStatus.Cancelled;

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains("Cannot validate a cancelled order", errors);
    }

    // ===== ValidateAll combined =====

    [Fact]
    public void ValidateAll_OrderBreakingMultipleRules_ReturnsAllErrors()
    {
        var order = new Order
        {
            Id = 99,
            Customer = new Customer { Id = 1, Name = "Test" },
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.Now.AddDays(3),
            Items = new List<OrderItem>()
        };

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Equal(3, errors.Count);
        Assert.Contains("Order must contain at least one item", errors);
        Assert.Contains("Order date cannot be in the future", errors);
        Assert.Contains("Cannot validate a cancelled order", errors);
    }

    // ===== Theory =====

    [Theory]
    [InlineData(OrderStatus.New,        true)]
    [InlineData(OrderStatus.Validated,  true)]
    [InlineData(OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Completed,  true)]
    [InlineData(OrderStatus.Cancelled,  false)]
    public void ValidateAll_VariousStatuses_ReturnsExpectedResult(OrderStatus status, bool expectedValid)
    {
        var order = CreateValidOrder();
        order.Status = status;

        var (isValid, _) = _validator.ValidateAll(order);

        Assert.Equal(expectedValid, isValid);
    }
}