using MVP_for_StudyGekko.Configuration;
using MVP_for_StudyGekko.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Services.Configure<BotConfiguration>(
    builder.Configuration.GetSection("BotConfiguration"));
builder.Services.Configure<LlmConfiguration>(
    builder.Configuration.GetSection("LlmConfiguration"));

// Telegram Bot
var botConfig = builder.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
builder.Services.AddHttpClient("telegram")
    .AddTypedClient<ITelegramBotClient>(client =>
        new TelegramBotClient(botConfig!.Token, client));
//хэндлер
builder.Services.AddScoped<UpdateHandler>();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//эндпоинт
app.MapPost("/bot", async (ITelegramBotClient bot, UpdateHandler handler, Update update, CancellationToken ct) =>
{
    await handler.HandleAsync(update, ct);
    return Results.Ok();
});

app.MapGet("/", () => "StudyGekko Bot is running");

app.Run();