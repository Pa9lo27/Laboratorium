using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Data;

public class OrderFlowContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite("Data Source=orderflow.db")
            .LogTo(System.Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId);

        modelBuilder.Entity<Order>()
            .Ignore(o => o.TotalAmount);

        modelBuilder.Entity<OrderItem>()
            .Ignore(oi => oi.TotalPrice);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Name)
            .HasDatabaseName("IX_Customer_Name");

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Status)
            .HasDatabaseName("IX_Order_Status");
    }
}