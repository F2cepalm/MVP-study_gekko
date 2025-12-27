using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MVP_for_StudyGekko.Configuration;

namespace MVP_for_StudyGekko.Services;

public class ClaudeService : ILlmService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public ClaudeService(HttpClient http, IOptions<LlmConfiguration> config)
    {
        _http = http;
        _apiKey = config.Value.ClaudeApiKey;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Claude API error: {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }
}