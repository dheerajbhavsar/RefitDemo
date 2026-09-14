using System.ComponentModel.DataAnnotations;

namespace RefitDemo.Configuration;

/// <summary>
/// Strongly typed configuration options for TMDB API.
/// Validated on startup via the modern .NET Options Pattern and DataAnnotations.
/// </summary>
public class TmdbOptions
{
    public const string SectionName = "Tmdb";

    /// <summary>
    /// Base URL for the TMDB API v3 endpoints.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "TMDB BaseUrl is required.")]
    [Url(ErrorMessage = "TMDB BaseUrl must be a valid URL.")]
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3";

    /// <summary>
    /// TMDB API Read Access Token (v4 auth Bearer token).
    /// Must be a valid 3-segment JWT starting with 'eyJ'.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "TMDB ApiReadAccessToken is required. Please set 'Tmdb:ApiReadAccessToken' in appsettings.json, user secrets, or environment variables.")]
    [RegularExpression(@"^eyJ[a-zA-Z0-9_\-=]+\.[a-zA-Z0-9_\-=]+\.[a-zA-Z0-9_\-=]+$", 
        ErrorMessage = "TMDB ApiReadAccessToken is not a valid JWT token format. It must start with 'eyJ' and consist of 3 Base64URL-encoded segments separated by dots.")]
    public string ApiReadAccessToken { get; set; } = string.Empty;
}
