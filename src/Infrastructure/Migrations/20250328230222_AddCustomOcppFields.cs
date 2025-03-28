using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomOcppFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Access",
                table: "ChargingStations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Internet",
                table: "ChargingStations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterNominalPower",
                table: "ChargingStations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scheduling",
                table: "ChargingStations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelfConsumption",
                table: "ChargingStations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vehicle",
                table: "ChargingStations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Access",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "Internet",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "MeterNominalPower",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "Scheduling",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "SelfConsumption",
                table: "ChargingStations");

            migrationBuilder.DropColumn(
                name: "Vehicle",
                table: "ChargingStations");
        }
    }
}
