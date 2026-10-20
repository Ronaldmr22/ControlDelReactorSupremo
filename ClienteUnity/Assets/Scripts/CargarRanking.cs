using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class CargarRankingGlobal : MonoBehaviour
{
    [SerializeField] private TMP_Text txtRankingContenido;

    private const string API_URL =
        "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/";

    [Serializable]
    private class JugadorRanking
    {
        public int posicion;
        public int idUsuario;
        public int puntosTotales;
        public int partidasJugadas;
        public int partidasGanadas;
    }

    [Serializable]
    private class ListaRanking
    {
        public JugadorRanking[] jugadores;
    }

    [Serializable]
    private class PerfilDto
    {
        public string nombreUsuario;
    }

    private void OnEnable()
    {
        StartCoroutine(CargarRanking());
    }

    private IEnumerator CargarRanking()
    {
        txtRankingContenido.text = "Cargando ranking...";

        using (UnityWebRequest request =
               UnityWebRequest.Get(API_URL + "ranking"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                txtRankingContenido.text =
                    "No se pudo cargar el ranking.";

                Debug.LogError(request.error);
                yield break;
            }

            // JsonUtility necesita un objeto raíz, no un arreglo.
            string json = "{\"jugadores\":" +
                          request.downloadHandler.text + "}";

            ListaRanking ranking =
                JsonUtility.FromJson<ListaRanking>(json);

            if (ranking == null ||
                ranking.jugadores == null ||
                ranking.jugadores.Length == 0)
            {
                txtRankingContenido.text =
                    "Todavía no hay partidas registradas.";
                yield break;
            }

            List<JugadorRanking> jugadores =
                new List<JugadorRanking>(ranking.jugadores);

            jugadores.Sort((a, b) =>
                a.posicion.CompareTo(b.posicion));

            StringBuilder texto = new StringBuilder();

            foreach (JugadorRanking jugador in jugadores)
            {
                string nombre = "Usuario " + jugador.idUsuario;

                // Consultar el nombre real del jugador.
                using (UnityWebRequest perfilRequest =
                       UnityWebRequest.Get(
                           API_URL + jugador.idUsuario + "/perfil"))
                {
                    yield return perfilRequest.SendWebRequest();

                    if (perfilRequest.result ==
                        UnityWebRequest.Result.Success)
                    {
                        PerfilDto perfil =
                            JsonUtility.FromJson<PerfilDto>(
                                perfilRequest.downloadHandler.text);

                        if (perfil != null &&
                            !string.IsNullOrEmpty(perfil.nombreUsuario))
                        {
                            nombre = perfil.nombreUsuario;
                        }
                    }
                }

                // Evitar que caracteres del nombre se interpreten
                // como etiquetas de TextMeshPro.
                nombre = nombre.Replace("&", "&amp;")
                               .Replace("<", "&lt;")
                               .Replace(">", "&gt;");

                string color = jugador.posicion switch
                {
                    1 => "#FFD700",
                    2 => "#C0C0C0",
                    3 => "#CD7F32",
                    _ => "#FFFFFF"
                };

                texto.AppendLine(
                    $"<color={color}>{jugador.posicion}. {nombre}</color>" +
                    $"<pos=75%>{jugador.puntosTotales} pts"
                );

                texto.AppendLine(
                    "<color=#B9C9E4>────────────────────────────</color>"
                );

                texto.AppendLine();
            }

            txtRankingContenido.text = texto.ToString();

            Debug.Log("Ranking global cargado correctamente.");
        }
    }
}