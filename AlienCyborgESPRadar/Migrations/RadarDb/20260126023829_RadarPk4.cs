using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlienCyborgESPRadar.Migrations.RadarDb
{
    /// <inheritdoc />
    public partial class RadarPk4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GpsLogs",
                table: "GpsLogs");

            migrationBuilder.DropIndex(
                name: "IX_GpsLogs_RadarLogId",
                table: "GpsLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BatteryLogs",
                table: "BatteryLogs");

            migrationBuilder.DropIndex(
                name: "IX_BatteryLogs_RadarLogId",
                table: "BatteryLogs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "GpsLogs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BatteryLogs");

            migrationBuilder.AlterColumn<long>(
                name: "RadarLogId",
                table: "GpsLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "RadarLogId",
                table: "BatteryLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GpsLogs",
                table: "GpsLogs",
                column: "RadarLogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BatteryLogs",
                table: "BatteryLogs",
                column: "RadarLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GpsLogs",
                table: "GpsLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BatteryLogs",
                table: "BatteryLogs");

            migrationBuilder.AlterColumn<long>(
                name: "RadarLogId",
                table: "GpsLogs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "GpsLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<long>(
                name: "RadarLogId",
                table: "BatteryLogs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "BatteryLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GpsLogs",
                table: "GpsLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BatteryLogs",
                table: "BatteryLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GpsLogs_RadarLogId",
                table: "GpsLogs",
                column: "RadarLogId",
                unique: true,
                filter: "[RadarLogId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BatteryLogs_RadarLogId",
                table: "BatteryLogs",
                column: "RadarLogId",
                unique: true,
                filter: "[RadarLogId] IS NOT NULL");
        }
    }
}
