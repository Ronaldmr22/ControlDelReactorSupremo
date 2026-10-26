using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Pruebas locales de componentes reales. No validan Azure ni hardware físico.
public class PruebasComponentesJuego
{
    private GameObject raiz;
    private readonly System.Collections.Generic.List<UnityEngine.Object> recursos = new();
    private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [SetUp]
    public void Preparar()
    {
        raiz = new GameObject("TestsComponentes");
        raiz.SetActive(false);
        InvocarEstatico("ContextoPartida", "Limpiar");
    }

    [TearDown]
    public void Limpiar()
    {
        InvocarEstatico("ContextoPartida", "Limpiar");
        if (raiz != null) UnityEngine.Object.DestroyImmediate(raiz);
        foreach (var r in recursos) if (r != null) UnityEngine.Object.DestroyImmediate(r);
        recursos.Clear();
    }

    [Test]
    public void US06_Positiva_AvatarGuardadoValido_SeMuestraEnPerfil()
    {
        var selector = Crear("SeleccionPersonaje");
        var imagen = CrearUI<Image>("ImagenAvatar");
        var sprite = SpritePrueba();
        Asignar(selector, "imagenPersonajeLobby", imagen);
        Asignar(selector, "personajes", new Sprite[] { sprite });
        Llamar(selector, "MostrarAvatarGuardado", 1);
        Assert.AreSame(sprite, imagen.sprite);
        Assert.IsTrue(imagen.preserveAspect);
    }

    [Test]
    public void US06_Negativa_AvatarFueraDeRango_NoReemplazaElActual()
    {
        var selector = Crear("SeleccionPersonaje");
        var imagen = CrearUI<Image>("ImagenAvatar");
        var sprite = SpritePrueba();
        imagen.sprite = sprite;
        Asignar(selector, "imagenPersonajeLobby", imagen);
        Asignar(selector, "personajes", new Sprite[] { sprite });
        LogAssert.Expect(LogType.Warning, "Avatar guardado inválido: 99");
        Llamar(selector, "MostrarAvatarGuardado", 99);
        Assert.AreSame(sprite, imagen.sprite);
    }

    [Test]
    public void US09_Positiva_ContextoConDosParticipantes_AsignaIdentidadesDistintas()
    {
        var sala = Sala(10, 20);
        LlamarEstatico("ContextoPartida", "Guardar", sala);
        Assert.AreEqual(10, Prop("ContextoPartida", "IdJugador1"));
        Assert.AreEqual(20, Prop("ContextoPartida", "IdJugador2"));
        Assert.AreEqual(0, Prop("ContextoPartida", "IdJugador3"));
        Assert.AreEqual("SALA", Prop("ContextoPartida", "CodigoSala"));
    }

    [Test]
    public void US09_Negativa_SinOtrosParticipantes_NoInventaSegundoJugador()
    {
        LlamarEstatico("ContextoPartida", "Guardar", Sala(10));
        Assert.AreEqual(0, Prop("ContextoPartida", "IdJugador2"));
        Assert.AreEqual(0, Prop("ContextoPartida", "IdJugador3"));
    }

    [Test]
    public void US10_Positiva_ContextoDeTresPersonas_NoDuplicaIdentidades()
    {
        LlamarEstatico("ContextoPartida", "Guardar", Sala(10, 20, 30));
        Assert.AreEqual(20, Prop("ContextoPartida", "IdJugador2"));
        Assert.AreEqual(30, Prop("ContextoPartida", "IdJugador3"));
    }

    [Test]
    public void US10_Negativa_ParticipanteDuplicado_NoOcupaOtraPlaza()
    {
        LlamarEstatico("ContextoPartida", "Guardar", Sala(10, 20, 20));
        Assert.AreEqual(20, Prop("ContextoPartida", "IdJugador2"));
        Assert.AreEqual(0, Prop("ContextoPartida", "IdJugador3"));
    }

