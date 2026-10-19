using System;
using System.Linq;
using System.Threading.Tasks;
using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.DTOs;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Datos.Repositorios
{
    public class SalaRepositorio : ISalaRepositorio
    {
        private readonly ControlReactorDbContext _context;

        public SalaRepositorio(ControlReactorDbContext context)
        {
            _context = context;
        }

        public async Task<(SalaDto? Sala, string Mensaje)> CrearAsync(
            int idUsuario)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
                return (null, "El usuario no existe.");

            if (usuario.AvatarSeleccionado is not (>= 1 and <= 5))
                return (null, "Selecciona un personaje antes de crear la sala.");

            string codigo;

            do
            {
                codigo = System.Security.Cryptography.RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();
            }
            while (await _context.Partidas.AnyAsync(p => p.CodigoSala == codigo));

            var partida = new Partida
            {
                CodigoSala = codigo,
                IdUsuarioCreador = idUsuario,
                Estado = "ESPERANDO",
                FechaInicio = DateTime.UtcNow,
                TipoFinalizacion = "TIEMPO",
                LimiteTiempo = 300,
                PuntajeObjetivo = null,
                SegundosCongelamiento = 5
            };

            // Guarda la sala junto con su creador.
            _context.JugadoresPartida.Add(new JugadorPartida
            {
                IdUsuario = idUsuario,
                Partida = partida
            });

            await _context.SaveChangesAsync();

            var sala = await ObtenerPorCodigoAsync(partida.CodigoSala);
            return (sala, "Sala creada correctamente.");
        }
        public async Task<(SalaDto? Sala, string Mensaje)> UnirseAsync(
            int idUsuario, string codigoSala)
        {
            codigoSala = (codigoSala ?? string.Empty)
                .Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(codigoSala))
                return (null, "Escribe el código de sala.");

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            // Serializa las entradas a esta sala para respetar su capacidad.
            var partidas = await _context.Partidas
                .FromSqlInterpolated(
                    $"""SELECT * FROM "Partidas" WHERE "CodigoSala" = {codigoSala} FOR UPDATE""")
                .ToListAsync();

            var partida = partidas.FirstOrDefault();

            if (partida == null)
                return (null, "La sala no existe.");

            if (partida.Estado != "ESPERANDO")
                return (null, "La sala ya no admite participantes.");

            var participantes = await _context.JugadoresPartida
                .Include(j => j.Usuario)
                .Where(j => j.IdPartida == partida.IdPartida)
                .ToListAsync();

            if (participantes.Any(j => j.IdUsuario == idUsuario))
            {
                await transaccion.CommitAsync();
                return (await ObtenerPorCodigoAsync(codigoSala),
                    "Ya perteneces a esta sala.");
            }

            if (participantes.Count >= 3)
                return (null, "La sala está llena.");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
                return (null, "El usuario no existe.");

            if (usuario.AvatarSeleccionado is not (>= 1 and <= 5))
                return (null, "Selecciona un personaje antes de unirte.");

            if (participantes.Any(j =>
                j.Usuario.AvatarSeleccionado == usuario.AvatarSeleccionado))
            {
                return (null,
                    "Ese personaje ya está ocupado. Elige otro en el lobby.");
            }

            _context.JugadoresPartida.Add(new JugadorPartida
            {
                IdUsuario = idUsuario,
                IdPartida = partida.IdPartida
            });

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return (await ObtenerPorCodigoAsync(codigoSala),
                "Te uniste correctamente.");
        }

        public async Task<SalaDto?> ObtenerPorCodigoAsync(string codigoSala)
        {
            codigoSala = (codigoSala ?? string.Empty)
                .Trim().ToUpperInvariant();

            var partida = await _context.Partidas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodigoSala == codigoSala);

            if (partida == null)
                return null;

            var participantes = await _context.JugadoresPartida
                .AsNoTracking()
                .Where(j => j.IdPartida == partida.IdPartida)
                .OrderBy(j => j.IdJugadorPartida)
                .Select(j => new ParticipanteSalaDto
                {
                    IdUsuario = j.IdUsuario,
                    NombreUsuario = j.Usuario.NombreUsuario,
                    AvatarSeleccionado = j.Usuario.AvatarSeleccionado ?? 1,
                    FotoPerfil = j.Usuario.UrlFotoPerfil
                })
                .ToListAsync();

            return new SalaDto
            {
                IdPartida = partida.IdPartida,
                CodigoSala = partida.CodigoSala!,
                Estado = partida.Estado,
                IdUsuarioCreador = partida.IdUsuarioCreador ?? 0,
                Participantes = participantes
            };
        }
    }
}


