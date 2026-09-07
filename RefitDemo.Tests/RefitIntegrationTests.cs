using System.Net;
using System.Text;
using Refit;
using RefitDemo.Models;
using RefitDemo.Services;
using Xunit;

namespace RefitDemo.Tests;

public class RefitIntegrationTests
{
    private class MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handlerFunc = handlerFunc;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handlerFunc(request);
        }
    }

    [Fact]
    public async Task Refit_GeneratesCorrectUrlAndHeaders_ForGetActors()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var jsonResponse = """
        {
            "results": [
                { "id": 31, "name": "Tom Hanks" }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3")
        };

        var refitSettings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (rq, ct) => new ValueTask<string>("test_token_123")
        };

        var tmdbApi = RestService.For<ITmdbApi>(httpClient, refitSettings);

        // Act
        var result = await tmdbApi.GetActors("Tom Hanks");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("https://api.themoviedb.org/3/search/person?query=Tom Hanks", capturedRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test_token_123", capturedRequest.Headers.Authorization?.Parameter);

        Assert.NotNull(result);
        Assert.Single(result.Actors);
        Assert.Equal(31, result.Actors[0].Id);
        Assert.Equal("Tom Hanks", result.Actors[0].Name);
    }

    [Fact]
    public async Task Refit_GeneratesCorrectBodyAndHeaders_ForAddRating()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var jsonResponse = """
        {
            "status_code": 1,
            "status_message": "Success."
        }
        """;

        var handler = new MockHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            if (request.Content != null)
            {
                capturedBody = await request.Content.ReadAsStringAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3")
        };

        var refitSettings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (rq, ct) => new ValueTask<string>("test_token_123")
        };

        var tmdbApi = RestService.For<ITmdbApi>(httpClient, refitSettings);

        // Act
        var result = await tmdbApi.AddRating(550, new Rating { Value = 9.0m });

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://api.themoviedb.org/3/movie/550/rating", capturedRequest.RequestUri?.ToString());
        Assert.NotNull(capturedBody);
        Assert.Contains("\"value\":9", capturedBody);

        Assert.NotNull(result);
        Assert.Equal(1, result.StatusCode);
        Assert.Equal("Success.", result.StatusMessage);
    }
}
