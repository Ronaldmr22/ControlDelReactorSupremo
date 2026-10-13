using UnityEngine;
using TMPro;

public class MensajeSobreAvatar : MonoBehaviour
{
    [SerializeField] private TextMeshPro texto;
    [SerializeField] private float segundosVisible = 1.5f;

    private float segundosRestantes;

    private void Start()
    {
        texto.text = "";
    }

    // La llama quien quiera mostrar un mensaje sobre este avatar.
    public void Mostrar(string mensaje)
    {
        texto.text = mensaje;
        segundosRestantes = segundosVisible;
    }

    private void Update()
    {
        if (segundosRestantes <= 0f) return;

        segundosRestantes -= Time.deltaTime;

        if (segundosRestantes <= 0f)
        {
            texto.text = "";
        }
    }
}