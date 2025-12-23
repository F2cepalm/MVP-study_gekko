namespace MVP_for_StudyGekko.Services;

public interface ILlmService
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
}