using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Refit;
using RefitDemo.Configuration;
using RefitDemo.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure health checks for container orchestration (Kubernetes / Docker)
builder.Services.AddHealthChecks();

// Configure OpenAPI for API exploration
builder.Services.AddOpenApi();

// Register and configure .NET 10 HybridCache (L1 Memory Cache + L2 Distributed with Stampede Protection)
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(10)
    };
});
#pragma warning restore EXTEXP0018

// Register and validate TMDB options on startup (Fail-Fast: Gold Standard)
builder.Services.AddOptions<TmdbOptions>()
    .BindConfiguration(TmdbOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register Refit client using validated IOptions<TmdbOptions>
builder.Services
    .AddRefitClient<ITmdbApi>(serviceProvider =>
    {
        var tmdbOptions = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;
        return new RefitSettings
        {
            AuthorizationHeaderValueGetter = (request, cancellationToken) => new ValueTask<string>(tmdbOptions.ApiReadAccessToken)
        };
    })
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var tmdbOptions = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;
        client.BaseAddress = new Uri(tmdbOptions.BaseUrl);
    });

var app = builder.Build();

// Health check endpoint for Docker / Kubernetes liveness & readiness probes
app.MapHealthChecks("/health");

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

app.UseAuthorization();

app.MapControllers();

app.Run();
