# FMS Log Nexus Python SDK

Python client library for FMS Log Nexus centralized logging system.

## Installation

```bash
pip install fms-lognexus
```

Or install from source:

```bash
cd sdk/python
pip install -e .
```

## Quick Start

```python
from fmslognexus import FMSClient

# Connect with API key
client = FMSClient(
    base_url="https://fms-lognexus.example.com",
    api_key="your-api-key"
)

# Or connect with username/password
client = FMSClient(
    base_url="https://fms-lognexus.example.com",
    username="admin",
    password="password"
)

# Send a log
client.logs.info("Application started")

# Get dashboard
dashboard = client.get_dashboard()
print(f"Online servers: {dashboard['servers']['online']}")
```

## Features

- Full API coverage
- Automatic retry with exponential backoff
- JWT and API key authentication
- Type hints and dataclasses
- Comprehensive exception handling

## Usage Examples

### Logging

```python
from fmslognexus import FMSClient, LogLevel

client = FMSClient("https://fms-lognexus.example.com", api_key="key")

# Simple logging
client.logs.info("Processing started")
client.logs.warning("Low disk space")
client.logs.error("Connection failed", exception=e)

# With context
client.logs.info(
    "Order processed",
    job_id="ORDER-PROCESS",
    correlation_id="txn-123",
    properties={"order_id": 12345, "amount": 99.99}
)

# Search logs
results = client.logs.search(
    level=LogLevel.ERROR,
    start_date=datetime.now() - timedelta(hours=1)
)
for log in results.items:
    print(f"{log.timestamp}: {log.message}")
```

### Job Executions

```python
from fmslognexus import FMSClient, ExecutionStatus

client = FMSClient("https://fms-lognexus.example.com", api_key="key")

# Register a job
job = client.jobs.register(
    job_id="NIGHTLY-BACKUP",
    display_name="Nightly Backup Job",
    priority="High",
    timeout_minutes=60
)

# Start execution
execution = client.executions.start("NIGHTLY-BACKUP")
print(f"Started execution: {execution.id}")

try:
    # Do work...
    client.logs.info("Backup in progress", execution_id=execution.id)
    
    # Complete successfully
    client.executions.complete(
        execution.id,
        status=ExecutionStatus.SUCCESS,
        output_message="Backup completed: 1.5GB"
    )
except Exception as e:
    # Report failure
    client.executions.complete(
        execution.id,
        status=ExecutionStatus.FAILED,
        error_message=str(e)
    )
```

### Server Management

```python
from fmslognexus import FMSClient, ServerStatus

client = FMSClient("https://fms-lognexus.example.com", api_key="key")

# Send heartbeat
client.servers.heartbeat(
    status=ServerStatus.ONLINE,
    agent_version="1.0.0",
    system_info={
        "os": "Linux",
        "cpu_usage": 45.2,
        "memory_usage": 62.1
    }
)

# List servers
servers = client.servers.list()
for server in servers:
    print(f"{server.server_name}: {server.status}")

# Set maintenance mode
client.servers.set_maintenance("SERVER01", enable=True)
```

### Alerts

```python
from fmslognexus import FMSClient

client = FMSClient("https://fms-lognexus.example.com", api_key="key")

# Get active alerts
active_alerts = client.alerts.get_active_instances()
for alert in active_alerts:
    print(f"Alert: {alert.alert_name} - {alert.severity}")

# Acknowledge an alert
client.alerts.acknowledge(alert_id, notes="Investigating...")

# Resolve an alert
client.alerts.resolve(alert_id, notes="Issue resolved")
```

## Exception Handling

```python
from fmslognexus import FMSClient
from fmslognexus.exceptions import (
    AuthenticationError,
    NotFoundError,
    ValidationError,
    RateLimitError,
    FMSError
)

client = FMSClient("https://fms-lognexus.example.com", api_key="key")

try:
    log = client.logs.get("invalid-id")
except NotFoundError:
    print("Log not found")
except AuthenticationError:
    print("Invalid credentials")
except ValidationError as e:
    print(f"Validation failed: {e.errors}")
except RateLimitError as e:
    print(f"Rate limited. Retry after {e.retry_after} seconds")
except FMSError as e:
    print(f"API error: {e}")
```

## Configuration

### Environment Variables

```python
import os
from fmslognexus import FMSClient

client = FMSClient(
    base_url=os.getenv("FMS_BASE_URL"),
    api_key=os.getenv("FMS_API_KEY"),
    server_name=os.getenv("FMS_SERVER_NAME", "default-server"),
    timeout=int(os.getenv("FMS_TIMEOUT", "30")),
)
```

### Custom Server Name

```python
from fmslognexus import FMSClient

client = FMSClient(
    base_url="https://fms-lognexus.example.com",
    api_key="key",
    server_name="my-custom-server"
)
```

## API Reference

### FMSClient

Main client class with sub-clients for each API area.

| Property | Description |
|----------|-------------|
| `logs` | Log operations |
| `servers` | Server operations |
| `jobs` | Job operations |
| `executions` | Execution operations |
| `alerts` | Alert operations |

### LogClient

| Method | Description |
|--------|-------------|
| `create(message, level, ...)` | Create a log entry |
| `info(message, ...)` | Create info log |
| `warning(message, ...)` | Create warning log |
| `error(message, exception, ...)` | Create error log |
| `critical(message, ...)` | Create critical log |
| `get(log_id)` | Get log by ID |
| `search(...)` | Search logs |

### ServerClient

| Method | Description |
|--------|-------------|
| `heartbeat(status, ...)` | Send heartbeat |
| `get(server_name)` | Get server details |
| `list()` | List all servers |
| `set_maintenance(server_name, enable)` | Set maintenance mode |

### JobClient

| Method | Description |
|--------|-------------|
| `register(job_id, ...)` | Register a job |
| `get(job_id)` | Get job by ID |
| `list(server_name)` | List jobs |
| `activate(job_id)` | Activate job |
| `deactivate(job_id)` | Deactivate job |

### ExecutionClient

| Method | Description |
|--------|-------------|
| `start(job_id, ...)` | Start execution |
| `complete(execution_id, status, ...)` | Complete execution |
| `cancel(execution_id, reason)` | Cancel execution |
| `get(execution_id)` | Get execution |
| `get_running()` | Get running executions |

### AlertClient

| Method | Description |
|--------|-------------|
| `get(alert_id)` | Get alert rule |
| `list()` | List alert rules |
| `get_active_instances()` | Get active instances |
| `acknowledge(instance_id, notes)` | Acknowledge alert |
| `resolve(instance_id, notes)` | Resolve alert |

## Requirements

- Python >= 3.9
- requests >= 2.28.0
- python-dateutil >= 2.8.0

## License

MIT License
