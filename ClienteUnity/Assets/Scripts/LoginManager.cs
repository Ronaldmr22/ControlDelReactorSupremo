
using TMPro;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField correoInput;
    [SerializeField]
    private ReenviarCodigo reenviarCodigo;
    [SerializeField]
    private TMP_Text mensajeLogin;

    public void VerificarCorreo()
    {
        string correo = correoInput.text.Trim();

        if (string.IsNullOrWhiteSpace(correo))
        {
            mensajeLogin.text = "Ingresa tu correo electrónico.";
            mensajeLogin.color = Color.red;
            return;
        }

        reenviarCodigo.SolicitarDesdeLogin(correo);
    }
}

