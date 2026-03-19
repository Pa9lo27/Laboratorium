using System;
using System.Collections.Generic;
using ShopApp.Models;
using ShopApp.Data;

namespace ShopApp.Validation
{

    public delegate bool ValidationRule(Order order, out string errorMessage);


    public class OrderValidator
    {
        
        private readonly List<ValidationRule> _delegateRules = new();

        
        private readonly List<(Func<Order, bool> Rule, string ErrorMessage)> _funcRules = new();

        public OrderValidator()
        {
           
            _delegateRules.Add(HasAtLeastOneItem);
            _delegateRules.Add(TotalAmountWithinLimit);
            _delegateRules.Add(AllQuantitiesPositive);

            
            _funcRules.Add((
                o => o.OrderDate <= DateTime.Now,
                "Data zamówienia nie może być z przyszłości."
            ));
            _funcRules.Add((
                o => o.Status != OrderStatus.Cancelled,
                "Nie można walidować zamówienia ze statusem Cancelled."
            ));
        }

        private static bool HasAtLeastOneItem(Order order, out string errorMessage)
        {
            if (order.Items.Count > 0) { errorMessage = string.Empty; return true; }
            errorMessage = "Zamówienie musi zawierać co najmniej jedną pozycję.";
            return false;
        }

        private static bool TotalAmountWithinLimit(Order order, out string errorMessage)
        {
            const decimal limit = 10_000m;
            if (order.TotalAmount <= limit) { errorMessage = string.Empty; return true; }
            errorMessage = $"Kwota ({order.TotalAmount:C2}) przekracza limit {limit:C2}.";
            return false;
        }

        private static bool AllQuantitiesPositive(Order order, out string errorMessage)
        {
            foreach (var item in order.Items)
            {
                if (item.Quantity <= 0)
                {
                    errorMessage = $"Pozycja '{item.Product.Name}' ma nieprawidłową ilość: {item.Quantity}.";
                    return false;
                }
            }
            errorMessage = string.Empty;
            return true;
        }

        public bool ValidateAll(Order order, out List<string> errors)
        {
            errors = new List<string>();

            foreach (var rule in _delegateRules)
                if (!rule(order, out string msg))
                    errors.Add($"[Delegate] {msg}");

            foreach (var (rule, msg) in _funcRules)
                if (!rule(order))
                    errors.Add($"[Func]     {msg}");

            return errors.Count == 0;
        }
    }

    

    public static class Zadanie2
    {
        public static void Run()
        {
            Console.WriteLine("\n" + new string('=', 55));
            Console.WriteLine("      ZADANIE 2 — WALIDACJA ZAMOWIEN");
            Console.WriteLine(new string('=', 55));

            var validator = new OrderValidator();

            
            PrintValidation(validator, SampleData.Orders[0], "POPRAWNE");

          
            var badOrder = new Order
            {
                Id        = 99,
                Customer  = SampleData.Customers[0],
                OrderDate = DateTime.Now.AddDays(5),
                Status    = OrderStatus.Cancelled,
                Items     = new()
                {
                    new OrderItem { Id = 99,  Product = SampleData.Products[0], Quantity = -1, UnitPrice = 3_499.00m },
                    new OrderItem { Id = 100, Product = SampleData.Products[0], Quantity =  3, UnitPrice = 3_499.00m },
                }
            };
            PrintValidation(validator, badOrder, "BLEDNE");
        }

        private static void PrintValidation(OrderValidator validator, Order order, string label)
        {
            Console.WriteLine($"\n--- Zamówienie {label} ---");
            Console.WriteLine($"    {order}");

            bool isValid = validator.ValidateAll(order, out var errors);

            if (isValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] Zamówienie jest POPRAWNE.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [BLAD] Znaleziono {errors.Count} blad(y/ow):");
                foreach (var e in errors)
                    Console.WriteLine($"    * {e}");
            }
            Console.ResetColor();
        }
    }
}