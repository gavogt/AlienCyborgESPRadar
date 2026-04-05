using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlienCyborgESPRadar.Migrations.RadarDb
{
    /// <inheritdoc />
    public partial class AddPirToRadarLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Pir",
                table: "RadarLogs",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pir",
                table: "RadarLogs");
        }
    }
}
