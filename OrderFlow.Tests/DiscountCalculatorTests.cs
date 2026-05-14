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

    // ... решта тестів
}