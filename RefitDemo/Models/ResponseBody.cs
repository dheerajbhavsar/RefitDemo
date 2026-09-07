using System.Text.Json.Serialization;

namespace RefitDemo.Models;

public class ResponseBody
{
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("status_message")]
    public string StatusMessage { get; set; } = string.Empty;
}
