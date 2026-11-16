using UnityEngine;

public class PuntosAparicionGuardianes : MonoBehaviour
{
    [SerializeField] private Transform[] puntos;

    public Vector2[] ObtenerPosiciones()
    {
        Vector2[] posiciones = new Vector2[puntos.Length];

        for (int i = 0; i < puntos.Length; i++)
        {
            posiciones[i] = puntos[i].position;
        }

        return posiciones;
    }

    [ContextMenu("Mostrar posiciones de guardianes")]
    public void MostrarPosiciones()
    {
        Vector2[] posiciones = ObtenerPosiciones();

        for (int i = 0; i < posiciones.Length; i++)
        {
            Debug.Log(
                $"Guardián {i + 1}: " +
                $"X = {posiciones[i].x}, " +
                $"Y = {posiciones[i].y}"
            );
        }
    }
}