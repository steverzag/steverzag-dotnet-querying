namespace Steverzag.QueryShaper.Tests.Infrastructure
{
	/// <summary>
	/// The fixed data every test runs against. Expected ids in the tests are derived from this table:
	///
	/// | Id | Name   | Description  | OptionalCode | IsActive | IsFeatured | NullableQty | DeletedAt  | Status       | OptionalStatus | Tags           | Category | Review stars | Grade | StatusHistory                  | Measurements |
	/// |----|--------|--------------|--------------|----------|------------|-------------|------------|--------------|----------------|----------------|----------|--------------|-------|--------------------------------|--------------|
	/// | 1  | Hammer | "Steel head" | Code1        | true     | true       | 100         | null       | Active       | Active         | tools, steel   | Tools    | 5, 4         | A     | Draft, Active                  | 1, null      |
	/// | 2  | Shovel | null         | null         | true     | null       | null        | null       | Draft        | null           | garden         | Garden   | 3            | B     | Draft                          | 2            |
	/// | 3  | Wrench | ""           | Code3        | false    | false      | 0           | 2024-07-01 | Discontinued | Discontinued   | (none)         | Tools    | (none)       | C     | Draft, Active, Discontinued    | (none)       |
	/// | 4  | Rake   | "Garden rake"| Code4        | true     | true       | 200         | null       | Active       | Draft          | garden, tools  | Garden   | 4, 4, 2      | A     | Active                         | 1, 2         |
	///
	/// Non-nullable numerics: row 1 high, row 2 zero, row 3 mid (<see cref="Mid"/>), row 4 highest.
	/// Dates: ascending by id, row 2 is the mid value.
	/// Nullable dates (DeletedAt, DiscontinuedOn, LastServicedAt): only row 3 has a value, see <see cref="Row3NullableDate"/>.
	/// Features ([Flags]): 1 Waterproof|Cordless (5), 2 None (0), 3 Foldable (2), 4 Waterproof|Foldable|Cordless (7).
	/// Batch (long-backed): 1 Current (5_000_000_000), 2 Legacy (1), 3 Next (long.MaxValue), 4 Current.
	/// </summary>
	public static class SeedData
	{
		public static readonly Guid Code1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
		public static readonly Guid Code2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
		public static readonly Guid Code3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
		public static readonly Guid Code4 = Guid.Parse("44444444-4444-4444-4444-444444444444");

		/// <summary>The "row 3" value of each non-nullable numeric property.</summary>
		public static class Mid
		{
			public const byte Rating = 2;
			public const short Stock = 50;
			public const int Quantity = 50;
			public const long Views = 500;
			public const float Weight = 0.75f;
			public const double Score = 2.25;
			public const decimal Price = 9.99m;
		}

		/// <summary>The "row 2" value of each date property.</summary>
		public static class MidDate
		{
			public static readonly DateTime CreatedAt = new(2024, 3, 15, 10, 0, 0);
			public static readonly DateOnly ReleaseDate = new(2024, 3, 1);
			public static readonly TimeOnly OpensAt = new(9, 30);
		}

		/// <summary>The only non-null value of each nullable date property (row 3).</summary>
		public static class Row3NullableDate
		{
			public static readonly DateTime DeletedAt = new(2024, 7, 1);
			public static readonly DateOnly DiscontinuedOn = new(2024, 7, 1);
			public static readonly TimeOnly LastServicedAt = new(14, 15);
		}

		public static Category[] Categories() =>
		[
			new() { Id = 1, Name = "Tools" },
			new() { Id = 2, Name = "Garden" },
		];

		public static Product[] Products() =>
		[
			new()
			{
				Id = 1, Name = "Hammer", Description = "Steel head",
				Code = Code1, OptionalCode = Code1, IsActive = true, IsFeatured = true,
				Rating = 4, Stock = 100, Quantity = 100, NullableQuantity = 100, Views = 1000,
				Weight = 1.5f, Score = 4.5, Price = 19.99m,
				CreatedAt = new(2024, 1, 10, 8, 0, 0), DeletedAt = null,
				ReleaseDate = new(2024, 1, 1), OpensAt = new(8, 0),
				Warranty = TimeSpan.FromDays(365),
				Status = ProductStatus.Active, OptionalStatus = ProductStatus.Active,
				Features = ProductFeatures.Waterproof | ProductFeatures.Cordless, Batch = ProductBatch.Current,
				Tags = ["tools", "steel"], Grade = 'A', StatusHistory = [ProductStatus.Draft, ProductStatus.Active], Measurements = [1, null], CategoryId = 1,
				Reviews = [new() { Id = 1, Stars = 5 }, new() { Id = 2, Stars = 4 }],
			},
			new()
			{
				Id = 2, Name = "Shovel", Description = null,
				Code = Code2, OptionalCode = null, IsActive = true, IsFeatured = null,
				Rating = 0, Stock = 0, Quantity = 0, NullableQuantity = null, Views = 0,
				Weight = 0f, Score = 0, Price = 0m,
				CreatedAt = MidDate.CreatedAt, DeletedAt = null,
				ReleaseDate = MidDate.ReleaseDate, OpensAt = MidDate.OpensAt,
				Warranty = TimeSpan.FromDays(180),
				Status = ProductStatus.Draft, OptionalStatus = null,
				Features = ProductFeatures.None, Batch = ProductBatch.Legacy,
				Tags = ["garden"], Grade = 'B', StatusHistory = [ProductStatus.Draft], Measurements = [2], CategoryId = 2,
				Reviews = [new() { Id = 3, Stars = 3 }],
			},
			new()
			{
				Id = 3, Name = "Wrench", Description = "",
				Code = Code3, OptionalCode = Code3, IsActive = false, IsFeatured = false,
				Rating = Mid.Rating, Stock = Mid.Stock, Quantity = Mid.Quantity, NullableQuantity = 0, Views = Mid.Views,
				Weight = Mid.Weight, Score = Mid.Score, Price = Mid.Price,
				CreatedAt = new(2024, 6, 20, 12, 0, 0), DeletedAt = Row3NullableDate.DeletedAt,
				ReleaseDate = new(2024, 6, 1), OpensAt = new(12, 0),
				DiscontinuedOn = Row3NullableDate.DiscontinuedOn, LastServicedAt = Row3NullableDate.LastServicedAt,
				Warranty = TimeSpan.Zero,
				Status = ProductStatus.Discontinued, OptionalStatus = ProductStatus.Discontinued,
				Features = ProductFeatures.Foldable, Batch = ProductBatch.Next,
				Tags = [], Grade = 'C', StatusHistory = [ProductStatus.Draft, ProductStatus.Active, ProductStatus.Discontinued], Measurements = [], CategoryId = 1,
				Reviews = [],
			},
			new()
			{
				Id = 4, Name = "Rake", Description = "Garden rake",
				Code = Code4, OptionalCode = Code4, IsActive = true, IsFeatured = true,
				Rating = 5, Stock = 200, Quantity = 200, NullableQuantity = 200, Views = 2000,
				Weight = 3f, Score = 5, Price = 29.99m,
				CreatedAt = new(2024, 9, 5, 16, 30, 0), DeletedAt = null,
				ReleaseDate = new(2024, 9, 1), OpensAt = new(18, 45),
				Warranty = TimeSpan.FromDays(730),
				Status = ProductStatus.Active, OptionalStatus = ProductStatus.Draft,
				Features = ProductFeatures.Waterproof | ProductFeatures.Foldable | ProductFeatures.Cordless, Batch = ProductBatch.Current,
				Tags = ["garden", "tools"], Grade = 'A', StatusHistory = [ProductStatus.Active], Measurements = [1, 2], CategoryId = 2,
				Reviews = [new() { Id = 4, Stars = 4 }, new() { Id = 5, Stars = 4 }, new() { Id = 6, Stars = 2 }],
			},
		];
	}
}
