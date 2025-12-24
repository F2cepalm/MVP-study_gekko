using System.Collections.Concurrent;
using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

/// <summary>
/// Сервис для хранения требований к форматированию документов пользователей
/// Использует in-memory хранилище с автоматической очисткой старых данных
/// </summary>
public interface ISessionService
{
    /// <summary>Сохранить требования для пользователя</summary>
    void SetRequirements(long chatId, DocumentRequirements requirements);

    /// <summary>Получить требования пользователя</summary>
    DocumentRequirements? GetRequirements(long chatId);

    /// <summary>Удалить требования пользователя</summary>
    void ClearRequirements(long chatId);

    /// <summary>Проверить наличие требований</summary>
    bool HasRequirements(long chatId);
}

public class SessionService : ISessionService
{
    private readonly ConcurrentDictionary<long, SessionData> _sessions = new();
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(24);

    public void SetRequirements(long chatId, DocumentRequirements requirements)
    {
        _sessions[chatId] = new SessionData
        {
            Requirements = requirements,
            LastUpdated = DateTime.UtcNow
        };
    }

    public DocumentRequirements? GetRequirements(long chatId)
    {
        if (_sessions.TryGetValue(chatId, out var session))
        {
            // Проверка на устаревание данных
            if (DateTime.UtcNow - session.LastUpdated < _sessionTimeout)
            {
                return session.Requirements;
            }

            // Удаляем устаревшие данные
            _sessions.TryRemove(chatId, out _);
        }

        return null;
    }

    public void ClearRequirements(long chatId)
    {
        _sessions.TryRemove(chatId, out _);
    }

    public bool HasRequirements(long chatId)
    {
        return GetRequirements(chatId) != null;
    }

    private class SessionData
    {
        public DocumentRequirements Requirements { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
