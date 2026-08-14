using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace K2PerfMonitor.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerStatRollups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerStatRollups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BucketStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BucketMinutes = table.Column<int>(type: "int", nullable: false),
                    AvgCpuPercent = table.Column<double>(type: "float", nullable: false),
                    MaxCpuPercent = table.Column<double>(type: "float", nullable: false),
                    AvgMemoryPercent = table.Column<double>(type: "float", nullable: false),
                    MaxMemoryPercent = table.Column<double>(type: "float", nullable: false),
                    AvgConnectionCount = table.Column<double>(type: "float", nullable: false),
                    MaxConnectionCount = table.Column<int>(type: "int", nullable: false),
                    AvgBatchRequestsPerSec = table.Column<double>(type: "float", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerStatRollups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerStatRollups_BucketMinutes_BucketStartUtc",
                table: "ServerStatRollups",
                columns: new[] { "BucketMinutes", "BucketStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerStatRollups");
        }
    }
}
