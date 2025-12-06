# Quick Start Guide - AI Audit Assistant

This guide will help you get the AI Audit Assistant up and running in minutes.

## Prerequisites

1. **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **PostgreSQL 18.1+** with **pgvector** extension
3. **OpenAI API key** OR **Azure OpenAI** credentials
4. **Podman 5.7.0+** OR **Docker Desktop** (for container setup)

## Step 1: Setup PostgreSQL with pgvector

### Option A: Using Podman Desktop (Windows 11 Users)

See **[PODMAN-SETUP.md](PODMAN-SETUP.md)** for detailed Podman Desktop instructions.

**Quick start:**
```powershell
# Pull and run PostgreSQL with pgvector
podman run -d `
  --name audit-assistant-db `
  -e POSTGRES_PASSWORD=audit123 `
  -e POSTGRES_DB=audit_assistant `
  -p 5432:5432 `
  -v audit-db-data:/var/lib/postgresql `
  docker.io/pgvector/pgvector:pg18

# Verify it's running
podman ps
```

**Or use podman-compose:**
```powershell
pip install podman-compose
podman-compose up -d
```

### Option B: Using Docker Desktop

```powershell
# Pull and run PostgreSQL with pgvector
docker run -d `
  --name audit-assistant-db `
  -e POSTGRES_PASSWORD=audit123 `
  -e POSTGRES_DB=audit_assistant `
  -p 5432:5432 `
  -v audit-db-data:/var/lib/postgresql `
  pgvector/pgvector:pg18

# Verify it's running
docker ps
```

### Option C: Local PostgreSQL Installation

1. Install PostgreSQL from https://www.postgresql.org/download/
2. Install pgvector extension:
   ```sql
   CREATE EXTENSION vector;
   ```

## Step 2: Configure the Application

1. Navigate to the CLI project:
   ```powershell
   cd src\AuditAssistant.CLI
   ```

2. Edit `appsettings.json`:

   **For OpenAI:**
   ```json
   {
     "ConnectionStrings": {
       "PostgreSQL": "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=audit123"
     },
     "AI": {
       "Provider": "OpenAI",
       "OpenAI": {
         "ApiKey": "sk-your-actual-openai-api-key-here",
         "Model": "gpt-4o",
         "EmbeddingModel": "text-embedding-3-small"
       }
     }
   }
   ```

   **For Azure OpenAI:**
   ```json
   {
     "ConnectionStrings": {
       "PostgreSQL": "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=audit123"
     },
     "AI": {
       "Provider": "AzureOpenAI",
       "AzureOpenAI": {
         "Endpoint": "https://your-resource.openai.azure.com/",
         "ApiKey": "your-azure-key",
         "DeploymentName": "gpt-4",
         "EmbeddingDeploymentName": "text-embedding-ada-002"
       }
     }
   }
   ```

   **⚠️ Security Note:** For production, use environment variables instead:
   ```powershell
   $env:AI__OpenAI__ApiKey = "sk-your-key"
   $env:ConnectionStrings__PostgreSQL = "Host=localhost;..."
   ```

## Step 3: Run the Application

```powershell
# From the repository root
dotnet run --project src\AuditAssistant.CLI\AuditAssistant.CLI.csproj
```

When prompted:
1. **Audit documents directory**: Enter `examples\audit-docs`
2. **Compliance standards directory**: Enter `examples\standards`

## Step 4: Review the Output

The application will:
1. ✅ Parse markdown documents
2. ✅ Generate embeddings and store in PostgreSQL
3. ✅ Analyze documents against compliance standards
4. ✅ Generate a professional audit report

**Output:** `AuditReport_YYYYMMDD_HHMMSS.md`

## Example Output

```markdown
# Audit Report - 2025-12-05

**Generated:** 2025-12-05 17:00:00 UTC
**Auditor:** AI Audit Assistant

## Executive Summary
The audit identified 8 findings across 2 compliance standards...

## Compliance Scores
- **ISO 27001:2013:** 85.7%
- **SOC 2 Trust Services Criteria:** 90.2%

## Findings

### Critical Severity

#### Missing Multi-Factor Authentication
**Description:** The system lacks MFA for privileged accounts...
**Affected Requirements:** A.9.2.3, CC6.1
**Recommendation:** Implement MFA for all administrative accounts...
```

## Troubleshooting

### "Connection refused" - PostgreSQL

**Solution:** Ensure PostgreSQL is running:
```powershell
# If using Podman
podman ps
podman start audit-assistant-db

# If using Docker
docker ps
docker start audit-assistant-db
```

### "Extension vector not found"

**Solution:** Create the extension:
```powershell
# If using Podman
podman exec -it audit-assistant-db psql -U postgres -d audit_assistant -c "CREATE EXTENSION IF NOT EXISTS vector;"

# If using Docker
docker exec -it audit-assistant-db psql -U postgres -d audit_assistant -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

### "Invalid API key"

**Solution:** Verify your API key in `appsettings.json` or environment variables:
```powershell
# Test OpenAI connection
$headers = @{ "Authorization" = "Bearer $env:AI__OpenAI__ApiKey" }
Invoke-WebRequest -Uri "https://api.openai.com/v1/models" -Headers $headers
```

### Build errors

**Solution:** Restore packages and rebuild:
```powershell
dotnet clean
dotnet restore
dotnet build
```

## Next Steps

### Analyze Your Own Documents

1. Create your audit documents folder:
   ```powershell
   mkdir my-audit\documents
   mkdir my-audit\standards
   ```

2. Add markdown files:
   - Place your audit documentation in `my-audit\documents\`
   - Place compliance standards/SOPs in `my-audit\standards\`

3. Run the analyzer:
   ```powershell
   dotnet run --project src\AuditAssistant.CLI\AuditAssistant.CLI.csproj
   ```

### Customize Analysis

Edit `src\AuditAssistant.AI\Services\AuditAnalyzerService.cs` to:
- Adjust AI prompts for specific audit types
- Modify finding severity thresholds
- Add custom compliance frameworks

### Use Vector Search

Query similar documents:
```powershell
# Connect to PostgreSQL (Podman)
podman exec -it audit-assistant-db psql -U postgres -d audit_assistant

# Connect to PostgreSQL (Docker)
docker exec -it audit-assistant-db psql -U postgres -d audit_assistant

# Find similar content
SELECT document_id, content, embedding <=> '[0.1, 0.2, ...]'::vector AS distance
FROM document_embeddings
ORDER BY distance
LIMIT 5;
```

## Performance Tips

1. **Batch Processing**: Process large document sets overnight
2. **Token Limits**: Split large documents into chunks
3. **Caching**: Embeddings are cached in PostgreSQL
4. **Model Selection**: 
   - Use `gpt-4o` for best analysis quality
   - Use `gpt-3.5-turbo` for faster, cheaper processing

## API Rate Limits

**OpenAI:**
- Free tier: 3 requests/minute
- Tier 1: 500 requests/minute
- Consider Azure OpenAI for higher limits

**Azure OpenAI:**
- Configurable quotas
- Better for enterprise use

## Cost Estimation

**Example audit (10 documents, 2 standards):**
- Embeddings: ~50,000 tokens × $0.0001 = **$0.005**
- Analysis: ~30,000 tokens × $0.03 = **$0.90**
- Report: ~5,000 tokens × $0.03 = **$0.15**
- **Total: ~$1.05 per audit**

## Support

- 📖 Full documentation: [README.md](README.md)
- 🐛 Issues: Create a GitHub issue
- 💬 Questions: Use GitHub Discussions

---

**Ready to audit? Run the application and see AI-powered audit analysis in action! 🚀**
