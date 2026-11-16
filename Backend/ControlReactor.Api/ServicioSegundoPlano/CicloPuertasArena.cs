using System;

namespace ControlReactor.Api.ServicioSegundoPlano;

internal sealed class CicloPuertasArena
{
    private bool solicitarCierre = true;
    private DateTimeOffset siguienteCambio;

    public CicloPuertasArena(DateTimeOffset ahora)
    {
        siguienteCambio = ahora.AddSeconds(Random.Shared.Next(15, 31));
    }

    // true: pedir cierre; false: pedir apertura; null: todavía no hay cambio.
    public bool? Revisar(DateTimeOffset ahora)
    {
        if (ahora < siguienteCambio)
            return null;

        solicitarCierre = !solicitarCierre;

        int segundos = solicitarCierre
            ? Random.Shared.Next(15, 31)
            : Random.Shared.Next(5, 11);

        siguienteCambio = ahora.AddSeconds(segundos);
        return solicitarCierre;
    }

    private readonly System.Collections.Generic.HashSet<int> puertasCerradas = new();

    public int[] ActualizarPuertas(
        System.Collections.Generic.IReadOnlyList<ZonaPuerta> zonas,
        Func<ZonaPuerta, bool> estaOcupada)
    {
        if (!solicitarCierre)
        {
            puertasCerradas.Clear();
            return Array.Empty<int>();
        }
        foreach (var zona in zonas)
        {
            if (!puertasCerradas.Contains(zona.Id) && !estaOcupada(zona))
                puertasCerradas.Add(zona.Id);
        }

        var estado = new int[puertasCerradas.Count];
        puertasCerradas.CopyTo(estado);
        Array.Sort(estado);
        return estado;
    }
}