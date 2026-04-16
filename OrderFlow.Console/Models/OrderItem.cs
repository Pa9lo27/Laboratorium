namespace OrderFlow.Console.Models;

public class OrderItem
{
    public int Id { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal TotalPrice => UnitPrice * Quantity;
}