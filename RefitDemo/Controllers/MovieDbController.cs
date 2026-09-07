using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Refit;
using RefitDemo.Models;
using RefitDemo.Services;

namespace RefitDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MovieDbController(ITmdbApi tmdbApi, ILogger<MovieDbController> logger) : ControllerBase
{
    private readonly ITmdbApi _tmdbApi = tmdbApi;
    private readonly ILogger<MovieDbController> _logger = logger;

    /// <summary>
    /// Search for actors/actresses by name using Refit.
    /// </summary>
    [HttpGet("actors")]
    public async Task<ActionResult<ActorList>> GetActors([FromQuery][Required] string name)
    {
        var response = await _tmdbApi.GetActors(name);
        return Ok(response);
    }

    /// <summary>
    /// Search for actors/actresses by name using ApiResponse&lt;T&gt; wrapper.
    /// Demonstrates inspecting HTTP metadata, status code, and handling errors.
    /// </summary>
    [HttpGet("actors/with-response")]
    public async Task<IActionResult> GetActorsWithResponse([FromQuery][Required] string name)
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
    /// Get movie credits for a given actor/actress ID.
    /// </summary>
    [HttpGet("actors/{actorId:int}/movies")]
    public async Task<ActionResult<MovieList>> GetMovies(int actorId)
    {
        var response = await _tmdbApi.GetMovies(actorId);
        return Ok(response);
    }

    /// <summary>
    /// Add a rating to a movie.
    /// </summary>
    [HttpPost("movies/{movieId:int}/rating")]
    public async Task<ActionResult<ResponseBody>> AddRating(int movieId, [FromBody] Rating rating)
    {
        var response = await _tmdbApi.AddRating(movieId, rating);
        return Ok(response);
    }

    /// <summary>
    /// Delete a rating from a movie.
    /// </summary>
    [HttpDelete("movies/{movieId:int}/rating")]
    public async Task<ActionResult<ResponseBody>> DeleteRating(int movieId)
    {
        var response = await _tmdbApi.DeleteRating(movieId);
        return Ok(response);
    }
}
