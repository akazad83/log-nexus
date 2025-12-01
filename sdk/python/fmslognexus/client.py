"""FMS Log Nexus Python SDK Client."""

import os
import socket
import threading
import time
from datetime import datetime
from typing import Any, Dict, List, Optional, Union
from collections import deque

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

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
    PagedResult,
)
from .exceptions import (
    FMSError,
    AuthenticationError,
    AuthorizationError,
    NotFoundError,
    ValidationError,
    ConflictError,
    RateLimitError,
    ServerError,
    ConnectionError,
    TimeoutError,
)


class FMSClient:
    """Main client for FMS Log Nexus API."""

    def __init__(
        self,
        base_url: str,
        api_key: Optional[str] = None,
        username: Optional[str] = None,
        password: Optional[str] = None,
        server_name: Optional[str] = None,
        timeout: int = 30,
        retry_attempts: int = 3,
        retry_delay: float = 1.0,
    ):
        """
        Initialize the FMS Log Nexus client.

        Args:
            base_url: Base URL of the FMS Log Nexus API.
            api_key: API key for authentication.
            username: Username for JWT authentication.
            password: Password for JWT authentication.
            server_name: Server name to use for this client.
            timeout: Request timeout in seconds.
            retry_attempts: Number of retry attempts.
            retry_delay: Delay between retries in seconds.
        """
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
        self.server_name = server_name or socket.gethostname()
        self.timeout = timeout
        self.retry_attempts = retry_attempts
        self.retry_delay = retry_delay

        self._access_token: Optional[str] = None
        self._refresh_token: Optional[str] = None
        self._token_expires_at: Optional[datetime] = None

        # Configure session with retry
        self._session = requests.Session()
        retry_strategy = Retry(
            total=retry_attempts,
            backoff_factor=retry_delay,
            status_forcelist=[500, 502, 503, 504],
        )
        adapter = HTTPAdapter(max_retries=retry_strategy)
        self._session.mount("http://", adapter)
        self._session.mount("https://", adapter)

        # Authenticate if credentials provided
        if username and password:
            self.login(username, password)

        # Initialize sub-clients
        self.logs = LogClient(self)
        self.servers = ServerClient(self)
        self.jobs = JobClient(self)
        self.executions = ExecutionClient(self)
        self.alerts = AlertClient(self)

    def _get_headers(self) -> Dict[str, str]:
        """Get headers for API requests."""
        headers = {
            "Accept": "application/json",
            "Content-Type": "application/json",
        }

        if self.api_key:
            headers["X-Api-Key"] = self.api_key
        elif self._access_token:
            headers["Authorization"] = f"Bearer {self._access_token}"

        return headers

    def _handle_response(self, response: requests.Response) -> Any:
        """Handle API response and raise appropriate exceptions."""
        try:
            data = response.json() if response.content else None
        except ValueError:
            data = None

        if response.status_code == 200 or response.status_code == 201:
            return data
        elif response.status_code == 204:
            return None
        elif response.status_code == 400:
            errors = data.get("errors") if data else None
            raise ValidationError(
                data.get("detail", "Validation failed") if data else "Validation failed",
                response.status_code,
                data,
                errors,
            )
        elif response.status_code == 401:
            raise AuthenticationError(
                data.get("detail", "Authentication failed") if data else "Authentication failed",
                response.status_code,
                data,
            )
        elif response.status_code == 403:
            raise AuthorizationError(
                data.get("detail", "Access denied") if data else "Access denied",
                response.status_code,
                data,
            )
        elif response.status_code == 404:
            raise NotFoundError(
                data.get("detail", "Resource not found") if data else "Resource not found",
                response.status_code,
                data,
            )
        elif response.status_code == 409:
            raise ConflictError(
                data.get("detail", "Resource conflict") if data else "Resource conflict",
                response.status_code,
                data,
            )
        elif response.status_code == 429:
            retry_after = response.headers.get("Retry-After")
            raise RateLimitError(
                data.get("detail", "Rate limit exceeded") if data else "Rate limit exceeded",
                response.status_code,
                data,
                int(retry_after) if retry_after else None,
            )
        elif response.status_code >= 500:
            raise ServerError(
                data.get("detail", "Server error") if data else "Server error",
                response.status_code,
                data,
            )
        else:
            raise FMSError(
                data.get("detail", f"HTTP {response.status_code}") if data else f"HTTP {response.status_code}",
                response.status_code,
                data,
            )

    def _request(
        self,
        method: str,
        endpoint: str,
        params: Optional[Dict[str, Any]] = None,
        data: Optional[Dict[str, Any]] = None,
    ) -> Any:
        """Make an API request."""
        url = f"{self.base_url}/api/{endpoint}"
        headers = self._get_headers()

        # Filter out None values from params
        if params:
            params = {k: v for k, v in params.items() if v is not None}

        try:
            response = self._session.request(
                method=method,
                url=url,
                headers=headers,
                params=params,
                json=data,
                timeout=self.timeout,
            )
            return self._handle_response(response)
        except requests.exceptions.ConnectionError as e:
            raise ConnectionError(f"Failed to connect to {self.base_url}: {e}")
        except requests.exceptions.Timeout:
            raise TimeoutError(f"Request to {url} timed out")
        except requests.exceptions.RequestException as e:
            raise FMSError(f"Request failed: {e}")

    def login(self, username: str, password: str) -> Dict[str, Any]:
        """Authenticate with username and password."""
        response = self._request(
            "POST",
            "auth/login",
            data={"username": username, "password": password},
        )
        self._access_token = response["accessToken"]
        self._refresh_token = response["refreshToken"]
        self._token_expires_at = datetime.fromisoformat(
            response["expiresAt"].replace("Z", "+00:00")
        )
        return response

    def logout(self) -> None:
        """Logout and invalidate tokens."""
        if self._refresh_token:
            try:
                self._request(
                    "POST",
                    "auth/logout",
                    data={"refreshToken": self._refresh_token},
                )
            except FMSError:
                pass
        self._access_token = None
        self._refresh_token = None
        self._token_expires_at = None

    def get_dashboard(self) -> Dict[str, Any]:
        """Get dashboard data."""
        return self._request("GET", "dashboard")


