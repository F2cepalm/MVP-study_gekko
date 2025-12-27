using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

public interface IOrchestrationService
{
    Task<WorkResult> GenerateWorkAsync(WorkRequest request, CancellationToken ct = default);
}

public class OrchestrationService : IOrchestrationService
{
    private readonly ILlmServiceFactory _llmFactory;
    private readonly ILogger<OrchestrationService> _logger;

    public OrchestrationService(ILlmServiceFactory llmFactory, ILogger<OrchestrationService> logger)
    {
        _llmFactory = llmFactory;
        _logger = logger;
    }

    public async Task<WorkResult> GenerateWorkAsync(WorkRequest request, CancellationToken ct = default)
    {
        try
        {
            var llm = _llmFactory.Create(LlmProvider.Claude); // Внутренний выбор
            var workTypeName = PromptTemplates.GetWorkTypeName(request.Type);

            _logger.LogInformation("Starting generation for topic: {Topic}", request.Topic);

            // Шаг 1: Генерация плана
            var outlinePrompt = PromptTemplates.GenerateOutline(request.Topic, workTypeName, request.TargetPages);
            var outline = await llm.GenerateAsync(outlinePrompt, ct);


            _logger.LogInformation("Outline generated");

            var introTask = llm.GenerateAsync(
    PromptTemplates.GenerateIntroduction(request.Topic, outline), ct);
            var conclusionTask = llm.GenerateAsync(
                PromptTemplates.GenerateConclusion(request.Topic, outline), ct);

            await Task.WhenAll(introTask, conclusionTask);

            var introduction = await introTask;
            var conclusion = await conclusionTask;

            _logger.LogInformation("Introduction generated");
            _logger.LogInformation("Conclusion generated");

            // Сборка документа
            var document = $"""
                # {request.Topic}

                ## Введение

                {introduction}

                ## Содержание

                {outline}

                ## Заключение

                {conclusion}
                """;

            return new WorkResult
            {
                Success = true,
                Content = document
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generation failed for topic: {Topic}", request.Topic);
            return new WorkResult
            {
                Success = false,
                ErrorMessage = "Не удалось сгенерировать работу. Попробуйте позже."
            };
        }
    }
}
