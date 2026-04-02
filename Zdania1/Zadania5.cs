using System;
using System.Collections.Generic;
using System.Threading;
using ShopApp.Models;
using ShopApp.Data;
using ShopApp.Validation;

namespace ShopApp.Events
{
    public class OrderStatusChangedEventArgs : EventArgs
    {
        public Order       Order     { get; }
        public OrderStatus OldStatus { get; }
        public OrderStatus NewStatus { get; }
        public DateTime    Timestamp { get; }

        public OrderStatusChangedEventArgs(Order order, OrderStatus oldStatus, OrderStatus newStatus)
        {
            Order     = order;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Timestamp = DateTime.Now;
        }
    }

    public class OrderValidationEventArgs : EventArgs
    {
        public Order        Order   { get; }
        public bool         IsValid { get; }
        public List<string> Errors  { get; }

        public OrderValidationEventArgs(Order order, bool isValid, List<string> errors)
        {
            Order   = order;
            IsValid = isValid;
            Errors  = errors ?? new List<string>();
        }
    }

    public class OrderPipeline
    {
        public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;
        public event EventHandler<OrderValidationEventArgs>?    ValidationCompleted;

        private readonly OrderValidator _validator = new();

        public void ProcessOrder(Order order)
        {
            Console.WriteLine($"\n  ▶ Rozpoczynam przetwarzanie: {order}");

            bool isValid = _validator.ValidateAll(order, out var errors);
            ValidationCompleted?.Invoke(this, new OrderValidationEventArgs(order, isValid, errors));

            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Zamówienie #{order.Id} odrzucone — błędy walidacji.");
                Console.ResetColor();
                return;
            }

            ChangeStatus(order, OrderStatus.Validated);
            Thread.Sleep(50);
            ChangeStatus(order, OrderStatus.Processing);
            Thread.Sleep(50);
            ChangeStatus(order, OrderStatus.Completed);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Zamówienie #{order.Id} zakończone.");
            Console.ResetColor();
        }

        private void ChangeStatus(Order order, OrderStatus newStatus)
        {
            var old = order.Status;
            order.Status = newStatus;
            StatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, old, newStatus));
        }
    }

    public static class Zadanie5
    {
        private static int     _completedCount = 0;
        private static decimal _totalRevenue   = 0m;

        public static void Run()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("      ZADANIE 5 — ZDARZENIA W PROCESIE ZAMOWIENIA");
            Console.WriteLine(new string('=', 60));

            var pipeline = new OrderPipeline();

            pipeline.StatusChanged += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    [LOG]   {e.Timestamp:HH:mm:ss.fff} | Order #{e.Order.Id} | {e.OldStatus} → {e.NewStatus}");
                Console.ResetColor();
            };

            pipeline.StatusChanged += (sender, e) =>
            {
                if (e.NewStatus == OrderStatus.Completed)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    [EMAIL] Wysłano potwierdzenie do: {e.Order.Customer.Email}");
                    Console.ResetColor();
                }
            };

            pipeline.StatusChanged += (sender, e) =>
            {
                if (e.NewStatus == OrderStatus.Completed)
                {
                    _completedCount++;
                    _totalRevenue += e.Order.TotalAmount;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"    [STAT]  Ukończone: {_completedCount} | Przychód: {_totalRevenue:C2}");
                    Console.ResetColor();
                }
            };

            pipeline.ValidationCompleted += (sender, e) =>
            {
                if (!e.IsValid)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"    [VALID] Order #{e.Order.Id} ODRZUCONE:");
                    foreach (var err in e.Errors)
                        Console.WriteLine($"      * {err}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    [VALID] Order #{e.Order.Id} — walidacja OK");
                    Console.ResetColor();
                }
            };

            var orders = new List<Order>
            {
                SampleData.Orders[0],
                SampleData.Orders[3],
                new Order
                {
                    Id        = 99,
                    Customer  = SampleData.Customers[1],
                    OrderDate = DateTime.Now.AddDays(10),
                    Status    = OrderStatus.New,
                    Items     = new() { new OrderItem { Id = 99, Product = SampleData.Products[0], Quantity = -1, UnitPrice = 3499m } }
                }
            };

            foreach (var order in orders)
            {
                order.Status = OrderStatus.New;
                pipeline.ProcessOrder(order);
            }

            Console.WriteLine($"\n  ═══ PODSUMOWANIE: ukończono {_completedCount} zamówień, przychód: {_totalRevenue:C2} ═══");
        }
    }
}