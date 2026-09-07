using System.Text.Json.Serialization;

namespace RefitDemo.Models;

public class MovieList
{
    [JsonPropertyName("cast")]
    public List<Movie> Movies { get; set; } = [];
}
