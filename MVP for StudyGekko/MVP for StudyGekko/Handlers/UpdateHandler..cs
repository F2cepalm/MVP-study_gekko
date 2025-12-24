using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MVP_for_StudyGekko.Services;
using MVP_for_StudyGekko.Models;
using System.IO;

namespace MVP_for_StudyGekko.Handlers;

public class UpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IOrchestrationService _orchestrator;
    private readonly IDocumentBuilder _documentBuilder;
    private readonly IRequirementsAnalyzerService _requirementsAnalyzer;
    private readonly ISessionService _sessionService;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        ITelegramBotClient bot,
        IOrchestrationService orchestrator,
        IDocumentBuilder documentBuilder,
        IRequirementsAnalyzerService requirementsAnalyzer,
        ISessionService sessionService,
        ILogger<UpdateHandler> logger)
    {
        _bot = bot;
        _orchestrator = orchestrator;
        _documentBuilder = documentBuilder;
        _requirementsAnalyzer = requirementsAnalyzer;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        if (update.Type == UpdateType.Message)
        {
            await OnMessage(update.Message!, ct);
        }
    }

    private async Task OnMessage(Message message, CancellationToken ct)
    {
        var text = message.Text ?? string.Empty;
        var chatId = message.Chat.Id;

        _logger.LogInformation("Message from {ChatId}: {Text}", chatId, text);

        if (text.StartsWith("/start"))
        {
            await _bot.SendMessage(
                chatId,
                "Привет! Я помогу написать студенческую работу.\n\n" +
                "Доступные команды:\n" +
                "/create [тема] — создать работу\n" +
                "/requirements — загрузить файл с требованиями к оформлению\n\n" +
                "Пример: /create Влияние социальных сетей на молодёжь",
                cancellationToken: ct);
            return;
        }

        if (text.StartsWith("/requirements"))
        {
            await _bot.SendMessage(
                chatId,
                "Отправь мне файл с требованиями к оформлению (DOCX, DOC, PDF или TXT).\n\n" +
                "Я проанализирую его и буду использовать эти требования при создании документов.",
                cancellationToken: ct);
            return;
        }

        // Обработка документов (файлов с требованиями)
        if (message.Document != null)
        {
            await HandleDocumentAsync(message, ct);
            return;
        }

        if (text.StartsWith("/create"))
        {
            var topic = text.Replace("/create", "").Trim();

            if (string.IsNullOrEmpty(topic))
            {
                await _bot.SendMessage(
                    chatId,
                    "Укажи тему после команды.\n\nПример: /create Экология городов",
                    cancellationToken: ct);
                return;
            }

            await _bot.SendMessage(chatId, "Начинаю работу. Это займёт пару минут...", cancellationToken: ct);
            await _bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

            var request = new WorkRequest
            {
                ChatId = chatId,
                Topic = topic,
                Type = WorkType.Essay,
                TargetPages = 5
            };

            var result = await _orchestrator.GenerateWorkAsync(request, ct);

            if (result.Success)
            {
                // Получаем требования пользователя из сессии
                var requirements = _sessionService.GetRequirements(chatId);
                var docBytes = await _documentBuilder.BuildAsync(result, request, requirements);
                var fileName = $"{SanitizeFileName(topic)}.docx";

                using var stream = new MemoryStream(docBytes);
                await _bot.SendDocument(
                    chatId,
                    new Telegram.Bot.Types.InputFileStream(stream, fileName),
                    caption: "Готово! Вот твоя работа.",
                    cancellationToken: ct);
            }
            else
            {
                await _bot.SendMessage(chatId, result.ErrorMessage!, cancellationToken: ct);
            }
            return;
        }

        // Неизвестная команда
        await _bot.SendMessage(
            chatId,
            "Используй /create [тема] для создания работы.",
            cancellationToken: ct);
    }

    private async Task SendLongMessage(long chatId, string text, CancellationToken ct)
    {
        const int maxLength = 4000;

        if (text.Length <= maxLength)
        {
            await _bot.SendMessage(chatId, text, cancellationToken: ct);
            return;
        }

        var chunks = SplitText(text, maxLength);
        foreach (var chunk in chunks)
        {
            await _bot.SendMessage(chatId, chunk, cancellationToken: ct);
            await Task.Delay(500, ct); // Небольшая пауза между сообщениями
        }
    }

    private static List<string> SplitText(string text, int maxLength)
    {
        var chunks = new List<string>();
        var remaining = text;

        while (remaining.Length > 0)
        {
            if (remaining.Length <= maxLength)
            {
                chunks.Add(remaining);
                break;
            }

            var splitIndex = remaining.LastIndexOf('\n', maxLength);
            if (splitIndex <= 0)
            {
                splitIndex = remaining.LastIndexOf(' ', maxLength);
            }
            if (splitIndex <= 0)
            {
                splitIndex = maxLength;
            }

            chunks.Add(remaining[..splitIndex]);
            remaining = remaining[splitIndex..].TrimStart();
        }

        return chunks;
    }

    private async Task HandleDocumentAsync(Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var document = message.Document!;

        try
        {
            _logger.LogInformation("Processing document: {FileName} from {ChatId}", document.FileName, chatId);

            await _bot.SendMessage(chatId, "Анализирую файл с требованиями...", cancellationToken: ct);
            await _bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

            // Скачиваем файл
            var file = await _bot.GetFile(document.FileId, ct);
            if (file.FilePath == null)
            {
                await _bot.SendMessage(chatId, "Не удалось получить файл. Попробуй еще раз.", cancellationToken: ct);
                return;
            }

            using var stream = new MemoryStream();
            await _bot.DownloadFile(file.FilePath, stream, ct);
            var fileContent = stream.ToArray();

            // Анализируем файл через Gemini
            var requirements = await _requirementsAnalyzer.AnalyzeFileAsync(fileContent, document.FileName ?? "document", ct);

            // Сохраняем требования в сессии пользователя
            _sessionService.SetRequirements(chatId, requirements);

            _logger.LogInformation("Requirements saved for {ChatId}", chatId);

            await _bot.SendMessage(
                chatId,
                "✅ Требования к оформлению сохранены!\n\n" +
                "Теперь все документы будут создаваться с этими настройками.\n\n" +
                "Используй /create [тема] для создания работы.",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document from {ChatId}", chatId);
            await _bot.SendMessage(
                chatId,
                "Не удалось обработать файл. Проверь формат файла и попробуй снова.\n\n" +
                "Поддерживаемые форматы: DOCX, DOC, PDF, TXT",
                cancellationToken: ct);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }
}