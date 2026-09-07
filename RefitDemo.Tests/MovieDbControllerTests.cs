using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using RefitDemo.Controllers;
using RefitDemo.Models;
using RefitDemo.Services;
using System.Net;
using Xunit;

namespace RefitDemo.Tests;

public class MovieDbControllerTests
{
    private readonly ITmdbApi _tmdbApi;
    private readonly ILogger<MovieDbController> _logger;
    private readonly MovieDbController _controller;

    public MovieDbControllerTests()
    {
        _tmdbApi = Substitute.For<ITmdbApi>();
        _logger = Substitute.For<ILogger<MovieDbController>>();
        _controller = new MovieDbController(_tmdbApi, _logger);
    }

    [Fact]
    public async Task GetActors_ReturnsOk_WithActorList()
    {
        // Arrange
        var expected = new ActorList
        {
            Actors = [new() { Id = 31, Name = "Tom Hanks" }]
        };
        _tmdbApi.GetActors("Tom Hanks").Returns(Task.FromResult(expected));

        // Act
        var result = await _controller.GetActors("Tom Hanks");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<ActorList>(okResult.Value);
        Assert.Single(actual.Actors);
        Assert.Equal("Tom Hanks", actual.Actors[0].Name);
    }

    [Fact]
    public async Task GetMovies_ReturnsOk_WithMovieList()
    {
        // Arrange
        var expected = new MovieList
        {
            Movies = [new() { Id = 550, Title = "Fight Club", ReleaseDate = "1999-10-15" }]
        };
        _tmdbApi.GetMovies(287).Returns(Task.FromResult(expected));

        // Act
        var result = await _controller.GetMovies(287);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<MovieList>(okResult.Value);
        Assert.Single(actual.Movies);
        Assert.Equal("Fight Club", actual.Movies[0].Title);
    }

    [Fact]
    public async Task AddRating_ReturnsOk_WithResponseBody()
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
