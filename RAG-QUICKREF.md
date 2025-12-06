# RAG Quick Reference - ALWAYS Follow This Pattern

## ⚠️ The Golden Rule

> **ALWAYS retrieve from PostgreSQL vector database BEFORE analyzing with AI**

## The Correct RAG Pattern

### ✅ CORRECT: Vector Search → AI Analysis

```csharp
// 1. Generate query embedding
var query = $"{requirement.Code}: {requirement.Title}";
var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

// 2. Search PostgreSQL for relevant chunks
var relevantChunks = await _vectorStore.SearchSimilarAsync(
    queryEmbedding, 
    topK: 10
);

// 3. Build context from ONLY relevant chunks
var context = string.Join("\n---\n", 
    relevantChunks.Take(5).Select(c => 
        $"[Relevance: {c.Similarity:F2}]\n{c.Content}"
    )
);

// 4. Send to AI
var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage(systemPrompt);
chatHistory.AddUserMessage(context);  // Only relevant chunks!
var response = await _chatService.GetChatMessageContentAsync(chatHistory);
```

### ❌ WRONG: Direct Document Analysis

```csharp
// ❌ NEVER DO THIS
var allDocuments = await documentParser.ParseDirectoryAsync(path);
var fullContent = string.Join("\n", allDocuments.Select(d => d.Content));

var chatHistory = new ChatHistory();
chatHistory.AddUserMessage(fullContent);  // ❌ Entire documents!
var response = await _chatService.GetChatMessageContentAsync(chatHistory);
```

**Why this is wrong:**
- Wastes API tokens ($$$)
- Exceeds context limits
- Lower accuracy (AI gets distracted by irrelevant content)
- Doesn't use the vector database we set up

## Storage Pattern

### ✅ CORRECT: Store Immediately

```csharp
// Generate embedding
var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);

// IMMEDIATELY store in PostgreSQL
await _vectorStore.StoreChunkAsync(
    chunk.DocumentId,
    chunk.Id,
    chunk.ChunkIndex,
    chunk.Content,
    embedding,
    chunk.Metadata
);
```

### ❌ WRONG: Generate Without Storing

```csharp
// ❌ NEVER DO THIS
var embedding = await _embeddingService.GenerateEmbeddingAsync(content);
// ... use embedding but don't store it
// Next time: generate again (wasting API calls)
```

## Retrieval Configuration

### Default Settings (Good Starting Point)

```csharp
var chunks = await _vectorStore.SearchSimilarAsync(
    queryEmbedding,
    topK: 10  // Get top 10 most similar chunks
);

// Use top 5 for analysis (filter out lowest relevance)
var topChunks = chunks.Take(5);
```

### Adjust Based on Use Case

| Use Case | topK | Filter | Rationale |
|----------|------|--------|-----------|
| **Focused Analysis** | 5 | Take(3) | Highly relevant only |
| **Balanced** (default) | 10 | Take(5) | Good coverage |
| **Comprehensive** | 20 | Take(10) | Maximum context |
| **Cost-Optimized** | 5 | Take(3) | Minimum tokens |

## Quality Checks

### ALWAYS Monitor Similarity Scores

```csharp
var chunks = await _vectorStore.SearchSimilarAsync(queryEmbedding, topK: 10);

// Log similarity scores
var avgSimilarity = chunks.Average(c => c.Similarity);
var minSimilarity = chunks.Min(c => c.Similarity);

_logger.LogInformation(
    "Retrieved {Count} chunks. Avg similarity: {Avg:F3}, Min: {Min:F3}",
    chunks.Count, avgSimilarity, minSimilarity
);

// Warn if relevance is low
if (avgSimilarity < 0.6)
{
    _logger.LogWarning(
        "Low relevance scores for requirement {Code}. Consider improving query or chunking.",
        requirement.Code
    );
}
```

### Filter by Similarity Threshold

```csharp
// Only use chunks above threshold
var relevantChunks = chunks
    .Where(c => c.Similarity > 0.5)  // At least 50% similarity
    .Take(5)
    .ToList();

if (!relevantChunks.Any())
{
    _logger.LogWarning("No relevant chunks found for {Query}", query);
    return new List<Finding>();  // No findings if no relevant content
}
```

## Common Mistakes to Avoid

