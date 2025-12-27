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
    private readonly UserStateService _userState;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        ITelegramBotClient bot,
        IOrchestrationService orchestrator,
        IDocumentBuilder documentBuilder,
        UserStateService userState,
        ILogger<UpdateHandler> logger)
    {
        _bot = bot;
        _orchestrator = orchestrator;
        _documentBuilder = documentBuilder;
        _userState = userState;
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
                "Используй команду:\n" +
                "/create [тема] — создать работу\n\n" +
                "Пример: /create Влияние социальных сетей на молодёжь",
                cancellationToken: ct);
            return;
        }

        if (text.StartsWith("/create"))
        {
            if (_userState.IsGenerating(chatId))
            {
                await _bot.SendMessage(chatId, "Предыдущая работа ещё генерируется. Подожди.", cancellationToken: ct);
                return;
            }

            var topic = text.Replace("/create", "").Trim();

            if (topic.Length > 500)
            {
                await _bot.SendMessage(chatId, "Тема слишком длинная. Максимум 500 символов.", cancellationToken: ct);
                return;
            }

            if (topic.Length < 10)
            {
                await _bot.SendMessage(chatId, "Тема слишком короткая. Опиши подробнее.", cancellationToken: ct);
                return;
            }

            _userState.SetGenerating(chatId, true);

            if (string.IsNullOrEmpty(topic))
            {
                await _bot.SendMessage(
                    chatId,
                    "Укажи тему после команды.\n\nПример: /create Экология городов",
                    cancellationToken: ct);
                return;
            }

            await _bot.SendMessage(chatId, "Начинаю работу. Это займёт пару минут...", cancellationToken: ct);

            try
            {

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
            }
            finally
            {
                _userState.SetGenerating(chatId, false);  // снимаем флаг в любом случае
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

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }
}