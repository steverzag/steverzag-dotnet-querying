using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	/// <summary>
	/// Client filter values reach SQL as parameters, so EF Core compiles one query per filter shape, not one per value.
	/// </summary>
	public class ParameterizedFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		private const string Needle = "zq-needle";

		private string SqlBody(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			var sql = apply(context.Products).ToQueryString();
			return string.Join('\n', sql.Split('\n').Where(line => !line.StartsWith(".param set")));
		}

		private string SqlBody(ConditionNode node) => SqlBody(q => q.ApplyLogicsFilter([node]));

		private static void AssertParameterized(string sql, string literal)
		{
			Assert.Contains("@__Value_", sql);
			Assert.DoesNotContain(literal, sql, StringComparison.OrdinalIgnoreCase);
		}

		// ---- the value is a parameter in the SQL

		[Fact]
		public void Number_IsAParameter()
			=> AssertParameterized(SqlBody(Node(nameof(Product.Quantity), FilterCondition.Equals, 987654)), "987654");

		[Fact]
		public void Decimal_IsAParameter()
			=> AssertParameterized(SqlBody(Node(nameof(Product.Price), FilterCondition.GreaterThan, 98765.4321m)), "98765.4321");

		[Fact]
		public void String_IsAParameter()
		{
			AssertParameterized(SqlBody(Node(nameof(Product.Name), FilterCondition.Contains, Needle)), Needle);
			AssertParameterized(SqlBody(Node(nameof(Product.Name), FilterCondition.Equals, Needle)), Needle);
			AssertParameterized(SqlBody(Node(nameof(Product.Name), FilterCondition.StartsWith, Needle)), Needle);
			AssertParameterized(SqlBody(Node(nameof(Product.Name), FilterCondition.EndsWith, Needle)), Needle);
		}

		[Fact]
		public void Guid_IsAParameter()
		{
			var code = Guid.Parse("6f1c2a77-0d4e-4b8a-9c11-3e5f7a9b2d40");
			AssertParameterized(SqlBody(Node(nameof(Product.Code), FilterCondition.Equals, code)), "6f1c2a77");
		}

		[Fact]
		public void Dates_AreParameters()
		{
			AssertParameterized(SqlBody(Node(nameof(Product.CreatedAt), FilterCondition.GreaterThan, new DateTime(2031, 7, 19))), "2031-07-19");
			AssertParameterized(SqlBody(Node(nameof(Product.ReleaseDate), FilterCondition.Equals, new DateOnly(2031, 7, 19))), "2031-07-19");
			AssertParameterized(SqlBody(Node(nameof(Product.OpensAt), FilterCondition.Equals, new TimeOnly(13, 57, 31))), "13:57:31");
		}

		[Fact]
		public void Enum_IsAParameter()
			=> AssertParameterized(SqlBody(Node(nameof(Product.Batch), FilterCondition.Equals, nameof(ProductBatch.Current))), "5000000000");

		[Fact]
		public void InList_IsOneJsonParameter()
		{
			var sql = SqlBody(Node(nameof(Product.Quantity), FilterCondition.In, new List<object> { 987654, 876543 }));

			Assert.Contains("json_each(@__Value_", sql);
			Assert.DoesNotContain("987654", sql);
		}

		[Fact]
		public void CollectionConditions_AreParameters()
		{
			Assert.Contains("json_each(@__Value_", SqlBody(Node(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object> { Needle })));
			Assert.Contains("json_each(@__Value_", SqlBody(Node(nameof(Product.Tags), FilterCondition.ContainsAll, new List<object> { Needle })));
			AssertParameterized(SqlBody(Node(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object> { Needle })), Needle);
			AssertParameterized(SqlBody(Node(nameof(Product.Tags), FilterCondition.ContainsAll, new List<object> { Needle })), Needle);
			AssertParameterized(SqlBody(Node(nameof(Product.Tags), FilterCondition.Contains, Needle)), Needle);
		}

		[Fact]
		public void TimeListValues_StayInlined()
		{
			// EF Core's SQLite provider writes TimeOnly and TimeSpan elements of a JSON list parameter in a text
			// format the column doesn't use, so a parameterized In silently matches nothing.
			var timeOnly = SqlBody(Node(nameof(Product.OpensAt), FilterCondition.In, new List<object> { new TimeOnly(13, 57, 31) }));
			var timeSpan = SqlBody(Node(nameof(Product.Warranty), FilterCondition.In, new List<object> { TimeSpan.FromDays(123) }));

			Assert.Contains("13:57:31", timeOnly);
			Assert.DoesNotContain("json_each", timeOnly);
			Assert.Contains("123.00:00:00", timeSpan);
			Assert.DoesNotContain("json_each", timeSpan);
		}

		[Fact]
		public void FixedValues_StayInlined()
		{
			var sql = SqlBody(Node(nameof(Product.NullableQuantity), FilterCondition.IsEmpty));

			Assert.DoesNotContain("@__", sql);
		}

		[Fact]
		public void BaseSearch_IsAParameter()
		{
			AssertParameterized(SqlBody(q => q.ApplyBaseFilter([nameof(Product.Name)], Needle)), Needle);

			var fromLookups = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name)]);
			AssertParameterized(SqlBody(q => q.ApplyBaseFilter(Needle, fromLookups)), Needle);

			var fromExpression = new BaseSearchConfiguration().Configure<Product>((p, search) => p.Name.Contains(search));
			AssertParameterized(SqlBody(q => q.ApplyBaseFilter(Needle, fromExpression)), Needle);
		}

		[Fact]
		public void BaseSearch_FromLookups_KeepsAnEmptyStringLiteralInALookup()
		{
			// The lookups used to be built with "" as a placeholder for the search value, and every "" in the
			// expression was then replaced, including this one: Shovel's null description matched any search.
			var configuration = new BaseSearchConfiguration().Configure<Product>(["Description|$FN_COALESCE(\"\")"]);

			using var context = Database.CreateContext();
			Assert.Empty(context.Products.ApplyBaseFilter(Needle, configuration).Select(p => p.Id).ToArray());
			Assert.Equal([4], context.Products.ApplyBaseFilter("rake", configuration).Select(p => p.Id).ToArray());
		}

		// ---- the same shape with another value reuses the compiled query

		private int CompilationsAfterTheFirst(params Func<IQueryable<Product>, IQueryable<Product>>[] queries)
		{
			var count = 0;
			var options = Database.CreateOptions(builder =>
				builder.LogTo(_ => Interlocked.Increment(ref count), [CoreEventId.QueryCompilationStarting], LogLevel.Debug));

			for (var i = 0; i < queries.Length; i++)
			{
				if (i == 1)
					count = 0;

				using var context = new TestDbContext(options);
				context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				_ = queries[i](context.Products).Select(p => p.Id).ToArray();
			}

			return count;
		}

		[Fact]
		public void TheCounter_SeesANewQuery()
		{
			// Control: an inlined constant makes a new shape, so it compiles.
			var unique = Random.Shared.Next(1_000_000, int.MaxValue);
			var parameter = Expression.Parameter(typeof(Product), "p");
			var predicate = Expression.Lambda<Func<Product, bool>>(
				Expression.Equal(Expression.Property(parameter, nameof(Product.Quantity)), Expression.Constant(unique)), parameter);

			Assert.Equal(1, CompilationsAfterTheFirst(q => q, q => q.Where(predicate)));
		}

		[Fact]
		public void ANumberFilter_CompilesOnce()
			=> Assert.Equal(0, CompilationsAfterTheFirst(
				q => q.ApplyLogicsFilter([Node(nameof(Product.Quantity), FilterCondition.Equals, 50)]),
				q => q.ApplyLogicsFilter([Node(nameof(Product.Quantity), FilterCondition.Equals, 51)]),
				q => q.ApplyLogicsFilter([Node(nameof(Product.Quantity), FilterCondition.Equals, 52)])));

		[Fact]
		public void AStringFilter_CompilesOnce()
			=> Assert.Equal(0, CompilationsAfterTheFirst(
				q => q.ApplyLogicsFilter([Node(nameof(Product.Name), FilterCondition.Contains, "a")]),
				q => q.ApplyLogicsFilter([Node(nameof(Product.Name), FilterCondition.Contains, "b")])));

		[Fact]
		public void AnInFilter_CompilesOnce_WhateverTheListLength()
			=> Assert.Equal(0, CompilationsAfterTheFirst(
				q => q.ApplyLogicsFilter([Node(nameof(Product.Quantity), FilterCondition.In, new List<object> { 1, 2 })]),
				q => q.ApplyLogicsFilter([Node(nameof(Product.Quantity), FilterCondition.In, new List<object> { 3, 4, 5 })])));

		[Fact]
		public void TheBaseSearch_CompilesOnce()
		{
			var configuration = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name), nameof(Product.Description)]);

			Assert.Equal(0, CompilationsAfterTheFirst(
				q => q.ApplyBaseFilter("a", configuration),
				q => q.ApplyBaseFilter("b", configuration)));
		}

		// ---- every scalar type still matches through the list parameter

		[Theory]
		[InlineData(nameof(Product.Name))]
		[InlineData(nameof(Product.Description))]
		[InlineData(nameof(Product.Code))]
		[InlineData(nameof(Product.OptionalCode))]
		[InlineData(nameof(Product.IsActive))]
		[InlineData(nameof(Product.IsFeatured))]
		[InlineData(nameof(Product.Rating))]
		[InlineData(nameof(Product.Stock))]
		[InlineData(nameof(Product.Quantity))]
		[InlineData(nameof(Product.NullableQuantity))]
		[InlineData(nameof(Product.Views))]
		[InlineData(nameof(Product.Weight))]
		[InlineData(nameof(Product.Score))]
		[InlineData(nameof(Product.Price))]
		[InlineData(nameof(Product.CreatedAt))]
		[InlineData(nameof(Product.DeletedAt))]
		[InlineData(nameof(Product.ReleaseDate))]
		[InlineData(nameof(Product.OpensAt))]
		[InlineData(nameof(Product.DiscontinuedOn))]
		[InlineData(nameof(Product.LastServicedAt))]
		[InlineData(nameof(Product.Warranty))]
		[InlineData(nameof(Product.Status))]
		[InlineData(nameof(Product.OptionalStatus))]
		[InlineData(nameof(Product.Features))]
		[InlineData(nameof(Product.Batch))]
		[InlineData(nameof(Product.Grade))]
		public void In_WithEachRowsOwnValue_FindsThatRow(string property)
		{
			using var context = Database.CreateContext();
			var getter = typeof(Product).GetProperty(property)!;

			foreach (var row in context.Products.OrderBy(p => p.Id).ToList())
			{
				if (getter.GetValue(row) is not { } value)
					continue;

				Assert.Contains(row.Id, FilterIds(property, FilterCondition.In, new List<object> { value }));
			}
		}
	}
}
