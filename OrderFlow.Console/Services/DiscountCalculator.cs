using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    private const decimal VipDiscountRate       = 0.10m;
    private const decimal HighValueDiscountRate = 0.05m;
    private const decimal VipBonusDiscountRate  = 0.05m;
    private const decimal MaxDiscountRate       = 0.25m;
    private const decimal HighValueThreshold    = 1000m;
    private const decimal VipBonusThreshold     = 5000m;

    public decimal Calculate(Order order)
    {
        var total = GetTotal(order);
        var rate  = GetDiscountRate(order, total);
        return total * rate;
    }

    private static decimal GetTotal(Order order)
        => order.Items.Sum(i => i.UnitPrice * i.Quantity);

    private static decimal GetDiscountRate(Order order, decimal total)
    {
        var rate = 0m;
        if (order.Customer.IsVip)                              rate += VipDiscountRate;
        if (total > HighValueThreshold)                        rate += HighValueDiscountRate;
        if (order.Customer.IsVip && total > VipBonusThreshold) rate += VipBonusDiscountRate;
        return Math.Min(rate, MaxDiscountRate);
    }
}