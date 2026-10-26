
using UnityEngine;
using TMPro;

public class ConfiguracionPartidaUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown formaFinalizar;
    [SerializeField] private TMP_Dropdown duracion;
    [SerializeField] private TMP_Dropdown puntajeObjetivo;
    [SerializeField] private TMP_Dropdown tiempoParalisis;
    [SerializeField] private TMP_Text textoError;

    private void OnEnable()
    {
        if (formaFinalizar == null || duracion == null ||
            puntajeObjetivo == null || tiempoParalisis == null)
        {
            Debug.LogError("Faltan referencias de configuración.", this);
            return;
        }

        formaFinalizar.onValueChanged.RemoveListener(ActualizarModo);
        formaFinalizar.onValueChanged.AddListener(ActualizarModo);
        ActualizarModo(formaFinalizar.value);
    }

    private void OnDisable()
    {
        if (formaFinalizar != null)
            formaFinalizar.onValueChanged.RemoveListener(ActualizarModo);
    }

    private void ActualizarModo(int opcion)
    {
        bool porTiempo = opcion == 0;

        duracion.interactable = porTiempo;
        puntajeObjetivo.interactable = !porTiempo;
    }

    public string ObtenerTipoFinalizacion()
    {
        return formaFinalizar.value == 0 ? "TIEMPO" : "PUNTOS";
    }

    public int ObtenerDuracion()
    {
        return LeerNumero(duracion);
    }

    public int ObtenerPuntaje()
    {
        return LeerNumero(puntajeObjetivo);
    }

    public int ObtenerTiempoParalisis()
    {
        return LeerNumero(tiempoParalisis);
    }

    // Lee el número inicial de opciones como "100 puntos".
    private int LeerNumero(TMP_Dropdown dropdown)
    {
        string texto = dropdown.options[dropdown.value].text;
        string numero = "";

        foreach (char caracter in texto)
        {
            if (char.IsDigit(caracter))
                numero += caracter;
            else if (numero.Length > 0)
                break;
        }

        return int.TryParse(numero, out int resultado) ? resultado : 0;
    }

    public void MostrarError(string error)
    {
        if (textoError != null)
            textoError.text = error;
    }

    public void LimpiarError()
    {
        if (textoError != null)
            textoError.text = "";
    }
}
