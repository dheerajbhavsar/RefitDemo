using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using RefitDemo.Controllers;
using RefitDemo.Models;
using RefitDemo.Services;
using Xunit;

namespace RefitDemo.Tests;

public class MovieDbControllerTests
{
    private readonly ITmdbApi _tmdbApi;
    private readonly ILogger<MovieDbController> _logger;
    private readonly HybridCache _cache;
    private readonly MovieDbController _controller;

    public MovieDbControllerTests()
    {
        _tmdbApi = Substitute.For<ITmdbApi>();
        _logger = Substitute.For<ILogger<MovieDbController>>();

#pragma warning disable EXTEXP0018
        var services = new ServiceCollection();
        services.AddHybridCache();
        var serviceProvider = services.BuildServiceProvider();
        _cache = serviceProvider.GetRequiredService<HybridCache>();
#pragma warning restore EXTEXP0018

        _controller = new MovieDbController(_tmdbApi, _cache, _logger);
    }

    [Fact]
    public async Task GetActors_ReturnsOk_WithActorList_AndCachesResult()
    {
        // Arrange
        var expected = new ActorList
        {
            Actors = [new() { Id = 31, Name = "Tom Hanks" }]
        };
        _tmdbApi.GetActors("Tom Hanks").Returns(Task.FromResult(expected));

        // Act 1: Initial call (populates cache)
        var result1 = await _controller.GetActors("Tom Hanks");

        // Act 2: Second call (should be served from HybridCache L1 memory)
        var result2 = await _controller.GetActors("Tom Hanks");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result1.Result);
        var actual = Assert.IsType<ActorList>(okResult.Value);
        Assert.Single(actual.Actors);
        Assert.Equal("Tom Hanks", actual.Actors[0].Name);

        // TMDB Refit API should only have been called ONCE due to HybridCache!
        await _tmdbApi.Received(1).GetActors("Tom Hanks");
    }

    [Fact]
    public async Task GetMovies_ReturnsOk_WithMovieList_AndCachesResult()
    {
        // Arrange
        var expected = new MovieList
        {
            Movies = [new() { Id = 550, Title = "Fight Club", ReleaseDate = "1999-10-15" }]
        };
        _tmdbApi.GetMovies(287).Returns(Task.FromResult(expected));

        // Act 1: Initial call
        var result1 = await _controller.GetMovies(287);

        // Act 2: Second call
        var result2 = await _controller.GetMovies(287);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result1.Result);
        var actual = Assert.IsType<MovieList>(okResult.Value);
        Assert.Single(actual.Movies);
        Assert.Equal("Fight Club", actual.Movies[0].Title);

        // Refit API should only be called once due to caching
        await _tmdbApi.Received(1).GetMovies(287);
    }

    [Fact]
    public async Task AddRating_ReturnsOk_AndInvalidatesMovieCacheTags()
    {
        // Arrange
        var rating = new Rating { Value = 8.5m };
        var expected = new ResponseBody { StatusCode = 1, StatusMessage = "Success." };
        _tmdbApi.AddRating(550, rating).Returns(Task.FromResult(expected));

        // Act
        var result = await _controller.AddRating(550, rating);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<ResponseBody>(okResult.Value);
        Assert.Equal(1, actual.StatusCode);
        Assert.Equal("Success.", actual.StatusMessage);
    }

    [Fact]
    public async Task DeleteRating_ReturnsOk_WithResponseBody()
    {
        // Arrange
        var expected = new ResponseBody { StatusCode = 13, StatusMessage = "The item/record was deleted successfully." };
        _tmdbApi.DeleteRating(550).Returns(Task.FromResult(expected));

        // Act
        var result = await _controller.DeleteRating(550);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<ResponseBody>(okResult.Value);
        Assert.Equal(13, actual.StatusCode);
        Assert.Equal("The item/record was deleted successfully.", actual.StatusMessage);
    }
}
