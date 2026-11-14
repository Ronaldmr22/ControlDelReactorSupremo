
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

    private SpriteRenderer imagen;

    public int EnergiaActual => energiaActual;
    public int EnergiaMaxima => energiaMaxima;
    public bool Disponible => energiaActual > 0;

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

    private void ActualizarVisual()
    {
        // Actualizar barra de energia
        if (barraEnergia != null)
        {
            barraEnergia.minValue = 0;
            barraEnergia.maxValue = energiaMaxima;
            barraEnergia.wholeNumbers = true;

            barraEnergia.SetValueWithoutNotify(energiaActual);
        }

        // Actualizar apariencia del reactor
        if (imagen != null)
        {
            imagen.color = Disponible
                ? colorDisponible
                : colorAgotado;
        }
    }

    // Metodos temporales para probar el reactor

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
