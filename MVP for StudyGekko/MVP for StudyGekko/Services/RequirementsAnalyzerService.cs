using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MVP_for_StudyGekko.Configuration;
using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

/// <summary>
/// Сервис для анализа файлов с требованиями к форматированию через Gemini API
/// Использует multimodal capabilities Gemini для обработки различных форматов файлов
/// </summary>
public interface IRequirementsAnalyzerService
{
    /// <summary>Анализировать файл и извлечь требования к форматированию</summary>
    Task<DocumentRequirements> AnalyzeFileAsync(byte[] fileContent, string fileName, CancellationToken ct = default);
}

public class RequirementsAnalyzerService : IRequirementsAnalyzerService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<RequirementsAnalyzerService> _logger;

    public RequirementsAnalyzerService(
        HttpClient http,
        IOptions<LlmConfiguration> config,
        ILogger<RequirementsAnalyzerService> logger)
    {
        _http = http;
        _apiKey = config.Value.GeminiApiKey;
        _logger = logger;
    }

    public async Task<DocumentRequirements> AnalyzeFileAsync(byte[] fileContent, string fileName, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing file: {FileName}", fileName);

            // Конвертируем файл в base64
            var base64Content = Convert.ToBase64String(fileContent);
            var mimeType = GetMimeType(fileName);

            // Создаем промпт для анализа
            var analysisPrompt = CreateAnalysisPrompt();

            // Формируем multimodal запрос к Gemini
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = analysisPrompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Content
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    response_mime_type = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={_apiKey}";

            var response = await _http.PostAsync(url, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Error}", responseJson);
                throw new Exception($"Gemini API error: {responseJson}");
            }

            // Парсим ответ
            using var doc = JsonDocument.Parse(responseJson);
            var responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            _logger.LogInformation("Gemini response: {Response}", responseText);

            // Парсим JSON с требованиями
            var requirements = JsonSerializer.Deserialize<DocumentRequirements>(responseText ?? "{}");

            return requirements ?? new DocumentRequirements();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze file: {FileName}", fileName);
            // Возвращаем пустые требования (все значения будут "default")
            return new DocumentRequirements();
        }
    }

    private string CreateAnalysisPrompt()
    {
        return @"Проанализируй этот документ и извлеки все параметры форматирования.
Верни результат СТРОГО в JSON формате согласно следующей структуре.

Важно:
1. Если какой-то параметр не может быть определен из документа, используй значение ""default""
2. Все числовые значения должны быть строками
3. Отступы измеряются в twips (1/1440 дюйма): 1см = 567 twips
4. Размеры шрифта в half-points: 14pt = 28
5. Межстрочный интервал: 1.0 = 240, 1.5 = 360, 2.0 = 480

Структура JSON:
{
  ""Page"": {
    ""TopMargin"": ""значение в twips или default"",
    ""BottomMargin"": ""значение в twips или default"",
    ""LeftMargin"": ""значение в twips или default"",
    ""RightMargin"": ""значение в twips или default"",
    ""PageSize"": ""A4 или Letter или default"",
    ""Orientation"": ""portrait или landscape или default""
  },
  ""Font"": {
    ""Name"": ""название шрифта или default"",
    ""Size"": ""размер в half-points или default"",
    ""Color"": ""hex цвет или default""
  },
  ""Paragraph"": {
    ""Alignment"": ""left/right/center/both или default"",
    ""LineSpacing"": ""значение в twips или default"",
    ""LineSpacingRule"": ""auto/exact/atLeast или default"",
    ""FirstLineIndent"": ""значение в twips или default"",
    ""SpaceBefore"": ""значение в twips или default"",
    ""SpaceAfter"": ""значение в twips или default""
  },
  ""Heading"": {
    ""FontSize"": ""размер в half-points или default"",
    ""Bold"": ""true/false или default"",
    ""Alignment"": ""left/right/center или default"",
    ""SpaceBefore"": ""значение в twips или default"",
    ""SpaceAfter"": ""значение в twips или default""
  },
  ""Title"": {
    ""FontSize"": ""размер в half-points или default"",
    ""Bold"": ""true/false или default"",
    ""Alignment"": ""left/right/center или default"",
    ""SpaceAfter"": ""значение в twips или default""
  }
}

Верни ТОЛЬКО JSON, без дополнительных комментариев или объяснений.";
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".rtf" => "application/rtf",
            _ => "application/octet-stream"
        };
    }
}
