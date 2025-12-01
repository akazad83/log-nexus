"""FMS Log Nexus SDK Exceptions."""

from typing import Any, Dict, Optional


class FMSError(Exception):
    """Base exception for FMS Log Nexus SDK."""

    def __init__(
        self,
        message: str,
        status_code: Optional[int] = None,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message)
        self.message = message
        self.status_code = status_code
        self.response = response

    def __str__(self) -> str:
        if self.status_code:
            return f"[{self.status_code}] {self.message}"
        return self.message


class AuthenticationError(FMSError):
    """Raised when authentication fails."""

    def __init__(
        self,
        message: str = "Authentication failed",
        status_code: Optional[int] = 401,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)


class AuthorizationError(FMSError):
    """Raised when authorization fails."""

    def __init__(
        self,
        message: str = "Access denied",
        status_code: Optional[int] = 403,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)


class NotFoundError(FMSError):
    """Raised when a resource is not found."""

    def __init__(
        self,
        message: str = "Resource not found",
        status_code: Optional[int] = 404,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)


class ValidationError(FMSError):
    """Raised when request validation fails."""

    def __init__(
        self,
        message: str = "Validation failed",
        status_code: Optional[int] = 400,
        response: Optional[Dict[str, Any]] = None,
        errors: Optional[Dict[str, list]] = None,
    ):
        super().__init__(message, status_code, response)
        self.errors = errors or {}


class ConflictError(FMSError):
    """Raised when there is a resource conflict."""

    def __init__(
        self,
        message: str = "Resource conflict",
        status_code: Optional[int] = 409,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)


class RateLimitError(FMSError):
    """Raised when rate limit is exceeded."""

    def __init__(
        self,
        message: str = "Rate limit exceeded",
        status_code: Optional[int] = 429,
        response: Optional[Dict[str, Any]] = None,
        retry_after: Optional[int] = None,
    ):
        super().__init__(message, status_code, response)
        self.retry_after = retry_after


class ServerError(FMSError):
    """Raised when the server returns an error."""

    def __init__(
        self,
        message: str = "Server error",
        status_code: Optional[int] = 500,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)


class ConnectionError(FMSError):
    """Raised when connection to the server fails."""

    def __init__(
        self,
        message: str = "Failed to connect to FMS Log Nexus",
        status_code: Optional[int] = None,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)


class TimeoutError(FMSError):
    """Raised when a request times out."""

    def __init__(
        self,
        message: str = "Request timed out",
        status_code: Optional[int] = None,
        response: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message, status_code, response)
