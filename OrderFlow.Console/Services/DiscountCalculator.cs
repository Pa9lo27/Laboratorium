using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public decimal Calculate(Order order)
    {
        var total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        var rate = 0m;
        if (order.Customer.IsVip) rate += 0.10m;
        if (total > 1000m) rate += 0.05m;
        if (order.Customer.IsVip && total > 5000m) rate += 0.05m;
        return total * rate;
    }
}