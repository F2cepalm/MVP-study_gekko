using MVP_for_StudyGekko.Configuration;
using MVP_for_StudyGekko.Handlers;
using MVP_for_StudyGekko.Services;
using Polly;
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
var botToken = builder.Configuration["BotConfiguration:Token"]
    ?? Environment.GetEnvironmentVariable("BOT_TOKEN")
    ?? throw new InvalidOperationException("Bot token not configured");
builder.Services.AddHttpClient("telegram")
    .AddTypedClient<ITelegramBotClient>(client =>
        new TelegramBotClient(botToken, client));
//хэндлер
builder.Services.AddScoped<UpdateHandler>();

// OpenAPI
builder.Services.AddOpenApi();

// LLM Services
builder.Services.AddHttpClient<ClaudeService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddSingleton<ILlmServiceFactory, LlmServiceFactory>();

// Orchestrator
builder.Services.AddScoped<IOrchestrationService, OrchestrationService>();

// Word Builder
builder.Services.AddScoped<IDocumentBuilder, WordDocumentBuilder>();

//Polly
builder.Services.AddHttpClient<ClaudeService>()
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

//Status
builder.Services.AddSingleton<UserStateService>();

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