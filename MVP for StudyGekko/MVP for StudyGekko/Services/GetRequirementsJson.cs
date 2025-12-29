using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services
{
    public class GetRequirementsJson : IGetResultThroughLLM
    {
        private readonly ILlmService _llmService;
        private FileData? _file;

        private const string Prompt = """
        Проанализируй этот файл и извлеки все требования.
        Верни результат строго в JSON формате:
        {
            "requirements": [
                {
                    "id": "REQ-001",
                    "description": "описание требования",
                    "priority": "high|medium|low",
                    "category": "категория"
                }
            ]
        }
        Только JSON, без пояснений.
        """;

        public GetRequirementsJson(ILlmService llmService)
        {
            _llmService = llmService;
        }

        public GetRequirementsJson WithFile(FileData file)
        {
            _file = file;
            return this;
        }

        public async Task<string> GetResultAsync(CancellationToken ct = default)
        {
            if (_file is null)
                throw new InvalidOperationException("File not provided. Call WithFile() first.");

            return await _llmService.GenerateFileAsync(Prompt, _file, ct);
        }
    }
}
