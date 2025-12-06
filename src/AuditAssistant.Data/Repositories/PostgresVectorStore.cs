using AuditAssistant.Core.Interfaces;
using AuditAssistant.Core.Models;
using AuditAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text.Json;

namespace AuditAssistant.Data.Repositories;

public class PostgresVectorStore : IVectorStore
{
    private readonly AuditDbContext _context;
    private readonly RAGOptions _ragOptions;
    private readonly ILogger<PostgresVectorStore> _logger;

    public PostgresVectorStore(
        AuditDbContext context,
        RAGOptions ragOptions,
        ILogger<PostgresVectorStore> logger)
    {
        _context = context;
        _ragOptions = ragOptions;
        _logger = logger;
    }

    public async Task StoreDocumentAsync(
        Guid documentId,
        string content,
        float[] embedding,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var vector = new Vector(embedding);
        var metadataJson = JsonSerializer.Serialize(metadata);

        var documentEmbedding = new DocumentEmbedding
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkId = null,
            ChunkIndex = 0,
            Content = content,
            Embedding = vector,
            MetadataJson = metadataJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentEmbeddings.Add(documentEmbedding);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task StoreChunkAsync(
        Guid documentId,
        Guid chunkId,
        int chunkIndex,
        string content,
        float[] embedding,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var vector = new Vector(embedding);
        var metadataJson = JsonSerializer.Serialize(metadata);

        var documentEmbedding = new DocumentEmbedding
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkId = chunkId,
            ChunkIndex = chunkIndex,
            Content = content,
            Embedding = vector,
            MetadataJson = metadataJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentEmbeddings.Add(documentEmbedding);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<(Guid DocumentId, string Content, double Similarity)>> SearchSimilarAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        
        if (queryEmbedding.Length != _ragOptions.EmbeddingDimension)
        {
            throw new ArgumentException(
                $"Expected embedding dimension {_ragOptions.EmbeddingDimension}, got {queryEmbedding.Length}",
                nameof(queryEmbedding));
        }

        if (topK < 1 || topK > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                topK,
                "TopK must be between 1 and 100");
        }

        var queryVector = new Vector(queryEmbedding);

        var results = await _context.DocumentEmbeddings
            .Select(e => new
            {
                e.DocumentId,
                e.Content,
                Distance = e.Embedding.CosineDistance(queryVector)
            })
            .OrderBy(e => e.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Vector search returned {ResultCount} results (topK: {TopK})",
            results.Count,
            topK);

        return results.Select(r => (
            r.DocumentId,
            r.Content,
            1.0 - r.Distance
        )).ToList();
    }

    public async Task StoreChunksBatchAsync(
        IEnumerable<(Guid DocumentId, Guid ChunkId, int ChunkIndex, string Content, float[] Embedding, Dictionary<string, string> Metadata)> chunks,
        CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        
        _logger.LogInformation("Storing batch of {ChunkCount} chunks", chunkList.Count);

        var embeddings = chunkList.Select(chunk => new DocumentEmbedding
        {
            Id = Guid.NewGuid(),
            DocumentId = chunk.DocumentId,
            ChunkId = chunk.ChunkId,
            ChunkIndex = chunk.ChunkIndex,
            Content = chunk.Content,
            Embedding = new Vector(chunk.Embedding),
            MetadataJson = JsonSerializer.Serialize(chunk.Metadata),
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.DocumentEmbeddings.AddRange(embeddings);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully stored {ChunkCount} chunks", chunkList.Count);
    }

    public async Task<bool> ChunkExistsAsync(Guid chunkId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentEmbeddings
            .AnyAsync(e => e.ChunkId == chunkId, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var embeddings = await _context.DocumentEmbeddings
            .Where(e => e.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Deleting {EmbeddingCount} embeddings for document {DocumentId}",
            embeddings.Count,
            documentId);

        _context.DocumentEmbeddings.RemoveRange(embeddings);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
