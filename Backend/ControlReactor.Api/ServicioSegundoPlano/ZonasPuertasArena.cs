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
}