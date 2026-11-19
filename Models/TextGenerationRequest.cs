using System.Text.Json.Serialization;

namespace AiDemo.Models;

public sealed class TextGenerationRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("max_output_tokens")]
    public int MaxOutputTokens { get; init; } = 250;
}
