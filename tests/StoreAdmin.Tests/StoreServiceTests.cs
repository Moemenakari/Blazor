using StoreAdmin.Models;
using StoreAdmin.Services;

namespace StoreAdmin.Tests;

public class StoreServiceTests
{
    private static (StoreService Store, Product Coffee) CreateStore()
    {
        var store = new StoreService();
        var coffee = store.SaveProduct(new Product { Name = "Coffee", Category = "Drinks", Price = 10m, Stock = 5 });
        return (store, coffee);
    }

    [Fact]
    public void SaveProduct_AssignsIdAndCanUpdate()
    {
        var (store, coffee) = CreateStore();
        Assert.True(coffee.Id > 0);

        coffee.Price = 11m;
        store.SaveProduct(coffee);

        Assert.Equal(11m, store.GetProducts().Single().Price);
    }

    [Fact]
    public void PlaceOrder_ReducesStockAndCalculatesTotal()
    {
        var (store, coffee) = CreateStore();

        var order = store.PlaceOrder("a@example.com", [(coffee.Id, 3)]);

        Assert.Equal(30m, order.Total);
        Assert.Equal(2, store.GetProducts().Single().Stock);
    }

    [Fact]
    public void PlaceOrder_WithTooLittleStock_ThrowsAndKeepsStock()
    {
        var (store, coffee) = CreateStore();

        Assert.Throws<InvalidOperationException>(() => store.PlaceOrder("a@example.com", [(coffee.Id, 6)]));

        Assert.Equal(5, store.GetProducts().Single().Stock);
        Assert.Empty(store.GetOrders());
    }

    [Fact]
    public void OrderKeepsPriceFromTimeOfPurchase()
    {
        var (store, coffee) = CreateStore();
        var order = store.PlaceOrder("a@example.com", [(coffee.Id, 1)]);

        var current = store.GetProducts().Single();
        current.Price = 99m;
        store.SaveProduct(current);

        Assert.Equal(10m, order.Total);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    public void CanMove_FollowsWorkflow(OrderStatus from, OrderStatus to, bool expected)
    {
        Assert.Equal(expected, StoreService.CanMove(from, to));
    }

    [Fact]
    public void CancellingOrder_RestoresStock_AndExcludesItFromRevenue()
    {
        var (store, coffee) = CreateStore();
        var order = store.PlaceOrder("a@example.com", [(coffee.Id, 4)]);

        Assert.True(store.ChangeStatus(order.Id, OrderStatus.Cancelled));

        Assert.Equal(5, store.GetProducts().Single().Stock);
        Assert.Equal(0m, store.GetSummary().Revenue);
    }

    [Fact]
    public void DeleteProduct_IsBlockedWhenProductHasOrders()
    {
        var (store, coffee) = CreateStore();
        var tea = store.SaveProduct(new Product { Name = "Tea", Category = "Drinks", Price = 4m, Stock = 9 });
        store.PlaceOrder("a@example.com", [(coffee.Id, 1)]);

        Assert.False(store.DeleteProduct(coffee.Id));
        Assert.True(store.DeleteProduct(tea.Id));
    }

    [Fact]
    public void Summary_ReportsLowStockAndTopProducts()
    {
        var store = SeedData.CreateSeededStore();

        var summary = store.GetSummary();

        Assert.Equal(4, summary.OrderCount);
        Assert.All(summary.LowStock, p => Assert.True(p.Stock <= StoreService.LowStockThreshold));
        Assert.Equal(summary.TopProducts.OrderByDescending(p => p.Revenue), summary.TopProducts);
    }
}
