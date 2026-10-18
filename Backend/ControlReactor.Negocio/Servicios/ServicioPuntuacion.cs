using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioPuntuacion
    {
        private readonly IJugadorPartidaRepositorio _repositorio;

        public ServicioPuntuacion(IJugadorPartidaRepositorio repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<int?> RegistrarMovimiento(int idJugadorPartida)
        {
            var jugador = await _repositorio.ObtenerPorIdAsync(idJugadorPartida);

            if (jugador == null)
                return null;

            // Solo se permite puntuar durante una partida activa.
            if (jugador.Partida == null || jugador.Partida.Estado != "EN_CURSO")
                return null;

            jugador.Puntaje += 1;

            await _repositorio.ActualizarAsync(jugador);

            return jugador.Puntaje;
        }

        public async Task<int?> ModificarPuntaje(int idJugadorPartida, int puntos)
        {
            var jugador = await _repositorio.ObtenerPorIdAsync(idJugadorPartida);

            if (jugador == null)
                return null;

            jugador.Puntaje = Math.Max(0, jugador.Puntaje + puntos);

            await _repositorio.ActualizarAsync(jugador);

            return jugador.Puntaje;
        }
    }
}