using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

// Pruebas EditMode: ejercitan metodos reales de los MonoBehaviour.
// No requieren Azure, una escena activa, ni cuentas reales.
public class PruebasEstadisticasYConfiguracion
{
    private GameObject raiz;

    [TearDown]
    public void Limpiar()
    {
        if (raiz != null)
            UnityEngine.Object.DestroyImmediate(raiz);
    }

    [Test]
    public void US04_Positiva_HistorialConVictoriaYMonto_MuestraResultadoYPuntos()
    {
        var estadisticas = CrearComponente("CargarEstadisticas");
        var texto = CrearTexto("Historial");
        Campo(estadisticas.GetType(), "txtHistorialPartidas").SetValue(estadisticas, texto);

        var partidas = CrearHistorial(estadisticas, (101, 80, true));
        InvocarMostrarHistorial(estadisticas, partidas);

        StringAssert.Contains("PARTIDA #101", texto.text);
        StringAssert.Contains("VICTORIA", texto.text);
        StringAssert.Contains("80 puntos", texto.text);
    }

    [Test]
    public void US04_Negativa_HistorialVacio_MuestraAvisoSinPartidas()
    {
        var estadisticas = CrearComponente("CargarEstadisticas");
        var texto = CrearTexto("Historial");
        Campo(estadisticas.GetType(), "txtHistorialPartidas").SetValue(estadisticas, texto);

        var tipoPartida = TipoAnidado(estadisticas.GetType(), "PartidaDto");
        var vacio = Array.CreateInstance(tipoPartida, 0);
        InvocarMostrarHistorial(estadisticas, vacio);

        Assert.AreEqual("Todavía no has jugado ninguna partida.", texto.text);
    }

    [Test]
    public void US04_Negativa_HistorialNulo_MuestraAvisoSinPartidas()
    {
        var estadisticas = CrearComponente("CargarEstadisticas");
        var texto = CrearTexto("Historial");
        Campo(estadisticas.GetType(), "txtHistorialPartidas").SetValue(estadisticas, texto);

        InvocarMostrarHistorial(estadisticas, null);

        Assert.AreEqual("Todavía no has jugado ninguna partida.", texto.text);
    }

    [Test]
    public void US08_Positiva_MostrarError_ReflejaMensajeEnPantallaConfiguracion()
    {
        var configuracion = CrearComponente("ConfiguracionPartidaUI");
        var texto = CrearTexto("Error");
        Campo(configuracion.GetType(), "textoError").SetValue(configuracion, texto);

        configuracion.GetType().GetMethod("MostrarError", BindingFlags.Instance | BindingFlags.Public)
            .Invoke(configuracion, new object[] { "La partida ya comenzó." });

        Assert.AreEqual("La partida ya comenzó.", texto.text);
    }

    [Test]
    public void US08_Positiva_LimpiarError_EliminaElMensajeAnterior()
    {
        var configuracion = CrearComponente("ConfiguracionPartidaUI");
        var texto = CrearTexto("Error");
        Campo(configuracion.GetType(), "textoError").SetValue(configuracion, texto);
        texto.text = "Error anterior";

        configuracion.GetType().GetMethod("LimpiarError", BindingFlags.Instance | BindingFlags.Public)
            .Invoke(configuracion, null);

        Assert.AreEqual("", texto.text);
    }

    // Helpers: usamos reflexion solo para acceder a DTOs privados y referencias SerializeField.
    private Component CrearComponente(string nombre)
    {
        if (raiz == null)
        {
            raiz = new GameObject("RaizPruebas");
            raiz.SetActive(false); // evita llamadas OnEnable que iniciarían HTTP.
        }
        Type tipo = Type.GetType(nombre + ", Assembly-CSharp");
        Assert.NotNull(tipo, "No existe " + nombre + " en Assembly-CSharp.");
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(raiz.transform);
        return objeto.AddComponent(tipo);
    }

    private TMP_Text CrearTexto(string nombre)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(raiz.transform);
        return objeto.AddComponent<TextMeshProUGUI>();
    }

    private static FieldInfo Campo(Type tipo, string nombre)
    {
        var campo = tipo.GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(campo, "Falta el campo " + nombre + " en " + tipo.Name);
        return campo;
    }

    private static Type TipoAnidado(Type tipo, string nombre)
    {
        var anidado = tipo.GetNestedType(nombre, BindingFlags.NonPublic);
        Assert.NotNull(anidado, "No existe el DTO " + nombre);
        return anidado;
    }

    private static Array CrearHistorial(Component estadisticas, (int id, int puntos, bool ganador) partida)
    {
        var tipo = TipoAnidado(estadisticas.GetType(), "PartidaDto");
        var instancia = Activator.CreateInstance(tipo);
        Campo(tipo, "idPartida").SetValue(instancia, partida.id);
        Campo(tipo, "puntaje").SetValue(instancia, partida.puntos);
        Campo(tipo, "esGanador").SetValue(instancia, partida.ganador);
        Campo(tipo, "fechaFin").SetValue(instancia, "2026-10-26T06:46:50Z");
        var lista = Array.CreateInstance(tipo, 1);
        lista.SetValue(instancia, 0);
        return lista;
    }

    private static void InvocarMostrarHistorial(Component estadisticas, Array partidas)
    {
        var metodo = estadisticas.GetType().GetMethod("MostrarHistorial", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(metodo, "No se encontró MostrarHistorial.");
        metodo.Invoke(estadisticas, new object[] { partidas });
    }
}
