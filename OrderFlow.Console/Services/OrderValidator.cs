using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public delegate bool ValidationRule(Order order, out string errorMessage);

public class OrderValidator
{
    private readonly List<ValidationRule> _rules = new();
    private readonly List<(Func<Order, bool> Rule, string ErrorMessage)> _funcRules = new();

    public OrderValidator()
    {
        _rules.Add(HasItems);
        _rules.Add(AmountWithinLimit);
        _rules.Add(AllQuantitiesPositive);
        
        _funcRules.Add((o => o.CreatedAt <= DateTime.Now, "Order date cannot be in the future"));
        _funcRules.Add((o => o.Status != OrderStatus.Cancelled, "Cannot validate a cancelled order"));
    }

    private static bool HasItems(Order order, out string errorMessage)
    {
        errorMessage = "Order must contain at least one item";
        return order.Items.Count > 0;
    }

    private static bool AmountWithinLimit(Order order, out string errorMessage)
    {
        errorMessage = "Order total cannot exceed 50000";
        return order.TotalAmount <= 50000m;
    }

    private static bool AllQuantitiesPositive(Order order, out string errorMessage)
    {
        errorMessage = "All item quantities must be greater than zero";
        return order.Items.All(i => i.Quantity > 0);
    }

    public (bool IsValid, List<string> Errors) ValidateAll(Order order)
    {
        var errors = new List<string>();

        foreach (var rule in _rules)
        {
            if (!rule(order, out var msg))
                errors.Add(msg);
        }

        foreach (var (rule, msg) in _funcRules)
        {
            if (!rule(order))
                errors.Add(msg);
        }

        return (errors.Count == 0, errors);
    }
}