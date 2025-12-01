/**
 * FMS Log Nexus SignalR Client Interface
 * 
 * This file provides TypeScript interfaces for the SignalR hubs.
 * Use with @microsoft/signalr package.
 * 
 * Installation:
 *   npm install @microsoft/signalr
 * 
 * Usage:
 *   import * as signalR from "@microsoft/signalr";
 *   import { LogHubClient, LogNotification } from "./signalr-client";
 */

// ============================================================================
// Enums (must match server-side enums)
// ============================================================================

export enum LogLevel {
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

export enum JobStatus {
    Pending = 0,
    Running = 1,
    Success = 2,
    Warning = 3,
    Failed = 4,
    Timeout = 5,
    Cancelled = 6,
    Skipped = 7
}

export enum ServerStatus {
    Unknown = 0,
    Online = 1,
    Offline = 2,
    Maintenance = 3,
    Degraded = 4
}

export enum AlertType {
    JobFailure = 0,
    JobTimeout = 1,
    ConsecutiveFailures = 2,
    ErrorThreshold = 3,
    ServerOffline = 4,
    Custom = 5
}

export enum AlertSeverity {
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

// ============================================================================
// Notification DTOs
// ============================================================================

export interface LogNotification {
    id: string;
    timestamp: string;
    level: LogLevel;
    message: string;
    serverName?: string;
    jobId?: string;
    executionId?: string;
    category?: string;
    exceptionType?: string;
}

export interface LogStatsNotification {
    timestamp: string;
    totalLogs: number;
    errorCount: number;
    warningCount: number;
    errorRate: number;
    serverName?: string;
    jobId?: string;
}

export interface ExecutionNotification {
    id: string;
    jobId: string;
    jobDisplayName: string;
    serverName: string;
    status: JobStatus;
    startedAt: string;
    completedAt?: string;
    durationMs?: number;
    triggerType?: string;
    errorMessage?: string;
}

export interface ExecutionProgressNotification {
    executionId: string;
    jobId: string;
    percentComplete: number;
    currentStep?: string;
    elapsedMs: number;
    estimatedRemainingMs?: number;
}

export interface JobStatusNotification {
    jobId: string;
    displayName: string;
    isActive: boolean;
    lastStatus?: JobStatus;
    lastExecutionAt?: string;
}

export interface JobHealthNotification {
    jobId: string;
    displayName: string;
    healthScore: number;
    healthStatus: string;
    consecutiveFailures: number;
    successRate?: number;
}

export interface ServerStatusNotification {
    serverName: string;
    status: ServerStatus;
    previousStatus?: ServerStatus;
    timestamp: string;
    isActive: boolean;
    inMaintenance: boolean;
}

export interface ServerHeartbeatNotification {
    serverName: string;
    timestamp: string;
    status: ServerStatus;
    agentVersion?: string;
}

export interface ServerMetricsNotification {
    serverName: string;
    timestamp: string;
    cpuUsage?: number;
    memoryUsage?: number;
    diskUsage?: number;
    activeConnections: number;
    runningJobs: number;
}

export interface AlertNotification {
    instanceId: string;
    alertId: string;
    alertName: string;
    alertType: AlertType;
    severity: AlertSeverity;
    message: string;
    triggeredAt: string;
    jobId?: string;
    serverName?: string;
}

export interface AlertActionNotification {
    instanceId: string;
    alertId: string;
    alertName: string;
    action: string;
    performedBy: string;
    performedAt: string;
    note?: string;
}

export interface AlertSummaryNotification {
    totalActive: number;
    newAlerts: number;
    criticalCount: number;
    highCount: number;
    mediumCount: number;
    lowCount: number;
}

export interface DashboardNotification {
    timestamp: string;
    logStats: DashboardLogStats;
    jobStats: DashboardJobStats;
    serverStats: DashboardServerStats;
    alertStats: DashboardAlertStats;
}

export interface DashboardLogStats {
    totalLogs24h: number;
    errorCount24h: number;
    warningCount24h: number;
    errorRate: number;
}

export interface DashboardJobStats {
    totalJobs: number;
    activeJobs: number;
    runningNow: number;
    failedLastRun: number;
    healthyJobs: number;
}

export interface DashboardServerStats {
    totalServers: number;
    onlineServers: number;
    offlineServers: number;
    maintenanceServers: number;
}

export interface DashboardAlertStats {
    activeAlerts: number;
    newAlerts: number;
    criticalAlerts: number;
}

export interface SystemNotificationMessage {
    type: string;
    title: string;
    message: string;
    severity: string;
    timestamp: string;
    data?: Record<string, unknown>;
}

// ============================================================================
// Subscription Options
// ============================================================================

export interface LogSubscriptionOptions {
    serverName?: string;
    jobId?: string;
    executionId?: string;
    minLevel?: LogLevel;
    categories?: string[];
    includeStatistics?: boolean;
}

// ============================================================================
// Hub Client Interfaces
// ============================================================================

export interface LogHubClient {
    // Server -> Client methods
    onReceiveLog(callback: (log: LogNotification) => void): void;
    onReceiveLogBatch(callback: (logs: LogNotification[]) => void): void;
    onReceiveLogStats(callback: (stats: LogStatsNotification) => void): void;

