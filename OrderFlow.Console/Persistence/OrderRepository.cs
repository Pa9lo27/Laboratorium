using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class OrderDto
{
    [JsonPropertyName("orderId")]
    public int Id { get; set; }

    [JsonPropertyName("customer")]
    public CustomerDto Customer { get; set; } = null!;

    [JsonPropertyName("items")]
    public List<OrderItemDto> Items { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public string InternalNote { get; set; } = "not serialized";

    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
}

public class CustomerDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("isVip")]
    public bool IsVip { get; set; }
}

public class OrderItemDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonIgnore]
    public string InternalTag { get; set; } = "not serialized";

    public decimal TotalPrice => UnitPrice * Quantity;
}

[XmlRoot("orders")]
public class OrderXmlList
{
    [XmlElement("order")]
    public List<OrderXmlDto> Orders { get; set; } = new();
}

public class OrderXmlDto
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("status")]
    public string Status { get; set; } = string.Empty;

    [XmlElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [XmlElement("customer")]
    public CustomerXmlDto Customer { get; set; } = null!;

    [XmlElement("item")]
    public List<OrderItemXmlDto> Items { get; set; } = new();

    [XmlIgnore]
    public string InternalNote { get; set; } = "not serialized";
}

public class CustomerXmlDto
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("isVip")]
    public bool IsVip { get; set; }

    [XmlElement("name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("city")]
    public string City { get; set; } = string.Empty;

    [XmlElement("email")]
    public string Email { get; set; } = string.Empty;
}

public class OrderItemXmlDto
{
    [XmlAttribute("productId")]
    public int ProductId { get; set; }

    [XmlAttribute("quantity")]
    public int Quantity { get; set; }

    [XmlElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [XmlElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [XmlIgnore]
    public string InternalTag { get; set; } = "not serialized";
}

public class OrderRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        Customer = new CustomerDto
        {
            Id = o.Customer.Id,
            Name = o.Customer.Name,
            City = o.Customer.City,
            Email = o.Customer.Email,
            IsVip = o.Customer.IsVip
        },
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.Product.Id,
            ProductName = i.Product.Name,
            Category = i.Product.Category,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    private static Order FromDto(OrderDto dto) => new()
    {
        Id = dto.Id,
        Status = Enum.Parse<OrderStatus>(dto.Status),
        CreatedAt = dto.CreatedAt,
        Customer = new Customer
        {
            Id = dto.Customer.Id,
            Name = dto.Customer.Name,
            City = dto.Customer.City,
            Email = dto.Customer.Email,
            IsVip = dto.Customer.IsVip
        },
        Items = dto.Items.Select(i => new OrderItem
        {
            Product = new Product
            {
                Id = i.ProductId,
                Name = i.ProductName,
                Category = i.Category,
                Price = i.UnitPrice
            },
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    public async Task SaveToJsonAsync(IEnumerable<Order> orders, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var dtos = orders.Select(ToDto).ToList();
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, dtos, JsonOptions);
    }

    public async Task<List<Order>> LoadFromJsonAsync(string path)
    {
        if (!File.Exists(path)) return new List<Order>();
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, JsonOptions);
            return dtos?.Select(FromDto).ToList() ?? new List<Order>();
        }
        catch
        {
            return new List<Order>();
        }
    }

    private static OrderXmlDto ToXmlDto(Order o) => new()
    {
        Id = o.Id,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        Customer = new CustomerXmlDto
        {
            Id = o.Customer.Id,
            Name = o.Customer.Name,
            City = o.Customer.City,
            Email = o.Customer.Email,
            IsVip = o.Customer.IsVip
        },
        Items = o.Items.Select(i => new OrderItemXmlDto
        {
            ProductId = i.Product.Id,
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    private static Order FromXmlDto(OrderXmlDto dto) => new()
    {
        Id = dto.Id,
        Status = Enum.Parse<OrderStatus>(dto.Status),
        CreatedAt = dto.CreatedAt,
        Customer = new Customer
        {
            Id = dto.Customer.Id,
            Name = dto.Customer.Name,
            City = dto.Customer.City,
            Email = dto.Customer.Email,
            IsVip = dto.Customer.IsVip
        },
        Items = dto.Items.Select(i => new OrderItem
        {
            Product = new Product
            {
                Id = i.ProductId,
                Name = i.ProductName,
                Price = i.UnitPrice
            },
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    public async Task SaveToXmlAsync(IEnumerable<Order> orders, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var xmlList = new OrderXmlList
        {
            Orders = orders.Select(ToXmlDto).ToList()
        };
        var serializer = new XmlSerializer(typeof(OrderXmlList));
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await Task.Run(() => serializer.Serialize(stream, xmlList));
    }

    public async Task<List<Order>> LoadFromXmlAsync(string path)
    {
        if (!File.Exists(path)) return new List<Order>();
        try
        {
            var serializer = new XmlSerializer(typeof(OrderXmlList));
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var result = await Task.Run(() => (OrderXmlList?)serializer.Deserialize(stream));
            return result?.Orders.Select(FromXmlDto).ToList() ?? new List<Order>();
        }
        catch
        {
            return new List<Order>();
        }
    }
}