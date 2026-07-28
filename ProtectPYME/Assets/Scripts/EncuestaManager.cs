using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

// --- ESTRUCTURAS DE DATOS ---

[System.Serializable]
public class PreguntaEncuesta
{
    public string idPregunta;
    [TextArea(2, 4)]
    public string enunciado;
    public string opcionA;
    public string opcionB;
    public string opcionC;
}

[System.Serializable]
public class RespuestaItem
{
    public string id_pregunta;
    public string respuesta_seleccionada;
}

[System.Serializable]
public class EnvioRespuestasEncuesta
{
    public List<RespuestaItem> respuestas = new List<RespuestaItem>();
}

// --- CONTROLADOR PRINCIPAL ---

public class EncuestaManager : MonoBehaviour
{
    [Header("Referencias de UI en Pantalla")]
    public TextMeshProUGUI txtPregunta;
    public TextMeshProUGUI txtOpcionA;
    public TextMeshProUGUI txtOpcionB;
    public TextMeshProUGUI txtOpcionC;
    public TextMeshProUGUI txtContador;

    [Header("Botones de Respuesta")]
    public Button btnOpcionA;
    public Button btnOpcionB;
    public Button btnOpcionC;

    [Header("Configuración de Navegación")]
    [Tooltip("Escribe el nombre EXACTO de la escena a la que irá al terminar")]
    public string nombreEscenaSiguiente = "MenuPrincipal";

    [Header("Preguntas Fijas")]
    public List<PreguntaEncuesta> preguntas = new List<PreguntaEncuesta>();

    private int indiceActual = 0;
    private List<RespuestaItem> listaRespuestas = new List<RespuestaItem>();

    void Start()
    {
        if (btnOpcionA != null) btnOpcionA.onClick.AddListener(() => SeleccionarOpcion("A"));
        if (btnOpcionB != null) btnOpcionB.onClick.AddListener(() => SeleccionarOpcion("B"));
        if (btnOpcionC != null) btnOpcionC.onClick.AddListener(() => SeleccionarOpcion("C"));

        MostrarPreguntaActual();
    }

    void MostrarPreguntaActual()
    {
        if (preguntas == null || preguntas.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay preguntas configuradas en el Inspector.");
            return;
        }

        PreguntaEncuesta p = preguntas[indiceActual];

        txtPregunta.text = p.enunciado;
        txtOpcionA.text = p.opcionA;
        txtOpcionB.text = p.opcionB;
        txtOpcionC.text = p.opcionC;

        txtContador.text = $"{indiceActual + 1}/{preguntas.Count}";
    }

    public void SeleccionarOpcion(string letraOpcion)
    {
        // Guardamos la respuesta
        RespuestaItem respuesta = new RespuestaItem
        {
            id_pregunta = string.IsNullOrEmpty(preguntas[indiceActual].idPregunta) 
                ? $"P{indiceActual + 1}" 
                : preguntas[indiceActual].idPregunta,
            respuesta_seleccionada = letraOpcion
        };
        
        listaRespuestas.Add(respuesta);
        Debug.Log($"Pregunta {indiceActual + 1}: Respondió {letraOpcion}");

        // Avanzar o Finalizar
        if (indiceActual < preguntas.Count - 1)
        {
            indiceActual++;
            MostrarPreguntaActual();
        }
        else
        {
            FinalizarEncuesta();
        }
    }

    void FinalizarEncuesta()
    {
        Debug.Log("✅ Encuesta completada. Cambiando de escena...");
        StartCoroutine(EnviarDatosYCambiarEscena());
    }

    IEnumerator EnviarDatosYCambiarEscena()
    {
        // Aquí puedes realizar la petición POST al backend cuando esté listo
        
        yield return null; // Pequeña espera por seguridad de ejecución

        // Carga la siguiente escena configurada en el Inspector
        SceneManager.LoadScene(nombreEscenaSiguiente);
    }
}