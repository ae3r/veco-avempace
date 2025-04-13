using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterDetailsToChargingStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MeterLine1Current",
                table: "ChargingStations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MeterLine1Power",
                table: "ChargingStations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MeterLine2Current",
                table: "ChargingStations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MeterLine2Power",
                table: "ChargingStations",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeterLine1Current",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "MeterLine1Power",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "MeterLine2Current",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "MeterLine2Power",
                table: "ChargingStations");
        }
    }
}
