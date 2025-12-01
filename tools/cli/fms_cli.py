#!/usr/bin/env python3
"""
FMS Log Nexus CLI - Command-line administration tool.

Usage:
    fms-cli [OPTIONS] COMMAND [ARGS]...

Examples:
    fms-cli login
    fms-cli logs list --level Error
    fms-cli servers list
    fms-cli jobs register --id JOB-001 --name "My Job"
"""

import os
import sys
import json
import click
from datetime import datetime, timedelta
from typing import Optional
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent.parent / "sdk" / "python"))

try:
    from fmslognexus import FMSClient
    from fmslognexus.exceptions import FMSError, AuthenticationError
except ImportError:
    click.echo("Error: FMS Log Nexus SDK not installed. Run: pip install fms-lognexus", err=True)
    sys.exit(1)

# Configuration file path
CONFIG_FILE = Path.home() / ".fms-lognexus" / "config.json"
TOKEN_FILE = Path.home() / ".fms-lognexus" / "token.json"


def load_config() -> dict:
    """Load configuration from file."""
    if CONFIG_FILE.exists():
        with open(CONFIG_FILE, "r") as f:
            return json.load(f)
    return {}


def save_config(config: dict) -> None:
    """Save configuration to file."""
    CONFIG_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(CONFIG_FILE, "w") as f:
        json.dump(config, f, indent=2)


def load_token() -> Optional[dict]:
    """Load stored token."""
    if TOKEN_FILE.exists():
        with open(TOKEN_FILE, "r") as f:
            return json.load(f)
    return None


def save_token(token_data: dict) -> None:
    """Save token to file."""
    TOKEN_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(TOKEN_FILE, "w") as f:
        json.dump(token_data, f, indent=2)
    # Secure the file
    os.chmod(TOKEN_FILE, 0o600)


def clear_token() -> None:
    """Clear stored token."""
    if TOKEN_FILE.exists():
        TOKEN_FILE.unlink()


def get_client(ctx: click.Context) -> FMSClient:
    """Get configured client from context."""
    config = load_config()
    base_url = ctx.obj.get("url") or config.get("base_url") or os.environ.get("FMS_BASE_URL")
    api_key = ctx.obj.get("api_key") or config.get("api_key") or os.environ.get("FMS_API_KEY")
    
    if not base_url:
        raise click.ClickException("No server URL configured. Use --url or run 'fms-cli configure'")
    
    client = FMSClient(base_url=base_url, api_key=api_key)
    
    # Try to use stored token if no API key
    if not api_key:
        token_data = load_token()
        if token_data:
            client._access_token = token_data.get("access_token")
            client._refresh_token = token_data.get("refresh_token")
    
    return client


def format_datetime(dt: Optional[datetime]) -> str:
    """Format datetime for display."""
    if dt is None:
        return "-"
    if isinstance(dt, str):
        dt = datetime.fromisoformat(dt.replace("Z", "+00:00"))
    return dt.strftime("%Y-%m-%d %H:%M:%S")


def format_json(data: dict, pretty: bool = False) -> str:
    """Format data as JSON."""
    if pretty:
        return json.dumps(data, indent=2, default=str)
    return json.dumps(data, default=str)


# Main CLI group
@click.group()
@click.option("--url", "-u", help="FMS Log Nexus server URL")
@click.option("--api-key", "-k", help="API key for authentication")
@click.option("--output", "-o", type=click.Choice(["table", "json"]), default="table", help="Output format")
@click.version_option(version="1.0.0")
@click.pass_context
def cli(ctx, url, api_key, output):
    """FMS Log Nexus CLI - Administration tool."""
    ctx.ensure_object(dict)
    ctx.obj["url"] = url
    ctx.obj["api_key"] = api_key
    ctx.obj["output"] = output


# Configure command
@cli.command()
@click.option("--url", prompt="Server URL", help="FMS Log Nexus server URL")
@click.option("--api-key", prompt="API Key (optional)", default="", help="API key for authentication")
def configure(url, api_key):
    """Configure CLI settings."""
    config = {
        "base_url": url,
        "api_key": api_key if api_key else None,
    }
    save_config(config)
    click.echo(f"Configuration saved to {CONFIG_FILE}")


