
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioGanador
    {
        private readonly IJugadorPartidaRepositorio _repositorio;
        private readonly IPartidaRepositorio _partidaRepositorio;

        public ServicioGanador(
            IJugadorPartidaRepositorio repositorio,
            IPartidaRepositorio partidaRepositorio)
        {
            _repositorio = repositorio;
            _partidaRepositorio = partidaRepositorio;
        }


        public async Task<(int? IdGanador, bool HayEmpate)> DeterminarGanadorAsync(int idPartida)
        {
            var jugadores = await _repositorio.ObtenerPorPartidaAsync(idPartida);

            if (jugadores.Count == 0)
                return (null, false);

            int puntajeMayor = jugadores[0].Puntaje;

            // Comprueba si varios jugadores comparten el primer lugar.
            int cantidadLideres = jugadores.Count(j => j.Puntaje == puntajeMayor);

            if (cantidadLideres > 1)
                return (null, true);

            return (jugadores[0].IdJugadorPartida, false);
        }
        
        public async Task<bool> DebeFinalizarAsync(int idPartida)
        {
            var partida = await _partidaRepositorio.ObtenerPorIdAsync(idPartida);

            if (partida == null || partida.Estado != "EN_CURSO")
                return false;

            var jugadores = await _repositorio.ObtenerPorPartidaAsync(idPartida);

            // Comprueba si alguien alcanzó el puntaje objetivo.
            if (partida.PuntajeObjetivo.HasValue && jugadores.Any(j => j.Puntaje >= partida.PuntajeObjetivo.Value))
            {
                return true;
            }

            // Comprueba si se agotó el tiempo configurado.
            if (partida.LimiteTiempo.HasValue)
            {
                var tiempoTranscurrido = DateTime.UtcNow - partida.FechaInicio.ToUniversalTime();

                if (tiempoTranscurrido.TotalSeconds >= partida.LimiteTiempo.Value)
                    return true;
            }

            return false;
        }
        
        public async Task<string> FinalizarPartidaAsync(int idPartida)
        {
            var partida = await _partidaRepositorio.ObtenerPorIdAsync(idPartida);

            if (partida == null)
                return "La partida no existe.";

            if (partida.Estado != "EN_CURSO")
                return "La partida no está en curso.";

            if (!await DebeFinalizarAsync(idPartida))
                return "La partida todavía no cumple las condiciones de finalización.";

            var resultado = await DeterminarGanadorAsync(idPartida);

            
            if (resultado.HayEmpate)
            {
                // Inicia una prórroga de dos minutos desde este momento.
                partida.FechaInicio = DateTime.UtcNow;
                partida.LimiteTiempo = 120;
                partida.PuntajeObjetivo = null;

                await _partidaRepositorio.ActualizarAsync(partida);

                return "Hay empate. La partida se extiende dos minutos.";
            }


            if (resultado.IdGanador == null)
                return "No hay participantes para determinar un ganador.";

            await _repositorio.RegistrarGanadorAsync(
                idPartida,
                resultado.IdGanador.Value
            );

            partida.Estado = "FINALIZADA";
            partida.FechaFin = DateTime.UtcNow;

            await _partidaRepositorio.ActualizarAsync(partida);

            return $"Partida finalizada. Ganador: participante {resultado.IdGanador.Value}.";
        }


    }
}
