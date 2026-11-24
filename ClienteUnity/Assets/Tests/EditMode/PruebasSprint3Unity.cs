using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas del cliente para historias del Sprint 3. Usan reflexión, igual que
// PruebasComponentesJuego, porque los scripts del juego están en Assembly-CSharp.
// No validan el servidor ni el control físico.
public class PruebasSprint3Unity
{
    private GameObject raiz;
    private readonly List<UnityEngine.Object> creados = new();
    private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [SetUp]
    public void Preparar()
    {
        raiz = new GameObject("TestsSprint3");
        raiz.SetActive(false);
    }

    [TearDown]
    public void Limpiar()
    {
        if (raiz != null) UnityEngine.Object.DestroyImmediate(raiz);
        foreach (var o in creados) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        creados.Clear();
    }

    // ---------- US-20 Trampas en el cliente ----------

    [Test]
    public void US20_Positiva_TrapsSpawned_InstanciaCadaTrampaSegunKind()
    {
        var trampas = CrearEn(raiz, "TrampasCliente");
        var celda = new GameObject("PrefabCelda"); creados.Add(celda);
        var invertida = new GameObject("PrefabInvertida"); creados.Add(invertida);
        Asignar(trampas, "prefabCeldaElectrica", celda);
        Asignar(trampas, "prefabControlesInvertidos", invertida);

        Llamar(trampas, "ProcesarMensaje",
            "{\"type\":\"traps_spawned\",\"roomCode\":\"123456\",\"traps\":[" +
            "{\"kind\":0,\"x\":1.5,\"y\":2.0},{\"kind\":1,\"x\":-3.0,\"y\":4.0}]}");

        Assert.AreEqual(2, trampas.transform.childCount);
        var primera = trampas.transform.GetChild(0);
        var segunda = trampas.transform.GetChild(1);
        StringAssert.StartsWith("PrefabCelda", primera.name);
        StringAssert.StartsWith("PrefabInvertida", segunda.name);
        Assert.AreEqual(new Vector3(1.5f, 2f, 0f), primera.position);
        Assert.AreEqual(new Vector3(-3f, 4f, 0f), segunda.position);
    }

    [Test]
    public void US20_Negativa_KindDesconocido_NoInstanciaNada()
    {
        var trampas = CrearEn(raiz, "TrampasCliente");
        var celda = new GameObject("PrefabCelda"); creados.Add(celda);
        Asignar(trampas, "prefabCeldaElectrica", celda);
        LogAssert.Expect(LogType.Warning, "Prefab no configurado para trampa 5");

        Llamar(trampas, "ProcesarMensaje",
            "{\"type\":\"traps_spawned\",\"roomCode\":\"123456\",\"traps\":[{\"kind\":5,\"x\":0,\"y\":0}]}");

        Assert.AreEqual(0, trampas.transform.childCount);
    }

    [Test]
    public void US20_Negativa_OtroTipoDeMensaje_SeIgnora()
    {
        var trampas = CrearEn(raiz, "TrampasCliente");
        var celda = new GameObject("PrefabCelda"); creados.Add(celda);
        Asignar(trampas, "prefabCeldaElectrica", celda);

        Llamar(trampas, "ProcesarMensaje",
            "{\"type\":\"player_moved\",\"traps\":[{\"kind\":0,\"x\":0,\"y\":0}]}");

        Assert.AreEqual(0, trampas.transform.childCount);
    }

    // ---------- US-21 / US-22 Movimiento con estados temporales ----------

