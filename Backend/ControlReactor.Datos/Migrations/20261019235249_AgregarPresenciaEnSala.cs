using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPresenciaEnSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnSala",
                table: "JugadoresPartida",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaActividadUtc",
                table: "JugadoresPartida",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnSala",
                table: "JugadoresPartida");

            migrationBuilder.DropColumn(
                name: "UltimaActividadUtc",
                table: "JugadoresPartida");
        }
    }
}
