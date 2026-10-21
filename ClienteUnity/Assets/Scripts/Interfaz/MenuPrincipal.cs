using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public GameObject Principal;
    public GameObject InicioSesión;
    public GameObject Registro;

    public void OpenInicioSesión()
    {
        Principal.SetActive(false);
        InicioSesión.SetActive(true);
    }

    public void OpenRegistro()
    {
        Principal.SetActive(false);
        Registro.SetActive(true);
    }

    public void OpenPrincipal()
    {
        InicioSesión.SetActive(false);
        Registro.SetActive(false);
        Principal.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
    }


}
