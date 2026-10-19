using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceUnicoCodigoSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Partidas_CodigoSala",
                table: "Partidas",
                column: "CodigoSala",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Partidas_CodigoSala",
                table: "Partidas");
        }
    }
}
