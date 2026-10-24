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

            if (jugador.Partida == null || jugador.Partida.Estado != "EN_CURSO")
                return null;

            var partida = jugador.Partida;

            // Limitar los puntos al objetivo configurado.
            if (partida.TipoFinalizacion == "PUNTOS" &&
                partida.PuntajeObjetivo.HasValue)
            {
                jugador.Puntaje = Math.Min(
                    jugador.Puntaje + 1,
                    partida.PuntajeObjetivo.Value
                );
            }
            else
            {
                jugador.Puntaje += 1;
            }

            await _repositorio.ActualizarAsync(jugador);

            return jugador.Puntaje;
        }

        public async Task<int?> ModificarPuntaje(int idJugadorPartida, int puntos)
        {
            var jugador = await _repositorio.ObtenerPorIdAsync(idJugadorPartida);

            if (jugador == null)
                return null;

            jugador.Puntaje = Math.Max(0, jugador.Puntaje + puntos);

            // Aplicar también el límite a modificaciones de puntaje.
            if (jugador.Partida != null &&
                jugador.Partida.TipoFinalizacion == "PUNTOS" &&
                jugador.Partida.PuntajeObjetivo.HasValue)
            {
                jugador.Puntaje = Math.Min(
                    jugador.Puntaje,
                    jugador.Partida.PuntajeObjetivo.Value
                );
            }

            await _repositorio.ActualizarAsync(jugador);

            return jugador.Puntaje;
        }
    }
}