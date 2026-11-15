using StoreAdmin.Models;

namespace StoreAdmin.Services;

public static class SeedData
{
    public static StoreService CreateSeededStore()
    {
        var store = new StoreService();
        var oil = store.SaveProduct(new Product { Name = "Olive Oil 750ml", Category = "Pantry", Price = 12.50m, Stock = 40 });
        var zaatar = store.SaveProduct(new Product { Name = "Zaatar Mix 250g", Category = "Pantry", Price = 4.75m, Stock = 80 });
        var molasses = store.SaveProduct(new Product { Name = "Pomegranate Molasses", Category = "Pantry", Price = 6.20m, Stock = 14 });
        var coffee = store.SaveProduct(new Product { Name = "Turkish Coffee 500g", Category = "Drinks", Price = 9.90m, Stock = 50 });
        var rose = store.SaveProduct(new Product { Name = "Rose Water 300ml", Category = "Drinks", Price = 3.40m, Stock = 60 });
        var baklava = store.SaveProduct(new Product { Name = "Baklava Box 12pc", Category = "Sweets", Price = 15.00m, Stock = 12 });

        var now = DateTime.UtcNow;
        var first = store.PlaceOrder("lina@example.com", [(oil.Id, 2), (zaatar.Id, 3)], now.AddDays(-3));
        var second = store.PlaceOrder("omar@example.com", [(baklava.Id, 4), (coffee.Id, 1)], now.AddDays(-2));
        store.PlaceOrder("maya@example.com", [(molasses.Id, 6), (rose.Id, 2)], now.AddDays(-1));
        store.PlaceOrder("karim@example.com", [(coffee.Id, 2)], now.AddHours(-5));

        store.ChangeStatus(first.Id, OrderStatus.Paid);
        store.ChangeStatus(first.Id, OrderStatus.Shipped);
        store.ChangeStatus(second.Id, OrderStatus.Paid);
        return store;
    }
}
