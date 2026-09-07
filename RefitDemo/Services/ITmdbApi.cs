using Refit;
using RefitDemo.Models;

namespace RefitDemo.Services;

/// <summary>
/// Refit interface representing TMDB (The Movie Database) v3 API.
/// Refit generates the concrete implementation at compile/runtime.
/// </summary>
[Headers("accept: application/json",
         "Authorization: Bearer")]
public interface ITmdbApi
{
    /// <summary>
    /// Search for an actor/actress by name.
    /// GET /search/person?query={name}
    /// </summary>
    [Get("/search/person?query={name}")]
    Task<ActorList> GetActors(string name);

    /// <summary>
    /// Search for an actor/actress by name with ApiResponse&lt;T&gt; wrapper
    /// which provides HTTP status, headers, raw response, and error details.
    /// GET /search/person?query={name}
    /// </summary>
    [Get("/search/person?query={name}")]
    Task<ApiResponse<ActorList>> GetActorsWithResponse(string name);

    /// <summary>
    /// Get the movie credits of an actor/actress.
    /// GET /person/{actorId}/movie_credits?language=en-US
    /// </summary>
    [Get("/person/{actorId}/movie_credits?language=en-US")]
    Task<MovieList> GetMovies(int actorId);

    /// <summary>
    /// Add rating to a movie.
    /// POST /movie/{movieId}/rating
    /// </summary>
    [Headers("Content-Type: application/json;charset=utf-8")]
    [Post("/movie/{movieId}/rating")]
    Task<ResponseBody> AddRating(int movieId, [Body] Rating rating);

    /// <summary>
    /// Delete rating from a movie.
    /// DELETE /movie/{movieId}/rating
    /// </summary>
    [Delete("/movie/{movieId}/rating")]
    Task<ResponseBody> DeleteRating(int movieId);
}
