using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlienCyborgESPRadar.Migrations.GpsDb
{
    /// <inheritdoc />
    public partial class GpsDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GpsLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GpsFix = table.Column<bool>(type: "bit", nullable: true),
                    GpsPresent = table.Column<bool>(type: "bit", nullable: true),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Satellites = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HdopX100 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FixAgeMs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpsLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GpsLogs");
        }
    }
}
