using System;

namespace ControlReactor.Api.ServicioSegundoPlano;

internal sealed class CicloPuertasArena
{
    private bool solicitarCierre = true;
    private DateTimeOffset siguienteCambio;

    private readonly (int Min, int Max) segundosCerradas;
    private readonly (int Min, int Max) segundosAbiertas;

    public CicloPuertasArena(
        DateTimeOffset ahora,
        (int Min, int Max)? segundosCerradas = null,
        (int Min, int Max)? segundosAbiertas = null)
    {
        this.segundosCerradas = segundosCerradas ?? (15, 30);
        this.segundosAbiertas = segundosAbiertas ?? (5, 10);

        siguienteCambio = ahora.AddSeconds(
            DuracionAleatoria(this.segundosCerradas));
    }

    private static int DuracionAleatoria((int Min, int Max) rango)
    {
        return Random.Shared.Next(rango.Min, rango.Max + 1);
    }
    // true: pedir cierre; false: pedir apertura; null: todavía no hay cambio.
    public bool? Revisar(DateTimeOffset ahora)
    {
        if (ahora < siguienteCambio)
            return null;

        solicitarCierre = !solicitarCierre;

        int segundos = DuracionAleatoria(
            solicitarCierre ? segundosCerradas : segundosAbiertas);

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