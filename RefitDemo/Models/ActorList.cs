using System.Text.Json.Serialization;

namespace RefitDemo.Models;

public class ActorList
{
    [JsonPropertyName("results")]
    public List<Actor> Actors { get; set; } = [];
}
