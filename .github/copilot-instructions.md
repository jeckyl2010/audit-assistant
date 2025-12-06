# GitHub Copilot Instructions for AI Audit Assistant

## ⚠️ CRITICAL: RAG Architecture - ALWAYS Follow These Principles

**This is a RAG (Retrieval-Augmented Generation) system. The #1 rule:**

> **ALWAYS use PostgreSQL vector search to retrieve relevant chunks BEFORE sending content to AI.**
> **NEVER send entire documents to AI.**
> **NEVER bypass the vector database.**

**The RAG Flow (MANDATORY):**
1. Chunk documents semantically
2. Generate embeddings for ALL chunks
3. Store embeddings in local PostgreSQL + pgvector
4. For each analysis: 
   - Generate query embedding
   - Search PostgreSQL for top-K similar chunks (cosine distance)
   - Send ONLY retrieved chunks to AI
5. Cache everything - NEVER regenerate embeddings

**If you're writing code that sends full documents to AI without vector search, you're doing it wrong.**

## Project Overview

This is an AI-powered audit assistant built with .NET that analyzes audit documentation against compliance standards and generates professional audit reports. The system uses OpenAI/Azure OpenAI for intelligent analysis and **PostgreSQL 18 with pgvector 0.8.1 as the primary data store** for semantic document retrieval.

## Architecture

This is a **RAG (Retrieval-Augmented Generation)** system following December 2025 best practices:

- **CLI Layer**: Console application (`AuditAssistant.CLI`)
- **AI Layer**: Semantic Kernel integration (`AuditAssistant.AI`) 
  - **ALWAYS use vector search** to find relevant chunks before AI analysis
  - Analyzes only relevant content per requirement (NEVER entire documents)
  - Vector search is the PRIMARY method for content retrieval
- **Core Layer**: Domain models and interfaces (`AuditAssistant.Core`)
- **Data Layer**: PostgreSQL 18+ with pgvector 0.8.1 (`AuditAssistant.Data`)
  - Local PostgreSQL is the single source of truth for embeddings
  - Stores all chunked document embeddings (NEVER regenerate)
  - Provides fast cosine similarity search (<100ms for 10K chunks)
  - Embeddings are CACHED and REUSED for all operations

## Key Patterns

- **RAG Pattern**: ALWAYS retrieve relevant chunks via vector search before AI analysis
- Dependency Injection for all services
- Repository Pattern for data access
- Strategy Pattern for AI providers
- Async/await throughout
- Vector-First Approach: Use PostgreSQL embeddings as primary data source

## Coding Standards

### Style
- File-scoped namespaces
- Nullable reference types
- Async methods with `Async` suffix
- Private fields with `_` prefix
- Interfaces with `I` prefix

### RAG Integration (CRITICAL)
```csharp
// ALWAYS use this pattern for document analysis
// 1. Generate query embedding
var query = $"{requirement.Code}: {requirement.Title}. {requirement.Description}";
var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

// 2. Search vector database for relevant chunks
var relevantChunks = await _vectorStore.SearchSimilarAsync(
    queryEmbedding, 
    topK: 10,  // Top 10 most relevant chunks
    cancellationToken
);

// 3. Send ONLY relevant chunks to AI (NOT entire documents)
var contextBuilder = new StringBuilder();
foreach (var (docId, content, similarity) in relevantChunks.Take(5))
{
    contextBuilder.AppendLine($"[Relevance: {similarity:F2}]");
    contextBuilder.AppendLine(content);
    contextBuilder.AppendLine("---");
}

// 4. Analyze with AI
var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage(systemPrompt);
chatHistory.AddUserMessage(contextBuilder.ToString());

try {
    var response = await chatService.GetChatMessageContentAsync(
        chatHistory, cancellationToken: cancellationToken);
} catch (Exception ex) {
    _logger.LogError(ex, "AI analysis failed");
}
```

### Vector Operations (CRITICAL - Always Use)
```csharp
// Always include using statement
using Pgvector.EntityFrameworkCore;

// ALWAYS use cosine distance for similarity search
// This is the core of the RAG architecture
var results = await _context.DocumentEmbeddings
    .Select(e => new { 
        e.DocumentId,
        e.Content,
        Distance = e.Embedding.CosineDistance(queryVector) 
    })
    .OrderBy(e => e.Distance)  // Lower distance = more similar
    .Take(topK)
    .ToListAsync(cancellationToken);

// Convert distance to similarity score (0.0 to 1.0)
var similarityResults = results.Select(r => (
    r.DocumentId,
    r.Content,
    Similarity: 1.0 - r.Distance
)).ToList();
```

