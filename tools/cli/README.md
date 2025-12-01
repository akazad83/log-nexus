# FMS Log Nexus CLI

Command-line administration tool for FMS Log Nexus.

## Installation

```bash
pip install fms-lognexus-cli

# Or install from source
cd tools/cli
pip install -e .
```

## Quick Start

### Configure Connection

```bash
# Interactive configuration
fms-cli configure

# Or use environment variables
export FMS_BASE_URL=https://fms-lognexus.example.com
export FMS_API_KEY=your-api-key
```

### Login

```bash
fms-cli login -u admin
# Enter password when prompted
```

### Check Status

```bash
fms-cli status
fms-cli dashboard
```

## Commands

### Global Options

| Option | Description |
|--------|-------------|
| `--url, -u` | Server URL |
| `--api-key, -k` | API key |
| `--output, -o` | Output format (table/json) |
| `--version` | Show version |
| `--help` | Show help |

### Configuration

```bash
# Configure CLI
fms-cli configure

# Check API status
fms-cli status

# Show dashboard
fms-cli dashboard
fms-cli dashboard -o json
```

### Authentication

```bash
# Login with username/password
fms-cli login -u admin

# Logout
fms-cli logout
```

### Logs

```bash
# List recent logs
fms-cli logs list
fms-cli logs list --level Error
fms-cli logs list --server SERVER01 --since 1h
fms-cli logs list --limit 50

# Send a log
fms-cli logs send -m "Test message" -l Warning
fms-cli logs send -m "Job completed" -j JOB-001 -l Information

# Show statistics
fms-cli logs stats
```

### Servers

```bash
# List servers
fms-cli servers list
fms-cli servers list --online-only

# Send heartbeat
fms-cli servers heartbeat
fms-cli servers heartbeat -s SERVER01 --status Maintenance
```

### Jobs

```bash
# List jobs
fms-cli jobs list
fms-cli jobs list --server SERVER01
fms-cli jobs list --active-only

# Register a job
fms-cli jobs register --id JOB-001 --name "Daily Backup"
fms-cli jobs register --id JOB-002 --name "Cleanup" --priority High --timeout 60

# Activate/deactivate
fms-cli jobs activate JOB-001
fms-cli jobs deactivate JOB-001
```

### Executions

```bash
# List executions
fms-cli executions list
fms-cli executions list --job JOB-001
fms-cli executions list --running

# Start execution
fms-cli executions start JOB-001

# Complete execution
fms-cli executions complete <execution-id> --status Success
fms-cli executions complete <execution-id> --status Failed -m "Connection timeout"

# Cancel execution
fms-cli executions cancel <execution-id> -r "User requested"
```

### Alerts

```bash
# List alerts
fms-cli alerts list
fms-cli alerts list --active

# Acknowledge alert
fms-cli alerts acknowledge <instance-id>
fms-cli alerts acknowledge <instance-id> -n "Looking into it"

# Resolve alert
fms-cli alerts resolve <instance-id> -n "Issue fixed"
```

## Output Formats

### Table (Default)

```
$ fms-cli servers list
Server Name              Status       Last Heartbeat       Agent Version  
---------------------------------------------------------------------------
SERVER01                 Online       2024-01-15 10:30:00  1.0.0          
SERVER02                 Offline      2024-01-15 09:15:00  1.0.0          
```

### JSON

```
$ fms-cli servers list -o json
[
  {
    "server_name": "SERVER01",
    "status": "Online",
    "last_heartbeat": "2024-01-15T10:30:00",
    "agent_version": "1.0.0"
  }
]
```

## Configuration File

Configuration is stored in `~/.fms-lognexus/config.json`:

```json
{
  "base_url": "https://fms-lognexus.example.com",
  "api_key": "optional-api-key"
}
```

Tokens are stored in `~/.fms-lognexus/token.json` (secured with 600 permissions).

## Environment Variables

| Variable | Description |
|----------|-------------|
| `FMS_BASE_URL` | Server URL |
| `FMS_API_KEY` | API key |

## Scripting Examples

### Batch Log Submission

```bash
#!/bin/bash
while read line; do
    fms-cli logs send -m "$line" -l Information
done < logfile.txt
```

### Health Check Script

```bash
#!/bin/bash
if fms-cli status > /dev/null 2>&1; then
    echo "API is healthy"
    exit 0
else
    echo "API is down"
    exit 1
fi
```

### Job Execution Wrapper

```bash
#!/bin/bash
JOB_ID="BACKUP-JOB"

# Start execution
EXEC_ID=$(fms-cli executions start $JOB_ID -o json | jq -r '.id')

# Run actual job
if /opt/scripts/backup.sh; then
    fms-cli executions complete $EXEC_ID --status Success -m "Backup completed"
else
    fms-cli executions complete $EXEC_ID --status Failed -m "Backup failed"
fi
```

### Export Logs to File

```bash
fms-cli logs list --level Error --since 24h -o json > errors.json
```

## Troubleshooting

### Connection Issues

```bash
# Check connectivity
fms-cli status

# With verbose URL
fms-cli --url http://localhost:5000 status
```

### Authentication Issues

```bash
# Clear stored credentials
fms-cli logout

# Re-login
fms-cli login -u admin
```

### Debug Mode

```bash
# Set environment variable for debugging
export FMS_DEBUG=1
fms-cli logs list
```

## License

MIT License
