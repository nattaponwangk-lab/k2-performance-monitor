using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace K2PerfMonitor.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlanHandle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ObjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    AvgDurationMs = table.Column<double>(type: "float", nullable: false),
                    AvgCpuMs = table.Column<double>(type: "float", nullable: false),
                    AvgLogicalReads = table.Column<double>(type: "float", nullable: false),
                    PlanXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueryText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_CollectedAtUtc",
                table: "ExecutionPlans",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_CollectedAtUtc_QueryHash",
                table: "ExecutionPlans",
                columns: new[] { "CollectedAtUtc", "QueryHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionPlans");
        }
    }
}
