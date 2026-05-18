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
    public void Calculate_VipClientSmallOrder_Returns10PercentDiscount()
    {
        var order = CreateOrder(isVip: true, unitPrice: 500m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(50m, discount);
    }

    [Fact]
    public void Calculate_StandardClientHighValueOrder_Returns5PercentDiscount()
    {
        var order = CreateOrder(isVip: false, unitPrice: 2000m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(100m, discount);
    }

    [Fact]
    public void Calculate_VipClientHighValueOrder_Returns15PercentDiscount()
    {
        var order = CreateOrder(isVip: true, unitPrice: 2000m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(300m, discount);
    }

    [Fact]
    public void Calculate_VipClientOrderAbove5000_Returns20PercentDiscount()
    {
        var order = CreateOrder(isVip: true, unitPrice: 6000m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(1200m, discount);
    }

    [Fact]
    public void Calculate_DiscountCappedAt25Percent()
    {
        var order = CreateOrder(isVip: true, unitPrice: 10000m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(2000m, discount);
    }

    [Fact]
    public void Calculate_OrderExactly1000_NoHighValueDiscount()
    {
        var order = CreateOrder(isVip: false, unitPrice: 1000m);

        var discount = _calculator.Calculate(order);

        Assert.Equal(0m, discount);
    }

    [Fact]
    public void Calculate_MultipleItems_TotalCalculatedCorrectly()
    {
        var order = new Order
        {
            Customer = new Customer { IsVip = true },
            Items = new List<OrderItem>
            {
                new() { UnitPrice = 300m, Quantity = 2 },
                new() { UnitPrice = 500m, Quantity = 1 }
            }
        };

        var discount = _calculator.Calculate(order);

        // total = 1100, VIP 10% + high value 5% = 15% → 165
        Assert.Equal(165m, discount);
    }
}