using MVP_for_StudyGekko.Models;
using MVP_for_StudyGekko.Services;
using System.IO;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MVP_for_StudyGekko.Handlers;

public class UpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IOrchestrationService _orchestrator;
    private readonly IDocumentBuilder _documentBuilder;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        ITelegramBotClient bot,
        IOrchestrationService orchestrator,
        IDocumentBuilder documentBuilder,
        ILogger<UpdateHandler> logger)
    {
        _bot = bot;
        _orchestrator = orchestrator;
        _documentBuilder = documentBuilder;
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
        var docum = message.Document!;

        _logger.LogInformation("Message from {ChatId}: {Text}", chatId, text);

        string? fileId = message switch
        {
            { Photo: { } photos } => photos[^1].FileId,  // берём наибольшее разрешение
            { Document: { } doc } => doc.FileId,
            { Video: { } video } => video.FileId,
            { Audio: { } audio } => audio.FileId,
            { Voice: { } voice } => voice.FileId,
            { VideoNote: { } vn } => vn.FileId,
            _ => null
        };


        //file processing method
        if (fileId != null)
        {
            var file = await _bot.GetFile(fileId, ct);
            await _bot.SendMessage(chatId, "📄 Анализирую файл...", cancellationToken: ct);

            var filePath = file.FilePath!;
            await using var stream = new MemoryStream();
            await _bot.DownloadFile(filePath, stream, ct);

            var fileData = new FileData(
                stream,
                docum.FileName ?? "document",
                docum.MimeType ?? "application/octet-stream"
            );

            var json = await _orchestrator
                .WithFile(fileData)
                .GetResultAsync(ct);

            if (string.IsNullOrWhiteSpace(json))
            {
                await _bot.SendMessage(chatId, "Не удалось извлечь данные из файла.", cancellationToken: ct);
                return;
            }

            // Если слишком длинный — отправляем как файл
            if (json.Length > 4000)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                using var streamO = new MemoryStream(bytes);
                stream.Position = 0;

                await _bot.SendDocument(
                    chatId,
                    InputFile.FromStream(stream, "requirements.json"),
                    caption: "Результат анализа",
                    cancellationToken: ct
                );
            }
            else
            {
                await _bot.SendMessage(chatId, $"```json\n{json}\n```",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }

            await stream.DisposeAsync();
        }

        if (text.StartsWith("/start"))
        {
            await _bot.SendMessage(
                chatId,
                "Привет! Я помогу написать студенческую работу.\n\n" +
                "Используй команду:\n" +
                "/create [тема] — создать работу\n\n" +
                "Пример: /create Влияние социальных сетей на молодёжь",
                cancellationToken: ct);
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
                var docBytes = await _documentBuilder.BuildAsync(result, request);
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

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }
}