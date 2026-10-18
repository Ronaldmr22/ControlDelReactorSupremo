using UnityEngine;
using UnityEngine.UI;

public class SeleccionPersonaje : MonoBehaviour
{
    [SerializeField] private Image imagenPersonajeLobby;
    [SerializeField] private Sprite[] personajes;
    [SerializeField] private GameObject fondoLobby;
    [SerializeField] private GameObject pantallaPersonajes;

    public void ElegirPersonaje(int indice)
    {
        // Cambia la vista previa del lobby.
        imagenPersonajeLobby.sprite = personajes[indice];

        fondoLobby.SetActive(true);
        pantallaPersonajes.SetActive(false);
    }
}