# Login command
@cli.command()
@click.option("--username", "-u", prompt=True, help="Username")
@click.option("--password", "-p", prompt=True, hide_input=True, help="Password")
@click.pass_context
def login(ctx, username, password):
    """Login with username and password."""
    config = load_config()
    base_url = ctx.obj.get("url") or config.get("base_url") or os.environ.get("FMS_BASE_URL")
    
    if not base_url:
        raise click.ClickException("No server URL configured. Use --url or run 'fms-cli configure'")
    
    client = FMSClient(base_url=base_url)
    
    try:
        result = client.login(username, password)
        save_token({
            "access_token": result["accessToken"],
            "refresh_token": result["refreshToken"],
            "expires_at": result["expiresAt"],
        })
        click.echo(f"Successfully logged in as {username}")
    except AuthenticationError as e:
        raise click.ClickException(f"Login failed: {e.message}")
    except Exception as e:
        raise click.ClickException(f"Error: {e}")


# Logout command
@cli.command()
@click.pass_context
def logout(ctx):
    """Logout and clear stored credentials."""
    try:
        client = get_client(ctx)
        client.logout()
    except Exception:
        pass
    clear_token()
    click.echo("Logged out successfully")


# Logs group
@cli.group()
def logs():
    """Log management commands."""
    pass


@logs.command("list")
@click.option("--server", "-s", help="Filter by server name")
@click.option("--job", "-j", help="Filter by job ID")
@click.option("--level", "-l", type=click.Choice(["Trace", "Debug", "Information", "Warning", "Error", "Critical"]))
@click.option("--since", help="Show logs since (e.g., '1h', '24h', '7d')")
@click.option("--limit", default=20, help="Number of logs to show")
@click.pass_context
def list_logs(ctx, server, job, level, since, limit):
    """List recent logs."""
    client = get_client(ctx)
    
    start_date = None
    if since:
        if since.endswith("h"):
            start_date = datetime.utcnow() - timedelta(hours=int(since[:-1]))
        elif since.endswith("d"):
            start_date = datetime.utcnow() - timedelta(days=int(since[:-1]))
    
    try:
        result = client.logs.search(
            server_name=server,
            job_id=job,
            level=level,
            start_date=start_date,
            page_size=limit,
        )
        
        if ctx.obj["output"] == "json":
            click.echo(format_json([vars(log) for log in result.items], pretty=True))
        else:
            if not result.items:
                click.echo("No logs found")
                return
            
            click.echo(f"{'Timestamp':<20} {'Level':<12} {'Server':<15} {'Message':<50}")
            click.echo("-" * 100)
            for log in result.items:
                msg = log.message[:47] + "..." if len(log.message) > 50 else log.message
                click.echo(f"{format_datetime(log.timestamp):<20} {log.level.value:<12} {(log.server_name or '-'):<15} {msg:<50}")
            
            click.echo(f"\nShowing {len(result.items)} of {result.total_count} logs")
            
    except FMSError as e:
        raise click.ClickException(str(e))


@logs.command("send")
@click.option("--message", "-m", required=True, help="Log message")
@click.option("--level", "-l", default="Information", type=click.Choice(["Trace", "Debug", "Information", "Warning", "Error", "Critical"]))
@click.option("--server", "-s", help="Server name")
@click.option("--job", "-j", help="Job ID")
@click.pass_context
def send_log(ctx, message, level, server, job):
    """Send a log entry."""
    client = get_client(ctx)
    
    try:
        log = client.logs.create(
            message=message,
            level=level,
            job_id=job,
        )
        if server:
            client.server_name = server
        
        click.echo(f"Log created: {log.id}")
    except FMSError as e:
        raise click.ClickException(str(e))


@logs.command("stats")
@click.option("--since", default="24h", help="Time period (e.g., '1h', '24h', '7d')")
@click.pass_context
def log_stats(ctx, since):
    """Show log statistics."""
    client = get_client(ctx)
    
    try:
        dashboard = client.get_dashboard()
        logs_data = dashboard.get("logs", {})
        
        click.echo("Log Statistics")
        click.echo("-" * 40)
        click.echo(f"Total (24h):     {logs_data.get('total24h', 0):,}")
        click.echo(f"Errors (24h):    {logs_data.get('errors24h', 0):,}")
        click.echo(f"Warnings (24h):  {logs_data.get('warnings24h', 0):,}")
        
    except FMSError as e:
        raise click.ClickException(str(e))


