using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlienCyborgESPRadar.Migrations.GpsDb
{
    /// <inheritdoc />
    public partial class GpsDbPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Satellites",
                table: "GpsLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "GpsLogs",
                type: "float",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "GpsLogs",
                type: "float",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HdopX100",
                table: "GpsLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FixAgeMs",
                table: "GpsLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RadarLogId",
                table: "GpsLogs",
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
                name: "BatteryLog",
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
                    table.PrimaryKey("PK_BatteryLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryLog_RadarLog_RadarLogId",
                        column: x => x.RadarLogId,
                        principalTable: "RadarLog",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GpsLogs_RadarLogId",
                table: "GpsLogs",
                column: "RadarLogId",
                unique: true,
                filter: "[RadarLogId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BatteryLog_RadarLogId",
                table: "BatteryLog",
                column: "RadarLogId",
                unique: true,
                filter: "[RadarLogId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_GpsLogs_RadarLog_RadarLogId",
                table: "GpsLogs",
                column: "RadarLogId",
                principalTable: "RadarLog",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GpsLogs_RadarLog_RadarLogId",
                table: "GpsLogs");

            migrationBuilder.DropTable(
                name: "BatteryLog");

            migrationBuilder.DropTable(
                name: "RadarLog");

            migrationBuilder.DropIndex(
                name: "IX_GpsLogs_RadarLogId",
                table: "GpsLogs");

            migrationBuilder.DropColumn(
                name: "RadarLogId",
                table: "GpsLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Satellites",
                table: "GpsLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Longitude",
                table: "GpsLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Latitude",
                table: "GpsLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HdopX100",
                table: "GpsLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FixAgeMs",
                table: "GpsLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
