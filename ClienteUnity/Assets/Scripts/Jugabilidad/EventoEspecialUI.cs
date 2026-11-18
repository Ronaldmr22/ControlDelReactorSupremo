using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventoEspecialUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject panelEvento;
    [SerializeField] private TMP_Text textoEvento;
    [SerializeField] private Image iconoEvento;

    [Header("Configuracion")]
    [SerializeField] private float duracionMensaje = 3f;

    [Header("Iconos")]
    [SerializeField] private Sprite iconoEnergiaAdicional;
    [SerializeField] private Sprite iconoGuardianes;

    private Coroutine rutinaMensaje;

    private void Awake()
    {
        if (panelEvento != null)
            panelEvento.SetActive(false);
    }

    public void MostrarEvento(string mensaje, Sprite icono)
    {
        if (panelEvento == null ||
            textoEvento == null ||
            iconoEvento == null)
        {
            Debug.LogError(
                "Faltan referencias en EventoEspecialUI.");
            return;
        }

        if (rutinaMensaje != null)
            StopCoroutine(rutinaMensaje);

        textoEvento.text = mensaje;

        iconoEvento.sprite = icono;
        iconoEvento.enabled = icono != null;

        panelEvento.SetActive(true);

        rutinaMensaje =
            StartCoroutine(OcultarMensaje());
    }

    public void MostrarEnergiaAdicional()
    {
        MostrarEvento(
            "¡ENERGÍA ADICIONAL!\n¡Recogé la cápsula y ganá 10 puntos!",
            iconoEnergiaAdicional
        );
    }

    public void MostrarOleadaGuardianes(int segundosRestantes)
    {
        MostrarEvento(
            $"¡ALERTA: 5 GUARDIANES EXTRA!\n¡Quedan {segundosRestantes} segundos!",
            iconoGuardianes
        );
    }

    public void MostrarFinOleadaGuardianes()
    {
        MostrarEvento("¡OLEADA FINALIZADA!", iconoGuardianes);
    }

    private IEnumerator OcultarMensaje()
    {
        yield return new WaitForSeconds(duracionMensaje);

        panelEvento.SetActive(false);

        rutinaMensaje = null;
    }
}
