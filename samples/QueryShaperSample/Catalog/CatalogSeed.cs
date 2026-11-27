namespace QueryShaperSample.Catalog;

public static class CatalogSeed
{
    public static void Seed(CatalogDb db)
    {
        db.Database.EnsureCreated();
        if (db.Products.Any())
            return;

        db.Products.AddRange(
            Product("Pixel Phone 9", "Android phone with a 6.3\" screen", 799m, Category.Phones, "2026-09-12", ["new", "android"], 5, 4, 5),
            Product("Budget Phone A2", "Entry-level phone, two-day battery", 149.99m, Category.Phones, "2025-03-01", ["sale", "android"], 3, 4),
            Product("Fold Phone X", null, 1799m, Category.Phones, "2026-10-30", ["new"]),
            Product("UltraBook 14", "14\" laptop, 16 GB RAM", 1299m, Category.Laptops, "2026-06-15", ["new", "work"], 5, 5, 4),
            Product("Student Laptop 15", "15\" laptop for school", 549m, Category.Laptops, "2024-08-20", ["sale"], 3, 2, 4),
            Product("Noise-Cancelling Headphones", "Over-ear, 30-hour battery", 349m, Category.Audio, "2025-11-05", ["sale", "wireless"], 5, 4, 4, 5),
            Product("Bluetooth Speaker Mini", "Pocket speaker, waterproof", 59.5m, Category.Audio, "2026-04-02", ["wireless"], 4),
            Product("USB-C Charger 65W", "Charges phones and laptops", 39m, Category.Accessories, "2025-01-10", [], 5, 5),
            Product("Phone Case Clear", "Clear case for Pixel Phone 9", 19.99m, Category.Accessories, "2026-09-12", ["new"], 2, 3),
            Product("Laptop Sleeve 14\"", null, 29m, Category.Accessories, "2024-12-01", ["sale", "work"]));

        db.SaveChanges();
    }

    private static Product Product(string name, string? description, decimal price, Category category, string createdAt,
        List<string> tags, params int[] stars) => new()
    {
        Name = name,
        Description = description,
        Price = price,
        Category = category,
        CreatedAt = DateTime.Parse(createdAt),
        Tags = tags,
        InternalNotes = $"Supplier cost for {name} is confidential.",
        Reviews = [.. stars.Select(s => new Review { Stars = s })],
    };
}
