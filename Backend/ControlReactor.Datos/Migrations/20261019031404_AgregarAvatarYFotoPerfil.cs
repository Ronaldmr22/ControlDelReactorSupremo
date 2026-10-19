using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAvatarYFotoPerfil : Migration
    {
        /// <inheritdoc />
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Usuarios"
                ALTER COLUMN "AvatarSeleccionado"
                TYPE integer
                USING "AvatarSeleccionado"::integer;
                """);

            migrationBuilder.AddColumn<string>(
                name: "UrlFotoPerfil",
                table: "Usuarios",
                type: "text",
                nullable: true);
        }


        /// <inheritdoc />
        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlFotoPerfil",
                table: "Usuarios");

            migrationBuilder.Sql("""
                ALTER TABLE "Usuarios"
                ALTER COLUMN "AvatarSeleccionado"
                TYPE text
                USING "AvatarSeleccionado"::text;
                """);
        }

    }
}
