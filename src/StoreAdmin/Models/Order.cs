namespace StoreAdmin.Models;

public enum OrderStatus { Pending, Paid, Shipped, Cancelled }

public record OrderLine(int ProductId, string ProductName, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public class Order
{
    public int Id { get; init; }
    public string CustomerEmail { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public List<OrderLine> Lines { get; init; } = [];
    public decimal Total => Lines.Sum(line => line.Total);
}
