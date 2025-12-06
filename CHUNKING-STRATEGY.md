# Document Chunking Strategy

## Overview

As of December 2025, this application implements **semantic text chunking** to ensure optimal information retrieval and context preservation when processing audit documents.

## Why Chunking?

### Problems with Single-Document Embeddings:
1. **Token Limits**: Embedding models have maximum input sizes (8,191 tokens for text-embedding-3-small)
2. **Context Dilution**: Large documents lose semantic precision in a single embedding
3. **Poor Retrieval**: Can't locate specific sections within documents
4. **Memory Inefficiency**: Processing entire documents is wasteful

### Benefits of Semantic Chunking:
✅ **Respects semantic boundaries** (headers, paragraphs, sections)  
✅ **Maintains context** through overlapping chunks  
✅ **Improves retrieval accuracy** by finding relevant passages  
✅ **Preserves hierarchical structure** (section titles, metadata)  
✅ **Handles large documents** without hitting token limits  

## Implementation Details

### Chunking Parameters (Configurable)

All chunking parameters are configurable via `appsettings.json`:

```json
{
  "Chunking": {
    "MaxChunkSize": 1000,
    "OverlapSize": 200,
    "Strategy": "Semantic",
    "PreserveCodeBlocks": true,
    "PreserveTables": true
  }
}
```

| Parameter | Default | Purpose |
|-----------|---------|---------|
| **MaxChunkSize** | 1,000 chars | ~750 tokens (safe for text-embedding-3-small) |
| **OverlapSize** | 200 chars | ~150 tokens to preserve context between chunks |
| **Strategy** | Semantic | Chunking algorithm (Semantic, FixedSize, Sentence) |
| **PreserveCodeBlocks** | true | Keep code blocks intact |
| **PreserveTables** | true | Keep tables intact |

**Note**: Chunk sizes should be adjusted based on your embedding model. See [EMBEDDING-MODEL-CONFIGS.md](EMBEDDING-MODEL-CONFIGS.md) for model-specific recommendations.

### Strategy: `SemanticTextChunker`

Our implementation follows best practices from December 2025:

#### 1. **Markdown-Aware Parsing**
- Detects headers (`#`, `##`, `###`, etc.) as natural boundaries
- Preserves code blocks without splitting
- Respects list structures
- Maintains table integrity

#### 2. **Semantic Boundaries**
```
Document → Sections → Paragraphs → Chunks
```

- Splits at **header boundaries** first
- Falls back to **paragraph boundaries** (double newlines)
- Preserves **sentence integrity** in overlaps

#### 3. **Context Preservation**

**Overlap Strategy:**
```
Chunk 1: [---------------]
Chunk 2:        [overlap][----------]
Chunk 3:                  [overlap][-----]
```

- 200-character overlap ensures context continuity
- Overlap text ends at sentence boundaries (. ! ?)
- Each chunk includes section title metadata

#### 4. **Metadata Enrichment**

Each chunk stores:
```json
{
  "ChunkIndex": "0",
  "SectionTitle": "Security Requirements",
  "StartPosition": "0",
  "EndPosition": "1000",
  "Title": "ISO 27001 Compliance",
  "Version": "2.0",
  "Date": "2025-12-05"
}
```

## Example

### Input Document:
```markdown
# Security Policy

## Access Control
Users must authenticate with MFA...
[1200 characters]

## Data Encryption
All data at rest must use AES-256...
[900 characters]
```

### Output Chunks:
```
Chunk 0: [Security Policy + Access Control section (1000 chars)]
Chunk 1: [Last 200 chars of Access Control + Data Encryption (1000 chars)]
```

## Database Schema

### DocumentEmbedding Table
```sql
CREATE TABLE document_embeddings (
    id UUID PRIMARY KEY,
    document_id UUID NOT NULL,
    chunk_id UUID,              -- NULL for non-chunked documents
    chunk_index INTEGER,         -- Position in document
    content TEXT NOT NULL,
    embedding vector(1536),      -- pgvector type
    metadata_json JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    
    INDEX idx_document_id (document_id),
    INDEX idx_chunk_id (chunk_id),
    INDEX idx_document_chunk (document_id, chunk_index)
);
```

## Vector Search

When searching for relevant content:

1. **Query → Embedding**: Convert search query to vector
2. **Cosine Similarity**: Find top-K similar chunks
3. **Ranking**: Results include:
   - Chunk content
   - Section title
   - Document metadata
   - Similarity score

```csharp
var results = await vectorStore.SearchSimilarAsync(queryEmbedding, topK: 10);
// Returns: List<(Guid DocumentId, string Content, double Similarity)>
```

## Optimization Tips

### For Short Documents (< 500 chars)
- No chunking needed
- Store as single embedding

### For Medium Documents (500-2000 chars)
- Use 2-3 chunks
- 150-char overlap

### For Large Documents (> 2000 chars)
- Full semantic chunking
- 200-char overlap
- More granular retrieval

### For Code-Heavy Documents
- Preserve code blocks intact
- Chunk at function boundaries
- Include surrounding context

## Token Counting

**Rough Approximation:**
- 1 character ≈ 0.75 tokens (English text)
- 1,000 chars ≈ 750 tokens
- text-embedding-3-small limit: 8,191 tokens
- Safe chunk size: 1,000 chars (750 tokens + margin)

## Future Enhancements

### Planned (2026):
- [ ] Token-aware chunking (using tiktoken or similar)
- [ ] Adaptive chunk sizes based on content type
- [ ] Hierarchical embeddings (document + chunk level)
- [ ] Sliding window chunking for dense content
- [ ] Multi-lingual chunking support

### Considered:
- Sentence-transformer chunking
- LLM-based semantic boundary detection
- Knowledge graph integration

## Configuration

### Model-Specific Settings

See [EMBEDDING-MODEL-CONFIGS.md](EMBEDDING-MODEL-CONFIGS.md) for detailed configuration recommendations for different embedding models.

**Quick Reference:**
- `text-embedding-3-small`: 1000 chars, 200 overlap
- `text-embedding-3-large`: 1200 chars, 250 overlap
- `text-embedding-ada-002`: 1000 chars, 200 overlap

## References

- [OpenAI Embedding Best Practices](https://platform.openai.com/docs/guides/embeddings)
- [LangChain Text Splitters](https://python.langchain.com/docs/modules/data_connection/document_transformers/)
- [pgvector Performance Guide](https://github.com/pgvector/pgvector)
- [EMBEDDING-MODEL-CONFIGS.md](EMBEDDING-MODEL-CONFIGS.md) - Model-specific configuration guide

## Performance Metrics

### Expected Results:
- **Retrieval Accuracy**: 85-95% on relevant chunks
- **Processing Speed**: ~50 chunks/second (including embedding generation)
- **Storage Overhead**: ~1.5x compared to full documents (due to overlap)

---

**Updated:** December 2025  
**Version:** 2.0
