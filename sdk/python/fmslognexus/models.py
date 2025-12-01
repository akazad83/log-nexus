"""FMS Log Nexus SDK Models."""

from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Any, Dict, List, Optional
from dateutil.parser import parse as parse_date


class LogLevel(str, Enum):
    """Log level enumeration."""
    TRACE = "Trace"
    DEBUG = "Debug"
    INFORMATION = "Information"
    WARNING = "Warning"
    ERROR = "Error"
    CRITICAL = "Critical"


class ServerStatus(str, Enum):
    """Server status enumeration."""
    UNKNOWN = "Unknown"
    ONLINE = "Online"
    OFFLINE = "Offline"
    MAINTENANCE = "Maintenance"
    ERROR = "Error"


class ExecutionStatus(str, Enum):
    """Execution status enumeration."""
    PENDING = "Pending"
    RUNNING = "Running"
    SUCCESS = "Success"
    FAILED = "Failed"
    CANCELLED = "Cancelled"
    TIMED_OUT = "TimedOut"


class JobPriority(str, Enum):
    """Job priority enumeration."""
    LOW = "Low"
    NORMAL = "Normal"
    HIGH = "High"
    CRITICAL = "Critical"


class TriggerType(str, Enum):
    """Trigger type enumeration."""
    MANUAL = "Manual"
    SCHEDULED = "Scheduled"
    TRIGGERED = "Triggered"
    RETRY = "Retry"


class AlertType(str, Enum):
    """Alert type enumeration."""
    ERROR_THRESHOLD = "ErrorThreshold"
    JOB_FAILURE = "JobFailure"
    SERVER_OFFLINE = "ServerOffline"
    EXECUTION_TIMEOUT = "ExecutionTimeout"
    CUSTOM = "Custom"


class AlertSeverity(str, Enum):
    """Alert severity enumeration."""
    INFO = "Info"
    WARNING = "Warning"
    ERROR = "Error"
    CRITICAL = "Critical"


@dataclass
class LogEntry:
    """Log entry model."""
    id: str
    timestamp: datetime
    level: LogLevel
    message: str
    server_name: Optional[str] = None
    job_id: Optional[str] = None
    execution_id: Optional[str] = None
    category: Optional[str] = None
    correlation_id: Optional[str] = None
    exception: Optional[str] = None
    properties: Optional[Dict[str, Any]] = None
    created_at: Optional[datetime] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "LogEntry":
        """Create a LogEntry from a dictionary."""
        return cls(
            id=data.get("id", ""),
            timestamp=parse_date(data["timestamp"]) if data.get("timestamp") else datetime.utcnow(),
            level=LogLevel(data.get("level", "Information")),
            message=data.get("message", ""),
            server_name=data.get("serverName"),
            job_id=data.get("jobId"),
            execution_id=data.get("executionId"),
            category=data.get("category"),
            correlation_id=data.get("correlationId"),
            exception=data.get("exception"),
            properties=data.get("properties"),
            created_at=parse_date(data["createdAt"]) if data.get("createdAt") else None,
        )


@dataclass
class Server:
    """Server model."""
    server_name: str
    display_name: Optional[str] = None
    status: ServerStatus = ServerStatus.UNKNOWN
    agent_version: Optional[str] = None
    last_heartbeat: Optional[datetime] = None
    is_active: bool = True
    ip_address: Optional[str] = None
    os_info: Optional[str] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Server":
        """Create a Server from a dictionary."""
        return cls(
            server_name=data.get("serverName", ""),
            display_name=data.get("displayName"),
            status=ServerStatus(data.get("status", "Unknown")),
            agent_version=data.get("agentVersion"),
            last_heartbeat=parse_date(data["lastHeartbeat"]) if data.get("lastHeartbeat") else None,
            is_active=data.get("isActive", True),
            ip_address=data.get("ipAddress"),
            os_info=data.get("osInfo"),
        )


