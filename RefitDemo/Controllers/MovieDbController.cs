using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Refit;
using RefitDemo.Models;
using RefitDemo.Services;

namespace RefitDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MovieDbController(
    ITmdbApi tmdbApi,
    HybridCache cache,
    ILogger<MovieDbController> logger) : ControllerBase
{
    private readonly ITmdbApi _tmdbApi = tmdbApi;
    private readonly HybridCache _cache = cache;
    private readonly ILogger<MovieDbController> _logger = logger;

    /// <summary>
    /// Search for actors/actresses by name using Refit with .NET 10 HybridCache.
    /// Responses are cached in L1 memory with built-in cache stampede protection.
    /// </summary>
    [HttpGet("actors")]
    public async Task<ActionResult<ActorList>> GetActors(
        [FromQuery][Required] string name,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tmdb:actors:search:{name.Trim().ToLowerInvariant()}";

        var response = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await _tmdbApi.GetActors(name),
            tags: ["actors"],
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Search for actors/actresses by name using ApiResponse&lt;T&gt; wrapper.
    /// Demonstrates inspecting HTTP metadata, status code, and handling errors.
    /// </summary>
    [HttpGet("actors/with-response")]
    public async Task<IActionResult> GetActorsWithResponse(
        [FromQuery][Required] string name,
        CancellationToken cancellationToken = default)
    {
        var response = await _tmdbApi.GetActorsWithResponse(name);
        if (response.IsSuccessStatusCode)
        {
            return Ok(response.Content);
        }

        var errorMessage = (response.Error as ApiException)?.Content 
                           ?? response.Error?.Message 
                           ?? response.ReasonPhrase 
                           ?? "Error calling TMDB API";

        var statusCode = (int)(response.StatusCode ?? HttpStatusCode.InternalServerError);

        _logger.LogError("TMDB API error: {StatusCode} - {ErrorContent}", statusCode, errorMessage);
        return StatusCode(statusCode, errorMessage);
    }

    /// <summary>
    /// Get movie credits for a given actor/actress ID with HybridCache.
    /// Cached with tag 'movies' and specific 'actor:{actorId}'.
    /// </summary>
    [HttpGet("actors/{actorId:int}/movies")]
    public async Task<ActionResult<MovieList>> GetMovies(
        int actorId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tmdb:actors:{actorId}:movies";

        var response = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await _tmdbApi.GetMovies(actorId),
            tags: ["movies", $"actor:{actorId}"],
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Add a rating to a movie and invalidate movie cache tags.
    /// </summary>
    [HttpPost("movies/{movieId:int}/rating")]
    public async Task<ActionResult<ResponseBody>> AddRating(
        int movieId,
        [FromBody] Rating rating,
        CancellationToken cancellationToken = default)
    {
        var response = await _tmdbApi.AddRating(movieId, rating);

        // Invalidate cached movie data using tag-based invalidation
        await _cache.RemoveByTagAsync([$"movie:{movieId}", "movies"], cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Delete a rating from a movie and invalidate movie cache tags.
    /// </summary>
    [HttpDelete("movies/{movieId:int}/rating")]
    public async Task<ActionResult<ResponseBody>> DeleteRating(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var response = await _tmdbApi.DeleteRating(movieId);

        // Invalidate cached movie data using tag-based invalidation
        await _cache.RemoveByTagAsync([$"movie:{movieId}", "movies"], cancellationToken);

        return Ok(response);
    }
}
