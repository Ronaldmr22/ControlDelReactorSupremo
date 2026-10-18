using UnityEngine;
using UnityEngine.UI;

public class SeleccionFotoPerfil : MonoBehaviour
{
    [SerializeField] private Image fotoLobby;
    [SerializeField] private Image fotoPerfil;
    [SerializeField] private Sprite[] fotosPredeterminadas;
    [SerializeField] private GameObject pantallaPerfil;
    [SerializeField] private GameObject pantallaFotosPerfil;

    public void ElegirFoto(int indice)
    {
        // Actualiza la foto en ambas pantallas.
        fotoLobby.sprite = fotosPredeterminadas[indice];
        fotoPerfil.sprite = fotosPredeterminadas[indice];

        pantallaPerfil.SetActive(true);
        pantallaFotosPerfil.SetActive(false);
    }
}