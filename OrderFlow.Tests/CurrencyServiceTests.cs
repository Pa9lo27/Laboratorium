using System.Net;
using System.Text;
using Moq;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;
    public int CallCount { get; private set; }

    public TestHttpMessageHandler(HttpStatusCode statusCode, string content = "")
    {
        _statusCode = statusCode;
        _content    = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

public class CurrencyServiceTests
{
    private static string MakeNbpJson(decimal rate) =>
        $"{{\"rates\":[{{\"mid\":{rate.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]}}";

    private static CurrencyService CreateService(HttpStatusCode code, string content = "")
    {
        var handler    = new TestHttpMessageHandler(code, content);
        var httpClient = new HttpClient(handler);
        return new CurrencyService(httpClient);
    }

    [Fact]
    public async Task GetRateAsync_ValidCurrency_ReturnsRate()
    {
        var service = CreateService(HttpStatusCode.OK, MakeNbpJson(4.20m));

        var rate = await service.GetRateAsync("USD");

        Assert.Equal(4.20m, rate);
    }

    [Fact]
    public async Task GetRateAsync_PlnCurrency_ReturnsOneWithoutCallingApi()
    {
        var handler    = new TestHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service    = new CurrencyService(httpClient);

        var rate = await service.GetRateAsync("PLN");

        Assert.Equal(1.0m, rate);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetRateAsync_UnknownCurrency_ReturnsNull()
    {
        var service = CreateService(HttpStatusCode.NotFound);

        var rate = await service.GetRateAsync("XYZ");

        Assert.Null(rate);
    }

    [Fact]
    public async Task GetRateAsync_ServerError_ThrowsCurrencyServiceException()
    {
        var service = CreateService(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<CurrencyServiceException>(
            () => service.GetRateAsync("USD"));
    }

    [Fact]
    public async Task GetRateAsync_ValidCurrency_CallsCorrectUrl()
    {
        string? calledUrl = null;
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, MakeNbpJson(4.20m),
            url => calledUrl = url);
        var service = new CurrencyService(new HttpClient(handler));

        await service.GetRateAsync("USD");

        Assert.Contains("/api/exchangerates/rates/A/USD/", calledUrl);
    }

    [Fact]
    public async Task ConvertAsync_UsdToEur_ReturnsCorrectAmount()
    {
        var callCount = 0;
        var handler = new DelegatingHttpMessageHandler(_ =>
        {
            callCount++;
            var rate = callCount == 1 ? 4.0m : 4.5m;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MakeNbpJson(rate), Encoding.UTF8, "application/json")
            };
        });
        var service = new CurrencyService(new HttpClient(handler));

        var result = await service.ConvertAsync(100m, "USD", "EUR");

        // 100 USD * 4.0 PLN = 400 PLN / 4.5 EUR = ~88.88
        Assert.Equal(Math.Round(400m / 4.5m, 10), Math.Round(result, 10));
    }
}

public class CapturingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;
    private readonly Action<string> _onRequest;

    public CapturingHttpMessageHandler(HttpStatusCode statusCode, string content, Action<string> onRequest)
    {
        _statusCode = statusCode;
        _content    = content;
        _onRequest  = onRequest;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _onRequest(request.RequestUri?.ToString() ?? "");
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        });
    }
}

public class DelegatingHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public DelegatingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}