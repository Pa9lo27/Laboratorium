using System.Text.Json;

namespace OrderFlow.Console.Services;

public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;

    public CurrencyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal?> GetRateAsync(string currencyCode)
    {
        if (currencyCode.Equals("PLN", StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        var url = $"https://api.nbp.pl/api/exchangerates/rates/A/{currencyCode}/?format=json";
        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new CurrencyServiceException((int)response.StatusCode,
                $"NBP API error for {currencyCode}: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var rate = doc.RootElement
            .GetProperty("rates")[0]
            .GetProperty("mid")
            .GetDecimal();

        return rate;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        var fromRate = await GetRateAsync(fromCurrency);
        var toRate   = await GetRateAsync(toCurrency);

        if (fromRate is null)
            throw new CurrencyServiceException(0, $"Unknown currency: {fromCurrency}");
        if (toRate is null)
            throw new CurrencyServiceException(0, $"Unknown currency: {toCurrency}");

        var amountInPln = amount * fromRate.Value;
        return amountInPln / toRate.Value;
    }
}