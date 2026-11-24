
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class ReactorCentral : MonoBehaviour
{
    [Header("Energia del reactor")]
    [SerializeField] private int energiaMaxima = 100;
    [SerializeField] private int energiaActual = 100;

    [Header("Interfaz")]
    [SerializeField] private Slider barraEnergia;

    [Header("Apariencia")]
    [SerializeField] private Color colorDisponible = Color.white;
    [SerializeField] private Color colorAgotado = Color.gray;
    [SerializeField] private Color colorSobrecarga = new Color(1f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color colorPuntosDobles = new Color(0.1f, 0.7f, 1f, 1f);

    private SpriteRenderer imagen;
    private bool enSobrecarga;
    private bool puntosDobles;

    public int EnergiaActual => energiaActual;
    public int EnergiaMaxima => energiaMaxima;
    public bool EnSobrecarga => enSobrecarga;
    public bool PuntosDobles => puntosDobles;
    public bool Disponible => energiaActual > 0 && !enSobrecarga;

    private void Awake()
    {
        imagen = GetComponent<SpriteRenderer>();
        ActualizarVisual();
    }

    private void OnValidate()
    {
        energiaMaxima = Mathf.Max(1, energiaMaxima);
        energiaActual = Mathf.Clamp(
            energiaActual, 0, energiaMaxima
        );
    }

    public void ActualizarEnergia(int nuevaEnergia)
    {
        energiaActual = Mathf.Clamp(
            nuevaEnergia, 0, energiaMaxima
        );

        ActualizarVisual();
    }

    public void EstablecerSobrecarga(bool activa)
    {
        if (enSobrecarga == activa)
            return;

        enSobrecarga = activa;
        ActualizarVisual();
    }

    public void EstablecerPuntosDobles(bool activa)
    {
        puntosDobles = activa;
        ActualizarVisual();
    }

    private void ActualizarVisual()
    {

        if (barraEnergia != null)
        {
            barraEnergia.minValue = 0;
            barraEnergia.maxValue = energiaMaxima;
            barraEnergia.wholeNumbers = true;

            barraEnergia.SetValueWithoutNotify(energiaActual);
        }

        if (imagen != null)
        {
            imagen.color = enSobrecarga
                ? colorSobrecarga
                : energiaActual <= 0 ? colorAgotado
                : puntosDobles ? colorPuntosDobles : colorDisponible;
        }
    }


    [ContextMenu("Probar extraccion de 10")]
    private void ProbarExtraccion()
    {
        ActualizarEnergia(energiaActual - 10);
    }

    [ContextMenu("Restablecer energia")]
    private void RestablecerEnergia()
    {
        ActualizarEnergia(energiaMaxima);
    }
}
