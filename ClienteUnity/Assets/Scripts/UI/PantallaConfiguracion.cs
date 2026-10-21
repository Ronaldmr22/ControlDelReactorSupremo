
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PantallaConfiguracion : MonoBehaviour
{
    [Header("Opciones de configuración")]
    [SerializeField] private TMP_Dropdown dropdownModo;
    [SerializeField] private TMP_Dropdown dropdownDuracion;
    [SerializeField] private TMP_Dropdown dropdownPuntos;
    [SerializeField] private TMP_Dropdown dropdownCongelamiento;

    [Header("Otros componentes")]
    [SerializeField] private TMP_Text textoMensaje;
    [SerializeField] private ClienteSalas clienteSalas;

    private bool guardando;

    private void OnEnable()
    {
        if (textoMensaje != null)
            textoMensaje.text = "";
    }

    public void Guardar()
    {
        if (guardando)
            return;

        if (dropdownModo == null ||
            dropdownDuracion == null ||
            dropdownPuntos == null ||
            dropdownCongelamiento == null ||
            textoMensaje == null ||
            clienteSalas == null)
        {
            Debug.LogError("Faltan referencias de configuración.");
            return;
        }

        // Opción 0 = tiempo, opción 1 = puntos.
        string modo = dropdownModo.value == 0
            ? "TIEMPO"
            : "PUNTOS";

        // Obtener los números de las opciones seleccionadas.
        if (!LeerNumero(dropdownDuracion, out int minutos) ||
            !LeerNumero(dropdownPuntos, out int puntos) ||
            !LeerNumero(dropdownCongelamiento, out int congelamiento))
        {
            textoMensaje.text = "Hay opciones de configuración inválidas.";
            return;
        }

        if ((modo == "TIEMPO" && minutos <= 0) ||
            (modo == "PUNTOS" && puntos <= 0) ||
            congelamiento <= 0)
        {
            textoMensaje.text = "Los valores deben ser mayores que cero.";
            return;
        }

        StartCoroutine(EnviarConfiguracion(
            modo, minutos, puntos, congelamiento));
    }

    private IEnumerator EnviarConfiguracion(
        string modo,
        int minutos,
        int puntos,
        int congelamiento)
    {
        guardando = true;
        textoMensaje.text = "Guardando configuración...";

        yield return clienteSalas.GuardarConfiguracion(
            modo,
            minutos,
            puntos,
            congelamiento,
            () =>
            {
                textoMensaje.text =
                    "Configuración guardada correctamente.";
            },
            error =>
            {
                textoMensaje.text = error;
                Debug.LogError(
                    "Error al guardar configuración: " + error);
            });

        guardando = false;
    }

    private bool LeerNumero(TMP_Dropdown dropdown, out int numero)
    {
        numero = 0;

        if (dropdown.options == null ||
            dropdown.value < 0 ||
            dropdown.value >= dropdown.options.Count)
        {
            return false;
        }

        // Ejemplos: "5 minutos", "100 puntos", "4 segundos".
        string texto = dropdown.options[dropdown.value].text;

        string[] partes = texto.Trim().Split(' ');

        return partes.Length > 0 &&
               int.TryParse(partes[0], out numero);
    }
}
