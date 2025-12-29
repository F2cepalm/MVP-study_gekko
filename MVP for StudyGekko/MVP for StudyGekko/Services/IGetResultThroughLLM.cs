namespace MVP_for_StudyGekko.Services
{
    public interface IGetResultThroughLLM
    {
        Task<string> GetResultAsync(CancellationToken ct = default);
    }
}
