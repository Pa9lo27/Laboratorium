using System;
using System.Collections.Generic;
using System.Linq;
using ShopApp.Models;
using ShopApp.Data;

namespace ShopApp.Processing
{
    // =========================================================================
    // ZADANIE 3 — Action, Func, Predicate
    // =========================================================================

    public class OrderProcessor
    {
        private readonly List<Order> _orders;

        public OrderProcessor(List<Order> orders)
        {
            _orders = orders;
        }

        // ── Predicate<Order> — filtrowanie ────────────────────────────────────

        public List<Order> Filter(Predicate<Order> predicate)
        {
            return _orders.FindAll(predicate);
        }

        // ── Action<Order> — akcja na zamówieniach ─────────────────────────────

        public void ForEach(Action<Order> action)
        {
            foreach (var order in _orders)
                action(order);
        }

        public void ForEach(List<Order> orders, Action<Order> action)
        {
            foreach (var order in orders)
                action(order);
        }

        // ── Func<Order, T> — projekcja ────────────────────────────────────────

        public List<T> Project<T>(Func<Order, T> selector)
        {
            return _orders.Select(selector).ToList();
        }

        // ── Func<IEnumerable<Order>, decimal> — agregacja ─────────────────────

        public decimal Aggregate(Func<IEnumerable<Order>, decimal> aggregator)
        {
            return aggregator(_orders);
        }
    }

    // ── Demo ──────────────────────────────────────────────────────────────────

    public static class Zadanie3
    {
        public static void Run()
        {
            Console.WriteLine("\n" + new string('=', 55));
            Console.WriteLine("      ZADANIE 3 — Action, Func, Predicate");
            Console.WriteLine(new string('=', 55));

            var processor = new OrderProcessor(SampleData.Orders);

            // =================================================================
            // 1. Predicate<Order> — min. 3 predykaty
            // =================================================================
            Console.WriteLine("\n--- Predicate<Order>: filtrowanie ---");

            // Predykat 1 — tylko zrealizowane zamówienia
            Predicate<Order> completed = o => o.Status == OrderStatus.Completed;
            var completedOrders = processor.Filter(completed);
            Console.WriteLine($"\n[Predykat 1] Zamówienia Completed ({completedOrders.Count}):");
            completedOrders.ForEach(o => Console.WriteLine($"  {o}"));

            // Predykat 2 — zamówienia z kwotą powyżej 1000 zł
            Predicate<Order> expensive = o => o.TotalAmount > 1_000m;
            var expensiveOrders = processor.Filter(expensive);
            Console.WriteLine($"\n[Predykat 2] Zamówienia > 1000 zł ({expensiveOrders.Count}):");
            expensiveOrders.ForEach(o => Console.WriteLine($"  {o}"));

            // Predykat 3 — zamówienia klientów VIP
            Predicate<Order> vipOnly = o => o.Customer.IsVip;
            var vipOrders = processor.Filter(vipOnly);
            Console.WriteLine($"\n[Predykat 3] Zamówienia klientów VIP ({vipOrders.Count}):");
            vipOrders.ForEach(o => Console.WriteLine($"  {o}"));

            // =================================================================
            // 2. Action<Order> — min. 2 zastosowania
            // =================================================================
            Console.WriteLine("\n--- Action<Order>: akcje ---");

            // Akcja 1 — wypisanie podsumowania zamówienia
            Console.WriteLine("\n[Akcja 1] Podsumowanie wszystkich zamówień:");
            Action<Order> printSummary = o =>
                Console.WriteLine($"  #{o.Id} | {o.Customer.FullName,-20} | {o.Status,-12} | {o.TotalAmount,10:C2}");

            processor.ForEach(printSummary);

            // Akcja 2 — zmiana statusu New → Validated
            Console.WriteLine("\n[Akcja 2] Zmiana statusu New → Validated:");
            var newOrders = processor.Filter(o => o.Status == OrderStatus.New);
            Action<Order> validateOrder = o =>
            {
                Console.WriteLine($"  Zmieniam status: {o} → Validated");
                o.Status = OrderStatus.Validated;
            };
            processor.ForEach(newOrders, validateOrder);

            // Sprawdzenie po zmianie
            Console.WriteLine("  Po zmianie:");
            processor.ForEach(newOrders, o => Console.WriteLine($"    {o}"));

            // =================================================================
            // 3. Func<Order, T> — projekcja (w tym typ anonimowy)
            // =================================================================
            Console.WriteLine("\n--- Func<Order, T>: projekcja ---");

            // Projekcja na string
            Console.WriteLine("\n[Projekcja 1] Id + klient + kwota (string):");
            var summaries = processor.Project(o => $"#{o.Id} {o.Customer.FullName} → {o.TotalAmount:C2}");
            summaries.ForEach(s => Console.WriteLine($"  {s}"));

            // Projekcja na typ anonimowy
            Console.WriteLine("\n[Projekcja 2] Typ anonimowy {{ Id, Klient, Kwota, Status }}:");
            var anonymous = processor.Project(o => new
            {
                o.Id,
                Klient = o.Customer.FullName,
                Kwota  = o.TotalAmount,
                o.Status
            });
            anonymous.ForEach(x => Console.WriteLine($"  {x}"));

            // =================================================================
            // 4. Agregacja — Func<IEnumerable<Order>, decimal>
            // =================================================================
            Console.WriteLine("\n--- Agregacja: Func<IEnumerable<Order>, decimal> ---");

            // Agregator 1 — suma
            Func<IEnumerable<Order>, decimal> sum =
                orders => orders.Sum(o => o.TotalAmount);
            Console.WriteLine($"\n[Agregacja 1] Suma wszystkich zamówień:    {processor.Aggregate(sum),10:C2}");

            // Agregator 2 — średnia
            Func<IEnumerable<Order>, decimal> avg =
                orders => orders.Average(o => o.TotalAmount);
            Console.WriteLine($"[Agregacja 2] Średnia wartość zamówienia:  {processor.Aggregate(avg),10:C2}");

            // Agregator 3 — maksimum
            Func<IEnumerable<Order>, decimal> max =
                orders => orders.Max(o => o.TotalAmount);
            Console.WriteLine($"[Agregacja 3] Najdroższe zamówienie:       {processor.Aggregate(max),10:C2}");
        }
    }
}