using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class ObjetivoPartidaUI : MonoBehaviour
{
    [SerializeField] private TMP_Text textoObjetivo;

    private bool esPorTiempo;
    private DateTime fechaInicioUtc;
    private int limiteTiempo;

    private void Start()
    {
        esPorTiempo = ContextoPartida.TipoFinalizacion == "TIEMPO";

        if (esPorTiempo)
        {
            limiteTiempo = ContextoPartida.LimiteTiempo;

            if (!DateTime.TryParse(
                ContextoPartida.FechaInicio,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out fechaInicioUtc))
            {
                Debug.LogError("No se pudo leer la fecha de inicio.");
                textoObjetivo.text = "Tiempo no disponible";
                enabled = false;
                return;
            }
        }
        else
        {
            textoObjetivo.text =
                "Meta: " + ContextoPartida.PuntajeObjetivo + " pts";
        }
    }

    private void Update()
    {
        if (!esPorTiempo)
            return;

        double segundosTranscurridos =
            (DateTime.UtcNow - fechaInicioUtc).TotalSeconds;

        double segundosRestantes =
            Math.Max(0, limiteTiempo - segundosTranscurridos);

        int segundos = (int)Math.Ceiling(segundosRestantes);

        int minutos = segundos / 60;
        int segundosDelMinuto = segundos % 60;

        textoObjetivo.text =
            $"Tiempo: {minutos:00}:{segundosDelMinuto:00}";
    }
}