    // Client -> Server methods
    subscribeToAllLogs(): Promise<void>;
    unsubscribeFromAllLogs(): Promise<void>;
    subscribeToServer(serverName: string): Promise<void>;
    unsubscribeFromServer(serverName: string): Promise<void>;
    subscribeToJob(jobId: string): Promise<void>;
    unsubscribeFromJob(jobId: string): Promise<void>;
    subscribeToExecution(executionId: string): Promise<void>;
    unsubscribeFromExecution(executionId: string): Promise<void>;
    subscribeToErrors(): Promise<void>;
    unsubscribeFromErrors(): Promise<void>;
    subscribeWithFilter(options: LogSubscriptionOptions): Promise<void>;
}

export interface JobHubClient {
    // Server -> Client methods
    onExecutionStarted(callback: (execution: ExecutionNotification) => void): void;
    onExecutionCompleted(callback: (execution: ExecutionNotification) => void): void;
    onExecutionProgress(callback: (progress: ExecutionProgressNotification) => void): void;
    onJobStatusChanged(callback: (status: JobStatusNotification) => void): void;
    onJobHealthUpdated(callback: (health: JobHealthNotification) => void): void;

    // Client -> Server methods
    subscribeToAllJobs(): Promise<void>;
    unsubscribeFromAllJobs(): Promise<void>;
    subscribeToAllExecutions(): Promise<void>;
    unsubscribeFromAllExecutions(): Promise<void>;
    subscribeToJob(jobId: string): Promise<void>;
    unsubscribeFromJob(jobId: string): Promise<void>;
    subscribeToExecution(executionId: string): Promise<void>;
    unsubscribeFromExecution(executionId: string): Promise<void>;
    subscribeToServer(serverName: string): Promise<void>;
    unsubscribeFromServer(serverName: string): Promise<void>;
    subscribeToCriticalJobs(): Promise<void>;
    unsubscribeFromCriticalJobs(): Promise<void>;
    subscribeToHealthUpdates(): Promise<void>;
    unsubscribeFromHealthUpdates(): Promise<void>;
}

export interface ServerHubClient {
    // Server -> Client methods
    onServerStatusChanged(callback: (status: ServerStatusNotification) => void): void;
    onServerHeartbeat(callback: (heartbeat: ServerHeartbeatNotification) => void): void;
    onServerMetrics(callback: (metrics: ServerMetricsNotification) => void): void;

    // Client -> Server methods
    subscribeToAllServers(): Promise<void>;
    unsubscribeFromAllServers(): Promise<void>;
    subscribeToServer(serverName: string): Promise<void>;
    unsubscribeFromServer(serverName: string): Promise<void>;
    subscribeToHeartbeats(): Promise<void>;
    unsubscribeFromHeartbeats(): Promise<void>;
    subscribeToMetrics(): Promise<void>;
    unsubscribeFromMetrics(): Promise<void>;
    subscribeToOfflineAlerts(): Promise<void>;
    unsubscribeFromOfflineAlerts(): Promise<void>;
}

export interface AlertHubClient {
    // Server -> Client methods
    onAlertTriggered(callback: (alert: AlertNotification) => void): void;
    onAlertAcknowledged(callback: (action: AlertActionNotification) => void): void;
    onAlertResolved(callback: (action: AlertActionNotification) => void): void;
    onAlertSummaryUpdated(callback: (summary: AlertSummaryNotification) => void): void;

    // Client -> Server methods
    subscribeToAllAlerts(): Promise<void>;
    unsubscribeFromAllAlerts(): Promise<void>;
    subscribeToCriticalAlerts(): Promise<void>;
    unsubscribeFromCriticalAlerts(): Promise<void>;
    subscribeToHighPriorityAlerts(): Promise<void>;
    unsubscribeFromHighPriorityAlerts(): Promise<void>;
    subscribeToJobAlerts(jobId: string): Promise<void>;
    unsubscribeFromJobAlerts(jobId: string): Promise<void>;
    subscribeToServerAlerts(serverName: string): Promise<void>;
    unsubscribeFromServerAlerts(serverName: string): Promise<void>;
    subscribeToAlertType(alertType: AlertType): Promise<void>;
    unsubscribeFromAlertType(alertType: AlertType): Promise<void>;
    subscribeToSummary(): Promise<void>;
    unsubscribeFromSummary(): Promise<void>;
}

export interface DashboardHubClient {
    // Server -> Client methods
    onDashboardUpdated(callback: (dashboard: DashboardNotification) => void): void;
    onSystemNotification(callback: (notification: SystemNotificationMessage) => void): void;

    // Client -> Server methods
    subscribeToDashboard(): Promise<void>;
    unsubscribeFromDashboard(): Promise<void>;
    subscribeToNotifications(): Promise<void>;
    unsubscribeFromNotifications(): Promise<void>;
    getConnectionStats(): Promise<ConnectionStats>;
}

export interface ConnectionStats {
    totalConnections: number;
    uniqueUsers: number;
    connectedUsers: string[];
}

// ============================================================================
// Hub URLs
// ============================================================================

export const HubUrls = {
    logs: "/hubs/logs",
    jobs: "/hubs/jobs",
    servers: "/hubs/servers",
    alerts: "/hubs/alerts",
    dashboard: "/hubs/dashboard"
} as const;

// ============================================================================
// Example Usage
// ============================================================================

/**
 * Example: Connect to LogHub
 * 
 * import * as signalR from "@microsoft/signalr";
 * import { HubUrls, LogNotification } from "./signalr-client";
 * 
 * const connection = new signalR.HubConnectionBuilder()
 *     .withUrl(`${API_BASE_URL}${HubUrls.logs}`, {
 *         accessTokenFactory: () => getAccessToken()
 *     })
 *     .withAutomaticReconnect()
 *     .configureLogging(signalR.LogLevel.Information)
 *     .build();
 * 
 * // Subscribe to events
 * connection.on("ReceiveLog", (log: LogNotification) => {
 *     console.log("New log:", log);
 * });
 * 
 * // Start connection
 * await connection.start();
 * 
 * // Subscribe to server logs
 * await connection.invoke("SubscribeToServer", "PROD-01");
 */
