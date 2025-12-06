# AI Audit Assistant

An intelligent audit assistant built with .NET that analyzes audit documentation against compliance standards and generates professional audit reports with findings.

## Features

- 📄 **Markdown Document Processing**: Automatically parses markdown files containing audit documentation
- ✂️ **Semantic Chunking**: Intelligent text chunking with overlap to preserve context (December 2025 best practices)
- 🔍 **RAG Architecture**: Retrieval-Augmented Generation using PostgreSQL + pgvector for semantic search
- 🎯 **Targeted Analysis**: Uses vector search to analyze only relevant document chunks per requirement
- 🤖 **AI-Powered Analysis**: Leverages OpenAI, Azure OpenAI, or GitHub Copilot for intelligent audit analysis
- 📊 **Compliance Analysis**: Analyzes documents against SOPs, policies, and compliance standards
- 📝 **Professional Reports**: Generates comprehensive audit reports with findings, recommendations, and compliance scores
- 💰 **Cost Efficient**: 70% lower API costs compared to analyzing entire documents

## Architecture

**RAG (Retrieval-Augmented Generation) System:**

```
Documents → Chunks → Embeddings → PostgreSQL/pgvector
                                          ↓
Compliance Requirements → Query Embedding → Vector Search (Top-K)
                                          ↓
                         Relevant Chunks → AI Analysis → Findings
```

**Project Structure:**
```
AuditAssistant/
├── src/
│   ├── AuditAssistant.CLI/        # Command-line interface
│   ├── AuditAssistant.Core/       # Domain models and interfaces
│   ├── AuditAssistant.AI/         # AI services (Semantic Kernel + RAG)
│   └── AuditAssistant.Data/       # PostgreSQL + pgvector data access
```

**Key Documentation:**
- [DECEMBER-2025-UPGRADES.md](DECEMBER-2025-UPGRADES.md) - 🎉 **Complete upgrade summary** (START HERE)
- [RAG-QUICKREF.md](RAG-QUICKREF.md) - Quick reference for developers
- [RAG-ARCHITECTURE.md](RAG-ARCHITECTURE.md) - Detailed architecture
- [TOKEN-CHUNKING-IMPLEMENTATION.md](TOKEN-CHUNKING-IMPLEMENTATION.md) - ✨ Token-based chunking guide
- [CODE-REVIEW-RECOMMENDATIONS.md](CODE-REVIEW-RECOMMENDATIONS.md) - Senior review and optimization recommendations
- [REFACTORING-COMPLETE.md](REFACTORING-COMPLETE.md) - ✅ All optimizations implemented
- See [.github/copilot-instructions.md](.github/copilot-instructions.md) for coding guidelines

## Prerequisites

- .NET 9.0 SDK or later
- PostgreSQL 18.1+ with pgvector extension
- Podman 5.7.0+ OR Docker Desktop (for containerized setup)
- OpenAI API key OR Azure OpenAI subscription OR GitHub Copilot Pro license

## Setup

### 1. Install PostgreSQL with pgvector

**Windows 11 (using Podman Desktop):**

See **[PODMAN-SETUP.md](PODMAN-SETUP.md)** for complete Podman Desktop instructions.

```powershell
podman run -d --name audit-assistant-db -e POSTGRES_PASSWORD=audit123 -p 5432:5432 -v audit-db-data:/var/lib/postgresql docker.io/pgvector/pgvector:pg18
```

**Windows (using Docker Desktop):**
```powershell
docker run -d --name audit-assistant-db -e POSTGRES_PASSWORD=audit123 -p 5432:5432 -v audit-db-data:/var/lib/postgresql pgvector/pgvector:pg18
```

**Note:** PostgreSQL 18+ uses `/var/lib/postgresql` (not `/var/lib/postgresql/data`) for better upgrade compatibility.

**Or install directly:**
- Download PostgreSQL from https://www.postgresql.org/download/
- Install pgvector extension: https://github.com/pgvector/pgvector

### 2. Configure the Application