@dataclass
class Job:
    """Job model."""
    job_id: str
    display_name: str
    server_name: str
    description: Optional[str] = None
    schedule: Optional[str] = None
    priority: JobPriority = JobPriority.NORMAL
    is_active: bool = True
    timeout_minutes: Optional[int] = None
    last_execution_at: Optional[datetime] = None
    last_execution_status: Optional[ExecutionStatus] = None
    tags: List[str] = field(default_factory=list)

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Job":
        """Create a Job from a dictionary."""
        return cls(
            job_id=data.get("jobId", ""),
            display_name=data.get("displayName", ""),
            server_name=data.get("serverName", ""),
            description=data.get("description"),
            schedule=data.get("schedule"),
            priority=JobPriority(data.get("priority", "Normal")),
            is_active=data.get("isActive", True),
            timeout_minutes=data.get("timeoutMinutes"),
            last_execution_at=parse_date(data["lastExecutionAt"]) if data.get("lastExecutionAt") else None,
            last_execution_status=ExecutionStatus(data["lastExecutionStatus"]) if data.get("lastExecutionStatus") else None,
            tags=data.get("tags", []),
        )


@dataclass
class Execution:
    """Execution model."""
    id: str
    job_id: str
    server_name: str
    status: ExecutionStatus = ExecutionStatus.PENDING
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None
    duration_ms: Optional[int] = None
    trigger_type: TriggerType = TriggerType.MANUAL
    triggered_by: Optional[str] = None
    error_message: Optional[str] = None
    output_message: Optional[str] = None
    parameters: Optional[Dict[str, Any]] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Execution":
        """Create an Execution from a dictionary."""
        return cls(
            id=data.get("id", ""),
            job_id=data.get("jobId", ""),
            server_name=data.get("serverName", ""),
            status=ExecutionStatus(data.get("status", "Pending")),
            started_at=parse_date(data["startedAt"]) if data.get("startedAt") else None,
            completed_at=parse_date(data["completedAt"]) if data.get("completedAt") else None,
            duration_ms=data.get("durationMs"),
            trigger_type=TriggerType(data.get("triggerType", "Manual")),
            triggered_by=data.get("triggeredBy"),
            error_message=data.get("errorMessage"),
            output_message=data.get("outputMessage"),
            parameters=data.get("parameters"),
        )


@dataclass
class Alert:
    """Alert rule model."""
    id: str
    name: str
    alert_type: AlertType
    severity: AlertSeverity
    is_active: bool = True
    description: Optional[str] = None
    condition: Optional[str] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Alert":
        """Create an Alert from a dictionary."""
        return cls(
            id=data.get("id", ""),
            name=data.get("name", ""),
            alert_type=AlertType(data.get("alertType", "Custom")),
            severity=AlertSeverity(data.get("severity", "Warning")),
            is_active=data.get("isActive", True),
            description=data.get("description"),
            condition=data.get("condition"),
        )


@dataclass
class AlertInstance:
    """Alert instance model."""
    id: str
    alert_id: str
    alert_name: str
    severity: AlertSeverity
    triggered_at: datetime
    acknowledged_at: Optional[datetime] = None
    resolved_at: Optional[datetime] = None
    acknowledged_by: Optional[str] = None
    resolved_by: Optional[str] = None
    message: Optional[str] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "AlertInstance":
        """Create an AlertInstance from a dictionary."""
        return cls(
            id=data.get("id", ""),
            alert_id=data.get("alertId", ""),
            alert_name=data.get("alertName", ""),
            severity=AlertSeverity(data.get("severity", "Warning")),
            triggered_at=parse_date(data["triggeredAt"]) if data.get("triggeredAt") else datetime.utcnow(),
            acknowledged_at=parse_date(data["acknowledgedAt"]) if data.get("acknowledgedAt") else None,
            resolved_at=parse_date(data["resolvedAt"]) if data.get("resolvedAt") else None,
            acknowledged_by=data.get("acknowledgedBy"),
            resolved_by=data.get("resolvedBy"),
            message=data.get("message"),
        )


@dataclass
class PagedResult:
    """Paged result container."""
    items: List[Any]
    page: int
    page_size: int
    total_count: int
    total_pages: int

    @classmethod
    def from_dict(cls, data: Dict[str, Any], item_class: type) -> "PagedResult":
        """Create a PagedResult from a dictionary."""
        items = [item_class.from_dict(item) for item in data.get("items", [])]
        return cls(
            items=items,
            page=data.get("page", 1),
            page_size=data.get("pageSize", 20),
            total_count=data.get("totalCount", 0),
            total_pages=data.get("totalPages", 0),
        )
