using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MVP_for_StudyGekko.Handlers;

public class UpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        _logger.LogInformation("Received update type: {Type}", update.Type);

        var handler = update.Type switch
        {
            UpdateType.Message => OnMessage(update.Message!, ct),
            UpdateType.CallbackQuery => OnCallbackQuery(update.CallbackQuery!, ct),
            _ => UnknownUpdateType(update)
        };

        await handler;
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
                "Привет! Я StudyGekko — помогу с написанием студенческих работ.\n\n" +
                "Отправь тему работы, и я начну генерацию.",
                cancellationToken: ct);
            return;
        }

        // Эхо для теста
        await _bot.SendMessage(chatId, $"Получил: {text}", cancellationToken: ct);
    }

    private async Task OnCallbackQuery(CallbackQuery query, CancellationToken ct)
    {
        await _bot.AnswerCallbackQuery(query.Id, "Обработано", cancellationToken: ct);
    }

    private Task UnknownUpdateType(Update update)
    {
        _logger.LogWarning("Unknown update type: {Type}", update.Type);
        return Task.CompletedTask;
    }
}
