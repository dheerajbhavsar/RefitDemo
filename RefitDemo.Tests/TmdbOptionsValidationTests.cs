using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RefitDemo.Configuration;
using Xunit;

namespace RefitDemo.Tests;

public class TmdbOptionsValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void TmdbOptions_WhenApiReadAccessTokenIsEmpty_ValidationFails()
    {
        // Arrange
        var options = new TmdbOptions
        {
            BaseUrl = "https://api.themoviedb.org/3",
            ApiReadAccessToken = string.Empty
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TmdbOptions.ApiReadAccessToken)));
    }

    [Theory]
    [InlineData("YOUR_TMDB_API_READ_ACCESS_TOKEN")]
    [InlineData("not_a_jwt_token")]
    [InlineData("12345")]
    [InlineData("header.payload")] // Missing signature / 3rd part
    [InlineData("ey!invalid!chars.payload.signature")]
    public void TmdbOptions_WhenApiReadAccessTokenIsNotValidJwt_ValidationFailsRegex(string invalidToken)
    {
        // Arrange
        var options = new TmdbOptions
        {
            BaseUrl = "https://api.themoviedb.org/3",
            ApiReadAccessToken = invalidToken
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        Assert.NotEmpty(results);
        var tokenError = Assert.Single(results, r => r.MemberNames.Contains(nameof(TmdbOptions.ApiReadAccessToken)));
        Assert.Contains("valid JWT token format", tokenError.ErrorMessage);
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiJ0bWRiIn0.c2lnbmF0dXJl")]
    public void TmdbOptions_WhenApiReadAccessTokenIsValidJwt_ValidationSucceeds(string validJwtToken)
    {
        // Arrange
        var options = new TmdbOptions
        {
            BaseUrl = "https://api.themoviedb.org/3",
            ApiReadAccessToken = validJwtToken
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void ValidateOnStart_WhenConfigurationInvalid_ThrowsOptionsValidationExceptionOnStartup()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Tmdb:BaseUrl"] = "https://api.themoviedb.org/3",
            ["Tmdb:ApiReadAccessToken"] = "INVALID_NON_JWT_PLACEHOLDER"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection(TmdbOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        // ValidateOnStart registers an IStartupValidator that executes on host startup
        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.Throws<OptionsValidationException>(() => startupValidator.Validate());
        Assert.Contains("valid JWT token format", exception.Message);
    }

    [Fact]
    public void ValidateOnStart_WhenConfigurationValid_PassesStartupValidation()
    {
        // Arrange
        var validToken = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiJ0bWRiIn0.c2lnbmF0dXJl";
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Tmdb:BaseUrl"] = "https://api.themoviedb.org/3",
            ["Tmdb:ApiReadAccessToken"] = validToken
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection(TmdbOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        startupValidator.Validate(); // Should not throw

        var options = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;

        // Assert
        Assert.Equal(validToken, options.ApiReadAccessToken);
    }
}
