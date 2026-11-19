using System.Text.Json.Serialization;

namespace AiDemo.Models;

public sealed class ImageGenerationRequest
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; } = 1024;

    [JsonPropertyName("height")]
    public int Height { get; init; } = 768;

    [JsonPropertyName("model")]
    public required string Model { get; init; }
}
