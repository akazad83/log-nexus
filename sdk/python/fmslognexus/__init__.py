"""
FMS Log Nexus Python SDK

A Python client library for interacting with FMS Log Nexus centralized logging system.

Example:
    from fmslognexus import FMSClient
    
    client = FMSClient("https://fms-lognexus.example.com", api_key="your-key")
    
    # Send a log
    client.logs.info("Application started")
    
    # Start an execution
    execution = client.executions.start("JOB-001")
    client.logs.info("Processing...", execution_id=execution.id)
    client.executions.complete(execution.id, status="Success")
"""

__version__ = "1.0.0"
__author__ = "FMS Log Nexus Team"

from .client import FMSClient
from .models import (
    LogLevel,
    ServerStatus,
    ExecutionStatus,
    JobPriority,
    TriggerType,
    LogEntry,
    Server,
    Job,
    Execution,
    Alert,
    AlertInstance,
)
from .exceptions import (
    FMSError,
    AuthenticationError,
    NotFoundError,
    ValidationError,
    RateLimitError,
)

__all__ = [
    "FMSClient",
    # Enums
    "LogLevel",
    "ServerStatus",
    "ExecutionStatus",
    "JobPriority",
    "TriggerType",
    # Models
    "LogEntry",
    "Server",
    "Job",
    "Execution",
    "Alert",
    "AlertInstance",
    # Exceptions
    "FMSError",
    "AuthenticationError",
    "NotFoundError",
    "ValidationError",
    "RateLimitError",
]
