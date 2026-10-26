
using Xunit;
using ControlReactor.Negocio.Servicios;

namespace ControlReactor.Tests;

public class PruebasLogros
{
    [Fact]
    public void SinPartidas_NoDebeObtenerLogros()
    {
        var servicio = new ServicioLogros();

        var logros = servicio.ObtenerLogros(0, 0);

        Assert.Empty(logros);
    }

    [Fact]
    public void PrimeraPartida_DebeObtenerPrimerosPasos()
    {
        var servicio = new ServicioLogros();

        var logros = servicio.ObtenerLogros(1, 0);

        Assert.Single(logros);
        Assert.Equal(1, logros[0].IdLogro);
        Assert.Equal("Primeros pasos", logros[0].Nombre);
    }

    [Fact]
    public void PrimeraVictoria_DebeObtenerLogroVictoria()
    {
        var servicio = new ServicioLogros();

        var logros = servicio.ObtenerLogros(0, 1);

        Assert.Single(logros);
        Assert.Equal(2, logros[0].IdLogro);
        Assert.Equal("Primera victoria", logros[0].Nombre);
    }

    [Fact]
    public void PartidaYVictoria_DebeObtenerAmbosLogros()
    {
        var servicio = new ServicioLogros();

        var logros = servicio.ObtenerLogros(1, 1);

        Assert.Equal(2, logros.Count);
        Assert.Contains(logros, l => l.Nombre == "Primeros pasos");
        Assert.Contains(logros, l => l.Nombre == "Primera victoria");
    }
}
