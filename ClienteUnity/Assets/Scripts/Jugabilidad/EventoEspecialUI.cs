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
    [SerializeField] private Color colorTextoSobrecarga = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Iconos")]
    [SerializeField] private Sprite iconoEnergiaAdicional;
    [SerializeField] private Sprite iconoGuardianes;
    [SerializeField] private Sprite iconoSobrecargaReactor;

    private Coroutine rutinaMensaje;
    private Color colorTextoOriginal;
    private FontStyles estiloTextoOriginal;

    private void Awake()
    {
        if (textoEvento != null)
        {
            colorTextoOriginal = textoEvento.color;
            estiloTextoOriginal = textoEvento.fontStyle;
        }

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
        textoEvento.color = colorTextoOriginal;
        textoEvento.fontStyle = estiloTextoOriginal;

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

    public void MostrarSobrecargaReactor(int segundosRestantes)
    {
        MostrarEvento(
            $"¡SOBRECARGA DEL REACTOR!\n¡No da puntos durante {segundosRestantes} segundos!",
            iconoSobrecargaReactor
        );

        if (textoEvento != null)
        {
            textoEvento.color = colorTextoSobrecarga;
            textoEvento.fontStyle |= FontStyles.Bold;
        }
    }

    public void MostrarFinSobrecargaReactor()
    {
        MostrarEvento("¡REACTOR REACTIVADO!", iconoSobrecargaReactor);
    }

    private IEnumerator OcultarMensaje()
    {
        yield return new WaitForSeconds(duracionMensaje);

        panelEvento.SetActive(false);

        rutinaMensaje = null;
    }
}
