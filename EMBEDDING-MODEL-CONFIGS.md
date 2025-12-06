# Embedding Model Configuration Guide

## Overview

Different embedding models have different token limits and optimal chunk sizes. This guide provides recommended configurations for common models.

## Recommended Configurations

### OpenAI Models

#### text-embedding-3-small (Default)
```json
{
  "AI": {
    "OpenAI": {
      "EmbeddingModel": "text-embedding-3-small"
    }
  },
  "Chunking": {
    "MaxChunkSize": 1000,
    "OverlapSize": 200,
    "Strategy": "Semantic"
  }
}
```
- **Token Limit**: 8,191 tokens
- **Dimensions**: 1,536
- **Cost**: $0.00002 / 1K tokens
- **Best For**: General purpose, cost-effective

#### text-embedding-3-large
```json
{
  "AI": {
    "OpenAI": {
      "EmbeddingModel": "text-embedding-3-large"
    }
  },
  "Chunking": {
    "MaxChunkSize": 1200,
    "OverlapSize": 250,
    "Strategy": "Semantic"
  }
}
```
- **Token Limit**: 8,191 tokens
- **Dimensions**: 3,072
- **Cost**: $0.00013 / 1K tokens
- **Best For**: Higher precision requirements

#### text-embedding-ada-002 (Legacy)
```json
{
  "AI": {
    "OpenAI": {
      "EmbeddingModel": "text-embedding-ada-002"
    }
  },
  "Chunking": {
    "MaxChunkSize": 1000,
    "OverlapSize": 200,
    "Strategy": "Semantic"
  }
}
```
- **Token Limit**: 8,191 tokens
- **Dimensions**: 1,536
- **Cost**: $0.00010 / 1K tokens
- **Note**: Consider upgrading to text-embedding-3-small

### Azure OpenAI Models

#### text-embedding-3-small (Azure)
```json
{
  "AI": {
    "AzureOpenAI": {
      "EmbeddingDeploymentName": "text-embedding-3-small"
    }
  },
  "Chunking": {
    "MaxChunkSize": 1000,
    "OverlapSize": 200,
    "Strategy": "Semantic"
  }
}
```
- Same specs as OpenAI version
- Pricing varies by region

### Other Models (Future Support)

#### Cohere embed-english-v3.0
```json
{
  "Chunking": {
    "MaxChunkSize": 800,
    "OverlapSize": 150,
    "Strategy": "Semantic"
  }
}
```
- **Token Limit**: 512 tokens
- **Dimensions**: 1,024
- **Note**: Smaller context window requires smaller chunks

#### Voyage AI voyage-2
```json
{
  "Chunking": {
    "MaxChunkSize": 2000,
    "OverlapSize": 300,
    "Strategy": "Semantic"
  }
}
```
- **Token Limit**: 16,000 tokens
- **Dimensions**: 1,536
- **Note**: Larger context allows bigger chunks

## Token Estimation

### Character to Token Ratio (English)
- **English text**: ~0.75 tokens per character
- **Code**: ~0.5 tokens per character
- **Technical docs**: ~0.7 tokens per character

### Safe Chunk Size Calculation
```
Safe Chunk Size = (Model Token Limit × 0.75) × 0.8
```

Example for text-embedding-3-small:
```
(8,191 × 0.75) × 0.8 = ~4,900 characters maximum
Recommended: 1,000 characters (safety margin)
```

## Chunk Size Guidelines

### By Document Type

| Document Type | Recommended Chunk Size | Overlap | Rationale |
|---------------|------------------------|---------|-----------|
| **Technical Specs** | 800-1,000 chars | 150-200 | Dense information, frequent references |
| **Policies** | 1,000-1,200 chars | 200-250 | Longer paragraphs, contextual |
| **Code Documentation** | 600-800 chars | 100-150 | Preserve function boundaries |
| **Audit Reports** | 1,000-1,500 chars | 200-300 | Structured sections |
| **Compliance Standards** | 800-1,000 chars | 200 | Requirement precision |

### By Content Density

| Density | Characteristics | Chunk Size | Overlap |
|---------|----------------|------------|---------|
| **High** | Lists, tables, code | 600-800 | 100-150 |
| **Medium** | Mixed content | 1,000 | 200 |
| **Low** | Narrative, descriptive | 1,200-1,500 | 250-300 |

## Overlap Strategy

### Purpose of Overlap
- Maintains context between chunks
- Prevents information loss at boundaries
- Improves retrieval for cross-boundary concepts

