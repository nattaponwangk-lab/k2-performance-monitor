using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace K2PerfMonitor.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstanceIdAndDatabaseStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "WaitStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "WaitStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "StoredProcedureStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "StoredProcedureStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "SlowQueries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "SlowQueries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "ServerStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "SqlInstanceName",
                table: "ServerStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "K2WorkflowStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "K2WorkflowStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "K2SmartObjectStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "K2SmartObjectStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "K2SmartFormStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "K2SmartFormStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "IoStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "IoStats",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "IndexRecommendations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "IndexRecommendations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "ExecutionPlans",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "ExecutionPlans",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "DeadlockEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "DeadlockEvents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "CollectorRuns",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "CollectorRuns",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "BlockingEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "BlockingEvents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InstanceId",
                table: "Alerts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "Alerts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DatabaseStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatabaseId = table.Column<int>(type: "int", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RecoveryModel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    CompatibilityLevel = table.Column<int>(type: "int", nullable: false),
                    IsSystemDatabase = table.Column<bool>(type: "bit", nullable: false),
                    DataSizeMb = table.Column<double>(type: "float", nullable: false),
                    LogSizeMb = table.Column<double>(type: "float", nullable: false),
                    TotalSizeMb = table.Column<double>(type: "float", nullable: false),
                    InstanceId = table.Column<long>(type: "bigint", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitStats_InstanceId_CollectedAtUtc",
                table: "WaitStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredProcedureStats_InstanceId_CollectedAtUtc",
                table: "StoredProcedureStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SlowQueries_InstanceId_CollectedAtUtc",
                table: "SlowQueries",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerStats_InstanceId_CollectedAtUtc",
                table: "ServerStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_K2WorkflowStats_InstanceId_CollectedAtUtc",
                table: "K2WorkflowStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_K2SmartObjectStats_InstanceId_CollectedAtUtc",
                table: "K2SmartObjectStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_K2SmartFormStats_InstanceId_CollectedAtUtc",
                table: "K2SmartFormStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IoStats_InstanceId_CollectedAtUtc",
                table: "IoStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexRecommendations_InstanceId_CollectedAtUtc",
                table: "IndexRecommendations",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_InstanceId_CollectedAtUtc",
                table: "ExecutionPlans",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlockEvents_InstanceId_CollectedAtUtc",
                table: "DeadlockEvents",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BlockingEvents_InstanceId_CollectedAtUtc",
                table: "BlockingEvents",
                columns: new[] { "InstanceId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseStats_CollectedAtUtc",
                table: "DatabaseStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseStats_CollectedAtUtc_DatabaseName",
                table: "DatabaseStats",
                columns: new[] { "CollectedAtUtc", "DatabaseName" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseStats_InstanceId_CollectedAtUtc",
                table: "DatabaseStats",
                columns: new[] { "InstanceId", "CollectedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatabaseStats");

            migrationBuilder.DropIndex(
                name: "IX_WaitStats_InstanceId_CollectedAtUtc",
                table: "WaitStats");

            migrationBuilder.DropIndex(
                name: "IX_StoredProcedureStats_InstanceId_CollectedAtUtc",
                table: "StoredProcedureStats");

            migrationBuilder.DropIndex(
                name: "IX_SlowQueries_InstanceId_CollectedAtUtc",
                table: "SlowQueries");

            migrationBuilder.DropIndex(
                name: "IX_ServerStats_InstanceId_CollectedAtUtc",
                table: "ServerStats");

            migrationBuilder.DropIndex(
                name: "IX_K2WorkflowStats_InstanceId_CollectedAtUtc",
                table: "K2WorkflowStats");

            migrationBuilder.DropIndex(
                name: "IX_K2SmartObjectStats_InstanceId_CollectedAtUtc",
                table: "K2SmartObjectStats");

            migrationBuilder.DropIndex(
                name: "IX_K2SmartFormStats_InstanceId_CollectedAtUtc",
                table: "K2SmartFormStats");

            migrationBuilder.DropIndex(
                name: "IX_IoStats_InstanceId_CollectedAtUtc",
                table: "IoStats");

            migrationBuilder.DropIndex(
                name: "IX_IndexRecommendations_InstanceId_CollectedAtUtc",
                table: "IndexRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionPlans_InstanceId_CollectedAtUtc",
                table: "ExecutionPlans");

            migrationBuilder.DropIndex(
                name: "IX_DeadlockEvents_InstanceId_CollectedAtUtc",
                table: "DeadlockEvents");

            migrationBuilder.DropIndex(
                name: "IX_BlockingEvents_InstanceId_CollectedAtUtc",
                table: "BlockingEvents");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "WaitStats");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "WaitStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "StoredProcedureStats");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "StoredProcedureStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "SlowQueries");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "SlowQueries");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "ServerStats");

            migrationBuilder.DropColumn(
                name: "SqlInstanceName",
                table: "ServerStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "K2WorkflowStats");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "K2WorkflowStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "K2SmartObjectStats");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "K2SmartObjectStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "K2SmartFormStats");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "K2SmartFormStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "IoStats");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "IoStats");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "IndexRecommendations");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "IndexRecommendations");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "ExecutionPlans");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "ExecutionPlans");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "DeadlockEvents");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "DeadlockEvents");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "CollectorRuns");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "CollectorRuns");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "BlockingEvents");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "BlockingEvents");

            migrationBuilder.DropColumn(
                name: "InstanceId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "Alerts");
        }
    }
}
