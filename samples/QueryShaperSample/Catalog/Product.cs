namespace QueryShaperSample.Catalog;

public enum Category
{
    Phones,
    Laptops,
    Audio,
    Accessories,
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Category Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = [];

    // Never reachable from a request: hidden by ProductMemberRules.
    public string InternalNotes { get; set; } = "";

    public List<Review> Reviews { get; set; } = [];
}

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
}
