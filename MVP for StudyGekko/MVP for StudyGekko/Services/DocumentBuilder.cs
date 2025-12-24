using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

public interface IDocumentBuilder
{
    Task<byte[]> BuildAsync(WorkResult result, WorkRequest request, DocumentRequirements? requirements = null);
}