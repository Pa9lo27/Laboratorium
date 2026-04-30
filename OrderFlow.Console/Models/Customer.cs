namespace OrderFlow.Console.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Email { get; set; }        
    public bool IsVip { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}