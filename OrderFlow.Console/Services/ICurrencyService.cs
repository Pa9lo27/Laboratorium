namespace OrderFlow.Console.Services;

public interface ICurrencyService
{
    Task<decimal?> GetRateAsync(string currencyCode);
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}