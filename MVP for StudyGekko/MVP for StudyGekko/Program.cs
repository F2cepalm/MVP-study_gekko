using MVP_for_StudyGekko.Configuration;
using MVP_for_StudyGekko.Handlers;
using MVP_for_StudyGekko.Services;
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

// OpenAPI
builder.Services.AddOpenApi();

// LLM Services
builder.Services.AddHttpClient<ClaudeService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddSingleton<ILlmServiceFactory, LlmServiceFactory>();
builder.Services.AddHttpClient<ILlmService, GeminiService>();
builder.Services.AddHttpClient<ILlmService, ClaudeService>();

//хэндлер
builder.Services.AddScoped<UpdateHandler>();

//builder.Services.AddScoped<GetRequirementsJson>();

// Orchestrator
builder.Services.AddScoped<IOrchestrationService, OrchestrationService>();

// Word Builder
builder.Services.AddScoped<IDocumentBuilder, WordDocumentBuilder>();

// Get Requirements JSON
builder.Services.AddSingleton<GetRequirementsJson>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

//эндпоинт
app.MapPost("/bot", async (ITelegramBotClient bot, UpdateHandler handler, Update update, CancellationToken ct) =>
{
    await handler.HandleAsync(update, ct);
    return Results.Ok();
});

app.MapGet("/", () => "StudyGekko Bot is running");

app.Run();