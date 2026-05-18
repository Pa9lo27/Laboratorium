using Moq;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderCurrencyConverterTests
{
    private static Order CreateOrder(decimal unitPrice) => new()
    {
        Customer = new Customer { Name = "Test" },
        Items = new List<OrderItem>
        {
            new() { UnitPrice = unitPrice, Quantity = 1 }
        }
    };

    [Fact]
    public async Task ConvertOrderTotalAsync_ValidOrder_ReturnsConvertedAmount()
    {
        var mockService = new Mock<ICurrencyService>();
        mockService
            .Setup(s => s.ConvertAsync(100m, "PLN", "USD"))
            .ReturnsAsync(25m);

        var converter = new OrderCurrencyConverter(mockService.Object);
        var order     = CreateOrder(100m);

        var result = await converter.ConvertOrderTotalAsync(order, "USD");

        Assert.Equal(25m, result);
    }

    [Fact]
    public async Task ConvertOrderTotalAsync_UnknownCurrency_ThrowsException()
    {
        var mockService = new Mock<ICurrencyService>();
        mockService
            .Setup(s => s.ConvertAsync(It.IsAny<decimal>(), "PLN", "XYZ"))
            .ThrowsAsync(new CurrencyServiceException(0, "Unknown currency: XYZ"));

        var converter = new OrderCurrencyConverter(mockService.Object);
        var order     = CreateOrder(100m);

        await Assert.ThrowsAsync<CurrencyServiceException>(
            () => converter.ConvertOrderTotalAsync(order, "XYZ"));
    }
}