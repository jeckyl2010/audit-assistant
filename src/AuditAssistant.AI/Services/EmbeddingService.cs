#pragma warning disable SKEXP0001
using Microsoft.SemanticKernel.Embeddings;

namespace AuditAssistant.AI.Services;

public class EmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public EmbeddingService(ITextEmbeddingGenerationService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embedding.ToArray();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.ToArray()).ToList();
    }
}
#pragma warning restore SKEXP0001
