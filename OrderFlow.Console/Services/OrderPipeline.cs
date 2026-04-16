using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Console.Services;

public class OrderPipeline
{
    private readonly OrderValidator _validator = new();

    public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<OrderValidationEventArgs>? ValidationCompleted;

    private void ChangeStatus(Order order, OrderStatus newStatus)
    {
        var old = order.Status;
        order.Status = newStatus;
        StatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, old, newStatus));
    }

    public void ProcessOrder(Order order)
    {
        System.Console.WriteLine($"\n>>> Processing Order #{order.Id} ({order.Customer.Name})");

        // Walidacja
        var (isValid, errors) = _validator.ValidateAll(order);
        ValidationCompleted?.Invoke(this, new OrderValidationEventArgs(order, isValid, errors));

        if (!isValid)
        {
            ChangeStatus(order, OrderStatus.Cancelled);
            return;
        }


        ChangeStatus(order, OrderStatus.Validated);
        Thread.Sleep(100);
        ChangeStatus(order, OrderStatus.Processing);
        Thread.Sleep(100);
        ChangeStatus(order, OrderStatus.Completed);
    }
}