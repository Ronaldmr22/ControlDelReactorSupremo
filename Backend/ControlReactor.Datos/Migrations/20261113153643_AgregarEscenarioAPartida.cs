using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEscenarioAPartida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdEscenario",
                table: "Partidas",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdEscenario",
                table: "Partidas");
        }
    }
}
