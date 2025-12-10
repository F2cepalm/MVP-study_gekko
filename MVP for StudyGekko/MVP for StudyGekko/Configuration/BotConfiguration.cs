namespace MVP_for_StudyGekko.Configuration;

public class BotConfiguration
{
    public string Token { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
}

public class LlmConfiguration
{
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
}
