using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlienCyborgESPRadar.Migrations.RadarDb
{
    /// <inheritdoc />
    public partial class RadarPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatteryLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RadarLogId = table.Column<long>(type: "bigint", nullable: true),
                    NodeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BatteryOk = table.Column<bool>(type: "bit", nullable: true),
                    BatteryVoltage = table.Column<double>(type: "float", nullable: true),
                    BatteryPercent = table.Column<double>(type: "float", nullable: true),
                    Max17048ChipId = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryLogs_RadarLogs_RadarLogId",
                        column: x => x.RadarLogId,
                        principalTable: "RadarLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_GpsLogs_RadarLogs_RadarLogId",
                        column: x => x.RadarLogId,
                        principalTable: "RadarLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatteryLogs");

            migrationBuilder.DropTable(
                name: "GpsLogs");
        }
    }
}