class LogClient:
    """Client for log operations."""

    def __init__(self, client: FMSClient):
        self._client = client
        self._buffer: deque = deque()
        self._buffer_lock = threading.Lock()

    def create(
        self,
        message: str,
        level: Union[LogLevel, str] = LogLevel.INFORMATION,
        job_id: Optional[str] = None,
        execution_id: Optional[str] = None,
        category: Optional[str] = None,
        correlation_id: Optional[str] = None,
        exception: Optional[str] = None,
        properties: Optional[Dict[str, Any]] = None,
    ) -> LogEntry:
        """Create a log entry."""
        if isinstance(level, str):
            level = LogLevel(level)

        data = {
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "level": level.value,
            "message": message,
            "serverName": self._client.server_name,
            "jobId": job_id,
            "executionId": execution_id,
            "category": category,
            "correlationId": correlation_id,
            "exception": exception,
            "properties": properties,
        }

        response = self._client._request("POST", "logs", data=data)
        return LogEntry.from_dict(response)

    def trace(self, message: str, **kwargs) -> LogEntry:
        """Create a trace log."""
        return self.create(message, LogLevel.TRACE, **kwargs)

    def debug(self, message: str, **kwargs) -> LogEntry:
        """Create a debug log."""
        return self.create(message, LogLevel.DEBUG, **kwargs)

    def info(self, message: str, **kwargs) -> LogEntry:
        """Create an info log."""
        return self.create(message, LogLevel.INFORMATION, **kwargs)

    def warning(self, message: str, **kwargs) -> LogEntry:
        """Create a warning log."""
        return self.create(message, LogLevel.WARNING, **kwargs)

    def error(self, message: str, exception: Optional[Exception] = None, **kwargs) -> LogEntry:
        """Create an error log."""
        if exception:
            kwargs["exception"] = str(exception)
        return self.create(message, LogLevel.ERROR, **kwargs)

    def critical(self, message: str, exception: Optional[Exception] = None, **kwargs) -> LogEntry:
        """Create a critical log."""
        if exception:
            kwargs["exception"] = str(exception)
        return self.create(message, LogLevel.CRITICAL, **kwargs)

    def get(self, log_id: str) -> LogEntry:
        """Get a log entry by ID."""
        response = self._client._request("GET", f"logs/{log_id}")
        return LogEntry.from_dict(response)

    def search(
        self,
        server_name: Optional[str] = None,
        job_id: Optional[str] = None,
        level: Optional[Union[LogLevel, str]] = None,
        start_date: Optional[datetime] = None,
        end_date: Optional[datetime] = None,
        page: int = 1,
        page_size: int = 50,
    ) -> PagedResult:
        """Search logs."""
        params = {
            "serverName": server_name,
            "jobId": job_id,
            "level": level.value if isinstance(level, LogLevel) else level,
            "startDate": start_date.isoformat() if start_date else None,
            "endDate": end_date.isoformat() if end_date else None,
            "page": page,
            "pageSize": page_size,
        }
        response = self._client._request("GET", "logs/search", params=params)
        return PagedResult.from_dict(response, LogEntry)


class ServerClient:
    """Client for server operations."""

    def __init__(self, client: FMSClient):
        self._client = client

    def heartbeat(
        self,
        status: Union[ServerStatus, str] = ServerStatus.ONLINE,
        agent_version: str = "1.0.0",
        system_info: Optional[Dict[str, Any]] = None,
    ) -> None:
        """Send a heartbeat."""
        if isinstance(status, str):
            status = ServerStatus(status)

        data = {
            "serverName": self._client.server_name,
            "status": status.value,
            "agentVersion": agent_version,
            "systemInfo": system_info,
        }
        self._client._request("POST", "servers/heartbeat", data=data)

    def get(self, server_name: Optional[str] = None) -> Server:
        """Get server details."""
        name = server_name or self._client.server_name
        response = self._client._request("GET", f"servers/{name}")
        return Server.from_dict(response)

    def list(self) -> List[Server]:
        """List all servers."""
        response = self._client._request("GET", "servers")
        return [Server.from_dict(s) for s in response]

    def set_maintenance(self, server_name: Optional[str] = None, enable: bool = True) -> None:
        """Set server maintenance mode."""
        name = server_name or self._client.server_name
        endpoint = f"servers/{name}/maintenance"
        self._client._request("POST" if enable else "DELETE", endpoint)


