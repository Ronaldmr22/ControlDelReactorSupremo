using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ControlReactor.Datos.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Logros",
                columns: table => new
                {
                    IdLogro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logros", x => x.IdLogro);
                });

            migrationBuilder.CreateTable(
                name: "Partidas",
                columns: table => new
                {
                    IdPartida = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TipoFinalizacion = table.Column<string>(type: "text", nullable: false),
                    LimiteTiempo = table.Column<int>(type: "integer", nullable: true),
                    PuntajeObjetivo = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidas", x => x.IdPartida);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreUsuario = table.Column<string>(type: "text", nullable: false),
                    Correo = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CorreoVerificado = table.Column<bool>(type: "boolean", nullable: false),
                    AvatarSeleccionado = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "JugadoresPartida",
                columns: table => new
                {
                    IdJugadorPartida = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    IdPartida = table.Column<int>(type: "integer", nullable: false),
                    Puntaje = table.Column<int>(type: "integer", nullable: false),
                    EsGanador = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JugadoresPartida", x => x.IdJugadorPartida);
                    table.ForeignKey(
                        name: "FK_JugadoresPartida_Partidas_IdPartida",
                        column: x => x.IdPartida,
                        principalTable: "Partidas",
                        principalColumn: "IdPartida",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JugadoresPartida_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosLogros",
                columns: table => new
                {
                    IdUsuarioLogro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    IdLogro = table.Column<int>(type: "integer", nullable: false),
                    FechaObtencion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosLogros", x => x.IdUsuarioLogro);
                    table.ForeignKey(
                        name: "FK_UsuariosLogros_Logros_IdLogro",
                        column: x => x.IdLogro,
                        principalTable: "Logros",
                        principalColumn: "IdLogro",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosLogros_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresPartida_IdPartida",
                table: "JugadoresPartida",
                column: "IdPartida");

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresPartida_IdUsuario",
                table: "JugadoresPartida",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosLogros_IdLogro",
                table: "UsuariosLogros",
                column: "IdLogro");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosLogros_IdUsuario",
                table: "UsuariosLogros",
                column: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JugadoresPartida");

            migrationBuilder.DropTable(
                name: "UsuariosLogros");

            migrationBuilder.DropTable(
                name: "Partidas");

            migrationBuilder.DropTable(
                name: "Logros");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
