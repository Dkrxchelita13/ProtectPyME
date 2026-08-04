using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneradorSopa : MonoBehaviour
{
    private const int GridSize = 10;
    private const int ExpectedCellCount = GridSize * GridSize;

    public GameObject casillaPrefab;
    public GameObject contenedorSopa;

    private string abecedario = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";
    private List<CasillaController> todasLasCasillas = new List<CasillaController>();

    private PreguntasController ctrl;

    public bool IsGridReady
    {
        get
        {
            return todasLasCasillas != null &&
                todasLasCasillas.Count == ExpectedCellCount;
        }
    }

    void Awake()
    {
        ctrl = GetComponent<PreguntasController>();
    }

    void Start()
    {
        EnsureGridInitialized();
    }

    public bool EnsureGridInitialized()
    {
        if (IsGridReady)
        {
            Debug.Log(
                "GeneradorSopa: grid listo con "
                + todasLasCasillas.Count
                + " celdas"
            );
            return true;
        }

        if (casillaPrefab == null || contenedorSopa == null)
        {
            Debug.LogError("GeneradorSopa: faltan referencias para crear el grid.");
            return false;
        }

        CrearSopa();

        if (!IsGridReady)
        {
            Debug.LogError(
                "GeneradorSopa: grid incompleto. Celdas="
                + GetCellCount()
                + ", esperadas="
                + ExpectedCellCount
            );
            return false;
        }

        Debug.Log(
            "GeneradorSopa: grid listo con "
            + todasLasCasillas.Count
            + " celdas"
        );
        return true;
    }

    private int GetCellCount()
    {
        return todasLasCasillas != null ? todasLasCasillas.Count : 0;
    }

    void CrearSopa()
    {
        todasLasCasillas.Clear();

        // 🔥 limpiar SOLO casillas clonadas
        List<GameObject> destruir = new List<GameObject>();

        foreach (Transform hijo in contenedorSopa.transform)
        {
            destruir.Add(hijo.gameObject);
        }

        foreach (GameObject obj in destruir)
        {
            Destroy(obj);
        }

        // 🔥 crear nuevas casillas
        for (int i = 0; i < ExpectedCellCount; i++)
        {
            GameObject nuevaCasilla = Instantiate(casillaPrefab, contenedorSopa.transform);

            nuevaCasilla.name = "Casilla_" + i;

            RectTransform rt = nuevaCasilla.GetComponent<RectTransform>();

            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(70, 70);

            var casilla = nuevaCasilla.GetComponent<CasillaController>();

            int fila = i / GridSize;
            int columna = i % GridSize;

            casilla.fila = fila;
            casilla.columna = columna;

            if (ctrl != null)
                casilla.EstablecerControlador(ctrl);

            todasLasCasillas.Add(casilla);
        }
    }
    public void GenerarLetras(string palabra)
    {
        if (string.IsNullOrEmpty(palabra))
        {
            Debug.LogError("GeneradorSopa: palabra vacia.");
            return;
        }

        palabra = palabra.Trim().ToUpper();

        if (string.IsNullOrEmpty(palabra))
        {
            Debug.LogError("GeneradorSopa: palabra vacia.");
            return;
        }

        if (!EnsureGridInitialized())
        {
            Debug.LogError(
                "GeneradorSopa: grid incompleto. Celdas="
                + GetCellCount()
                + ", esperadas="
                + ExpectedCellCount
            );
            return;
        }

        if (palabra.Length > GridSize)
        {
            Debug.LogError(
                "GeneradorSopa: la palabra no cabe en el grid. Largo="
                + palabra.Length
            );
            return;
        }

        // Llenar con letras aleatorias
        foreach (var c in todasLasCasillas)
        {
            string azar = abecedario[Random.Range(0, abecedario.Length)].ToString();
            c.letraDeEsteBoton = azar;
            c.miTexto.text = azar;
            c.Resetear();
        }

        // Colocar palabra horizontal
        int fila = Random.Range(0, GridSize);
        int col = Random.Range(0, Mathf.Max(1, GridSize - palabra.Length + 1));
        for (int i = 0; i < palabra.Length; i++)
        {
            int index = (fila * GridSize) + col + i;

            if (index < 0 || index >= todasLasCasillas.Count)
            {
                Debug.LogError(
                    "GeneradorSopa: indice fuera de rango. Index="
                    + index
                    + ", celdas="
                    + todasLasCasillas.Count
                );
                return;
            }

            todasLasCasillas[index].letraDeEsteBoton = palabra[i].ToString();
            todasLasCasillas[index].miTexto.text = palabra[i].ToString();
        }
    }
}
