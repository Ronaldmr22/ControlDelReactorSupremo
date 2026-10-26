using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class PruebasInterfazUnity
{
    private GameObject raiz;
    private Component configuracion;
    private TMP_Dropdown modo;
    private TMP_Dropdown duracion;
    private TMP_Dropdown puntos;
    private TMP_Dropdown paralisis;

    [SetUp]
    public void Preparar()
    {
        // Inactivo para asignar referencias antes de OnEnable().
        raiz = new GameObject("Configuracion_Prueba");
        raiz.SetActive(false);

        modo = CrearDropdown("Modo", "Tiempo", "Puntos");
        duracion = CrearDropdown("Duracion", "5 minutos", "10 minutos");
        puntos = CrearDropdown("Puntos", "100 puntos", "200 puntos");
        paralisis = CrearDropdown("Paralisis", "3 segundos", "5 segundos");

        Type tipo = TipoJuego("ConfiguracionPartidaUI");
        configuracion = raiz.AddComponent(tipo);
        Campo(tipo, "formaFinalizar").SetValue(configuracion, modo);
        Campo(tipo, "duracion").SetValue(configuracion, duracion);
        Campo(tipo, "puntajeObjetivo").SetValue(configuracion, puntos);
        Campo(tipo, "tiempoParalisis").SetValue(configuracion, paralisis);
        raiz.SetActive(true);
    }

    [TearDown]
    public void Limpiar()
    {
        if (raiz != null)
            UnityEngine.Object.DestroyImmediate(raiz);
    }

    [Test]
    public void US08_Positiva_ModoTiempo_HabilitaDuracionYLeeValores()
    {
        modo.value = 0;
        ActualizarModo(0);
        duracion.value = 1;
        paralisis.value = 1;

        Assert.AreEqual("TIEMPO", Invocar<string>("ObtenerTipoFinalizacion"));
        Assert.IsTrue(duracion.interactable);
        Assert.IsFalse(puntos.interactable);
        Assert.AreEqual(10, Invocar<int>("ObtenerDuracion"));
        Assert.AreEqual(5, Invocar<int>("ObtenerTiempoParalisis"));
    }

    [Test]
    public void US08_Positiva_ModoPuntos_HabilitaPuntajeYLeeObjetivo()
    {
        modo.value = 1;
        ActualizarModo(1);
        puntos.value = 1;

        Assert.AreEqual("PUNTOS", Invocar<string>("ObtenerTipoFinalizacion"));
        Assert.IsFalse(duracion.interactable);
        Assert.IsTrue(puntos.interactable);
        Assert.AreEqual(200, Invocar<int>("ObtenerPuntaje"));
    }

    [Test]
    public void US08_Negativa_OpcionSinNumero_DevuelveCero()
    {
        duracion.ClearOptions();
        duracion.AddOptions(new System.Collections.Generic.List<string> { "Sin duración" });

        Assert.AreEqual(0, Invocar<int>("ObtenerDuracion"));
    }

    [Test]
    public void US08_Negativa_ReferenciasObligatoriasFaltantes_EmiteError()
    {
        raiz.SetActive(false);
        Campo(configuracion.GetType(), "duracion").SetValue(configuracion, null);

        LogAssert.Expect(LogType.Error, "Faltan referencias de configuración.");
        configuracion.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(configuracion, null);
    }

    [Test]
    public void US04_Positiva_JsonDeEstadisticas_LeeLogrosObtenidos()
    {
        const string json = "{\"partidasJugadas\":4,\"partidasGanadas\":2,\"logros\":[{\"idLogro\":1,\"nombre\":\"Primeros pasos\"},{\"idLogro\":2,\"nombre\":\"Primera victoria\"}]}";
        Assert.AreEqual(2, CantidadLogrosDesdeJson(json));
    }

    [Test]
    public void US04_Negativa_JsonSinLogros_NoInventaLogros()
    {
        const string json = "{\"partidasJugadas\":0,\"partidasGanadas\":0}";
        Assert.AreEqual(0, CantidadLogrosDesdeJson(json));
    }

    // Prueba la lógica real del componente sin depender del despacho de eventos UI en EditMode.
    private void ActualizarModo(int opcion)
    {
        var metodo = configuracion.GetType().GetMethod(
            "ActualizarModo", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(metodo, "No se encontró ActualizarModo en ConfiguracionPartidaUI.");
        metodo.Invoke(configuracion, new object[] { opcion });
    }

    private TMP_Dropdown CrearDropdown(string nombre, params string[] opciones)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(raiz.transform, false);
        var dropdown = objeto.AddComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(opciones));
        dropdown.value = 0;
        return dropdown;
    }

    private static Type TipoJuego(string nombre)
    {
        var tipo = Type.GetType(nombre + ", Assembly-CSharp");
        Assert.NotNull(tipo, "No se encontró " + nombre + " en Assembly-CSharp. Revisá el nombre o ensamblado del script.");
        return tipo;
    }

    private static FieldInfo Campo(Type tipo, string nombre)
    {
        var campo = tipo.GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(campo, "No se encontró el campo " + nombre + " en " + tipo.Name);
        return campo;
    }

    private T Invocar<T>(string metodo)
    {
        var info = configuracion.GetType().GetMethod(metodo, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(info, "No se encontró el método " + metodo);
        return (T)info.Invoke(configuracion, null);
    }

    private static int CantidadLogrosDesdeJson(string json)
    {
        Type estadisticas = TipoJuego("CargarEstadisticas")
            .GetNestedType("EstadisticasDto", BindingFlags.NonPublic);
        Assert.NotNull(estadisticas, "No existe EstadisticasDto.");

        object datos = JsonUtility.FromJson(json, estadisticas);
        var campoLogros = Campo(estadisticas, "logros");
        var logros = campoLogros.GetValue(datos) as Array;
        return logros == null ? 0 : logros.Length;
    }
}
