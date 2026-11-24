namespace Steverzag.QueryShaper.Tests.Infrastructure
{
	/// <summary>
	/// Test-only model. Each property exists to hit one type branch of the filtering code,
	/// so the set of types matters more than the domain.
	/// </summary>
	public class Product
	{
		public int Id { get; set; }

		// string
		public string Name { get; set; } = null!;
		public string? Description { get; set; }

		// Guid / bool
		public Guid Code { get; set; }
		public Guid? OptionalCode { get; set; }
		public bool IsActive { get; set; }
		public bool? IsFeatured { get; set; }

		// numerics: every non-nullable one follows the same shape across the seed
		// (row 1 high, row 2 zero, row 3 mid, row 4 highest), see SeedData.
		public byte Rating { get; set; }
		public short Stock { get; set; }
		public int Quantity { get; set; }
		public int? NullableQuantity { get; set; }
		public long Views { get; set; }
		public float Weight { get; set; }
		public double Score { get; set; }
		public decimal Price { get; set; }

		// dates: ascending by Id across the seed
		public DateTime CreatedAt { get; set; }
		public DateTime? DeletedAt { get; set; }
		public DateOnly ReleaseDate { get; set; }
		public TimeOnly OpensAt { get; set; }

		// nullable dates: only row 3 has a value, like DeletedAt
		public DateOnly? DiscontinuedOn { get; set; }
		public TimeOnly? LastServicedAt { get; set; }

		// a type the filter has no branch for
		public TimeSpan Warranty { get; set; }

		// enums
		public ProductStatus Status { get; set; }
		public ProductStatus? OptionalStatus { get; set; }
		public ProductFeatures Features { get; set; }

		// a scalar with no dedicated filter branch (only In/NotIn apply)
		public char Grade { get; set; }

		// primitive collections (stored as JSON by EF Core)
		public List<string> Tags { get; set; } = [];
		public List<ProductStatus> StatusHistory { get; set; } = [];
		public List<int?> Measurements { get; set; } = [];

		// navigations
		public int CategoryId { get; set; }
		public Category Category { get; set; } = null!;
		public List<Review> Reviews { get; set; } = [];
	}

	public enum ProductStatus
	{
		Draft = 0,
		Active = 1,
		Discontinued = 2
	}

	/// <summary>A <see cref="FlagsAttribute"/> enum: combinations of declared bits are valid values (decision 008.2).</summary>
	[Flags]
	public enum ProductFeatures
	{
		None = 0,
		Waterproof = 1,
		Foldable = 2,
		Cordless = 4,
	}

	public class Category
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
	}

	public class Review
	{
		public int Id { get; set; }
		public int ProductId { get; set; }
		public int Stars { get; set; }
	}
}