### ❌ Mistake #1: Bypassing Vector Search

```csharp
// ❌ WRONG
var documents = await _documentParser.ParseDirectoryAsync(path);
await _aiAnalyzer.AnalyzeDocumentsAsync(documents, standards);
```

```csharp
// ✅ CORRECT
var documents = await _documentParser.ParseDirectoryAsync(path);

// 1. Chunk and embed
foreach (var doc in documents) {
    var chunks = _textChunker.ChunkDocument(doc);
    foreach (var chunk in chunks) {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);
        await _vectorStore.StoreChunkAsync(...);
    }
}

// 2. Analyze using vector search
foreach (var requirement in standards.Requirements) {
    var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(requirement.Description);
    var relevantChunks = await _vectorStore.SearchSimilarAsync(queryEmbedding, topK: 10);
    await _aiAnalyzer.AnalyzeChunksAsync(relevantChunks, requirement);
}
```

### ❌ Mistake #2: Regenerating Embeddings

```csharp
// ❌ WRONG - Regenerating every time
foreach (var requirement in requirements) {
    var embedding = await _embeddingService.GenerateEmbeddingAsync(requirement.Description);
    // ... use but don't store
}
```

```csharp
// ✅ CORRECT - Check if exists first
foreach (var requirement in requirements) {
    var existing = await _vectorStore.GetEmbeddingAsync(requirement.Id);
    
    if (existing == null) {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(requirement.Description);
        await _vectorStore.StoreAsync(requirement.Id, embedding);
    }
    
    var chunks = await _vectorStore.SearchSimilarAsync(existing ?? embedding, topK: 10);
}
```

### ❌ Mistake #3: Ignoring Chunk Metadata

```csharp
// ❌ WRONG - Just content
var context = string.Join("\n", chunks.Select(c => c.Content));
```

```csharp
// ✅ CORRECT - Include metadata for context
var context = string.Join("\n\n", chunks.Select(c => 
    $"[Source: {c.Metadata["Title"]}, Section: {c.Metadata["SectionTitle"]}, Relevance: {c.Similarity:F2}]\n" +
    c.Content
));
```

## Performance Tips

### Batch Embedding Generation

```csharp
// Generate multiple embeddings in one call
var texts = chunks.Select(c => c.Content).ToList();
var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

for (int i = 0; i < chunks.Count; i++) {
    await _vectorStore.StoreChunkAsync(chunks[i], embeddings[i]);
}
```

### Use Database Transactions

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();

try {
    foreach (var chunk in chunks) {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);
        await _vectorStore.StoreChunkAsync(chunk, embedding);
    }
    
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

### Monitor Query Performance

```csharp
var sw = Stopwatch.StartNew();
var chunks = await _vectorStore.SearchSimilarAsync(queryEmbedding, topK: 10);
sw.Stop();

_logger.LogInformation(
    "Vector search completed in {Ms}ms for {Count} results",
    sw.ElapsedMilliseconds, chunks.Count
);

// Should be < 100ms for most queries
if (sw.ElapsedMilliseconds > 200) {
    _logger.LogWarning("Slow vector search detected. Consider adding indexes.");
}
```

## Checklist for Every AI Analysis Feature

Before implementing any AI analysis feature, ask:

- [ ] Am I chunking the documents?
- [ ] Am I generating embeddings for all chunks?
- [ ] Am I storing embeddings in PostgreSQL?
- [ ] Am I using vector search to find relevant chunks?
- [ ] Am I sending ONLY relevant chunks to AI?
- [ ] Am I including similarity scores in the context?
- [ ] Am I logging similarity metrics?
- [ ] Am I reusing embeddings instead of regenerating?
- [ ] Am I using cosine distance for similarity?
- [ ] Am I respecting topK limits?

**If you answered NO to any of these, revise your approach.**

## References

- [RAG-ARCHITECTURE.md](RAG-ARCHITECTURE.md) - Detailed architecture
- [CHUNKING-STRATEGY.md](CHUNKING-STRATEGY.md) - Chunking implementation
- [.github/copilot-instructions.md](.github/copilot-instructions.md) - Full guidelines

---

**Last Updated**: December 5, 2025  
**Remember**: PostgreSQL + pgvector is our primary data store. Use it!
