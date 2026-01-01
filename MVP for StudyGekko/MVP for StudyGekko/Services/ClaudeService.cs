using DocumentFormat.OpenXml.EMMA;
using Microsoft.Extensions.Options;
using MVP_for_StudyGekko.Configuration;
using MVP_for_StudyGekko.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MVP_for_StudyGekko.Services;

public class ClaudeService : ILlmService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public ClaudeService(HttpClient http, IOptions<LlmConfiguration> config)
    {
        _http = http;
        _apiKey = config.Value.ClaudeApiKey;

        _http.BaseAddress = new Uri("https://api.anthropic.com/");
        _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var request = new
        {
            model = "claude-sonnet-4-5-20250929",
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
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

    public async Task<string> GenerateFileAsync(string prompt, FileData file, CancellationToken ct = default)
    {
        // Claude принимает файлы как base64 в content
        file.Content.Position = 0;
        using var ms = new MemoryStream();
        await file.Content.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());

        var mediaType = GetClaudeMediaType(file.MimeType);

        object[] contentParts;

        if (mediaType.StartsWith("image/"))
        {
            // Изображения
            contentParts = new object[]
            {
                new
                {
                    type = "image",
                    source = new
                    {
                        type = "base64",
                        media_type = mediaType,
                        data = base64
                    }
                },
                new { type = "text", text = prompt }
            };
        }
        else if (mediaType == "application/pdf")
        {
            // PDF (Claude поддерживает напрямую)
            contentParts = new object[]
            {
                new
                {
                    type = "document",
                    source = new
                    {
                        type = "base64",
                        media_type = mediaType,
                        data = base64
                    }
                },
                new { type = "text", text = prompt }
            };
        }
        else
        {
            // Текстовые файлы — читаем как текст
            file.Content.Position = 0;
            using var reader = new StreamReader(file.Content, leaveOpen: true);
            var textContent = await reader.ReadToEndAsync(ct);

            contentParts = new object[]
            {
                new
                {
                    type = "text",
                    text = $"File: {file.FileName}\n\n{textContent}"
                },
                new { type = "text", text = prompt }
            };
        }

        var request = new
        {
            model = "claude-sonnet-4-5-20250929",
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = contentParts }
            }
        };

        var response = await SendRequestAsync(request, ct);
        return ExtractText(response);
    }

    private async Task<string> SendRequestAsync(object request, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("v1/messages", content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Claude API error: {responseJson}");
        }

        return responseJson;
    }

    private static string ExtractText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);

        var contentArray = doc.RootElement.GetProperty("content");

        foreach (var block in contentArray.EnumerateArray())
        {
            if (block.GetProperty("type").GetString() == "text")
            {
                return block.GetProperty("text").GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string GetClaudeMediaType(string mimeType)
    {
        // Claude поддерживает ограниченный набор типов
        return mimeType.ToLower() switch
        {
            "image/jpeg" or "image/jpg" => "image/jpeg",
            "image/png" => "image/png",
            "image/gif" => "image/gif",
            "image/webp" => "image/webp",
            "application/pdf" => "application/pdf",
            _ => mimeType
        };
    }
}
