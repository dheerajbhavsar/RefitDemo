using Refit;
using RefitDemo.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure OpenAPI for API exploration
builder.Services.AddOpenApi();

// Read TMDB API configuration
var tmdbConfig = builder.Configuration.GetSection("Tmdb");
var baseUrl = tmdbConfig["BaseUrl"] ?? "https://api.themoviedb.org/3";
var authToken = tmdbConfig["ApiReadAccessToken"] ?? string.Empty;

// Configure Refit settings with Authorization Header Value Getter (ValueTask in Refit v15+)
var refitSettings = new RefitSettings
{
    AuthorizationHeaderValueGetter = (request, cancellationToken) => new ValueTask<string>(authToken)
};

// Register Refit client with HttpClientFactory
builder.Services
    .AddRefitClient<ITmdbApi>(refitSettings)
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(baseUrl);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Refit TMDB Demo API (.NET 10)")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
