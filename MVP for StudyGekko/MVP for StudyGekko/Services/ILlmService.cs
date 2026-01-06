using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

public interface ILlmService
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateFileAsync(string prompt, FileData file, CancellationToken ct = default);
}