    [Test]
    public void US21_Positiva_MovimientoNormal_AplicaVelocidadBase()
    {
        var (mov, cuerpo) = CrearJugador();
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));

        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(3f, cuerpo.linearVelocity.x, 0.001f);
        Assert.AreEqual(0f, cuerpo.linearVelocity.y, 0.001f);
    }

    [Test]
    public void US22_Positiva_ArenaMovediza_ReduceLaVelocidadAl60PorCiento()
    {
        var (mov, cuerpo) = CrearJugador();
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));
        Llamar(mov, "EstablecerArenaMovediza", true);

        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(1.8f, cuerpo.linearVelocity.x, 0.001f);
    }

    [Test]
    public void US21_Negativa_JugadorCongelado_NoSeMueve()
    {
        var (mov, cuerpo) = CrearJugador();
        var estrellas = new GameObject("Estrellas"); creados.Add(estrellas);
        estrellas.SetActive(false);
        Asignar(mov, "efectoEstrellas", estrellas);
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));

        Llamar(mov, "EstablecerCongelado", true);
        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(Vector2.zero, cuerpo.linearVelocity);
        Assert.AreEqual(Vector2.zero, (Vector2)LeerCampo(mov, "movimiento"));
        Assert.IsTrue(estrellas.activeSelf);
    }

    [Test]
    public void US21_Positiva_AlDescongelar_SeOcultaElEfecto()
    {
        var (mov, _) = CrearJugador();
        var estrellas = new GameObject("Estrellas"); creados.Add(estrellas);
        Asignar(mov, "efectoEstrellas", estrellas);

        Llamar(mov, "EstablecerCongelado", true);
        Llamar(mov, "EstablecerCongelado", false);

        Assert.IsFalse(estrellas.activeSelf);
        Assert.IsFalse((bool)LeerCampo(mov, "estaCongelado"));
    }

    [Test]
    public void US20_Positiva_TrampaControlesInvertidos_ActivaElEstado()
    {
        var (mov, _) = CrearJugador();

        Llamar(mov, "EstablecerControlesInvertidos", true);

        Assert.IsTrue((bool)LeerCampo(mov, "controlesInvertidos"));
    }

    // ---------- Pruebas agregadas (CA-20.5, CA-20.9) ----------

    // Tercer tipo de trampa (kind 2) que envía el servidor.
    [Test]
    public void US20_Positiva_TrampaCongelada_SeInstanciaConKind2()
    {
        var trampas = CrearEn(raiz, "TrampasCliente");
        var congelada = new GameObject("PrefabCongelada"); creados.Add(congelada);
        Asignar(trampas, "prefabTrampaCongelada", congelada);

        Llamar(trampas, "ProcesarMensaje",
            "{\"type\":\"traps_spawned\",\"roomCode\":\"123456\",\"traps\":[{\"kind\":2,\"x\":0.5,\"y\":-1.0}]}");

        Assert.AreEqual(1, trampas.transform.childCount);
        StringAssert.StartsWith("PrefabCongelada", trampas.transform.GetChild(0).name);
    }

    // DEF-02 (abierto, reportado al responsable de TrampasCliente): con un prefab faltante
    // se llama Instantiate(null). Quitar Ignore cuando se corrija.
    [Test]
    [Ignore("DEF-02 abierto: TrampasCliente no valida prefabs nulos.")]
    public void US20_Negativa_PrefabFaltante_NoImpideLasDemasTrampas()
    {
        var trampas = CrearEn(raiz, "TrampasCliente");
        var celda = new GameObject("PrefabCelda"); creados.Add(celda);
        Asignar(trampas, "prefabCeldaElectrica", celda);
        LogAssert.Expect(LogType.Warning, "Prefab no configurado para trampa 2");

        Llamar(trampas, "ProcesarMensaje",
            "{\"type\":\"traps_spawned\",\"traps\":[{\"kind\":2,\"x\":0,\"y\":0},{\"kind\":0,\"x\":1,\"y\":1}]}");

        Assert.AreEqual(1, trampas.transform.childCount);
        StringAssert.StartsWith("PrefabCelda", trampas.transform.GetChild(0).name);
    }

    // CA-20.5: la trampa congelada reduce la velocidad al 60 %.
    [Test]
    public void US20_Positiva_TrampaCongelada_ReduceLaVelocidad()
    {
        var (mov, cuerpo) = CrearJugador();
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));
        Llamar(mov, "EstablecerTrampaCongelada", true);

        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(1.8f, cuerpo.linearVelocity.x, 0.001f);
    }

    // Dos efectos lentos a la vez no se multiplican: se aplica el más fuerte.
    [Test]
    public void US20_Negativa_ArenaYTrampaJuntas_NoAcumulanReduccion()
    {
        var (mov, cuerpo) = CrearJugador();
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));
        Llamar(mov, "EstablecerArenaMovediza", true);
        Llamar(mov, "EstablecerTrampaCongelada", true);

        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(1.8f, cuerpo.linearVelocity.x, 0.001f);
    }

    // CA-20.9: la celda eléctrica impide que el avatar continúe su camino.
    [Test]
    public void US20_Positiva_CeldaElectrica_DetieneAlJugador()
    {
        var (mov, cuerpo) = CrearJugador();
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));

        Llamar(mov, "EstablecerCeldaElectrica", true);
        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(Vector2.zero, cuerpo.linearVelocity);
    }

    // CA-20.9: al terminar la celda el jugador recupera el movimiento.
    [Test]
    public void US20_Positiva_FinDeCeldaElectrica_RecuperaMovimiento()
    {
        var (mov, cuerpo) = CrearJugador();
        Llamar(mov, "EstablecerCeldaElectrica", true);
        Llamar(mov, "EstablecerCeldaElectrica", false);
        AsignarCampo(mov, "movimiento", new Vector2(1f, 0f));

        Llamar(mov, "FixedUpdate");

        Assert.AreEqual(3f, cuerpo.linearVelocity.x, 0.001f);
    }

    // ---------- Utilidades ----------

    // Jugador activo con Rigidbody2D: en EditMode Awake se invoca explícitamente.
    private (Component Mov, Rigidbody2D Cuerpo) CrearJugador()
    {
        var objeto = new GameObject("Jugador", typeof(Rigidbody2D), typeof(SpriteRenderer));
        creados.Add(objeto);
        var cuerpo = objeto.GetComponent<Rigidbody2D>();
        cuerpo.gravityScale = 0f;
        var mov = objeto.AddComponent(Tipo("MovimientoJugador"));
        Llamar(mov, "Awake");
        return (mov, cuerpo);
    }

    private static Component CrearEn(GameObject padre, string tipo)
    {
        var objeto = new GameObject(tipo);
        objeto.transform.SetParent(padre.transform, false);
        return objeto.AddComponent(Tipo(tipo));
    }

    private static Type Tipo(string nombre)
    {
        var tipo = Type.GetType(nombre + ", Assembly-CSharp");
        Assert.IsNotNull(tipo, "No existe el script " + nombre + " en Assembly-CSharp.");
        return tipo;
    }

    private static FieldInfo Campo(Type tipo, string nombre)
    {
        var campo = tipo.GetField(nombre, Inst);
        Assert.IsNotNull(campo, "No existe " + tipo.Name + "." + nombre);
        return campo;
    }

    private static void Asignar(Component c, string campo, object valor) => Campo(c.GetType(), campo).SetValue(c, valor);
    private static void AsignarCampo(Component c, string campo, object valor) => Asignar(c, campo, valor);
    private static object LeerCampo(Component c, string campo) => Campo(c.GetType(), campo).GetValue(c);

    private static void Llamar(Component c, string metodo, params object[] args)
    {
        var m = c.GetType().GetMethod(metodo, Inst);
        Assert.IsNotNull(m, "No existe " + c.GetType().Name + "." + metodo);
        m.Invoke(c, args);
    }
}