### Recommended Overlap Ratios
- **Conservative**: 20-25% of chunk size (e.g., 200 chars for 1,000 char chunks)
- **Balanced**: 15-20% of chunk size
- **Aggressive**: 10-15% of chunk size (use when storage is a concern)

### Overlap Calculation
```
Overlap Size = MaxChunkSize × 0.20
```

Examples:
- 500 char chunks → 100 char overlap
- 1,000 char chunks → 200 char overlap
- 1,500 char chunks → 300 char overlap

## Configuration Examples

### For Cost Optimization
```json
{
  "AI": {
    "OpenAI": {
      "EmbeddingModel": "text-embedding-3-small"
    }
  },
  "Chunking": {
    "MaxChunkSize": 1500,
    "OverlapSize": 200,
    "Strategy": "Semantic"
  }
}
```
- Larger chunks = fewer API calls = lower cost
- Trade-off: Slightly less precise retrieval

### For Maximum Precision
```json
{
  "AI": {
    "OpenAI": {
      "EmbeddingModel": "text-embedding-3-large"
    }
  },
  "Chunking": {
    "MaxChunkSize": 800,
    "OverlapSize": 200,
    "Strategy": "Semantic"
  }
}
```
- Smaller chunks = more precise retrieval
- Higher quality embeddings
- Trade-off: More API calls, higher cost

### For Code-Heavy Documents
```json
{
  "Chunking": {
    "MaxChunkSize": 600,
    "OverlapSize": 100,
    "Strategy": "Semantic",
    "PreserveCodeBlocks": true,
    "PreserveTables": true
  }
}
```
- Smaller chunks preserve function boundaries
- Code blocks kept intact

## Performance Impact

### Chunk Size vs API Calls

| Max Chunk Size | Avg Chunks per 10KB Doc | API Calls | Est. Cost (3-small) |
|----------------|-------------------------|-----------|---------------------|
| 500 chars | 25 | 25 | $0.00050 |
| 1,000 chars | 12 | 12 | $0.00024 |
| 1,500 chars | 8 | 8 | $0.00016 |
| 2,000 chars | 6 | 6 | $0.00012 |

### Retrieval Quality

Smaller chunks generally provide:
- ✅ More precise matching
- ✅ Better context isolation
- ❌ More storage required
- ❌ Slower initial processing

Larger chunks provide:
- ✅ Faster processing
- ✅ Less storage
- ❌ Less precise matching
- ❌ Context dilution risk

## Dynamic Configuration

### Environment-Specific Settings

**Development:**
```json
{
  "Chunking": {
    "MaxChunkSize": 500,
    "OverlapSize": 100
  }
}
```
- Smaller for faster iteration

**Production:**
```json
{
  "Chunking": {
    "MaxChunkSize": 1000,
    "OverlapSize": 200
  }
}
```
- Balanced for real-world use

**High-Volume:**
```json
{
  "Chunking": {
    "MaxChunkSize": 1500,
    "OverlapSize": 200
  }
}
```
- Larger to reduce API costs

## Migration Guide

### Changing Chunk Sizes

If you need to change chunk configuration for existing data:

1. **Update appsettings.json** with new values
2. **Drop existing embeddings**:
   ```sql
   TRUNCATE TABLE document_embeddings;
   ```
3. **Re-process documents** with new configuration
4. **Test retrieval quality**

### A/B Testing Configurations

```csharp
// Test different configurations
var configs = new[]
{
    new ChunkingOptions { MaxChunkSize = 800, OverlapSize = 150 },
    new ChunkingOptions { MaxChunkSize = 1000, OverlapSize = 200 },
    new ChunkingOptions { MaxChunkSize = 1200, OverlapSize = 250 }
};

foreach (var config in configs)
{
    var chunks = textChunker.ChunkDocument(document, config);
    // Evaluate retrieval quality
}
```

## Best Practices

1. ✅ **Start with defaults** (1,000/200) and adjust based on results
2. ✅ **Match chunk size to model capacity** (use ~10-15% of token limit)
3. ✅ **Maintain 20% overlap ratio** for context preservation
4. ✅ **Test retrieval quality** after configuration changes
5. ✅ **Monitor costs** vs accuracy trade-offs
6. ✅ **Document your configuration** and reasoning

## References

- [OpenAI Embeddings Pricing](https://openai.com/api/pricing/)
- [Azure OpenAI Models](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models)
- [Embedding Best Practices](https://platform.openai.com/docs/guides/embeddings)

---

**Last Updated**: December 2025  
**Configuration Version**: 2.0
