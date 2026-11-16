namespace ControlReactor.Api.ServicioSegundoPlano;

internal readonly record struct ZonaPuerta(
    int Id, float MinX, float MaxX, float MinY, float MaxY)
{
    public bool Contiene(float x, float y, float margenX, float margenY)
    {
        return x >= MinX - margenX && x <= MaxX + margenX
            && y >= MinY - margenY && y <= MaxY + margenY;
    }

    public bool OcupadaPorJugador(float x, float y)
{
    const float escala = 1.1061f;

    float centroX = x + 0.06478718f * escala;
    float centroY = y - 0.0610362f * escala;
    float mitadAncho = 0.6228656f * escala / 2f;
    float mitadAlto = 0.8577428f * escala / 2f;

    return Contiene(centroX, centroY, mitadAncho, mitadAlto);
}
}

internal static class ZonasPuertasArena
{
    public static IReadOnlyList<ZonaPuerta> Mapa1 { get; } = new ZonaPuerta[]
    {
        new(0, -13.198f, -12.183f, -7.298f, -6.284f),
        new(1, -21.148f, -20.133f, -4.348f, -3.334f),
        new(2, -20.218f, -19.203f, -7.251f, -6.237f),
        new(3,  -5.238f,  -4.223f, -7.278f, -6.264f)
    };
    public static IReadOnlyList<ZonaPuerta> Mapa2 { get; } = new ZonaPuerta[]
    {
        new(0, -12.207f, -11.191f,  0.738f,  1.752f),
        new(1, -10.210f,  -9.194f, -2.293f, -1.279f),
        new(2,  -9.176f,  -8.160f, -2.293f, -1.279f),
        new(3,  -4.219f,  -3.203f, -6.263f, -5.249f)
    };
    public static IReadOnlyList<ZonaPuerta> Mapa3 { get; } = new ZonaPuerta[]
    {
        new(0,  -3.200f, -2.184f,  -2.245f,  -1.231f),
        new(1, -10.204f, -9.188f, -11.303f, -10.289f),
        new(2, -10.204f, -9.188f, -12.343f, -11.330f),
        new(3,  10.809f, 11.825f,  -6.267f,  -5.254f),
        new(4,   5.802f,  6.817f,  -8.265f,  -7.251f),
        new(5,   6.814f,  7.830f,   0.681f,   1.695f),
        new(6,   6.786f,  7.802f,   1.750f,   2.763f)
    };
    public static IReadOnlyList<ZonaPuerta> ObtenerPorEscenario(int? idEscenario)
    {
        return idEscenario switch
        {
            1 => Mapa1,
            2 => Mapa2,
            3 => Mapa3,
            _ => System.Array.Empty<ZonaPuerta>()
        };
    }
}