# Servers group
@cli.group()
def servers():
    """Server management commands."""
    pass


@servers.command("list")
@click.option("--online-only", is_flag=True, help="Show only online servers")
@click.pass_context
def list_servers(ctx, online_only):
    """List all servers."""
    client = get_client(ctx)
    
    try:
        servers_list = client.servers.list()
        
        if online_only:
            servers_list = [s for s in servers_list if s.status.value == "Online"]
        
        if ctx.obj["output"] == "json":
            click.echo(format_json([vars(s) for s in servers_list], pretty=True))
        else:
            if not servers_list:
                click.echo("No servers found")
                return
            
            click.echo(f"{'Server Name':<25} {'Status':<12} {'Last Heartbeat':<20} {'Agent Version':<15}")
            click.echo("-" * 75)
            for server in servers_list:
                click.echo(f"{server.server_name:<25} {server.status.value:<12} {format_datetime(server.last_heartbeat):<20} {server.agent_version or '-':<15}")
                
    except FMSError as e:
        raise click.ClickException(str(e))


@servers.command("heartbeat")
@click.option("--server", "-s", help="Server name (default: hostname)")
@click.option("--status", default="Online", type=click.Choice(["Online", "Offline", "Maintenance"]))
@click.pass_context
def send_heartbeat(ctx, server, status):
    """Send a heartbeat."""
    client = get_client(ctx)
    
    if server:
        client.server_name = server
    
    try:
        client.servers.heartbeat(status=status)
        click.echo(f"Heartbeat sent for {client.server_name}: {status}")
    except FMSError as e:
        raise click.ClickException(str(e))


# Jobs group
@cli.group()
def jobs():
    """Job management commands."""
    pass


@jobs.command("list")
@click.option("--server", "-s", help="Filter by server name")
@click.option("--active-only", is_flag=True, help="Show only active jobs")
@click.pass_context
def list_jobs(ctx, server, active_only):
    """List jobs."""
    client = get_client(ctx)
    
    try:
        jobs_list = client.jobs.list(server_name=server)
        
        if active_only:
            jobs_list = [j for j in jobs_list if j.is_active]
        
        if ctx.obj["output"] == "json":
            click.echo(format_json([vars(j) for j in jobs_list], pretty=True))
        else:
            if not jobs_list:
                click.echo("No jobs found")
                return
            
            click.echo(f"{'Job ID':<20} {'Name':<25} {'Server':<15} {'Priority':<10} {'Active':<8}")
            click.echo("-" * 80)
            for job in jobs_list:
                active = "Yes" if job.is_active else "No"
                click.echo(f"{job.job_id:<20} {job.display_name[:22]:<25} {job.server_name:<15} {job.priority.value:<10} {active:<8}")
                
    except FMSError as e:
        raise click.ClickException(str(e))


@jobs.command("register")
@click.option("--id", "job_id", required=True, help="Job ID")
@click.option("--name", required=True, help="Display name")
@click.option("--description", help="Job description")
@click.option("--server", "-s", help="Server name")
@click.option("--priority", default="Normal", type=click.Choice(["Low", "Normal", "High", "Critical"]))
@click.option("--timeout", type=int, help="Timeout in minutes")
@click.pass_context
def register_job(ctx, job_id, name, description, server, priority, timeout):
    """Register a new job."""
    client = get_client(ctx)
    
    if server:
        client.server_name = server
    
    try:
        job = client.jobs.register(
            job_id=job_id,
            display_name=name,
            description=description,
            priority=priority,
            timeout_minutes=timeout,
        )
        click.echo(f"Job registered: {job.job_id}")
    except FMSError as e:
        raise click.ClickException(str(e))


@jobs.command("activate")
@click.argument("job_id")
@click.pass_context
def activate_job(ctx, job_id):
    """Activate a job."""
    client = get_client(ctx)
    
    try:
        client.jobs.activate(job_id)
        click.echo(f"Job {job_id} activated")
    except FMSError as e:
        raise click.ClickException(str(e))


