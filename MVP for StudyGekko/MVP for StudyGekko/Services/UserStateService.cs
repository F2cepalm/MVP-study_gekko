using System.Collections.Concurrent;

namespace MVP_for_StudyGekko.Services;

public class UserStateService
{
    private readonly ConcurrentDictionary<long, UserState> _states = new();

    public bool IsGenerating(long chatId) =>
        _states.TryGetValue(chatId, out var state) && state.IsGenerating;

    public void SetGenerating(long chatId, bool value)
    {
        _states.AddOrUpdate(chatId,
            new UserState { IsGenerating = value },
            (_, existing) => { existing.IsGenerating = value; return existing; });
    }
}

public class UserState
{
    public bool IsGenerating { get; set; }
    public DateTime? StartedAt { get; set; }
}