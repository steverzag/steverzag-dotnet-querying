namespace QueryShaperSample.Catalog;

// The response row. Properties with the same name as on Product are copied; the others come from
// the selectors in SampleQueryShaperConfig.
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public Category Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public int ReviewCount { get; set; }

    // Nullable: an average over no reviews is null.
    public double? AverageStars { get; set; }
}
