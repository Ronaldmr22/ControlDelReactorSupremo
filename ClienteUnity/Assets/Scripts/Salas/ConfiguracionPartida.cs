using UnityEngine;
using TMPro;

public class ConfiguracionPartidaUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown formaFinalizar;
    [SerializeField] private TMP_Dropdown duracion;
    [SerializeField] private TMP_Dropdown puntajeObjetivo;
    private void OnEnable()
    {
        if (formaFinalizar == null || duracion == null || puntajeObjetivo == null)
        {
            Debug.LogError(
                $"Faltan referencias en {gameObject.name}: " +
                $"FormaFinalizar={formaFinalizar}, Duracion={duracion}, " +
                $"PuntajeObjetivo={puntajeObjetivo}", this);
            return;
        }

        ActualizarModo(formaFinalizar.value);
        formaFinalizar.onValueChanged.AddListener(ActualizarModo);
    }

    private void OnDisable()
    {
        if (formaFinalizar != null)
            formaFinalizar.onValueChanged.RemoveListener(ActualizarModo);
    }

    private void ActualizarModo(int opcion)
    {
        // Opción 0: por duración. Opción 1: por puntos.
        bool porTiempo = opcion == 0;

        duracion.interactable = porTiempo;
        puntajeObjetivo.interactable = !porTiempo;
    }
}
