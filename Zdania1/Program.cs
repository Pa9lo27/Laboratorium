using System;
using System.Collections.Generic;
using System.Linq;

namespace ShopApp.Models
{

    public enum OrderStatus
    {
        New,
        Validated,
        Processing,
        Completed,
        Cancelled
    }


    public class Product
    {
        public int     Id        { get; set; }
        public string  Name      { get; set; } = string.Empty;
        public string  Category  { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int     Stock     { get; set; }

        public override string ToString() =>
            $"[{Id}] {Name} ({Category}) – {UnitPrice:C2}, stock: {Stock}";
    }

    // ── Customer ──────────────────────────────────────────────────────────────

    public class Customer
    {
        public int    Id        { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string City      { get; set; } = string.Empty;
        public bool   IsVip     { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public override string ToString() =>
            $"[{Id}] {FullName} <{Email}> {City}{(IsVip ? " ★VIP" : "")}";
    }


    public class OrderItem
    {
        public int     Id        { get; set; }
        public Product Product   { get; set; } = null!;
        public int     Quantity  { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;

        public override string ToString() =>
            $"  {Product.Name} x{Quantity} @ {UnitPrice:C2} = {TotalPrice:C2}";
    }


    public class Order
    {
        public int             Id        { get; set; }
        public Customer        Customer  { get; set; } = null!;
        public DateTime        OrderDate { get; set; }
        public OrderStatus     Status    { get; set; }
        public List<OrderItem> Items     { get; set; } = new();

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

        public override string ToString() =>
            $"Order #{Id} | {Customer.FullName} | {OrderDate:yyyy-MM-dd} | {Status} | {TotalAmount:C2}";
    }
}

namespace ShopApp.Data
{
    using ShopApp.Models;

    public static class SampleData
    {
        public static readonly List<Product> Products = new()
        {
            new Product { Id = 1,  Name = "Laptop UltraBook 15\"",   Category = "Elektronika",    UnitPrice = 3_499.00m, Stock = 12  },
            new Product { Id = 2,  Name = "Sluchawki BT Pro X",      Category = "Elektronika",    UnitPrice =   349.99m, Stock = 45  },
            new Product { Id = 3,  Name = "Krzeslo biurowe ErgoFit", Category = "Meble",          UnitPrice =   799.00m, Stock =  8  },
            new Product { Id = 4,  Name = "Biurko Standing Desk",    Category = "Meble",          UnitPrice = 1_250.00m, Stock =  5  },
            new Product { Id = 5,  Name = "Kawa Arabica 1 kg",       Category = "Spozywcze",      UnitPrice =    89.90m, Stock = 120 },
            new Product { Id = 6,  Name = "Herbata Sencha 200 g",    Category = "Spozywcze",      UnitPrice =    34.50m, Stock =  80 },
            new Product { Id = 7,  Name = "Czysty kod R. Martin",    Category = "Ksiazki",        UnitPrice =    69.00m, Stock =  30 },
            new Product { Id = 8,  Name = "Design Patterns GoF",     Category = "Ksiazki",        UnitPrice =    79.00m, Stock =  18 },
            new Product { Id = 9,  Name = "Mata do cwiczen 180x60",  Category = "Sport & Fitness",UnitPrice =   129.00m, Stock =  25 },
            new Product { Id = 10, Name = "Kettlebell 16 kg",        Category = "Sport & Fitness",UnitPrice =   189.00m, Stock =  14 },
        };

        public static readonly List<Customer> Customers = new()
        {
            new Customer { Id = 1, FirstName = "Anna",   LastName = "Kowalska",   Email = "anna.kowalska@email.pl", City = "Warszawa", IsVip = true  },
            new Customer { Id = 2, FirstName = "Piotr",  LastName = "Nowak",      Email = "p.nowak@firma.pl",       City = "Krakow",   IsVip = false },
            new Customer { Id = 3, FirstName = "Marta",  LastName = "Wisniewska", Email = "marta.w@webmail.com",    City = "Gdansk",   IsVip = false },
            new Customer { Id = 4, FirstName = "Tomasz", LastName = "Zajac",      Email = "t.zajac@it.pl",          City = "Wroclaw",  IsVip = false },
        };

        public static readonly List<Order> Orders = BuildOrders();

        private static List<Order> BuildOrders()
        {
            var p = Products;
            var c = Customers;

            return new List<Order>
            {
                new Order
                {
                    Id = 1, Customer = c[0], OrderDate = new DateTime(2025, 1, 10), Status = OrderStatus.Completed,
                    Items = new()
                    {
                        new OrderItem { Id = 1, Product = p[0], Quantity = 1, UnitPrice = p[0].UnitPrice },
                        new OrderItem { Id = 2, Product = p[1], Quantity = 2, UnitPrice = p[1].UnitPrice },
                    }
                },
                new Order
                {
                    Id = 2, Customer = c[0], OrderDate = new DateTime(2025, 3, 5), Status = OrderStatus.Processing,
                    Items = new()
                    {
                        new OrderItem { Id = 3, Product = p[2], Quantity = 2, UnitPrice = p[2].UnitPrice },
                        new OrderItem { Id = 4, Product = p[3], Quantity = 1, UnitPrice = p[3].UnitPrice },
                    }
                },
                new Order
                {
                    Id = 3, Customer = c[1], OrderDate = new DateTime(2025, 2, 20), Status = OrderStatus.Validated,
                    Items = new()
                    {
                        new OrderItem { Id = 5, Product = p[6], Quantity = 1, UnitPrice = p[6].UnitPrice },
                        new OrderItem { Id = 6, Product = p[7], Quantity = 1, UnitPrice = p[7].UnitPrice },
                        new OrderItem { Id = 7, Product = p[4], Quantity = 3, UnitPrice = p[4].UnitPrice },
                    }
                },
                new Order
                {
                    Id = 4, Customer = c[2], OrderDate = new DateTime(2025, 3, 18), Status = OrderStatus.New,
                    Items = new()
                    {
                        new OrderItem { Id = 8, Product = p[8], Quantity = 1, UnitPrice = p[8].UnitPrice },
                        new OrderItem { Id = 9, Product = p[9], Quantity = 1, UnitPrice = p[9].UnitPrice },
                    }
                },
                new Order
                {
                    Id = 5, Customer = c[3], OrderDate = new DateTime(2025, 1, 28), Status = OrderStatus.Cancelled,
                    Items = new()
                    {
                        new OrderItem { Id = 10, Product = p[5], Quantity = 4, UnitPrice = p[5].UnitPrice },
                        new OrderItem { Id = 11, Product = p[6], Quantity = 1, UnitPrice = p[6].UnitPrice },
                    }
                },
                new Order
                {
                    Id = 6, Customer = c[3], OrderDate = new DateTime(2025, 3, 1), Status = OrderStatus.Completed,
                    Items = new()
                    {
                        new OrderItem { Id = 12, Product = p[0], Quantity = 1, UnitPrice = 3_299.00m },
                    }
                },
            };
        }
    }
}

namespace ShopApp
{
    using ShopApp.Data;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== PRODUKTY ===");
            foreach (var p in SampleData.Products)
                Console.WriteLine(p);

            Console.WriteLine("\n=== KLIENCI ===");
            foreach (var c in SampleData.Customers)
                Console.WriteLine(c);

            Console.WriteLine("\n=== ZAMOWIENIA ===");
            foreach (var o in SampleData.Orders)
            {
                Console.WriteLine(o);
                foreach (var item in o.Items)
                    Console.WriteLine(item);
            }
        }
    }
}