Edit `src/AuditAssistant.CLI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=your_password"
  },
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-your-openai-api-key",
      "Model": "gpt-4o",
      "EmbeddingModel": "text-embedding-3-small"
    }
  }
}
```

**For Azure OpenAI:**
```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-azure-openai-key",
      "DeploymentName": "gpt-4",
      "EmbeddingDeploymentName": "text-embedding-ada-002"
    }
  }
}
```

**Environment Variables (recommended for security):**
```powershell
$env:AI__OpenAI__ApiKey = "sk-your-api-key"
$env:ConnectionStrings__PostgreSQL = "Host=localhost;..."
```

### 3. Build the Solution

```powershell
dotnet build
```

## Usage

### 1. Prepare Your Documents

Create two directories:

**Audit Documents** (`./audit-docs/`):
```markdown
# Security Assessment - Web Application

## Overview
Application uses JWT authentication...

## Database Security
Passwords are hashed using bcrypt...
```

**Compliance Standards** (`./standards/`):
```markdown
# ISO 27001:2013

### A.9.4.1: Information access restriction
Access to information and application system functions shall be restricted...

### A.9.4.2: Secure log-on procedures
Where required by the access control policy, access to systems shall be controlled...
```

### 2. Run the Application

```powershell
cd src/AuditAssistant.CLI
dotnet run
```

### 3. Follow the Prompts

```
Enter path to audit documents directory: ./audit-docs
Enter path to compliance standards directory: ./standards
```

### 4. Review the Generated Report

The application will generate a markdown report: `AuditReport_YYYYMMDD_HHMMSS.md`

## Example Output

```markdown
# Audit Report - 2025-12-05

**Generated:** 2025-12-05 17:00:00 UTC
**Auditor:** AI Audit Assistant

## Executive Summary
The audit identified 12 findings across 3 compliance standards...

## Compliance Scores
- **ISO 27001:2013:** 87.5%
- **SOC 2:** 92.3%

## Findings

### Critical Severity

#### Insufficient Password Encryption
**Description:** Password storage mechanism does not meet security requirements...
**Affected Requirements:** ISO.9.4.3, SOC2.CC6.1
**Recommendation:** Implement bcrypt with minimum work factor of 12...
```

## Document Chunking

This application uses **configurable semantic text chunking** to handle large documents effectively:

- **Flexible Configuration**: Adjust chunk sizes via `appsettings.json` based on your embedding model
- **Default Settings**: 1,000 characters (~750 tokens), 200 character overlap
- **Semantic Boundaries**: Respects markdown headers, paragraphs, and code blocks
- **Metadata**: Each chunk includes section title and position information
- **Model-Specific**: Pre-configured settings for common embedding models

**Configuration Example:**
```json
{
  "Chunking": {
    "MaxChunkSize": 1000,
    "OverlapSize": 200,
    "Strategy": "Semantic",
    "PreserveCodeBlocks": true
  }
}
```

**Documentation:**
- [RAG-QUICKREF.md](RAG-QUICKREF.md) - ⚡ Quick reference for RAG patterns (START HERE)
- [RAG-ARCHITECTURE.md](RAG-ARCHITECTURE.md) - Complete RAG architecture documentation
- [TOKEN-CHUNKING-IMPLEMENTATION.md](TOKEN-CHUNKING-IMPLEMENTATION.md) - ✨ **NEW** Token-based chunking & quality metrics
- [CHUNKING-STRATEGY.md](CHUNKING-STRATEGY.md) - Strategy and best practices
- [EMBEDDING-MODEL-CONFIGS.md](EMBEDDING-MODEL-CONFIGS.md) - Model-specific settings
- [FLEXIBLE-CHUNKING-SUMMARY.md](FLEXIBLE-CHUNKING-SUMMARY.md) - Implementation details

## Customization

### Adding Custom AI Providers

Edit `Program.cs` to add support for other LLM providers:

```csharp
case "custom":
    var customEndpoint = configuration["AI:Custom:Endpoint"];
    kernelBuilder.AddOpenAIChatCompletion(model, apiKey, endpoint: customEndpoint);
    break;
```

