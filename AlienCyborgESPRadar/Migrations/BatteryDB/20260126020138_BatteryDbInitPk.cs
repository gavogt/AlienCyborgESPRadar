using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlienCyborgESPRadar.Migrations.BatteryDb
{
    /// <inheritdoc />
    public partial class BatteryDbInitPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RadarLogId",
                table: "BatteryLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RadarLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motion = table.Column<bool>(type: "bit", nullable: false),
                    TsMs = table.Column<long>(type: "bigint", nullable: true),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadarLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GpsLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RadarLogId = table.Column<long>(type: "bigint", nullable: true),
                    NodeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GpsFix = table.Column<bool>(type: "bit", nullable: true),
                    GpsPresent = table.Column<bool>(type: "bit", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Satellites = table.Column<int>(type: "int", nullable: true),
                    HdopX100 = table.Column<int>(type: "int", nullable: true),
                    FixAgeMs = table.Column<int>(type: "int", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpsLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GpsLogs_RadarLog_RadarLogId",
                        column: x => x.RadarLogId,
                        principalTable: "RadarLog",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatteryLogs_RadarLogId",
                table: "BatteryLogs",
                column: "RadarLogId",
                unique: true,
                filter: "[RadarLogId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GpsLogs_RadarLogId",
                table: "GpsLogs",
                column: "RadarLogId",
                unique: true,
                filter: "[RadarLogId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BatteryLogs_RadarLog_RadarLogId",
                table: "BatteryLogs",
                column: "RadarLogId",
                principalTable: "RadarLog",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatteryLogs_RadarLog_RadarLogId",
                table: "BatteryLogs");

            migrationBuilder.DropTable(
                name: "GpsLogs");

            migrationBuilder.DropTable(
                name: "RadarLog");

            migrationBuilder.DropIndex(
                name: "IX_BatteryLogs_RadarLogId",
                table: "BatteryLogs");

            migrationBuilder.DropColumn(
                name: "RadarLogId",
                table: "BatteryLogs");
        }
    }
}
