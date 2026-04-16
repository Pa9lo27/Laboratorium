namespace OrderFlow.Console.Data;

using Models;

public static class SampleData
{
    public static List<Product> Products => new()
    {
        new Product { Id = 1, Name = "Laptop",       Category = "Electronics", Price = 3500m, IsAvailable = true  },
        new Product { Id = 2, Name = "Mouse",         Category = "Electronics", Price = 120m,  IsAvailable = true  },
        new Product { Id = 3, Name = "Desk",          Category = "Furniture",   Price = 850m,  IsAvailable = true  },
        new Product { Id = 4, Name = "C# in Depth",  Category = "Books",       Price = 180m,  IsAvailable = true  },
        new Product { Id = 5, Name = "Coffee Maker",  Category = "Appliances",  Price = 450m,  IsAvailable = false },
        new Product { Id = 6, Name = "Monitor",       Category = "Electronics", Price = 1200m, IsAvailable = true  },
    };

    public static List<Customer> Customers => new()
    {
        new Customer { Id = 1, Name = "Anna Kowalska",  City = "Warsaw",  Email = "anna@mail.com",  IsVip = false },
        new Customer { Id = 2, Name = "Piotr Nowak",    City = "Krakow",  Email = "piotr@mail.com", IsVip = true  },
        new Customer { Id = 3, Name = "Olena Kovalenko",City = "Warsaw",  Email = "olena@mail.com", IsVip = false },
        new Customer { Id = 4, Name = "Dmytro Bondar",  City = "Gdansk",  Email = "dmytro@mail.com",IsVip = true  },
    };

    public static List<Order> Orders
    {
        get
        {
            var p = Products;
            var c = Customers;

            return new List<Order>
            {
                new Order
                {
                    Id = 1, Customer = c[0], Status = OrderStatus.Completed,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1, Product = p[0], Quantity = 1, UnitPrice = p[0].Price },
                        new OrderItem { Id = 2, Product = p[1], Quantity = 2, UnitPrice = p[1].Price },
                    }
                },
                new Order
                {
                    Id = 2, Customer = c[1], Status = OrderStatus.Processing,
                    CreatedAt = DateTime.Now.AddDays(-5),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 3, Product = p[2], Quantity = 1, UnitPrice = p[2].Price },
                        new OrderItem { Id = 4, Product = p[5], Quantity = 2, UnitPrice = p[5].Price },
                    }
                },
                new Order
                {
                    Id = 3, Customer = c[2], Status = OrderStatus.New,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 5, Product = p[3], Quantity = 3, UnitPrice = p[3].Price },
                    }
                },
                new Order
                {
                    Id = 4, Customer = c[3], Status = OrderStatus.Validated,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 6, Product = p[1], Quantity = 1, UnitPrice = p[1].Price },
                        new OrderItem { Id = 7, Product = p[4], Quantity = 1, UnitPrice = p[4].Price },
                    }
                },
                new Order
                {
                    Id = 5, Customer = c[1], Status = OrderStatus.Cancelled,
                    CreatedAt = DateTime.Now.AddDays(-7),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 8, Product = p[0], Quantity = 2, UnitPrice = p[0].Price },
                    }
                },
                new Order
                {
                    Id = 6, Customer = c[0], Status = OrderStatus.Completed,
                    CreatedAt = DateTime.Now.AddDays(-15),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 9,  Product = p[3], Quantity = 1, UnitPrice = p[3].Price },
                        new OrderItem { Id = 10, Product = p[2], Quantity = 1, UnitPrice = p[2].Price },
                    }
                },
            };
        }
    }
}