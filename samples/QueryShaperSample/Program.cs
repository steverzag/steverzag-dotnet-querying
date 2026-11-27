using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueryShaperSample;
using QueryShaperSample.Catalog;
using Steverzag.QueryShaper;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.EntityFrameworkCore;
using Steverzag.QueryShaper.Pagination;

var builder = WebApplication.CreateBuilder(args);

// An in-memory SQLite database lives as long as its connection, so the app keeps one open.
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

builder.Services.AddDbContext<CatalogDb>(options => options
    .UseSqlite(connection)
    .UseDecimalMathAsDouble());

builder.Services.AddQueryShaper<SampleQueryShaperConfig>(o =>
{
    o.MaxPageSize = 50;
    // Date and time members that translate on SQLite, plus the rules in this assembly (ProductMemberRules).
    o.MemberFilter = ProviderMemberFilters.Sqlite.ApplyFromAssembly(typeof(Program).Assembly);
    // SQLite can't ORDER BY a decimal, so an order on Price is a 400 instead of a 500.
    o.UnsortableTypes.UnionWith(ProviderUnsortableTypes.Sqlite);
});

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<JsonBindingErrorHandler>();

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
    CatalogSeed.Seed(scope.ServiceProvider.GetRequiredService<CatalogDb>());

app.MapPost("/products/query", async (QueryRequest request, CatalogDb db, IQueryShaper shaper, CancellationToken ct) =>
    {
        // With no "order", and to break ties in one, the page is ordered by Product's primary key.
        var page = await shaper.ApplyPagedAsync<Product, ProductDto>(db.Products, request, ct);
        return PageResponse.From(page);
    })
    .AddEndpointFilter<QueryRequestErrorFilter>();

app.Run();

// A PaginatedList serializes as a plain array, so the counts are sent in a response of our own.
public record PageResponse<T>(IReadOnlyList<T> Items, int PageIndex, int PageSize, int TotalCount, int TotalPages);

public static class PageResponse
{
    public static PageResponse<T> From<T>(PaginatedList<T> page) =>
        new(page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages);
}