@jobs.command("deactivate")
@click.argument("job_id")
@click.pass_context
def deactivate_job(ctx, job_id):
    """Deactivate a job."""
    client = get_client(ctx)
    
    try:
        client.jobs.deactivate(job_id)
        click.echo(f"Job {job_id} deactivated")
    except FMSError as e:
        raise click.ClickException(str(e))


# Executions group
@cli.group()
def executions():
    """Execution management commands."""
    pass


@executions.command("list")
@click.option("--job", "-j", help="Filter by job ID")
@click.option("--running", is_flag=True, help="Show only running executions")
@click.pass_context
def list_executions(ctx, job, running):
    """List executions."""
    client = get_client(ctx)
    
    try:
        if running:
            execs = client.executions.get_running()
        else:
            # Get recent executions via job if specified
            execs = []
            if job:
                result = client._request("GET", f"executions/job/{job}")
                from fmslognexus.models import Execution
                execs = [Execution.from_dict(e) for e in result]
        
        if ctx.obj["output"] == "json":
            click.echo(format_json([vars(e) for e in execs], pretty=True))
        else:
            if not execs:
                click.echo("No executions found")
                return
            
            click.echo(f"{'ID':<36} {'Job':<15} {'Status':<10} {'Started':<20}")
            click.echo("-" * 85)
            for ex in execs:
                click.echo(f"{ex.id:<36} {ex.job_id:<15} {ex.status.value:<10} {format_datetime(ex.started_at):<20}")
                
    except FMSError as e:
        raise click.ClickException(str(e))


@executions.command("start")
@click.argument("job_id")
@click.option("--server", "-s", help="Server name")
@click.pass_context
def start_execution(ctx, job_id, server):
    """Start a job execution."""
    client = get_client(ctx)
    
    if server:
        client.server_name = server
    
    try:
        execution = client.executions.start(job_id)
        click.echo(f"Execution started: {execution.id}")
    except FMSError as e:
        raise click.ClickException(str(e))


@executions.command("complete")
@click.argument("execution_id")
@click.option("--status", default="Success", type=click.Choice(["Success", "Failed"]))
@click.option("--message", "-m", help="Output/error message")
@click.pass_context
def complete_execution(ctx, execution_id, status, message):
    """Complete an execution."""
    client = get_client(ctx)
    
    try:
        if status == "Success":
            client.executions.complete(execution_id, status=status, output_message=message)
        else:
            client.executions.complete(execution_id, status=status, error_message=message)
        click.echo(f"Execution {execution_id} completed: {status}")
    except FMSError as e:
        raise click.ClickException(str(e))


@executions.command("cancel")
@click.argument("execution_id")
@click.option("--reason", "-r", help="Cancellation reason")
@click.pass_context
def cancel_execution(ctx, execution_id, reason):
    """Cancel an execution."""
    client = get_client(ctx)
    
    try:
        client.executions.cancel(execution_id, reason=reason)
        click.echo(f"Execution {execution_id} cancelled")
    except FMSError as e:
        raise click.ClickException(str(e))


# Alerts group
@cli.group()
def alerts():
    """Alert management commands."""
    pass


@alerts.command("list")
@click.option("--active", is_flag=True, help="Show only active alert instances")
@click.pass_context
def list_alerts(ctx, active):
    """List alerts."""
    client = get_client(ctx)
    
    try:
        if active:
            alerts_list = client.alerts.get_active_instances()
            
            if ctx.obj["output"] == "json":
                click.echo(format_json([vars(a) for a in alerts_list], pretty=True))
            else:
                if not alerts_list:
                    click.echo("No active alerts")
                    return
                
                click.echo(f"{'ID':<36} {'Alert':<25} {'Severity':<10} {'Triggered':<20}")
                click.echo("-" * 95)
                for alert in alerts_list:
                    click.echo(f"{alert.id:<36} {alert.alert_name[:22]:<25} {alert.severity.value:<10} {format_datetime(alert.triggered_at):<20}")
        else:
            alerts_list = client.alerts.list()
            
            if ctx.obj["output"] == "json":
                click.echo(format_json([vars(a) for a in alerts_list], pretty=True))
            else:
                if not alerts_list:
                    click.echo("No alert rules found")
                    return
                
                click.echo(f"{'ID':<36} {'Name':<30} {'Type':<20} {'Active':<8}")
                click.echo("-" * 95)
                for alert in alerts_list:
                    active = "Yes" if alert.is_active else "No"
                    click.echo(f"{alert.id:<36} {alert.name[:27]:<30} {alert.alert_type.value:<20} {active:<8}")
                    
    except FMSError as e:
        raise click.ClickException(str(e))


