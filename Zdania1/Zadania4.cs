using System;
using System.Collections.Generic;
using System.Linq;
using ShopApp.Models;
using ShopApp.Data;

namespace ShopApp.Queries
{
    // =========================================================================
    // ZADANIE 4 — LINQ (method syntax + query syntax)
    // =========================================================================

    public static class Zadanie4
    {
        public static void Run()
        {
            Console.WriteLine("\n" + new string('=', 55));
            Console.WriteLine("      ZADANIE 4 — LINQ");
            Console.WriteLine(new string('=', 55));

            var orders   = SampleData.Orders;
            var products = SampleData.Products;
            var customers = SampleData.Customers;

            // =================================================================
            // ZAPYTANIE 1 — JOIN: grupowanie zamówień po mieście klienta
            // Składnia: QUERY SYNTAX
            // Dlaczego: join..into..on w query syntax jest czytelniejszy
            //           i bliższy składni SQL — łatwo widać relację.
            // =================================================================
            Console.WriteLine("\n--- [1] JOIN: Zamówienia per miasto (query syntax) ---");

            var ordersByCity =
                from o in orders
                join c in customers on o.Customer.Id equals c.Id
                group o by c.City into cityGroup
                select new
                {
                    Miasto   = cityGroup.Key,
                    Liczba   = cityGroup.Count(),
                    Suma     = cityGroup.Sum(o => o.TotalAmount)
                };

            foreach (var g in ordersByCity.OrderByDescending(x => x.Suma))
                Console.WriteLine($"  {g.Miasto,-12} | zamówień: {g.Liczba} | łącznie: {g.Suma:C2}");

            // =================================================================
            // ZAPYTANIE 2 — SelectMany: spłaszczenie Order → OrderItems → Product
            // Składnia: METHOD SYNTAX
            // Dlaczego: SelectMany jest naturalny w method syntax — łańcuch
            //           .SelectMany().Where().Select() czyta się jak pipeline.
            // =================================================================
            Console.WriteLine("\n--- [2] SelectMany: wszystkie kupione produkty (method syntax) ---");

            var allPurchasedProducts = orders
                .SelectMany(o => o.Items, (o, item) => new
                {
                    Zamowienie = o.Id,
                    Klient     = o.Customer.FullName,
                    Produkt    = item.Product.Name,
                    Kategoria  = item.Product.Category,
                    Ilosc      = item.Quantity,
                    Wartosc    = item.TotalPrice
                })
                .OrderBy(x => x.Kategoria)
                .ThenBy(x => x.Produkt);

            foreach (var x in allPurchasedProducts)
                Console.WriteLine($"  #{x.Zamowienie} {x.Klient,-20} | {x.Kategoria,-15} | {x.Produkt,-25} | x{x.Ilosc} = {x.Wartosc:C2}");

            // =================================================================
            // ZAPYTANIE 3 — GroupBy z agregacją: top klientów wg łącznej kwoty
            // Składnia: METHOD SYNTAX
            // Dlaczego: GroupBy z wieloma agregacjami (.Sum, .Count, .Max)
            //           jest bardziej zwięzły w method syntax.
            // =================================================================
            Console.WriteLine("\n--- [3] GroupBy: top klientów wg kwoty (method syntax) ---");

            var topCustomers = orders
                .GroupBy(o => o.Customer)
                .Select(g => new
                {
                    Klient       = g.Key.FullName,
                    IsVip        = g.Key.IsVip,
                    LiczbaZam    = g.Count(),
                    LacznaKwota  = g.Sum(o => o.TotalAmount),
                    MaxZamowienie = g.Max(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.LacznaKwota);

            foreach (var x in topCustomers)
                Console.WriteLine($"  {x.Klient,-20} {(x.IsVip ? "★VIP" : "    ")} | zamówień: {x.LiczbaZam} | suma: {x.LacznaKwota:C2} | max: {x.MaxZamowienie:C2}");

            // =================================================================
            // ZAPYTANIE 4 — GroupBy z agregacją: średnia wartość per kategoria
            // Składnia: QUERY SYNTAX
            // Dlaczego: group..by..into w query syntax jest bardziej czytelny
            //           gdy chcemy pokazać hierarchię grup.
            // =================================================================
            Console.WriteLine("\n--- [4] GroupBy: średnia wartość per kategoria produktu (query syntax) ---");

            var avgByCategory =
                from o in orders
                from item in o.Items
                group item by item.Product.Category into catGroup
                orderby catGroup.Average(i => i.TotalPrice) descending
                select new
                {
                    Kategoria    = catGroup.Key,
                    SredniWartosc = catGroup.Average(i => i.TotalPrice),
                    LacznaWartosc = catGroup.Sum(i => i.TotalPrice),
                    LiczbaPozycji = catGroup.Count()
                };

            foreach (var x in avgByCategory)
                Console.WriteLine($"  {x.Kategoria,-16} | pozycji: {x.LiczbaPozycji} | avg: {x.SredniWartosc:C2} | suma: {x.LacznaWartosc:C2}");

            // =================================================================
            // ZAPYTANIE 5 — GroupJoin: left join klientów z zamówieniami
            //               (pokazuje też klientów BEZ zamówień)
            // Składnia: QUERY SYNTAX (join..into)
            // Dlaczego: left join pattern z "into" + DefaultIfEmpty jest
            //           bardziej naturalny w query syntax niż method syntax.
            // =================================================================
            Console.WriteLine("\n--- [5] GroupJoin (left join): wszyscy klienci + ich zamówienia ---");

            var customersWithOrders =
                from c in customers
                join o in orders on c.Id equals o.Customer.Id into customerOrders
                select new
                {
                    Klient      = c.FullName,
                    IsVip       = c.IsVip,
                    LiczbaZam   = customerOrders.Count(),
                    LacznaKwota = customerOrders.Any()
                                    ? customerOrders.Sum(o => o.TotalAmount)
                                    : 0m
                };

            foreach (var x in customersWithOrders.OrderByDescending(x => x.LacznaKwota))
                Console.WriteLine($"  {x.Klient,-20} {(x.IsVip ? "★VIP" : "    ")} | zamówień: {x.LiczbaZam} | suma: {x.LacznaKwota:C2}");

            // =================================================================
            // ZAPYTANIE 6 — MIXED SYNTAX: raport per klient z ulubioną kategorią
            // Zewnętrzna pętla: query syntax (czytelny join klientów)
            // Wewnętrzna agregacja: method syntax (SelectMany + GroupBy + MaxBy)
            // Dlaczego: mixed syntax pozwala wybrać najlepszą składnię
            //           dla każdej części zapytania osobno.
            // =================================================================
            Console.WriteLine("\n--- [6] MIXED SYNTAX: raport klienta z ulubioną kategorią ---");

            // Query syntax — zewnętrzna część
            var clientReport =
                from c in customers
                join o in orders on c.Id equals o.Customer.Id into clientOrders
                where clientOrders.Any()
                select new
                {
                    Klient      = c.FullName,
                    IsVip       = c.IsVip,
                    LacznaKwota = clientOrders.Sum(o => o.TotalAmount),
                    // Method syntax — wewnętrzna agregacja ulubionych kategorii
                    UlubionaKategoria = clientOrders
                        .SelectMany(o => o.Items)
                        .GroupBy(i => i.Product.Category)
                        .OrderByDescending(g => g.Sum(i => i.TotalPrice))
                        .Select(g => g.Key)
                        .FirstOrDefault() ?? "brak"
                };

            foreach (var x in clientReport.OrderByDescending(x => x.LacznaKwota))
                Console.WriteLine($"  {x.Klient,-20} {(x.IsVip ? "★VIP" : "    ")} | suma: {x.LacznaKwota:C2} | ulub. kategoria: {x.UlubionaKategoria}");
        }
    }
}