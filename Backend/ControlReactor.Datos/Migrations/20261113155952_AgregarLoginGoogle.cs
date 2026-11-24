using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLoginGoogle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuariosLoginGoogle",
                columns: table => new
                {
                    IdUsuarioLoginGoogle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Proveedor = table.Column<string>(type: "text", nullable: false),
                    IdGoogle = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosLoginGoogle", x => x.IdUsuarioLoginGoogle);
                    table.ForeignKey(
                        name: "FK_UsuariosLoginGoogle_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosLoginGoogle_IdGoogle",
                table: "UsuariosLoginGoogle",
                column: "IdGoogle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosLoginGoogle_IdUsuario",
                table: "UsuariosLoginGoogle",
                column: "IdUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuariosLoginGoogle");
        }
    }
}
