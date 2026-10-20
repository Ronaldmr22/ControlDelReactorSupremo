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
                Partida = partida,
                EnSala = true,
                UltimaActividadUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var sala = await ObtenerPorCodigoAsync(partida.CodigoSala);
            return (sala, "Sala creada correctamente.");
        }
        public async Task<(SalaDto? Sala, string Mensaje)> UnirseAsync(int idUsuario, string codigoSala)
        {
            codigoSala = (codigoSala ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(codigoSala))
                return (null, "Escribe el código de sala.");

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            var partida = await BuscarSalaBloqueadaAsync(codigoSala);

            if (partida == null)
                return (null, "La sala no existe.");

            if (partida.Estado != "ESPERANDO")
                return (null, "La sala ya no admite participantes.");

            var ahora = DateTime.UtcNow;
            var limite = ahora.AddSeconds(-60);

            var participantes = await _context.JugadoresPartida
                .Include(j => j.Usuario)
                .Where(j => j.IdPartida == partida.IdPartida)
                .ToListAsync();

            var activos = participantes
                .Where(j => j.EnSala &&
                    j.UltimaActividadUtc.HasValue &&
                    j.UltimaActividadUtc.Value >= limite)
                .ToList();

            var existente = participantes
                .FirstOrDefault(j => j.IdUsuario == idUsuario);

            // Una solicitud repetida no duplica al participante.
            if (existente != null && activos.Contains(existente))
            {
                existente.UltimaActividadUtc = ahora;

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return (await ObtenerPorCodigoAsync(codigoSala),
                    "Ya perteneces a esta sala.");
            }

            if (activos.Count >= 3)
                return (null, "La sala está llena.");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
                return (null, "El usuario no existe.");

            if (usuario.AvatarSeleccionado is not (>= 1 and <= 5))
                return (null, "Selecciona un personaje antes de unirte.");

            if (activos.Any(j =>
                j.Usuario.AvatarSeleccionado == usuario.AvatarSeleccionado))
            {
                return (null,
                    "Ese personaje ya está ocupado. Elige otro en el lobby.");
            }

            // Reutiliza el registro si el usuario había salido.
            if (existente == null)
            {
                existente = new JugadorPartida
                {
                    IdUsuario = idUsuario,
                    IdPartida = partida.IdPartida
                };

                _context.JugadoresPartida.Add(existente);
            }

            existente.EnSala = true;
            existente.UltimaActividadUtc = ahora;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return (await ObtenerPorCodigoAsync(codigoSala),
                "Te uniste correctamente.");
        }

        public async Task<SalaDto?> ObtenerPorCodigoAsync(string codigoSala)
        {
            codigoSala = (codigoSala ?? string.Empty).Trim().ToUpperInvariant();

            var partida = await _context.Partidas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodigoSala == codigoSala);

            if (partida == null)
                return null;
            
            var limite = DateTime.UtcNow.AddSeconds(-60);

            var participantes = await _context.JugadoresPartida
                .AsNoTracking()
                .Where(j => j.IdPartida == partida.IdPartida &&
                    j.EnSala &&
                    j.UltimaActividadUtc.HasValue &&
                    j.UltimaActividadUtc.Value >= limite)
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

        public async Task<(bool Exito, string Mensaje)> RegistrarActividadAsync(int idUsuario, string codigoSala)
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync();

            var partida = await BuscarSalaBloqueadaAsync(codigoSala);

            if (partida == null || partida.Estado != "ESPERANDO")
                return (false, "La sala ya no está disponible.");

            var jugador = await _context.JugadoresPartida
                .FirstOrDefaultAsync(j =>
                    j.IdPartida == partida.IdPartida &&
                    j.IdUsuario == idUsuario);

            var ahora = DateTime.UtcNow;

            if (jugador == null || !jugador.EnSala ||
                !jugador.UltimaActividadUtc.HasValue ||
                jugador.UltimaActividadUtc.Value < ahora.AddSeconds(-60))
            {
                return (false, "Tu conexión con la sala expiró. Vuelve a unirte.");
            }

            jugador.UltimaActividadUtc = ahora;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return (true, "Actividad registrada.");
        }

        public async Task<(bool Exito, string Mensaje)> SalirAsync(int idUsuario, string codigoSala)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            var partida = await BuscarSalaBloqueadaAsync(codigoSala);

            if (partida == null)
                return (false, "La sala no existe.");

            if (partida.Estado != "ESPERANDO")
                return (false, "La sala ya no está en espera.");

            var jugador = await _context.JugadoresPartida
                .FirstOrDefaultAsync(j =>
                    j.IdPartida == partida.IdPartida &&
                    j.IdUsuario == idUsuario);

            if (jugador == null)
                return (false, "No perteneces a esta sala.");

            jugador.EnSala = false;

            // Si el creador sale, se cancela la sala.
            if (partida.IdUsuarioCreador == idUsuario)
            {
                partida.Estado = "CANCELADA";

                var participantes = await _context.JugadoresPartida
                    .Where(j => j.IdPartida == partida.IdPartida)
                    .ToListAsync();

                foreach (var participante in participantes)
                    participante.EnSala = false;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return (true, "Saliste de la sala.");
        }

        public async Task<(bool Exito, string Mensaje)> IniciarPartidaAsync(
        int idUsuario, string codigoSala)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            var partida = await BuscarSalaBloqueadaAsync(codigoSala);

            if (partida == null)
                return (false, "La sala no existe.");

            // Solo el creador puede iniciar la partida.
            if (partida.IdUsuarioCreador != idUsuario)
                return (false, "Solo el creador puede iniciar la partida.");

            if (partida.Estado != "ESPERANDO")
                return (false, "La partida ya no está en espera.");

            var ahora = DateTime.UtcNow;
            var limite = ahora.AddSeconds(-60);

            // Contamos únicamente los participantes activos.
            var participantesActivos = await _context.JugadoresPartida
                .CountAsync(j =>
                    j.IdPartida == partida.IdPartida &&
                    j.EnSala &&
                    j.UltimaActividadUtc.HasValue &&
                    j.UltimaActividadUtc.Value >= limite);

            if (participantesActivos < 2)
                return (false, "Se necesitan al menos 2 jugadores para iniciar.");

            // Verificamos que el creador siga activo.
            var creadorActivo = await _context.JugadoresPartida
                .AnyAsync(j =>
                    j.IdPartida == partida.IdPartida &&
                    j.IdUsuario == idUsuario &&
                    j.EnSala &&
                    j.UltimaActividadUtc.HasValue &&
                    j.UltimaActividadUtc.Value >= limite);

            if (!creadorActivo)
                return (false, "Tu conexión con la sala expiró.");

            // Cambiamos el estado de la partida.
            partida.Estado = "INICIADA";

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return (true, "Partida iniciada correctamente.");
        }

        private async Task<Partida?> BuscarSalaBloqueadaAsync(string codigoSala)
        {
            string codigo = (codigoSala ?? string.Empty)
                .Trim().ToUpperInvariant();

            // Evita modificar simultáneamente la membresía de una sala.
            var partidas = await _context.Partidas
                .FromSqlInterpolated(
                    $"""SELECT * FROM "Partidas" WHERE "CodigoSala" = {codigo} FOR UPDATE""")
                .ToListAsync();

            return partidas.FirstOrDefault();
        }
    }
}


