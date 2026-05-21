using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneradorSopa : MonoBehaviour
{
    public GameObject casillaPrefab;
    public GameObject contenedorSopa;

    private string abecedario = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";
    private List<CasillaController> todasLasCasillas = new List<CasillaController>();

    private PreguntasController ctrl;

    void Start()
    {
        ctrl = GetComponent<PreguntasController>();
        CrearSopa();
    }

    void CrearSopa()
    {
        todasLasCasillas.Clear();

        // 🔥 limpiar SOLO casillas clonadas
        List<GameObject> destruir = new List<GameObject>();

        foreach (Transform hijo in contenedorSopa.transform)
        {
            if (hijo.name.Contains("(Clone)"))
            {
                destruir.Add(hijo.gameObject);
            }
        }

        foreach (GameObject obj in destruir)
        {
            Destroy(obj);
        }

        // 🔥 crear nuevas casillas
        for (int i = 0; i < 100; i++)
        {
            GameObject nuevaCasilla = Instantiate(casillaPrefab, contenedorSopa.transform);

            nuevaCasilla.name = "Casilla_" + i;

            RectTransform rt = nuevaCasilla.GetComponent<RectTransform>();

            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(70, 70);

            var casilla = nuevaCasilla.GetComponent<CasillaController>();

            int fila = i / 10;
            int columna = i % 10;

            casilla.fila = fila;
            casilla.columna = columna;

            if (ctrl != null)
                casilla.EstablecerControlador(ctrl);

            todasLasCasillas.Add(casilla);
        }
    }
    public void GenerarLetras(string palabra)
    {
        palabra = palabra.Trim().ToUpper();

        // Llenar con letras aleatorias
        foreach (var c in todasLasCasillas)
        {
            string azar = abecedario[Random.Range(0, abecedario.Length)].ToString();
            c.letraDeEsteBoton = azar;
            c.miTexto.text = azar;
            c.Resetear();
        }

        // Colocar palabra horizontal
        int fila = Random.Range(0, 10);
        int col = Random.Range(0, Mathf.Max(1, 10 - palabra.Length + 1));
        for (int i = 0; i < palabra.Length; i++)
        {
            int index = (fila * 10) + col + i;

            todasLasCasillas[index].letraDeEsteBoton = palabra[i].ToString();
            todasLasCasillas[index].miTexto.text = palabra[i].ToString();
        }
    }
}