using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calculator = new();

    private static Order CreateOrder(bool isVip, decimal unitPrice, int quantity = 1) => new()
    {
        Customer = new Customer { IsVip = isVip },
        Items = new List<OrderItem> { new() { UnitPrice = unitPrice, Quantity = quantity } }
    };

    [Fact]
    public void Calculate_StandardClientSmallOrder_ReturnsZeroDiscount()
    {
        var order = CreateOrder(isVip: false, unitPrice: 100m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(0m, discount);
    }

    
    [Fact]
    public void Calculate_StandardClientHighValueOrder_Returns5PercentDiscount()
    {
        var calculator = new DiscountCalculator();
        var order = new Order
        {
            Customer = new Customer { IsVip = false },
            Items = new List<OrderItem> { new() { UnitPrice = 2000m, Quantity = 1 } }
        };
    
        var discount = calculator.Calculate(order);
    
        Assert.Equal(100m, discount);
    }
    
    [Fact]
    public void Calculate_VipClientHighValueOrder_Returns15PercentDiscount()
    {
        var calculator = new DiscountCalculator();
        var order = new Order
        {
            Customer = new Customer { IsVip = true },
            Items = new List<OrderItem> { new() { UnitPrice = 2000m, Quantity = 1 } }
        };
    
        var discount = calculator.Calculate(order);
    
        Assert.Equal(300m, discount);
    }
    
    
    [Fact]
    public void Calculate_VipClientOrderAbove5000_Returns20PercentDiscount()
    {
        var calculator = new DiscountCalculator();
        var order = new Order
        {
            Customer = new Customer { IsVip = true },
            Items = new List<OrderItem> { new() { UnitPrice = 6000m, Quantity = 1 } }
        };

        var discount = calculator.Calculate(order);

        Assert.Equal(1200m, discount);
    }
    
    [Fact]
    public void Calculate_DiscountCappedAt25Percent()
    {
        var calculator = new DiscountCalculator();
        var order = new Order
        {
            Customer = new Customer { IsVip = true },
            Items = new List<OrderItem> { new() { UnitPrice = 10000m, Quantity = 1 } }
        };

        var discount = calculator.Calculate(order);

        Assert.Equal(2500m, discount);
    }
    
}