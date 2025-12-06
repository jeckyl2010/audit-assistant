# PostgreSQL with pgvector Setup using Podman Desktop on Windows 11

This guide provides step-by-step instructions for setting up PostgreSQL with the pgvector extension using Podman Desktop on Windows 11.

## Prerequisites

- **Windows 11**
- **Podman Desktop 1.14+** with **Podman 5.7.0+** ([Download here](https://podman-desktop.io/downloads))
- **.NET 10.0 SDK** (for the application)

## Why Podman Desktop?

Podman Desktop is a great Docker Desktop alternative that:
- ✅ Is completely free and open-source
- ✅ Works without a daemon (more secure)
- ✅ Is compatible with Docker commands and compose files
- ✅ Integrates well with Windows 11

## Installation Steps

### Step 1: Verify Podman Desktop Installation

1. Open **Podman Desktop** from the Start menu
2. Verify installation in PowerShell:

```powershell
podman --version
```

**Expected output:**
```
podman version 5.7.0
```

**Note:** This guide is tested with Podman 5.7.0 (December 2025). Earlier versions should also work.

If not installed, download from: https://podman-desktop.io/downloads/windows

### Step 2: Pull the PostgreSQL pgvector Image

Open **PowerShell** (or Windows Terminal) and run:

```powershell
podman pull docker.io/pgvector/pgvector:pg18
```

**What this does:**
- Downloads the PostgreSQL 18 image with pgvector extension pre-installed
- Image includes PostgreSQL 18 (released September 2024, stable in 2025) with vector similarity search support
- Uses the official pgvector/pgvector image (ankane/pgvector is archived)

**⚠️ Important:** PostgreSQL 18+ changed data directory structure to `/var/lib/postgresql` (instead of `/var/lib/postgresql/data`) for better upgrade support.

**Expected output:**
```
Trying to pull docker.io/ankane/pgvector:latest...
Getting image source signatures
Copying blob sha256:...
...
Writing manifest to image destination
```

### Step 3: Create and Start the PostgreSQL Container

#### Option A: Using Podman Command (Quick Start)

```powershell
podman run -d `
  --name audit-assistant-db `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=audit123 `
  -e POSTGRES_DB=audit_assistant `
  -p 5432:5432 `
  -v audit-db-data:/var/lib/postgresql `
  docker.io/pgvector/pgvector:pg18
```

**Command breakdown:**
- `-d` - Run in detached mode (background)
- `--name audit-assistant-db` - Container name
- `-e POSTGRES_USER=postgres` - Database username
- `-e POSTGRES_PASSWORD=audit123` - Database password (⚠️ change in production!)
- `-e POSTGRES_DB=audit_assistant` - Database name
- `-p 5432:5432` - Port mapping (host:container)
- `-v audit-db-data:/var/lib/postgresql` - Persistent storage volume (PostgreSQL 18+ format)

**Expected output:**
```
a1b2c3d4e5f6... (container ID)
```

#### Option B: Using Podman Compose (Recommended)

The repository includes a `podman-compose.yml` file. Create it if needed:

**Create `podman-compose.yml`:**
```yaml
version: '3.8'

services:
  postgres:
    image: docker.io/pgvector/pgvector:pg18
    container_name: audit-assistant-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: audit123
      POSTGRES_DB: audit_assistant
    ports:
      - "5432:5432"
    volumes:
      - audit-db-data:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  audit-db-data:
```

**Start with Podman Compose:**
```powershell
# Install podman-compose if not already installed
pip install podman-compose

# Start the container
podman-compose up -d
```

### Step 4: Verify Container is Running

```powershell
podman ps
```

**Expected output:**
```
CONTAINER ID  IMAGE                                   COMMAND     CREATED        STATUS        PORTS                   NAMES
a1b2c3d4e5f6  docker.io/pgvector/pgvector:pg18        postgres    2 minutes ago  Up 2 minutes  0.0.0.0:5432->5432/tcp  audit-assistant-db
```

### Step 5: Verify pgvector Extension

Connect to the database and verify the extension:

```powershell
podman exec -it audit-assistant-db psql -U postgres -d audit_assistant
```

Inside the PostgreSQL prompt, run:

```sql
-- Check if vector extension is available
SELECT * FROM pg_available_extensions WHERE name = 'vector';

-- Enable the vector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify extension is enabled
\dx vector

-- Test vector functionality
SELECT '[1,2,3]'::vector;

-- Exit
\q
```

**Expected output:**
```
      name      | default_version | installed_version | comment
----------------+-----------------+-------------------+----------
 vector         | 0.x.x          | 0.x.x            | vector data type and ivfflat and hnsw access methods

       List of installed extensions
  Name  | Version |   Schema   |         Description
--------+---------+------------+------------------------------
 vector | 0.x.x   | public     | vector data type and ivfflat...

  vector
----------
 [1,2,3]
```

## Managing the Container

### Start the Container
```powershell
podman start audit-assistant-db
```

### Stop the Container
```powershell
podman stop audit-assistant-db
```

### Restart the Container
```powershell
podman restart audit-assistant-db
```

### View Container Logs
```powershell
podman logs audit-assistant-db

# Follow logs in real-time
podman logs -f audit-assistant-db
```

### Remove Container (⚠️ Data will be lost if volume not used)
```powershell
# Stop first
podman stop audit-assistant-db

# Remove container
podman rm audit-assistant-db

# Remove volume (permanently deletes data)
podman volume rm audit-db-data
```

## Connecting from the Application

Update your `src/AuditAssistant.CLI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=audit123"
  }
}
```

**Or use environment variables (recommended):**

```powershell
$env:ConnectionStrings__PostgreSQL = "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=audit123"
```

## Testing the Connection

Test the database connection from PowerShell:

```powershell
# Using Podman
podman exec -it audit-assistant-db psql -U postgres -d audit_assistant -c "SELECT version();"
```

**Expected output:**
```
                                                    version
----------------------------------------------------------------------------------------------------------------
 PostgreSQL 18.x on x86_64-pc-linux-gnu, compiled by gcc (Debian 12.x.x-x) 12.x.x, 64-bit
```

## Troubleshooting

### Issue: "Error: unable to start container"

**Solution:**
```powershell
# Check if port 5432 is already in use
netstat -ano | findstr :5432

# If another service is using the port, either stop it or change the port mapping:
podman run -d --name audit-assistant-db -p 5433:5432 ...

# Then update connection string to use port 5433
```

### Issue: "connection refused"

**Solution:**
```powershell
# Check if container is running
podman ps -a

# Check container logs
podman logs audit-assistant-db

# Restart container
podman restart audit-assistant-db
```

### Issue: "password authentication failed"

**Solution:**
```powershell
# Verify environment variables in running container
podman exec audit-assistant-db env | findstr POSTGRES

# Recreate container with correct password
podman stop audit-assistant-db
podman rm audit-assistant-db
podman run -d --name audit-assistant-db -e POSTGRES_PASSWORD=yournewpassword ...
```

### Issue: "vector extension not found"

**Solution:**
```powershell
# Verify you're using the correct image
podman images | findstr pgvector

# If using wrong image, remove and pull correct one
podman stop audit-assistant-db
podman rm audit-assistant-db
podman pull docker.io/pgvector/pgvector:pg18
# Then recreate container
```

### Issue: Podman Desktop not starting container

**Solution:**
1. Open **Podman Desktop** GUI
2. Go to **Containers** tab
3. Find `audit-assistant-db` container
4. Click the **▶ Start** button
5. Check logs in the **Logs** tab if it fails

## Using Podman Desktop GUI

### Visual Management

1. **Open Podman Desktop**
2. Navigate to **Containers** in the left sidebar
3. You'll see `audit-assistant-db` listed

**Available Actions:**
- ▶️ **Start** - Start the container
- ⏸️ **Stop** - Stop the container
- 🔄 **Restart** - Restart the container
- 📋 **Logs** - View container logs
- 🖥️ **Terminal** - Open interactive shell
- 🗑️ **Delete** - Remove container

### Connecting via Terminal in Podman Desktop

1. Click on `audit-assistant-db` container
2. Click **Terminal** tab
3. Run PostgreSQL commands directly:
   ```bash
   psql -U postgres -d audit_assistant
   ```

## Data Persistence

### Understanding Volumes

The volume `audit-db-data` ensures your data persists even if the container is removed.

**List volumes:**
```powershell
podman volume ls
```

**Inspect volume:**
```powershell
podman volume inspect audit-db-data
```

**Backup data:**
```powershell
# Backup database to file
podman exec audit-assistant-db pg_dump -U postgres audit_assistant > backup.sql

# Restore from backup
Get-Content backup.sql | podman exec -i audit-assistant-db psql -U postgres audit_assistant
```

## Performance Tuning

For better performance with large datasets:

```powershell
podman run -d `
  --name audit-assistant-db `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=audit123 `
  -e POSTGRES_DB=audit_assistant `
  -p 5432:5432 `
  -v audit-db-data:/var/lib/postgresql `
  --memory=4g `
  --cpus=2 `
  docker.io/pgvector/pgvector:pg18 `
  -c shared_buffers=1GB `
  -c effective_cache_size=3GB `
  -c maintenance_work_mem=512MB `
  -c max_connections=100
```

## Security Best Practices

### 1. Change Default Password

```powershell
# Use strong password
podman run -d `
  --name audit-assistant-db `
  -e POSTGRES_PASSWORD=YourStr0ngP@ssw0rd! `
  ...
```

### 2. Use Environment Variables

Never hardcode passwords in config files:

```powershell
# Set environment variable
$env:DB_PASSWORD = "YourStr0ngP@ssw0rd!"

# Use in connection string
$env:ConnectionStrings__PostgreSQL = "Host=localhost;Port=5432;Database=audit_assistant;Username=postgres;Password=$env:DB_PASSWORD"
```

### 3. Restrict Network Access

```powershell
# Only allow localhost connections
podman run -d `
  --name audit-assistant-db `
  -p 127.0.0.1:5432:5432 `
  ...
```

### 4. Enable SSL/TLS (Production)

For production environments, configure SSL certificates.

## Podman vs Docker: Key Differences

| Aspect | Podman | Docker Desktop |
|--------|--------|----------------|
| **Cost** | Free, Open Source | Requires license for enterprise |
| **Daemon** | Daemonless (more secure) | Requires daemon |
| **Root Access** | Rootless by default | Requires admin |
| **Commands** | Same as Docker | Docker commands |
| **Compose** | Supports docker-compose | Native support |

**Good News:** All Docker commands work with Podman! Just replace `docker` with `podman`:
- `docker run` → `podman run`
- `docker ps` → `podman ps`
- `docker-compose up` → `podman-compose up`

## Quick Reference

### Common Commands

```powershell
# Start database
podman start audit-assistant-db

# Stop database
podman stop audit-assistant-db

# View logs
podman logs -f audit-assistant-db

# Connect to database
podman exec -it audit-assistant-db psql -U postgres -d audit_assistant

# Backup database
podman exec audit-assistant-db pg_dump -U postgres audit_assistant > backup_$(Get-Date -Format 'yyyyMMdd').sql

# Container stats
podman stats audit-assistant-db

# Remove everything
podman stop audit-assistant-db
podman rm audit-assistant-db
podman volume rm audit-db-data
```

## Next Steps

After setting up PostgreSQL:

1. ✅ Verify connection: `podman exec -it audit-assistant-db psql -U postgres -d audit_assistant`
2. ✅ Configure application: Update `appsettings.json` with connection string
3. ✅ Run application: `dotnet run --project src\AuditAssistant.CLI\AuditAssistant.CLI.csproj`
4. ✅ Test with examples: Use `examples\audit-docs` and `examples\standards`

## Support

### Podman Desktop Resources
- 📖 Documentation: https://podman-desktop.io/docs/intro
- 💬 Community: https://github.com/containers/podman-desktop/discussions
- 🐛 Issues: https://github.com/containers/podman-desktop/issues

### PostgreSQL Resources
- 📖 pgvector: https://github.com/pgvector/pgvector
- 📖 PostgreSQL: https://www.postgresql.org/docs/

### Project Resources
- 📖 Quick Start: [QUICKSTART.md](QUICKSTART.md)
- 📖 Full Documentation: [README.md](README.md)

---

**You're all set! Your PostgreSQL database with pgvector is ready for AI-powered audit analysis! 🚀**
