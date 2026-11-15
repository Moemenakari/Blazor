using System.ComponentModel.DataAnnotations;

namespace StoreAdmin.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = "";

    [Required, StringLength(40)]
    public string Category { get; set; } = "";

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int Stock { get; set; }

    public Product Clone() => (Product)MemberwiseClone();
}
