using UnityEngine;
using UnityEngine.Tilemaps;

public class LimitesMapa : MonoBehaviour
{
    void Start()
    {
        Tilemap tilemap = GetComponent<Tilemap>();

        Bounds limites = tilemap.localBounds;

        Vector3 minimo = tilemap.transform.TransformPoint(limites.min);
        Vector3 maximo = tilemap.transform.TransformPoint(limites.max);

        Debug.Log("X mínima: " + minimo.x);
        Debug.Log("X máxima: " + maximo.x);
        Debug.Log("Y mínima: " + minimo.y);
        Debug.Log("Y máxima: " + maximo.y);
    }
}