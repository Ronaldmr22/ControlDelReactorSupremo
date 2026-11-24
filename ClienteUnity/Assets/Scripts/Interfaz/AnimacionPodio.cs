
using System.Collections;
using UnityEngine;

public class AnimacionPodio : MonoBehaviour
{
    [Header("Puestos del podio")]
    [SerializeField] private GameObject primerLugar;
    [SerializeField] private RectTransform trofeoGanador;
    [SerializeField] private ParticleSystem confetiVictoria;
    [SerializeField] private GameObject segundoLugar;
    [SerializeField] private GameObject tercerLugar;

    [Header("Configuración")]
    [SerializeField] private float duracion = 1.2f;
    [SerializeField] private float desplazamientoInicial = 120f;
    [SerializeField] private float esperaEntrePuestos = 0.4f;

    private Coroutine animacionActual;

    public void ReproducirAnimacion()
    {
        if (animacionActual != null)
            StopCoroutine(animacionActual);

        animacionActual = StartCoroutine(AnimarCeremonia());
    }

    private IEnumerator AnimarCeremonia()
    {
        // Guardamos las posiciones originales.
        GameObject[] puestos =
        {
            tercerLugar,
            segundoLugar,
            primerLugar
        };

        Vector2[] posiciones = new Vector2[puestos.Length];
        CanvasGroup[] grupos = new CanvasGroup[puestos.Length];

        for (int i = 0; i < puestos.Length; i++)
        {
            if (puestos[i] == null)
                continue;

            RectTransform rect = puestos[i].GetComponent<RectTransform>();
            posiciones[i] = rect.anchoredPosition;

            grupos[i] = puestos[i].GetComponent<CanvasGroup>();

            if (grupos[i] == null)
                grupos[i] = puestos[i].AddComponent<CanvasGroup>();

            if (puestos[i].activeSelf)
            {
                rect.anchoredPosition =
                    posiciones[i] + Vector2.down * desplazamientoInicial;

                grupos[i].alpha = 0f;
            }
        }

        // Revelamos los puestos uno por uno.
        for (int i = 0; i < puestos.Length; i++)
        {
            if (puestos[i] == null || !puestos[i].activeSelf)
                continue;

            yield return AnimarPuesto(
                puestos[i].GetComponent<RectTransform>(),
                grupos[i],
                posiciones[i]
            );

            if (puestos[i] == primerLugar)
            {
                // Primero, el campeón hace su animación especial.
                yield return AnimarCampeon(
                    puestos[i].GetComponent<RectTransform>()
                );

                // Después, baja el trofeo.
                yield return AnimarTrofeo();

                // Finalmente, explota el confeti.
                if (confetiVictoria != null)
                {
                    confetiVictoria.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear
                    );

                    confetiVictoria.Play();
                }
            }

            yield return new WaitForSecondsRealtime(esperaEntrePuestos);
        }

        animacionActual = null;
    }

    private IEnumerator AnimarPuesto(
        RectTransform rect,
        CanvasGroup grupo,
        Vector2 posicionFinal)
    {
        Vector2 posicionInicial =
            posicionFinal + Vector2.down * desplazamientoInicial;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracion);
            float suavizado = Mathf.SmoothStep(0f, 1f, progreso);

            rect.anchoredPosition =
                Vector2.Lerp(posicionInicial, posicionFinal, suavizado);

            grupo.alpha = suavizado;

            yield return null;
        }

        rect.anchoredPosition = posicionFinal;
        grupo.alpha = 1f;
    }
    private IEnumerator AnimarCampeon(RectTransform rect)
    {
        Vector3 escalaOriginal = rect.localScale;
        float tiempo = 0f;
        float duracionCelebracion = 0.6f;

        while (tiempo < duracionCelebracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / duracionCelebracion
            );

            float aumento = 1f + 0.12f * Mathf.Sin(progreso * Mathf.PI);

            rect.localScale = escalaOriginal * aumento;

            yield return null;
        }

        rect.localScale = escalaOriginal;
    }
    private IEnumerator AnimarTrofeo()
    {
        if (trofeoGanador == null)
            yield break;

        // Posición final configurada en el Inspector.
        Vector2 posicionFinal = trofeoGanador.anchoredPosition;

        // El trofeo comienza 350 unidades más arriba.
        Vector2 posicionInicial =
            posicionFinal + Vector2.up * 350f;

        trofeoGanador.anchoredPosition = posicionInicial;
        trofeoGanador.gameObject.SetActive(true);

        float duracionTrofeo = 1.5f;
        float tiempo = 0f;

        while (tiempo < duracionTrofeo)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracionTrofeo);
            float suavizado = Mathf.SmoothStep(0f, 1f, progreso);

            trofeoGanador.anchoredPosition =
                Vector2.Lerp(posicionInicial, posicionFinal, suavizado);

            yield return null;
        }

        trofeoGanador.anchoredPosition = posicionFinal;
    }
}