@alerts.command("acknowledge")
@click.argument("instance_id")
@click.option("--notes", "-n", help="Acknowledgement notes")
@click.pass_context
def acknowledge_alert(ctx, instance_id, notes):
    """Acknowledge an alert instance."""
    client = get_client(ctx)
    
    try:
        client.alerts.acknowledge(instance_id, notes=notes)
        click.echo(f"Alert {instance_id} acknowledged")
    except FMSError as e:
        raise click.ClickException(str(e))


@alerts.command("resolve")
@click.argument("instance_id")
@click.option("--notes", "-n", help="Resolution notes")
@click.pass_context
def resolve_alert(ctx, instance_id, notes):
    """Resolve an alert instance."""
    client = get_client(ctx)
    
    try:
        client.alerts.resolve(instance_id, notes=notes)
        click.echo(f"Alert {instance_id} resolved")
    except FMSError as e:
        raise click.ClickException(str(e))


# Dashboard command
@cli.command()
@click.pass_context
def dashboard(ctx):
    """Show dashboard summary."""
    client = get_client(ctx)
    
    try:
        data = client.get_dashboard()
        
        if ctx.obj["output"] == "json":
            click.echo(format_json(data, pretty=True))
        else:
            servers = data.get("servers", {})
            logs = data.get("logs", {})
            jobs = data.get("jobs", {})
            executions = data.get("executions", {})
            alerts = data.get("alerts", {})
            
            click.echo("=" * 50)
            click.echo("         FMS Log Nexus Dashboard")
            click.echo("=" * 50)
            click.echo()
            
            click.echo("SERVERS")
            click.echo(f"  Total:       {servers.get('total', 0)}")
            click.echo(f"  Online:      {servers.get('online', 0)}")
            click.echo(f"  Offline:     {servers.get('offline', 0)}")
            click.echo()
            
            click.echo("LOGS (24h)")
            click.echo(f"  Total:       {logs.get('total24h', 0):,}")
            click.echo(f"  Errors:      {logs.get('errors24h', 0):,}")
            click.echo(f"  Warnings:    {logs.get('warnings24h', 0):,}")
            click.echo()
            
            click.echo("JOBS")
            click.echo(f"  Active:      {jobs.get('active', 0)}")
            click.echo(f"  Total:       {jobs.get('total', 0)}")
            click.echo()
            
            click.echo("EXECUTIONS (24h)")
            click.echo(f"  Success:     {executions.get('success24h', 0)}")
            click.echo(f"  Failed:      {executions.get('failed24h', 0)}")
            click.echo(f"  Running:     {executions.get('running', 0)}")
            click.echo()
            
            click.echo("ALERTS")
            click.echo(f"  Active:      {alerts.get('active', 0)}")
            click.echo()
            
    except FMSError as e:
        raise click.ClickException(str(e))


# Status command
@cli.command()
@click.pass_context
def status(ctx):
    """Check API status."""
    config = load_config()
    base_url = ctx.obj.get("url") or config.get("base_url") or os.environ.get("FMS_BASE_URL")
    
    if not base_url:
        raise click.ClickException("No server URL configured")
    
    import requests
    
    try:
        response = requests.get(f"{base_url}/health", timeout=10)
        
        if response.status_code == 200:
            click.echo(f"✓ API is healthy at {base_url}")
        else:
            click.echo(f"✗ API returned status {response.status_code}")
            
    except requests.exceptions.ConnectionError:
        click.echo(f"✗ Cannot connect to {base_url}")
    except Exception as e:
        click.echo(f"✗ Error: {e}")


def main():
    """Main entry point."""
    cli(obj={})


if __name__ == "__main__":
    main()
