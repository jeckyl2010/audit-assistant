# Podman Desktop Quick Reference - PostgreSQL with pgvector

## 🚀 Quick Start (Copy & Paste)

### Install and Run (One Command)
```powershell
podman run -d --name audit-assistant-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=audit123 -e POSTGRES_DB=audit_assistant -p 5432:5432 -v audit-db-data:/var/lib/postgresql docker.io/pgvector/pgvector:pg18
```

**Note:** PostgreSQL 18+ uses `/var/lib/postgresql` instead of `/var/lib/postgresql/data`

### Or Use Compose
```powershell
# Install podman-compose (one time)
pip install podman-compose

# Start database
podman-compose up -d

# Stop database
podman-compose down
```

## 📋 Essential Commands

| Task | Command |
|------|---------|
| **Start** | `podman start audit-assistant-db` |
| **Stop** | `podman stop audit-assistant-db` |
| **Restart** | `podman restart audit-assistant-db` |
| **Status** | `podman ps` |
| **Logs** | `podman logs -f audit-assistant-db` |
| **Connect** | `podman exec -it audit-assistant-db psql -U postgres -d audit_assistant` |
| **Remove** | `podman stop audit-assistant-db && podman rm audit-assistant-db` |

## 🔧 Daily Operations

### Start Your Work Session
```powershell
# Start database
podman start audit-assistant-db

# Verify running
podman ps

# Run your app
dotnet run --project src\AuditAssistant.CLI\AuditAssistant.CLI.csproj
```

### End Your Work Session
```powershell
# Stop database (optional - keeps data)
podman stop audit-assistant-db
```

### Check Database Status
```powershell
# Quick health check
podman exec audit-assistant-db pg_isready -U postgres

# Detailed status
podman exec audit-assistant-db psql -U postgres -c "SELECT version();"
```

## 🔍 Troubleshooting (1-Liners)

| Problem | Solution |
|---------|----------|
| Not running | `podman start audit-assistant-db` |
| Can't connect | `podman restart audit-assistant-db` |
| Port in use | `podman run ... -p 5433:5432 ...` (change port) |
| Check logs | `podman logs audit-assistant-db` |
| Reset container | `podman stop audit-assistant-db && podman rm audit-assistant-db` then recreate |

## 💾 Backup & Restore

### Quick Backup
```powershell
podman exec audit-assistant-db pg_dump -U postgres audit_assistant > backup_$(Get-Date -Format 'yyyyMMdd').sql
```

### Quick Restore
```powershell
Get-Content backup_20251205.sql | podman exec -i audit-assistant-db psql -U postgres audit_assistant
```

## 🎯 Connection String

**For appsettings.json:**
```json
"ConnectionStrings": {
  "PostgreSQL": "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=audit123"
}
```

**For environment variable:**
```powershell
$env:ConnectionStrings__PostgreSQL = "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=audit123"
```

## 🔐 Security Tip

**Change default password:**
```powershell
podman run -d --name audit-assistant-db -e POSTGRES_PASSWORD=YourStrongP@ssw0rd! ...
```

## 🆚 Podman vs Docker

**Good news:** All commands are the same! Just replace `docker` with `podman`:
- ✅ `docker ps` → `podman ps`
- ✅ `docker run` → `podman run`
- ✅ `docker exec` → `podman exec`

## 📱 Podman Desktop GUI

1. Open **Podman Desktop** app
2. Go to **Containers** tab
3. Find **audit-assistant-db**
4. Use buttons: ▶️ Start | ⏸️ Stop | 📋 Logs | 🖥️ Terminal

## ⚡ Performance Settings

For large datasets, use more resources:
```powershell
podman run -d --name audit-assistant-db --memory=4g --cpus=2 -e POSTGRES_PASSWORD=audit123 -p 5432:5432 -v audit-db-data:/var/lib/postgresql docker.io/pgvector/pgvector:pg18 -c shared_buffers=1GB
```

## 🔗 Full Documentation

📖 Complete guide: [PODMAN-SETUP.md](PODMAN-SETUP.md)

---

**Keep this file bookmarked for quick reference! 📌**
