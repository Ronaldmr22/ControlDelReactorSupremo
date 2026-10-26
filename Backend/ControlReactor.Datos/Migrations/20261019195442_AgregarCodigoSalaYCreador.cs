using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCodigoSalaYCreador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoSala",
                table: "Partidas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioCreador",
                table: "Partidas",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoSala",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "IdUsuarioCreador",
                table: "Partidas");
        }
    }
}
