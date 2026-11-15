using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ExportadorColisiones : MonoBehaviour
{
    [SerializeField] private int idEscenario = 1;
    [Header("Colisiones del escenario")]
    [SerializeField] private GameObject[] objetosEscenario;

    [Serializable]
    public class Obstaculo
    {
        public Vector2[] vertices;
    }

    [Serializable]
    public class DatosMapa
    {
        public int idEscenario;
        public List<Obstaculo> obstaculos = new();
    }

    [ContextMenu("Exportar colisiones del mapa")]
    public void Exportar()
    {
        DatosMapa datos = new DatosMapa
        {
            idEscenario = idEscenario
        };


        List<Collider2D> colisiones = new List<Collider2D>();

        if (objetosEscenario == null || objetosEscenario.Length == 0)
        {
            Debug.LogWarning("No se asignaron objetos del escenario.");
            return;
        }

        foreach (GameObject objeto in objetosEscenario)
        {
            if (objeto == null)
                continue;

            // Priorizar el Composite Collider 2D.
            CompositeCollider2D compuesto =
                objeto.GetComponent<CompositeCollider2D>();

            if (compuesto != null)
            {
                colisiones.Add(compuesto);
                continue;
            }

            // Si no tiene Composite, buscar otros colliders.
            Collider2D colision = objeto.GetComponent<Collider2D>();

            if (colision != null)
                colisiones.Add(colision);
        }

        foreach (Collider2D colision in colisiones)
        {
            if (!colision.enabled || colision.isTrigger)
                continue;

            // Los personajes y guardianes no son
            // obstáculos fijos del escenario.
            if (colision.GetComponent<MovimientoJugador>() != null ||
                colision.GetComponent<ControladorGuardianes>() != null)
                continue;

            if (colision is BoxCollider2D caja)
            {
                AgregarCaja(datos, caja);
            }
            else if (colision is CompositeCollider2D compuesto)
            {
                AgregarComposite(datos, compuesto);
            }
        }
        Debug.Log(
            "Escenario " + idEscenario +
            ": se detectaron " + datos.obstaculos.Count +
            " polígonos de colisión."
        );

        string json = JsonUtility.ToJson(datos, true);

        string ruta = Path.Combine(
            Application.dataPath,
            "ColisionesMapa" + idEscenario + ".json"
        );

        File.WriteAllText(ruta, json);

        Debug.Log("Colisiones exportadas en: " + ruta);
    }

    private void AgregarCaja(
        DatosMapa datos,
        BoxCollider2D caja)
    {
        Vector2 mitad = caja.size / 2f;
        Vector2 centro = caja.offset;

        Vector2[] esquinas =
        {
            centro + new Vector2(-mitad.x, -mitad.y),
            centro + new Vector2(-mitad.x, mitad.y),
            centro + new Vector2(mitad.x, mitad.y),
            centro + new Vector2(mitad.x, -mitad.y)
        };

        AgregarPoligono(datos, caja.transform, esquinas);
    }

    private void AgregarPoligono(
        DatosMapa datos,
        Transform objeto,
        Vector2[] verticesLocales)
    {
        Vector2[] verticesMundo =
            new Vector2[verticesLocales.Length];

        for (int i = 0; i < verticesLocales.Length; i++)
        {
            verticesMundo[i] =
                objeto.TransformPoint(verticesLocales[i]);
        }

        datos.obstaculos.Add(new Obstaculo
        {
            vertices = verticesMundo
        });
    }


    private void AgregarComposite(
        DatosMapa datos,
        CompositeCollider2D compuesto)
    {
        int cantidadPoligonos = compuesto.pathCount;

        for (int i = 0; i < cantidadPoligonos; i++)
        {
            int cantidadVertices = compuesto.GetPathPointCount(i);

            if (cantidadVertices < 3)
                continue;

            Vector2[] vertices = new Vector2[cantidadVertices];

            compuesto.GetPath(i, vertices);

            AgregarPoligono(
                datos,
                compuesto.transform,
                vertices
            );
        }
    }

}
