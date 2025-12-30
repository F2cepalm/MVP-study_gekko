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
            var llm_claude = _llmFactory.Create(LlmProvider.Claude); // Выбор LLM для разных этапов
            var llm_gemini = _llmFactory.Create(LlmProvider.Gemini); 
            var workTypeName = PromptTemplates.GetWorkTypeName(request.Type);

            _logger.LogInformation("Starting generation for topic: {Topic}", request.Topic);

            // Шаг 1: Генерация плана
            var outlinePrompt = PromptTemplates.GenerateOutline(request.Topic, workTypeName, request.TargetPages);
            var outline = await llm_gemini.GenerateAsync(outlinePrompt, ct);

            _logger.LogInformation("Outline generated");

            // Шаг 2: Генерация введения
            var introPrompt = PromptTemplates.GenerateIntroduction(request.Topic, outline);
            var introduction = await llm_claude.GenerateAsync(introPrompt, ct);

            _logger.LogInformation("Introduction generated");

            // Шаг 3: Генерация заключения
            var conclusionPrompt = PromptTemplates.GenerateConclusion(request.Topic, outline);
            var conclusion = await llm_claude.GenerateAsync(conclusionPrompt, ct);

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
