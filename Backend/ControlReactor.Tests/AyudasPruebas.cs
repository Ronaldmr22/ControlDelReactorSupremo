using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Tests;

// Utilidades compartidas por las pruebas del Sprint 3.
internal static class AyudasPruebas
{
    // Busca la carpeta con ColisionesMapa1.json a ColisionesMapa3.json:
    // primero en la salida de las pruebas y luego en ControlReactor.Api/Mapas.
    public static string RutaMapas()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio != null)
        {
            string local = Path.Combine(directorio.FullName, "Mapas");
            if (File.Exists(Path.Combine(local, "ColisionesMapa1.json")))
                return local;

            string api = Path.Combine(directorio.FullName, "ControlReactor.Api", "Mapas");
            if (File.Exists(Path.Combine(api, "ColisionesMapa1.json")))
                return api;

            directorio = directorio.Parent;
        }

        throw new DirectoryNotFoundException(
            "No se encontró la carpeta Mapas con los archivos de colisiones.");
    }

    // Partida en curso por tiempo; FechaInicio se fija para controlar el reloj de la prueba.
    public static Partida PartidaEnCurso(
        int idPartida,
        DateTime fechaInicio,
        int limiteTiempo = 300,
        int idEscenario = 1,
        string codigoSala = "123456")
    {
        return new Partida
        {
            IdPartida = idPartida,
            CodigoSala = codigoSala,
            IdEscenario = idEscenario,
            Estado = "EN_CURSO",
            TipoFinalizacion = "TIEMPO",
            LimiteTiempo = limiteTiempo,
            FechaInicio = fechaInicio
        };
    }

    // La conexión no necesita un WebSocket real para las validaciones en memoria.
    public static ClientConnection Conexion(
        string playerId,
        string? codigoSala = "123456",
        int? idJugadorPartida = 1)
    {
        return new ClientConnection(null!, playerId)
        {
            RoomCode = codigoSala,
            IdJugadorPartida = idJugadorPartida
        };
    }

    public static readonly IReadOnlyList<ClientConnection> SinConexiones =
        new List<ClientConnection>();
}