    [Test]
    public void US11_Positiva_MovimientoHorizontal_ActualizaSpriteDerecho()
    {
        var jugador = Crear("MovimientoJugador", typeof(Rigidbody2D), typeof(SpriteRenderer));
        var abajo = SpritePrueba(); var derecha = SpritePrueba();
        Llamar(jugador, "ConfigurarSprites", abajo, abajo, abajo, derecha);
        Llamar(jugador, "ActualizarDireccionVisual", Vector2.right);
        Assert.AreSame(derecha, jugador.GetComponent<SpriteRenderer>().sprite);
    }

    [Test]
    public void US11_Negativa_SinMovimiento_MantieneLaUltimaOrientacion()
    {
        var jugador = Crear("MovimientoJugador", typeof(Rigidbody2D), typeof(SpriteRenderer));
        var abajo = SpritePrueba(); var derecha = SpritePrueba();
        Llamar(jugador, "ConfigurarSprites", abajo, abajo, abajo, derecha);
        Llamar(jugador, "ActualizarDireccionVisual", Vector2.right);
        Llamar(jugador, "ActualizarDireccionVisual", Vector2.zero);
        Assert.AreSame(derecha, jugador.GetComponent<SpriteRenderer>().sprite);
    }

    [Test]
    public void US13_Positiva_MensajeRemoto_ActualizaPosicionCorrecta()
    {
        var sincronizador = Crear("SincronizacionJugadores");
        var generador = Crear("GeneradorJugadores");
        Asignar(sincronizador, "generador", generador);
        var remoto = new GameObject("Remoto"); remoto.transform.SetParent(raiz.transform);
        var diccionario = (IDictionary)Campo(generador.GetType(), "jugadoresRemotos").GetValue(generador);
        diccionario.Add("id-remoto", remoto.transform);
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"room_subscribed\",\"playerId\":\"mi-id\"}");
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"player_moved\",\"playerId\":\"id-remoto\",\"x\":4,\"y\":7}");
        Assert.AreEqual(new Vector3(4,7,0), remoto.transform.position);
    }

    [Test]
    public void US13_Negativa_MensajePropio_NoMueveJugadorRemoto()
    {
        var sincronizador = Crear("SincronizacionJugadores");
        var generador = Crear("GeneradorJugadores");
        Asignar(sincronizador, "generador", generador);
        var remoto = new GameObject("Remoto"); remoto.transform.SetParent(raiz.transform);
        var diccionario = (IDictionary)Campo(generador.GetType(), "jugadoresRemotos").GetValue(generador);
        diccionario.Add("mi-id", remoto.transform);
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"room_subscribed\",\"playerId\":\"mi-id\"}");
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"player_moved\",\"playerId\":\"mi-id\",\"x\":4,\"y\":7}");
        Assert.AreEqual(Vector3.zero, remoto.transform.position);
    }

    [Test]
    public void US14_Positiva_AccionRemota_MuestraTextoSobreAvatar()
    {
        var sincronizador = Crear("SincronizacionJugadores");
        var generador = Crear("GeneradorJugadores");
        Asignar(sincronizador, "generador", generador);
        var remoto = new GameObject("AvatarRemoto"); remoto.transform.SetParent(raiz.transform);
        remoto.SetActive(false);
        var mensaje = (Component)remoto.AddComponent(Tipo("MensajeSobreAvatar"));
        var texto = remoto.AddComponent<TextMeshPro>();
        Asignar(mensaje, "texto", texto);
        var diccionario = (IDictionary)Campo(generador.GetType(), "jugadoresRemotos").GetValue(generador);
        diccionario.Add("id-remoto", remoto.transform);
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"room_subscribed\",\"playerId\":\"mi-id\"}");
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"player_action\",\"playerId\":\"id-remoto\"}");
        Assert.AreEqual("Acción", texto.text);
    }

    [Test]
    public void US14_Negativa_AccionPropia_NoSeReplicaComoAccionRemota()
    {
        var sincronizador = Crear("SincronizacionJugadores");
        var generador = Crear("GeneradorJugadores");
        Asignar(sincronizador, "generador", generador);
        var remoto = new GameObject("Avatar"); remoto.transform.SetParent(raiz.transform); remoto.SetActive(false);
        var mensaje = (Component)remoto.AddComponent(Tipo("MensajeSobreAvatar"));
        var texto = remoto.AddComponent<TextMeshPro>(); texto.text = "Antes";
        Asignar(mensaje, "texto", texto);
        var diccionario = (IDictionary)Campo(generador.GetType(), "jugadoresRemotos").GetValue(generador);
        diccionario.Add("mi-id", remoto.transform);
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"room_subscribed\",\"playerId\":\"mi-id\"}");
        Llamar(sincronizador, "ProcesarMensaje", "{\"type\":\"player_action\",\"playerId\":\"mi-id\"}");
        Assert.AreEqual("Antes", texto.text);
    }

    [Test]
    public void US15_Positiva_MensajePuntaje_ActualizaMarcadorDelJugador()
    {
        LlamarEstatico("ContextoPartida", "Guardar", Sala(10,20));
        var marcador = Crear("MarcadorPartidaUI");
        var a = CrearUI<TextMeshProUGUI>("J1"); var b = CrearUI<TextMeshProUGUI>("J2");
        Asignar(marcador, "textosJugadores", new TMP_Text[] { a, b });
        Llamar(marcador, "InicializarMarcador");
        Llamar(marcador, "ProcesarMensaje", "{\"type\":\"score_updated\",\"idUsuario\":20,\"puntaje\":15}");
        StringAssert.Contains("15 pts", b.text);
        StringAssert.Contains("0 pts", a.text);
    }

    [Test]
    public void US15_Negativa_PuntajeUsuarioDesconocido_NoModificaMarcador()
    {
        LlamarEstatico("ContextoPartida", "Guardar", Sala(10,20));
        var marcador = Crear("MarcadorPartidaUI");
        var a = CrearUI<TextMeshProUGUI>("J1"); var b = CrearUI<TextMeshProUGUI>("J2");
        Asignar(marcador, "textosJugadores", new TMP_Text[] { a, b });
        Llamar(marcador, "InicializarMarcador");
        Llamar(marcador, "ProcesarMensaje", "{\"type\":\"score_updated\",\"idUsuario\":999,\"puntaje\":99}");
        StringAssert.Contains("0 pts", a.text);
        StringAssert.Contains("0 pts", b.text);
    }

    [Test]
    public void US17_Positiva_ResultadoValido_MuestraGanadorYPuntos()
    {
        var vista = Crear("VictoriaUI");
        var titulo = CrearUI<TextMeshProUGUI>("Titulo"); var nombre = CrearUI<TextMeshProUGUI>("Nombre");
        var puntos = CrearUI<TextMeshProUGUI>("Puntos"); var imagen = CrearUI<Image>("Ganador");
        Asignar(vista,"txtTitulo",titulo); Asignar(vista,"txtNombreGanador",nombre);
        Asignar(vista,"txtPuntos",puntos); Asignar(vista,"imagenGanador",imagen);
        Asignar(vista,"personajes",new Sprite[] { SpritePrueba() });
        var dto = Activator.CreateInstance(Tipo("DatosResultadoPartida"));
        var ganador = Activator.CreateInstance(Tipo("GanadorPartida"));
        Campo(ganador.GetType(),"nombreUsuario").SetValue(ganador,"Ana");
        Campo(ganador.GetType(),"puntaje").SetValue(ganador,125);
        Campo(ganador.GetType(),"avatarSeleccionado").SetValue(ganador,1);
        Campo(dto.GetType(),"ganador").SetValue(dto,ganador);
        Llamar(vista,"MostrarResultado",dto);
        Assert.AreEqual("Ana",nombre.text);
        StringAssert.Contains("125",puntos.text);
        Assert.IsTrue(imagen.enabled);
    }

    [Test]
    public void US17_Negativa_ResultadoNoDisponible_MuestraErrorSinAvatar()
    {
        var vista = Crear("VictoriaUI");
        var titulo = CrearUI<TextMeshProUGUI>("Titulo"); var nombre = CrearUI<TextMeshProUGUI>("Nombre");
        var puntos = CrearUI<TextMeshProUGUI>("Puntos"); var imagen = CrearUI<Image>("Ganador");
        Asignar(vista,"txtTitulo",titulo); Asignar(vista,"txtNombreGanador",nombre);
        Asignar(vista,"txtPuntos",puntos); Asignar(vista,"imagenGanador",imagen);
        LogAssert.Expect(LogType.Warning,"Sin conexión");
        Llamar(vista,"MostrarError","Sin conexión");
        Assert.AreEqual("Resultado no disponible",titulo.text);
        Assert.AreEqual("Sin conexión",nombre.text);
        Assert.IsFalse(imagen.enabled);
    }

    private Component Crear(string tipo, params Type[] componentes)
    {
        var objeto = new GameObject(tipo, componentes);
        objeto.transform.SetParent(raiz.transform, false);
        return objeto.AddComponent(Tipo(tipo));
    }
    private T CrearUI<T>(string nombre) where T: Component
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(raiz.transform, false);
        return objeto.AddComponent<T>();
    }
    private Sprite SpritePrueba()
    {
        var textura = new Texture2D(2,2);
        recursos.Add(textura);
        var sprite = Sprite.Create(textura, new Rect(0,0,2,2),new Vector2(0.5f,0.5f));
        recursos.Add(sprite);
        return sprite;
    }
    private static Type Tipo(string nombre)
    {
        var tipo = Type.GetType(nombre+", Assembly-CSharp");
        Assert.IsNotNull(tipo,"No existe el script "+nombre+" en Assembly-CSharp.");
        return tipo;
    }
    private static FieldInfo Campo(Type tipo,string nombre)
    {
        var campo=tipo.GetField(nombre,Inst);
        Assert.IsNotNull(campo,"No existe "+tipo.Name+"."+nombre);
        return campo;
    }
    private static void Asignar(Component objeto,string campo,object valor) => Campo(objeto.GetType(),campo).SetValue(objeto,valor);
    private static object Prop(string tipo,string nombre) => Tipo(tipo).GetProperty(nombre,BindingFlags.Public|BindingFlags.Static).GetValue(null);
    private static void LlamarEstatico(string tipo,string metodo,params object[] args) => Tipo(tipo).GetMethod(metodo,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
    private static void InvocarEstatico(string tipo,string metodo) => LlamarEstatico(tipo,metodo);
    private static void Llamar(Component c,string metodo,params object[] args)
    {
        var m=c.GetType().GetMethod(metodo,Inst);
        Assert.IsNotNull(m,"No existe "+c.GetType().Name+"."+metodo);
        m.Invoke(c,args);
    }
    private static object Sala(params int[] ids)
    {
        var sala = Activator.CreateInstance(Tipo("DatosSala"));
        Campo(sala.GetType(),"idPartida").SetValue(sala,101);
        Campo(sala.GetType(),"codigoSala").SetValue(sala,"SALA");
        Campo(sala.GetType(),"idUsuarioCreador").SetValue(sala,10);
        Campo(sala.GetType(),"tipoFinalizacion").SetValue(sala,"PUNTOS");
        Campo(sala.GetType(),"puntajeObjetivo").SetValue(sala,100);
        var participanteTipo=Tipo("ParticipanteSala");
        var participantes=Array.CreateInstance(participanteTipo,ids.Length);
        for(int i=0;i<ids.Length;i++)
        {
            var p=Activator.CreateInstance(participanteTipo);
            Campo(participanteTipo,"idUsuario").SetValue(p,ids[i]);
            Campo(participanteTipo,"nombreUsuario").SetValue(p,"Usuario"+ids[i]);
            Campo(participanteTipo,"avatarSeleccionado").SetValue(p,i+1);
            participantes.SetValue(p,i);
        }
        Campo(sala.GetType(),"participantes").SetValue(sala,participantes);
        return sala;
    }
}