## Common Tasks

### Adding Compliance Standard
1. Create markdown in `examples/standards/`
2. Format: `### CODE: Title` for requirements
3. Include Control Objective and Guidance

### Adding AI Provider
1. Add config section in `appsettings.json`
2. Add case in `Program.cs` switch
3. Use Semantic Kernel methods

### Document Chunking (December 2025 Best Practices - TOKEN-AWARE)
1. **Use TOKEN-BASED chunking** (not character-based)
2. Use `SemanticTextChunker` with TiktokenSharp integration
3. Configure chunk sizes in TOKENS in `appsettings.json`
4. **Default: 512 tokens max, 50 tokens overlap** (optimal for text-embedding-3-small)
5. Preserve semantic boundaries (headers, paragraphs)
6. Maintain section metadata for context
7. **Calculate quality metrics** for every chunk (completeness, coherence, context richness, length)
8. Store quality metrics in chunk metadata for debugging and optimization
9. See `TOKEN-CHUNKING-IMPLEMENTATION.md` for complete guide
10. See `CHUNKING-STRATEGY.md` for strategy details
11. See `EMBEDDING-MODEL-CONFIGS.md` for model-specific settings

## Best Practices

✅ DO:

**Code Quality & Performance (December 2025):**
- **Use parallel processing** with SemaphoreSlim for concurrent operations (N+1 query prevention)
- **Use batch operations** (AddRange + single SaveChanges) for bulk inserts
- **Add comprehensive logging** with ILogger<T> for observability
- **Externalize configuration** (use RAGOptions, ChunkingOptions from appsettings.json)
- **Add input validation** (ArgumentNullException, range checks, dimension validation)
- **Calculate quality metrics** for all chunks (don't filter unless explicitly required)
- Use modern C# features (range operators `[start..end]`, raw string literals)
- Follow orchestrator pattern for complex workflows (separate concerns)

**RAG Architecture (CRITICAL - ALWAYS FOLLOW):**
- **ALWAYS use vector search** before sending content to AI
- **ALWAYS retrieve from PostgreSQL** - embeddings are the source of truth
- **NEVER send entire documents to AI** - use vector search to find relevant chunks
- Store ALL embeddings in local PostgreSQL immediately after generation
- Use cosine similarity for all vector searches
- Retrieve top 5-10 most relevant chunks per query (configurable via topK)
- Include similarity scores in analysis context
- Cache ALL embeddings - NEVER regenerate for existing content
- Use PostgreSQL as the single source of truth for all document content

**General Best Practices:**
- Use async/await for all I/O
- Include cancellation tokens
- Log at appropriate levels (especially similarity scores)
- Handle multiple AI response formats
- Validate file paths and extensions
- Use environment variables for secrets
- **Use official Docker/container images** (e.g., `pgvector/pgvector:pg18`)
- Verify image availability before updating documentation

**Chunking & Embeddings (TOKEN-AWARE - December 2025):**
- **Use TOKEN-BASED chunking** with TiktokenSharp (NEVER character-based)
- **Chunk large documents** using token-aware semantic boundaries
- Configure chunk sizes in TOKENS in `appsettings.json` based on embedding model
- Default: 512 tokens (optimal for most models), 50 token overlap
- Preserve context between chunks with metadata and token-based overlaps
- Use ~5-10% of model token limit for chunk size (512 tokens for 8,191 limit)
- **Generate quality metrics** for ALL chunks (completeness, coherence, context, length)
- Store quality metrics in metadata (QualityScore, TokenCount, QualityLevel, etc.)
- Generate embeddings for ALL chunks immediately
- Store in PostgreSQL with rich metadata (section title, position, quality metrics)
- **Never filter chunks by quality** unless explicitly required - keep all for analysis

❌ DON'T:

**RAG Anti-Patterns (NEVER DO THESE):**
- ❌ **NEVER send entire documents to AI without vector search**
- ❌ **NEVER bypass PostgreSQL for content retrieval**
- ❌ **NEVER regenerate embeddings for content already in database**
- ❌ **NEVER skip storing embeddings in PostgreSQL**
- ❌ **NEVER use embeddings without storing them first**
- ❌ **NEVER analyze documents without checking vector similarity first**
- ❌ **NEVER ignore similarity scores** (always log and validate)
- ❌ **NEVER retrieve all chunks** (always use topK to limit scope)

**General Anti-Patterns:**
- Hardcode secrets
- Use .Result or .Wait()
- Forget Pgvector.EntityFrameworkCore using
- Ignore JSON parsing failures
- Use wrong embedding dimensions (must match model: 1536 for text-embedding-3-small)
- Use archived or unofficial container images
- Create embeddings for entire large documents without chunking
- Split chunks mid-sentence or mid-code-block
- **Use character-based chunking** (ALWAYS use token-based with TiktokenSharp)
- **Ignore quality metrics** (ALWAYS calculate and log quality scores)
- Use magic numbers instead of RAGOptions configuration

## Security

- Sanitize all file paths
- Validate file extensions
- Never commit secrets
- Use environment variables for API keys

## Infrastructure

### Container Images
- **Always use official images** from verified sources
- PostgreSQL with pgvector: `pgvector/pgvector:pg18` (official, December 2025)
- PostgreSQL 18.x with pgvector 0.8.1 is the current production version
- Avoid archived images like `ankane/pgvector`
- Verify image tags exist before updating configuration
- Document image sources in setup guides

### Local PostgreSQL with pgvector (CRITICAL)
- **This is our primary data store** for all document embeddings
- PostgreSQL 18+ required for latest features
- pgvector 0.8.1+ required for optimal performance
- Volume mount: `/var/lib/postgresql` (NOT `/var/lib/postgresql/data` for PG 18+)
- Vector dimensions: 1536 for text-embedding-3-small, 3072 for text-embedding-3-large
- Indexes: ALWAYS index on `(document_id, chunk_index)` for performance
- Cosine distance operator: `<=>` (built into pgvector)

## Questions Before Suggesting

**RAG Architecture Questions (ALWAYS ASK FIRST):**
1. Am I using vector search to retrieve relevant chunks?
2. Am I sending only retrieved chunks to AI (not entire documents)?
3. Am I storing embeddings in PostgreSQL?
4. Am I reusing embeddings from PostgreSQL instead of regenerating?
5. Am I using cosine similarity search correctly?
6. Am I including similarity scores in the analysis context?
7. **Am I using TOKEN-BASED chunking** (not character-based)?
8. **Am I calculating quality metrics** for chunks?

**Code Quality Questions (December 2025):**
1. Does this follow existing patterns?
2. Is async/await needed?
3. Are there security implications?
4. Does error handling exist?
5. Is it performant for large datasets?
6. Am I using the local PostgreSQL database effectively?
7. **Am I using parallel processing** where appropriate (avoid N+1)?
8. **Am I using batch operations** for bulk inserts?
9. **Is logging comprehensive** (ILogger with structured logging)?
10. **Are magic numbers externalized** to configuration?
11. **Is input validated** (null checks, range checks)?
12. **Are quality metrics being tracked**?

---

## Final Reminder: RAG-First Development

**Every time you write code that touches documents or AI:**

1. **Ask**: "Am I using vector search to retrieve relevant chunks?"
2. **Ask**: "Am I storing embeddings in PostgreSQL?"
3. **Ask**: "Am I sending only retrieved chunks to AI?"

**If the answer to any is NO, stop and refactor.**

**Our PostgreSQL database with pgvector is NOT just storage—it's the core of our RAG architecture.**

See [RAG-QUICKREF.md](../RAG-QUICKREF.md) for quick reference patterns.

---

**This is enterprise audit software with RAG architecture. Prioritize:**
1. **RAG best practices** (vector search first, always)
2. **PostgreSQL as source of truth** (embeddings cached locally)
3. **Code quality, security, and reliability**
4. **Cost efficiency** (use cached embeddings, limit topK)

**December 2025 Status**: Production-ready RAG system ✅

## Recent Enhancements (December 2025)

### ✅ Token-Based Chunking & Quality Metrics
- **TiktokenSharp integration** for accurate token counting
- **512 token default** chunk size (optimal for text-embedding-3-small)
- **50 token overlap** between chunks
- **Quality metrics** calculated for every chunk:
  - Completeness (sentence boundaries)
  - Coherence (semantic flow)
  - Context richness (headers, metadata)
  - Length score (optimal token count)
- **Quality metadata** stored in PostgreSQL for debugging and optimization
- See `TOKEN-CHUNKING-IMPLEMENTATION.md` for details

### ✅ Performance Optimizations
- **Parallel processing** with controlled concurrency (MaxConcurrentRequirements)
- **Batch operations** for embeddings (50x faster bulk inserts)
- **Idempotency checks** to prevent duplicate embeddings
- **Comprehensive logging** with performance metrics
- **5-10x faster** analysis for large audits

### ✅ Code Quality Improvements
- **RAGOptions** configuration class for all RAG parameters
- **Input validation** on all public methods
- **AuditWorkflowOrchestrator** for clean separation of concerns
- **Modern C# patterns** (range operators, raw string literals)
- **Zero build warnings** - production-ready quality
