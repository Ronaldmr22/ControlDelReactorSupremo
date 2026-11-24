using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MovimientoJugador : MonoBehaviour
{
    [SerializeField] private float velocidad = 3f;
    [Header("Eventos especiales")]
    [SerializeField, Range(0.1f, 1f)] private float factorArenaMovediza = 0.6f;
    private bool sobreArenaMovediza;

    [Header("Trampas")]
    private bool controlesInvertidos;

    [SerializeField, Range(0.1f, 1f)]
    private float factorTrampaCongelada = 0.6f;

    private bool sobreTrampaCongelada;
    private bool atrapadoCeldaElectrica;

    [SerializeField] private Sprite abajo;
    [SerializeField] private Sprite arriba;
    [SerializeField] private Sprite izquierda;
    [SerializeField] private Sprite derecha;
    private Rigidbody2D cuerpo;
    private SpriteRenderer imagen;
    private Vector2 movimiento;
    private bool estaCongelado;
    [Header("Efectos visuales")]
    [SerializeField] private GameObject efectoEstrellas;
    private MensajeSobreAvatar mensaje;

    public event Action AccionEjecutada;
    public event Action AccionFinalizada;
    private bool accionMantenida;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        imagen = GetComponent<SpriteRenderer>();
    }

    public void ConfigurarSprites(
    Sprite spriteAbajo,
    Sprite spriteArriba,
    Sprite spriteIzquierda,
    Sprite spriteDerecha)
    {
        abajo = spriteAbajo;
        arriba = spriteArriba;
        izquierda = spriteIzquierda;
        derecha = spriteDerecha;

        if (imagen == null)
            imagen = GetComponent<SpriteRenderer>();

        imagen.sprite = abajo;
    }

    private void Update()
    {
        if (estaCongelado || atrapadoCeldaElectrica)
        {
            movimiento = Vector2.zero;
            return;
        }
        Gamepad control = Gamepad.current;

        movimiento = control != null
            ? control.dpad.ReadValue()
            : Vector2.zero;

        // Trampa de controles invertidos.
        if (controlesInvertidos)
        {
            movimiento = -movimiento;
        }

        movimiento = Vector2.ClampMagnitude(movimiento, 1f);

        movimiento = Vector2.ClampMagnitude(movimiento, 1f);

        bool presionada = control != null &&
                            control.buttonSouth.isPressed;

        if (presionada && !accionMantenida)
        {
            accionMantenida = true;
            AccionEjecutada?.Invoke();
        }
        else if (!presionada && accionMantenida)
        {
            accionMantenida = false;
            AccionFinalizada?.Invoke();
        }


        ActualizarDireccionVisual(movimiento);
    }
    public void ActualizarDireccionVisual(Vector2 direccion)
    {
        if (direccion.sqrMagnitude < 0.000001f)
            return;

        if (imagen == null)
            imagen = GetComponent<SpriteRenderer>();

        Sprite siguiente;

        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y))
            siguiente = direccion.x > 0 ? derecha : izquierda;
        else
            siguiente = direccion.y > 0 ? arriba : abajo;

        if (siguiente != null)
            imagen.sprite = siguiente;
    }
    private void FixedUpdate()
    {
        if (estaCongelado || atrapadoCeldaElectrica)
        {
            cuerpo.linearVelocity = Vector2.zero;
            return;
        }

        float factorVelocidad = 1f;

        if (sobreArenaMovediza)
        {
            factorVelocidad = factorArenaMovediza;
        }

        if (sobreTrampaCongelada)
        {
            factorVelocidad = Mathf.Min(
                factorVelocidad,
                factorTrampaCongelada);
        }

        cuerpo.linearVelocity =
            movimiento * velocidad * factorVelocidad;
    }
    private void OnDisable()
    {

        if (accionMantenida)
        {
            accionMantenida = false;
            AccionFinalizada?.Invoke();
        }

        movimiento = Vector2.zero;

        if (cuerpo != null)
            cuerpo.linearVelocity = Vector2.zero;
    }
    public void EstablecerCongelado(bool congelado)
    {
        estaCongelado = congelado;

        if (efectoEstrellas != null)
        {
            efectoEstrellas.SetActive(congelado);
        }

        if (congelado)
        {
            movimiento = Vector2.zero;
            cuerpo.linearVelocity = Vector2.zero;
        }
    }

    public void EstablecerArenaMovediza(bool activa)
    {
        sobreArenaMovediza = activa;
    }
    public void EstablecerControlesInvertidos(bool invertidos)
    {
        controlesInvertidos = invertidos;
    }
        public void EstablecerTrampaCongelada(bool activa)
    {
        sobreTrampaCongelada = activa;
    }
    public void EstablecerCeldaElectrica(bool activa)
    {
        atrapadoCeldaElectrica = activa;

        if (activa)
        {
            movimiento = Vector2.zero;
            cuerpo.linearVelocity = Vector2.zero;
        }
    }
}

