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
}