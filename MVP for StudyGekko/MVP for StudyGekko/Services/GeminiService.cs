using Aspose.Words;
using Microsoft.Extensions.Options;
using MVP_for_StudyGekko.Configuration;
using MVP_for_StudyGekko.Models;
using System.Text;
using System.Text.Json;
using Telegram.Bot.Types;

namespace MVP_for_StudyGekko.Services;

public class GeminiService : ILlmService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiService(HttpClient http, IOptions<LlmConfiguration> config)
    {
        _http = http;
        _apiKey = config.Value.GeminiApiKey;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            //model = "gemini-3-flash-preview"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

        var response = await _http.PostAsync(url, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API error: {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    public async Task<string> GenerateFileAsync(string prompt, FileData file, CancellationToken ct = default)
    {
        file.Content.Position = 0;

        // Конвертируем если нужно
        var (processedStream, mimeType) = await PrepareFileAsync(file, ct);

        try
        {
            // 1. Загружаем файл в Gemini
            var fileUri = await UploadFileToGeminiAsync(processedStream, file.FileName, mimeType, ct);

            // 2. Генерируем контент с файлом
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                file_data = new
                                {
                                    mime_type = mimeType,
                                    file_uri = fileUri
                                }
                            },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var response = await _http.PostAsync(url, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API error: {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }
        finally
        {
            if (processedStream != file.Content)
            {
                await processedStream.DisposeAsync();
            }
        }
    }

    private async Task<(Stream stream, string mimeType)> PrepareFileAsync(FileData file, CancellationToken ct)
    {
        var fileName = file.FileName.ToLower();

        // Word форматы → конвертируем в PDF
        if (IsWordFormat(fileName))
        {
            var pdfStream = await ConvertToPdfAsync(file.Content, ct);
            return (pdfStream, "application/pdf");
        }

        // PDF и изображения — как есть
        return (file.Content, file.MimeType);
    }

    private static bool IsWordFormat(string fileName)
    {
        var extensions = new[] { ".docx", ".doc", ".rtf", ".odt" };
        return extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<MemoryStream> ConvertToPdfAsync(Stream inputStream, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            inputStream.Position = 0;
            var pdfStream = new MemoryStream();

            var doc = new Aspose.Words.Document(inputStream);
            doc.Save(pdfStream, SaveFormat.Pdf);

            pdfStream.Position = 0;
            return pdfStream;
        }, ct);
    }

    private async Task<string> UploadFileToGeminiAsync(
    Stream fileStream,
    string fileName,
    string mimeType,
    CancellationToken ct)
    {
        fileStream.Position = 0;
        var fileBytes = new byte[fileStream.Length];
        await fileStream.ReadAsync(fileBytes, ct);

        // Шаг 1: Инициализация upload — получаем URL
        var initUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={_apiKey}";

        var initRequest = new HttpRequestMessage(HttpMethod.Post, initUrl);
        initRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        initRequest.Headers.Add("X-Goog-Upload-Command", "start");
        initRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", fileBytes.Length.ToString());
        initRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);

        var metadata = new { file = new { display_name = fileName } };
        initRequest.Content = new StringContent(
            JsonSerializer.Serialize(metadata),
            Encoding.UTF8,
            "application/json"
        );

        var initResponse = await _http.SendAsync(initRequest, ct);

        if (!initResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrls))
        {
            var error = await initResponse.Content.ReadAsStringAsync(ct);
            throw new Exception($"Failed to get upload URL: {error}");
        }

        var uploadUrl = uploadUrls.First();

        // Шаг 2: Загружаем файл
        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
        uploadRequest.Content = new ByteArrayContent(fileBytes);
        uploadRequest.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

        var uploadResponse = await _http.SendAsync(uploadRequest, ct);
        var responseJson = await uploadResponse.Content.ReadAsStringAsync(ct);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini upload error: {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var uri = doc.RootElement
            .GetProperty("file")
            .GetProperty("uri")
            .GetString();

        return uri ?? throw new Exception("No file URI in response");
    }
}