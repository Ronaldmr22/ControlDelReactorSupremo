using System.Collections.Generic;
using UnityEngine;

// Se usa tanto para puertas como para barreras con un collider rectangular.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ObstaculoArena : MonoBehaviour
{
    [SerializeField] private bool iniciaCerrado = true;
    [SerializeField] private GameObject visualCerrado;
    [SerializeField] private GameObject visualAbierto;

    private readonly List<Collider2D> contactos = new();
    private BoxCollider2D bloqueo;
    private bool cierrePendiente;

    public bool Cerrado => bloqueo.enabled;

    private void Awake()
    {
        bloqueo = GetComponent<BoxCollider2D>();
        bloqueo.isTrigger = false;
        AplicarEstado(false);
        SolicitarEstado(iniciaCerrado);
    }

    // Puede llamarse desde un evento de Unity o desde la lógica de la partida.
    public void SolicitarEstado(bool cerrar)
    {
        cierrePendiente = cerrar && !Cerrado;
        if (!cerrar)
            AplicarEstado(false);
    }

    [ContextMenu("Prueba / Abrir")]
    private void ProbarApertura()
    {
        if (Application.isPlaying)
            SolicitarEstado(false);
    }

    [ContextMenu("Prueba / Cerrar")]
    private void ProbarCierre()
    {
        if (Application.isPlaying)
            SolicitarEstado(true);
    }

    private void FixedUpdate()
    {
        if (!cierrePendiente || HuecoOcupado())
            return;

        cierrePendiente = false;
        AplicarEstado(true);
    }

    private bool HuecoOcupado()
    {
        var escala = transform.lossyScale;
        var tamano = Vector2.Scale(bloqueo.size,
            new Vector2(Mathf.Abs(escala.x), Mathf.Abs(escala.y)));
        var filtro = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapBox(transform.TransformPoint(bloqueo.offset),
            tamano, transform.eulerAngles.z, filtro, contactos);

        foreach (var contacto in contactos)
        {
            if (contacto.GetComponentInParent<MovimientoJugador>() != null)
                return true;
        }

        return false;
    }

    private void AplicarEstado(bool cerrado)
    {
        bloqueo.enabled = cerrado;
        if (visualCerrado != null)
            visualCerrado.SetActive(cerrado);
        if (visualAbierto != null)
            visualAbierto.SetActive(!cerrado);
    }
}
