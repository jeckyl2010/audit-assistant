#pragma warning disable SKEXP0001
using Microsoft.Extensions.AI;

namespace AuditAssistant.AI.Services;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
        return embedding.Vector.ToArray();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingGenerator.GenerateAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.Vector.ToArray()).ToList();
    }
}
#pragma warning restore SKEXP0001
