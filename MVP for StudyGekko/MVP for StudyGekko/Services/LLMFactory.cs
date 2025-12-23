namespace MVP_for_StudyGekko.Services;

public enum LlmProvider
{
    Claude,
    Gemini
}

public interface ILlmServiceFactory
{
    ILlmService Create(LlmProvider provider);
}

public class LlmServiceFactory : ILlmServiceFactory
{
    private readonly IServiceProvider _services;

    public LlmServiceFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ILlmService Create(LlmProvider provider)
    {
        return provider switch
        {
            LlmProvider.Claude => _services.GetRequiredService<ClaudeService>(),
            LlmProvider.Gemini => _services.GetRequiredService<GeminiService>(),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };
    }
}