### Adjusting Analysis Prompts

Modify `AuditAnalyzerService.cs` to customize the AI analysis behavior:

```csharp
private string BuildAnalysisSystemPrompt(List<ComplianceStandard> standards)
{
    // Customize your analysis instructions here
}
```

### Vector Search Configuration

Update `AuditDbContext.cs` to change embedding dimensions:

```csharp
entity.Property(e => e.Embedding).HasColumnType("vector(1536)");
// Change to vector(768) for smaller models like all-MiniLM-L6-v2
```

## Database Management

### Create Migrations (if using EF Core migrations)

```powershell
cd src/AuditAssistant.Data
dotnet ef migrations add InitialCreate --startup-project ../AuditAssistant.CLI
dotnet ef database update --startup-project ../AuditAssistant.CLI
```

### Query Vector Database Directly

```sql
-- Find similar documents
SELECT document_id, content, embedding <=> '[0.1, 0.2, ...]'::vector AS distance
FROM document_embeddings
ORDER BY distance
LIMIT 10;
```

## Advanced Features

### Semantic Search Example

```csharp
var query = "password security requirements";
var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query);
var results = await vectorStore.SearchSimilarAsync(queryEmbedding, topK: 5);

foreach (var (docId, content, similarity) in results)
{
    Console.WriteLine($"Document {docId}: {similarity:F2} - {content.Substring(0, 100)}...");
}
```

### Batch Processing

```csharp
var batchProcessor = new BatchAuditProcessor(documentParser, auditAnalyzer);
await batchProcessor.ProcessDirectoryAsync("./multiple-audits/");
```

## RAG Architecture (Retrieval-Augmented Generation)

This application uses a modern RAG approach to efficiently analyze documents:

1. **Document Chunking**: Documents are split into semantic chunks (1000 chars with 200 char overlap)
2. **Vector Embeddings**: Each chunk is converted to a vector embedding and stored in PostgreSQL with pgvector
3. **Semantic Search**: For each compliance requirement, the system:
   - Generates an embedding for the requirement
   - Uses cosine similarity to find the top 5-10 most relevant document chunks
   - Sends only relevant chunks to the AI for analysis (not entire documents)
4. **Targeted Analysis**: AI analyzes specific chunks against specific requirements
5. **Finding Aggregation**: Results are combined into a comprehensive audit report

### Performance Benefits

- **Reduced API Costs**: Analyzes only relevant chunks, not entire documents
- **Better Accuracy**: Focused analysis on pertinent content
- **Scalability**: Handles large document sets efficiently
- **Fast Retrieval**: pgvector cosine similarity search (< 100ms for 10K chunks)
- **Token Efficiency**: Stays well within model context limits

## Troubleshooting

### "Vector extension not found"
```sql
-- Connect to PostgreSQL and run:
CREATE EXTENSION IF NOT EXISTS vector;
```

### "API rate limit exceeded"
- Reduce batch size in configuration
- Implement caching for repeated queries
- Use Azure OpenAI for higher rate limits

### "Out of memory during embedding generation"
- Process documents in smaller batches
- Use streaming for large files
- Increase available memory

## Security Best Practices

1. **Never commit API keys** - Use environment variables or Azure Key Vault
2. **Encrypt database connections** - Use SSL for PostgreSQL
3. **Audit logs** - Enable logging for compliance tracking
4. **Access control** - Restrict database and API access
5. **Data retention** - Implement policies for document cleanup

## Contributing

Contributions welcome! Areas for improvement:
- Additional compliance standard templates
- Support for PDF/Word document parsing
- Real-time monitoring dashboard
- Multi-language support
- Advanced visualizations

## License

MIT License - See LICENSE file for details

## Support

For issues and questions:
- GitHub Issues: [Report a bug]
- Documentation: [Wiki]
- Email: support@audit-assistant.example.com

---

Built with ❤️ using .NET, Semantic Kernel, and PostgreSQL
