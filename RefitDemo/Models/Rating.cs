using System.Text.Json.Serialization;

namespace RefitDemo.Models;

public class Rating
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}
