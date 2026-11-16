using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEnergiaYRecargaReactor : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnergiaRestante",
                table: "Partidas",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "IdEscenario",
                table: "Partidas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReactorRechargeReadyAtUtc",
                table: "Partidas",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnergiaRestante",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "IdEscenario",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "ReactorRechargeReadyAtUtc",
                table: "Partidas");
        }

    }
}
