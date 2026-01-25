using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChargingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    ConnectorId = table.Column<int>(type: "int", nullable: false),
                    IdTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSec = table.Column<int>(type: "int", nullable: true),
                    LastUpdateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartMeterWh = table.Column<int>(type: "int", nullable: true),
                    StopMeterWh = table.Column<int>(type: "int", nullable: true),
                    EnergyKWh = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargingSessions_ChargingStations_StationId",
                        column: x => x.StationId,
                        principalTable: "ChargingStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSessions_StartTimeUtc",
                table: "ChargingSessions",
                column: "StartTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSessions_StationId",
                table: "ChargingSessions",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSessions_TransactionId",
                table: "ChargingSessions",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargingSessions");
        }
    }
}