class JobClient:
    """Client for job operations."""

    def __init__(self, client: FMSClient):
        self._client = client

    def register(
        self,
        job_id: str,
        display_name: str,
        description: Optional[str] = None,
        schedule: Optional[str] = None,
        priority: Union[JobPriority, str] = JobPriority.NORMAL,
        timeout_minutes: Optional[int] = None,
        tags: Optional[List[str]] = None,
    ) -> Job:
        """Register a job."""
        if isinstance(priority, str):
            priority = JobPriority(priority)

        data = {
            "jobId": job_id,
            "displayName": display_name,
            "description": description,
            "serverName": self._client.server_name,
            "schedule": schedule,
            "priority": priority.value,
            "timeoutMinutes": timeout_minutes,
            "tags": tags,
        }
        response = self._client._request("POST", "jobs/register", data=data)
        return Job.from_dict(response)

    def get(self, job_id: str) -> Job:
        """Get a job by ID."""
        response = self._client._request("GET", f"jobs/{job_id}")
        return Job.from_dict(response)

    def list(self, server_name: Optional[str] = None) -> List[Job]:
        """List jobs."""
        if server_name:
            response = self._client._request("GET", f"jobs/server/{server_name}")
        else:
            response = self._client._request("GET", "jobs")
        return [Job.from_dict(j) for j in response]

    def activate(self, job_id: str) -> None:
        """Activate a job."""
        self._client._request("POST", f"jobs/{job_id}/activate")

    def deactivate(self, job_id: str) -> None:
        """Deactivate a job."""
        self._client._request("POST", f"jobs/{job_id}/deactivate")


class ExecutionClient:
    """Client for execution operations."""

    def __init__(self, client: FMSClient):
        self._client = client

    def start(
        self,
        job_id: str,
        trigger_type: Union[TriggerType, str] = TriggerType.MANUAL,
        triggered_by: Optional[str] = None,
        parameters: Optional[Dict[str, Any]] = None,
    ) -> Execution:
        """Start a job execution."""
        if isinstance(trigger_type, str):
            trigger_type = TriggerType(trigger_type)

        data = {
            "jobId": job_id,
            "serverName": self._client.server_name,
            "triggerType": trigger_type.value,
            "triggeredBy": triggered_by or os.getenv("USER", "unknown"),
            "parameters": parameters,
        }
        response = self._client._request("POST", "executions", data=data)
        return Execution.from_dict(response)

    def complete(
        self,
        execution_id: str,
        status: Union[ExecutionStatus, str] = ExecutionStatus.SUCCESS,
        output_message: Optional[str] = None,
        error_message: Optional[str] = None,
    ) -> Execution:
        """Complete an execution."""
        if isinstance(status, str):
            status = ExecutionStatus(status)

        data = {
            "status": status.value,
            "outputMessage": output_message,
            "errorMessage": error_message,
        }
        response = self._client._request("PUT", f"executions/{execution_id}/complete", data=data)
        return Execution.from_dict(response)

    def cancel(self, execution_id: str, reason: Optional[str] = None) -> None:
        """Cancel an execution."""
        self._client._request("POST", f"executions/{execution_id}/cancel", data={"reason": reason})

    def get(self, execution_id: str) -> Execution:
        """Get an execution by ID."""
        response = self._client._request("GET", f"executions/{execution_id}")
        return Execution.from_dict(response)

    def get_running(self) -> List[Execution]:
        """Get running executions."""
        response = self._client._request("GET", "executions/running")
        return [Execution.from_dict(e) for e in response]


class AlertClient:
    """Client for alert operations."""

    def __init__(self, client: FMSClient):
        self._client = client

    def get(self, alert_id: str) -> Alert:
        """Get an alert rule by ID."""
        response = self._client._request("GET", f"alerts/{alert_id}")
        return Alert.from_dict(response)

    def list(self) -> List[Alert]:
        """List all alert rules."""
        response = self._client._request("GET", "alerts")
        return [Alert.from_dict(a) for a in response]

    def get_active_instances(self) -> List[AlertInstance]:
        """Get active alert instances."""
        response = self._client._request("GET", "alerts/instances/active")
        return [AlertInstance.from_dict(a) for a in response]

    def acknowledge(self, instance_id: str, notes: Optional[str] = None) -> None:
        """Acknowledge an alert instance."""
        self._client._request("POST", f"alerts/instances/{instance_id}/acknowledge", data={"notes": notes})

    def resolve(self, instance_id: str, notes: Optional[str] = None) -> None:
        """Resolve an alert instance."""
        self._client._request("POST", f"alerts/instances/{instance_id}/resolve", data={"notes": notes})
