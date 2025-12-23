namespace MVP_for_StudyGekko.Models;

public enum WorkType
{
    Essay,
    Referat,
    Coursework
}

public class WorkRequest
{
    public long ChatId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public WorkType Type { get; set; } = WorkType.Essay;
    public int TargetPages { get; set; } = 5;
}

public class WorkResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
