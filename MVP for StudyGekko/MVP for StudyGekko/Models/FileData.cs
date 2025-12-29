namespace MVP_for_StudyGekko.Models
{
    public record FileData(
        Stream Content,
        string FileName,
        string MimeType
    );
}
