using System;
using System.Collections.Generic;

namespace ControlReactor.Api.ServicioSegundoPlano;

internal static class ZonasBarrerasArena
{
    private static readonly IReadOnlyList<ZonaPuerta> Mapa1 = new ZonaPuerta[]
    {
        // BarreraArena0: los tres cuadros forman una sola zona.
        new(0, -17.200893f, -14.191717f, -7.302549f, -6.300244f),
        new(1,  -6.1843f,    -5.1788f,   -6.2768f,   -3.2666f)
    };
    private static readonly IReadOnlyList<ZonaPuerta> Mapa2 = new ZonaPuerta[]
    {
        new(0, -1.220893f, 0.837614f, -0.317737f, 0.709756f),
        new(1, -12.2076f,   -11.1777f,   -6.3148f,   -2.2786f)
    };
    private static readonly IReadOnlyList<ZonaPuerta> Mapa3 = new ZonaPuerta[]
    {
        new(0,  2.9124f,  3.9423f, -9.7048f, -5.6686f),
        new(1, 10.8624f, 11.8923f, -9.6848f, -5.6486f)
    };
    public static IReadOnlyList<ZonaPuerta> ObtenerPorEscenario(int? idEscenario)
    {
        return idEscenario switch
        {
            1 => Mapa1,
            2 => Mapa2,
            3 => Mapa3,
            _ => Array.Empty<ZonaPuerta>()
        };
    }
}
