using System;
using UnityEngine;
using TMPro;
using BattleArena.Core;

public class PantallaConfiguracion : MonoBehaviour
{
    // Elementos de la pantalla. Si se usan otros se deben agregar.
    [SerializeField] private TMP_Dropdown dropdownModo;
    [SerializeField] private TMP_InputField campoMinutos;
    [SerializeField] private TMP_InputField campoPuntos;
    [SerializeField] private TMP_InputField campoCongelamiento;
    [SerializeField] private TextMeshProUGUI textoMensaje;

    private ConfiguracionPartida configuracion;

    // Awake corre al cargarse el objeto
    private void Awake()
    {
        // Por ahora se crea con valores por defecto.
        // Después se recuperará la última configuración guardada.
        configuracion = new ConfiguracionPartida();
    }

    // Start corre justo antes del primer frame
    private void Start()
    {
        MostrarValores();
        textoMensaje.text = "";
    }

    // La llama el botón Guardar.
    public void Guardar()
    {
        int minutos;
        int puntos;
        int congelamiento;

        bool minutosValidos = int.TryParse(campoMinutos.text, out minutos);
        bool puntosValidos = int.TryParse(campoPuntos.text, out puntos);
        bool congelamientoValido = int.TryParse(campoCongelamiento.text, out congelamiento);

        if (!minutosValidos || !puntosValidos || !congelamientoValido)
        {
            textoMensaje.text = "Completa todos los campos con números enteros.";
            return;
        }

        // Se hace un cast del valor del dropdown al enum ModoFinalizacion.
        ModoFinalizacion modo = (ModoFinalizacion)dropdownModo.value;

        try
        {
            configuracion.Configurar(modo, minutos, puntos, congelamiento);
            textoMensaje.text = "Configuración guardada.";
        }
        catch (ArgumentException error)
        {
            textoMensaje.text = error.Message;
        }
    }

    private void MostrarValores()
    {
        dropdownModo.value = (int)configuracion.Modo;
        campoMinutos.text = configuracion.Minutos.ToString();
        campoPuntos.text = configuracion.PuntosObjetivo.ToString();
        campoCongelamiento.text = configuracion.SegundosCongelamiento.ToString();
    }
}
