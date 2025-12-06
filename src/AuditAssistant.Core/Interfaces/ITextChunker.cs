using AuditAssistant.Core.Models;

namespace AuditAssistant.Core.Interfaces;

public interface ITextChunker
{
    List<DocumentChunk> ChunkDocument(AuditDocument document, ChunkingOptions? options = null);
}
