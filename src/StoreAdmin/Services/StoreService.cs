using StoreAdmin.Models;

namespace StoreAdmin.Services;

/// <summary>
/// In-memory store data, registered as a singleton for the lifetime of the app.
/// Swap this class for an EF Core or HTTP implementation to connect a real backend.
/// </summary>
public class StoreService
{
    public const int LowStockThreshold = 10;

    private readonly object _gate = new();
    private readonly List<Product> _products = [];
    private readonly List<Order> _orders = [];
    private int _nextProductId = 1;
    private int _nextOrderId = 1001;

    public IReadOnlyList<Product> GetProducts()
    {
        lock (_gate) return _products.Select(p => p.Clone()).OrderBy(p => p.Name).ToList();
    }

    public IReadOnlyList<Order> GetOrders()
    {
        lock (_gate) return _orders.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public Product SaveProduct(Product product)
    {
        lock (_gate)
        {
            if (product.Id == 0)
            {
                var added = product.Clone();
                added.Id = _nextProductId++;
                _products.Add(added);
                return added.Clone();
            }

            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index < 0) throw new KeyNotFoundException($"Product {product.Id} not found.");
            _products[index] = product.Clone();
            return product.Clone();
        }
    }

    /// <summary>Products that appear in an order are kept so order history stays readable.</summary>
    public bool DeleteProduct(int id)
    {
        lock (_gate)
        {
            if (_orders.Any(o => o.Lines.Any(l => l.ProductId == id))) return false;
            return _products.RemoveAll(p => p.Id == id) > 0;
        }
    }

    public Order PlaceOrder(string customerEmail, IEnumerable<(int ProductId, int Quantity)> items, DateTime? createdAt = null)
    {
        lock (_gate)
        {
            var requested = items.ToList();
            if (requested.Count == 0) throw new InvalidOperationException("An order needs at least one item.");

            // Validate everything first so a failed order never changes stock.
            foreach (var (productId, quantity) in requested)
            {
                var product = _products.FirstOrDefault(p => p.Id == productId)
                    ?? throw new KeyNotFoundException($"Product {productId} not found.");
                if (quantity < 1) throw new InvalidOperationException("Quantity must be at least 1.");
                if (product.Stock < quantity)
                    throw new InvalidOperationException($"Not enough stock for {product.Name}.");
            }

            var order = new Order
            {
                Id = _nextOrderId++,
                CustomerEmail = customerEmail,
                CreatedAt = createdAt ?? DateTime.UtcNow,
            };
            foreach (var (productId, quantity) in requested)
            {
                var product = _products.First(p => p.Id == productId);
                product.Stock -= quantity;
                order.Lines.Add(new OrderLine(product.Id, product.Name, quantity, product.Price));
            }
            _orders.Add(order);
            return order;
        }
    }

    public static bool CanMove(OrderStatus from, OrderStatus to) => from switch
    {
        OrderStatus.Pending => to is OrderStatus.Paid or OrderStatus.Cancelled,
        OrderStatus.Paid => to is OrderStatus.Shipped or OrderStatus.Cancelled,
        _ => false,
    };

    public bool ChangeStatus(int orderId, OrderStatus next)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null || !CanMove(order.Status, next)) return false;

            if (next == OrderStatus.Cancelled)
            {
                foreach (var line in order.Lines)
                {
                    var product = _products.FirstOrDefault(p => p.Id == line.ProductId);
                    if (product is not null) product.Stock += line.Quantity;
                }
            }
            order.Status = next;
            return true;
        }
    }

    public DashboardSummary GetSummary()
    {
        lock (_gate)
        {
            var active = _orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            var topProducts = active
                .SelectMany(o => o.Lines)
                .GroupBy(l => l.ProductName)
                .Select(g => new ProductSales(g.Key, g.Sum(l => l.Quantity), g.Sum(l => l.Total)))
                .OrderByDescending(s => s.Revenue)
                .Take(5)
                .ToList();

            return new DashboardSummary(
                Revenue: active.Sum(o => o.Total),
                OrderCount: active.Count,
                PendingCount: _orders.Count(o => o.Status == OrderStatus.Pending),
                AverageOrder: active.Count == 0 ? 0 : active.Sum(o => o.Total) / active.Count,
                LowStock: _products.Where(p => p.Stock <= LowStockThreshold).OrderBy(p => p.Stock).Select(p => p.Clone()).ToList(),
                TopProducts: topProducts);
        }
    }
}

public record ProductSales(string Name, int Units, decimal Revenue);

public record DashboardSummary(
    decimal Revenue,
    int OrderCount,
    int PendingCount,
    decimal AverageOrder,
    IReadOnlyList<Product> LowStock,
    IReadOnlyList<ProductSales> TopProducts);
