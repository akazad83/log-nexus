using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMSLogNexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "alert");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.EnsureSchema(
                name: "system");

            migrationBuilder.EnsureSchema(
                name: "job");

            migrationBuilder.EnsureSchema(
                name: "log");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "Configuration",
                schema: "system",
                columns: table => new
                {
                    ConfigKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "string"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuration", x => x.ConfigKey);
                });

            migrationBuilder.CreateTable(
                name: "DashboardCache",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchemaVersions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Script = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                schema: "job",
                columns: table => new
                {
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServerName1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HeartbeatIntervalSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.ServerName);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValueSql: "NEWID()"),
                    Role = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Preferences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                schema: "job",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobId1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JobType = table.Column<int>(type: "int", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExecutablePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Schedule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScheduleDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    MaxDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    MinExecutionsPerDay = table.Column<int>(type: "int", nullable: true),
                    LastExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastExecutionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastStatus = table.Column<int>(type: "int", nullable: true),
                    LastDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    TotalExecutions = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SuccessCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FailureCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AvgDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Jobs_Servers_ServerName",
                        column: x => x.ServerName,
                        principalSchema: "job",
                        principalTable: "Servers",
                        principalColumn: "ServerName",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByUserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalSchema: "auth",
                        principalTable: "RefreshTokens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserApiKeys",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KeyHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserApiKeys_Servers_ServerName",
                        column: x => x.ServerName,
                        principalSchema: "job",
                        principalTable: "Servers",
                        principalColumn: "ServerName",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                schema: "alert",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ThrottleMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    LastTriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggerCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NotificationChannels = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_Servers_ServerName",
                        column: x => x.ServerName,
                        principalSchema: "job",
                        principalTable: "Servers",
                        principalColumn: "ServerName",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JobExecutions",
                schema: "job",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Manual"),
                    TriggeredBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultCode = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TraceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DebugCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    InfoCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    WarningCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CriticalCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobExecutions_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobExecutions_Servers_ServerName",
                        column: x => x.ServerName,
                        principalSchema: "job",
                        principalTable: "Servers",
                        principalColumn: "ServerName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertInstances",
                schema: "alert",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcknowledgeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotificationsSent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServerId = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertInstances_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalSchema: "alert",
                        principalTable: "Alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertInstances_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Jobs",
                        principalColumn: "JobId");
                    table.ForeignKey(
                        name: "FK_AlertInstances_Servers_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "job",
                        principalTable: "Servers",
                        principalColumn: "ServerName");
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                schema: "log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceContext = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SpanId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ParentSpanId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExceptionStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionSource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ClientIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_JobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalSchema: "job",
                        principalTable: "JobExecutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LogEntries_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Jobs",
                        principalColumn: "JobId");
                    table.ForeignKey(
                        name: "FK_LogEntries_Servers_ServerName",
                        column: x => x.ServerName,
                        principalSchema: "job",
                        principalTable: "Servers",
                        principalColumn: "ServerName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_AlertId",
                schema: "alert",
                table: "AlertInstances",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_AlertId_Status",
                schema: "alert",
                table: "AlertInstances",
                columns: new[] { "AlertId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_JobId",
                schema: "alert",
                table: "AlertInstances",
                column: "JobId",
                filter: "[JobId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_ServerId",
                schema: "alert",
                table: "AlertInstances",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_ServerName",
                schema: "alert",
                table: "AlertInstances",
                column: "ServerName",
                filter: "[ServerName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_Severity",
                schema: "alert",
                table: "AlertInstances",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_Status",
                schema: "alert",
                table: "AlertInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_Status_TriggeredAt",
                schema: "alert",
                table: "AlertInstances",
                columns: new[] { "Status", "TriggeredAt" },
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_TriggeredAt",
                schema: "alert",
                table: "AlertInstances",
                column: "TriggeredAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertType",
                schema: "alert",
                table: "Alerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive",
                schema: "alert",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive_AlertType",
                schema: "alert",
                table: "Alerts",
                columns: new[] { "IsActive", "AlertType" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_JobId",
                schema: "alert",
                table: "Alerts",
                column: "JobId",
                filter: "[JobId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_ServerName",
                schema: "alert",
                table: "Alerts",
                column: "ServerName",
                filter: "[ServerName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Severity",
                schema: "alert",
                table: "Alerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                schema: "dbo",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Configuration_Category",
                schema: "system",
                table: "Configuration",
                column: "Category",
                filter: "[Category] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_CorrelationId",
                schema: "job",
                table: "JobExecutions",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobId_StartedAt",
                schema: "job",
                table: "JobExecutions",
                columns: new[] { "JobId", "StartedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobId_Status",
                schema: "job",
                table: "JobExecutions",
                columns: new[] { "JobId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_ServerName",
                schema: "job",
                table: "JobExecutions",
                column: "ServerName");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_StartedAt",
                schema: "job",
                table: "JobExecutions",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Status",
                schema: "job",
                table: "JobExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Status_StartedAt",
                schema: "job",
                table: "JobExecutions",
                columns: new[] { "Status", "StartedAt" },
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Category",
                schema: "job",
                table: "Jobs",
                column: "Category",
                filter: "[Category] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsActive",
                schema: "job",
                table: "Jobs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsActive_LastStatus",
                schema: "job",
                table: "Jobs",
                columns: new[] { "IsActive", "LastStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsCritical",
                schema: "job",
                table: "Jobs",
                column: "IsCritical",
                filter: "[IsCritical] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_LastStatus",
                schema: "job",
                table: "Jobs",
                column: "LastStatus",
                filter: "[LastStatus] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ServerName",
                schema: "job",
                table: "Jobs",
                column: "ServerName",
                filter: "[ServerName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_CorrelationId",
                schema: "log",
                table: "LogEntries",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_JobExecutionId_Timestamp",
                schema: "log",
                table: "LogEntries",
                columns: new[] { "JobExecutionId", "Timestamp" },
                filter: "[JobExecutionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_JobId_Timestamp",
                schema: "log",
                table: "LogEntries",
                columns: new[] { "JobId", "Timestamp" },
                filter: "[JobId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Level_Timestamp",
                schema: "log",
                table: "LogEntries",
                columns: new[] { "Level", "Timestamp" },
                filter: "[Level] >= 4");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_ServerName_Timestamp",
                schema: "log",
                table: "LogEntries",
                columns: new[] { "ServerName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp",
                schema: "log",
                table: "LogEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp_Level",
                schema: "log",
                table: "LogEntries",
                columns: new[] { "Timestamp", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_TraceId",
                schema: "log",
                table: "LogEntries",
                column: "TraceId",
                filter: "[TraceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Cleanup",
                schema: "auth",
                table: "RefreshTokens",
                columns: new[] { "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                schema: "auth",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                schema: "auth",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RevokedAt",
                schema: "auth",
                table: "RefreshTokens",
                column: "RevokedAt",
                filter: "[RevokedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "auth",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "auth",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_RevokedAt_ExpiresAt",
                schema: "auth",
                table: "RefreshTokens",
                columns: new[] { "UserId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Servers_IsActive",
                schema: "job",
                table: "Servers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_IsActive_Status",
                schema: "job",
                table: "Servers",
                columns: new[] { "IsActive", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Servers_LastHeartbeat",
                schema: "job",
                table: "Servers",
                column: "LastHeartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_Status",
                schema: "job",
                table: "Servers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_ExpiresAt",
                schema: "auth",
                table: "UserApiKeys",
                column: "ExpiresAt",
                filter: "[ExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_IsActive",
                schema: "auth",
                table: "UserApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_KeyHash",
                schema: "auth",
                table: "UserApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_KeyPrefix",
                schema: "auth",
                table: "UserApiKeys",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_RevokedAt",
                schema: "auth",
                table: "UserApiKeys",
                column: "RevokedAt",
                filter: "[RevokedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_ServerName",
                schema: "auth",
                table: "UserApiKeys",
                column: "ServerName",
                filter: "[ServerName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_UserId",
                schema: "auth",
                table: "UserApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_UserId_IsActive",
                schema: "auth",
                table: "UserApiKeys",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "auth",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_Role",
                schema: "auth",
                table: "Users",
                columns: new[] { "IsActive", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsLocked",
                schema: "auth",
                table: "Users",
                column: "IsLocked",
                filter: "[IsLocked] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                schema: "auth",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Username",
                schema: "auth",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertInstances",
                schema: "alert");

            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Configuration",
                schema: "system");

            migrationBuilder.DropTable(
                name: "DashboardCache",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LogEntries",
                schema: "log");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "SchemaVersions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserApiKeys",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Alerts",
                schema: "alert");

            migrationBuilder.DropTable(
                name: "JobExecutions",
                schema: "job");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Jobs",
                schema: "job");

            migrationBuilder.DropTable(
                name: "Servers",
                schema: "job");
        }
    }
}
