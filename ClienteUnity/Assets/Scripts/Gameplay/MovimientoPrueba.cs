using UnityEngine;
using BattleArena.Core;

[RequireComponent(typeof(Rigidbody2D))]            // Exige que el objeto tenga Rigidbody2D
[RequireComponent(typeof(FuenteEntradaTeclado))]   // Exige la fuente de entrada (provisional)
[RequireComponent(typeof(MensajeSobreAvatar))] 
public class MovimientoPrueba : MonoBehaviour
{
    // Aparece en el Inspector para ajustarlo sin tocar el código
    [SerializeField] private float velocidad = 5f;

    private Rigidbody2D cuerpo;                // Cuerpo físico que se mueve y choca
    private FuenteEntradaTeclado entrada;      // De aquí salen los comandos
    private Vector2 direccion;                 // Par (x, y): hacia dónde quiere ir
    private MensajeSobreAvatar mensaje;

    // Unity la llama sola, una vez, al cargar el objeto.
    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();            // Busca el Rigidbody2D del mismo objeto
        entrada = GetComponent<FuenteEntradaTeclado>();  // Busca nuestra fuente en el mismo objeto (provisional)
        mensaje = GetComponent<MensajeSobreAvatar>();
    }

    // Unity la llama sola en cada cuadro.
    private void Update()
    {
        // Lee los ejes de la pizarra y arma la dirección deseada.
        direccion = new Vector2(entrada.Estado.EjeHorizontal, entrada.Estado.EjeVertical);

        // Si hay una acción pendiente, se ejecuta (una sola vez por pulsación).
        if (entrada.Estado.ConsumeAction())
        {
            EjecutarAccion();
        }
    }

    // Unity la llama sola a intervalos fijos, sincronizada con la física.
    private void FixedUpdate()
    {
        // normalized: la diagonal no es más rápida que ir en línea recta.
        cuerpo.linearVelocity = direccion.normalized * velocidad;
    }

    // Lo que pasa cuando el jugador pulsa Acción (Pendiente).
    private void EjecutarAccion()
    {
        Debug.Log("Acción ejecutada por " + entrada.Estado.PlayerId);
        mensaje.Mostrar("Acción");
        // AvisarAlOtroJugador();  // Cuando exista la red
    }
}