using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using AiDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AiDemo.Pages;

public sealed class IndexModel(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<IndexModel> logger) : PageModel
{
    private const string PlaceholderImage = "https://placehold.co/800x500?text=Image+Preview";

    [BindProperty, Required, StringLength(1000)]
    public string Prompt { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(4000)]
    public string TextPrompt { get; set; } = string.Empty;

    public string GeneratedImageUrl { get; private set; } = PlaceholderImage;
    public string TextAnswer { get; private set; } = string.Empty;
    public string StatusMessage { get; private set; } = string.Empty;

    public async Task<IActionResult> OnPostGenerateImageAsync(CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(TextPrompt));
        if (!ModelState.IsValid)
        {
            StatusMessage = "Enter an image description.";
            return Page();
        }

        string? endpoint = configuration["Foundry:Endpoint"];
        string? apiKey = configuration["Foundry:ApiKey"];
        string? model = configuration["Foundry:ImageModel"];
        if (!HasConfiguration(endpoint, apiKey, model))
        {
            StatusMessage = "Missing Foundry image configuration. Configure it with .NET User Secrets.";
            return Page();
        }

        try
        {
            using HttpClient client = CreateClient(apiKey!);
            var request = new ImageGenerationRequest { Prompt = Prompt, Model = model! };
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{endpoint!.TrimEnd('/')}/mai/v1/images/generations", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                StatusMessage = $"Image request failed with status {(int)response.StatusCode}.";
                return Page();
            }

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            string? base64 = document.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
            if (string.IsNullOrWhiteSpace(base64))
            {
                StatusMessage = "The image service returned no image data.";
                return Page();
            }

            GeneratedImageUrl = $"data:image/png;base64,{base64}";
            StatusMessage = "Image generated successfully.";
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogError(exception, "Image generation failed");
            StatusMessage = "The image request could not be completed.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateTextAsync(CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(Prompt));
        if (!ModelState.IsValid)
        {
            StatusMessage = "Enter a text prompt.";
            return Page();
        }

        string? endpoint = configuration["Foundry:Endpoint"];
        string? apiKey = configuration["Foundry:ApiKey"];
        string? model = configuration["Foundry:TextModel"];
        if (!HasConfiguration(endpoint, apiKey, model))
        {
            StatusMessage = "Missing Foundry text configuration. Configure it with .NET User Secrets.";
            return Page();
        }

        try
        {
            using HttpClient client = CreateClient(apiKey!);
            var request = new TextGenerationRequest { Model = model!, Input = TextPrompt };
            string responseEndpoint = BuildResponsesEndpoint(endpoint!);
            using HttpResponseMessage response = await client.PostAsJsonAsync(responseEndpoint, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                StatusMessage = $"Text request failed with status {(int)response.StatusCode}.";
                return Page();
            }

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            TextAnswer = FindOutputText(document.RootElement) ?? "No text answer was returned.";
            StatusMessage = "Text generated successfully.";
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogError(exception, "Text generation failed");
            StatusMessage = "The text request could not be completed.";
        }

        return Page();
    }

    private HttpClient CreateClient(string apiKey)
    {
        HttpClient client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        return client;
    }

    private static bool HasConfiguration(params string?[] values) =>
        values.All(value => !string.IsNullOrWhiteSpace(value));

    private static string BuildResponsesEndpoint(string endpoint)
    {
        string value = endpoint.TrimEnd('/');
        if (!value.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
        {
            value = value.Contains("/openai", StringComparison.OrdinalIgnoreCase)
                ? $"{value}/responses"
                : $"{value}/openai/v1/responses";
        }

        return $"{value}?api-version=v1";
    }

    private static string? FindOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out JsonElement output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out JsonElement content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out JsonElement type) &&
                    type.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out JsonElement text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }
}
