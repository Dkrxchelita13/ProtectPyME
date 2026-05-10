using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class Pregunta
{
    public string textoPregunta;
    public string respuestaCorrecta;
}

[System.Serializable]
public class CrosswordBackend
{
    public string clue;
    public string answer;
}

[System.Serializable]
public class CrosswordList
{
    public CrosswordBackend[] items;
}

public class PreguntasController : MonoBehaviour
{
    public TextMeshProUGUI txtPreguntaDisplay;
    public Pregunta[] bancoDePreguntas;

    private int indicePreguntaActual = 0;

    private List<CasillaController> casillasSeleccionadas = new List<CasillaController>();

    void Start()
    {
        // 🔥 RESET TOTAL
        indicePreguntaActual = 0;
        bloqueado = false;
        corriendoTiempo = false;
        casillasSeleccionadas.Clear();

        string token = APIManager.Instance.GetToken();

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");
            return;
        }

        StartCoroutine(APIManager.Instance.GetCrossword(OnCrosswordLoaded));
    }
    void OnCrosswordLoaded(string json)
    {
        indicePreguntaActual = 0;
        if (!string.IsNullOrEmpty(json) && json != "ERROR")
        {
            string fixedJson = "{\"items\":" + json + "}";
            CrosswordList data = JsonUtility.FromJson<CrosswordList>(fixedJson);

            if (data?.items != null && data.items.Length > 0)
            {
                bancoDePreguntas = new Pregunta[data.items.Length];

                for (int i = 0; i < data.items.Length; i++)
                {
                    bancoDePreguntas[i] = new Pregunta
                    {
                        textoPregunta = data.items[i].clue,
                        respuestaCorrecta = data.items[i].answer
                    };
                }
            }
        }

        MostrarPregunta();
    }
    void MostrarPregunta()
    {
        casillasSeleccionadas.Clear();

        if (bancoDePreguntas == null || bancoDePreguntas.Length == 0)
        {
            Debug.LogError("❌ No hay preguntas");
            return;
        }

        if (indicePreguntaActual < bancoDePreguntas.Length)
        {
            tiempoRestante = TIEMPO_MAX;
            corriendoTiempo = true;

            var pregunta = bancoDePreguntas[indicePreguntaActual];

            txtPreguntaDisplay.text = pregunta.textoPregunta;

            GetComponent<GeneradorSopa>().GenerarLetras(pregunta.respuestaCorrecta);
        }
        else
        {
            Debug.Log("🎉 Juego terminado");

            txtPreguntaDisplay.text = "¡Completaste el juego!";

            bloqueado = true;
            corriendoTiempo = false;

            if (barraTiempo != null)
                barraTiempo.fillAmount = 0f;
        }
    }

    // 🔥 MÉTODO PRINCIPAL
    public void AgregarLetra(string letra, CasillaController casilla, bool seleccionada)
    {
        if (bloqueado) return; // 🚫 BLOQUEO

        if (indicePreguntaActual >= bancoDePreguntas.Length)
            return;

        if (seleccionada)
        {
            if (!casillasSeleccionadas.Contains(casilla))
                casillasSeleccionadas.Add(casilla);
        }
        else
        {
            casillasSeleccionadas.Remove(casilla);
        }

        ValidarSeleccion();
    }
    // 🔥 VALIDACIÓN ROBUSTA
    void ValidarSeleccion()
    {
        if (bloqueado) return; // 🔥 PRIMERA LÍNEA SIEMPRE

        if (casillasSeleccionadas.Count == 0)
            return;

        if (casillasSeleccionadas.Count == 1)
            return;

        // Detectar dirección
        int deltaFila = casillasSeleccionadas[1].fila - casillasSeleccionadas[0].fila;
        int deltaCol = casillasSeleccionadas[1].columna - casillasSeleccionadas[0].columna;

        // Normalizar dirección (-1, 0, 1)
        deltaFila = Mathf.Clamp(deltaFila, -1, 1);
        deltaCol = Mathf.Clamp(deltaCol, -1, 1);

        // Validar que todas sigan la misma dirección
        for (int i = 1; i < casillasSeleccionadas.Count; i++)
        {
            int df = casillasSeleccionadas[i].fila - casillasSeleccionadas[i - 1].fila;
            int dc = casillasSeleccionadas[i].columna - casillasSeleccionadas[i - 1].columna;

            df = Mathf.Clamp(df, -1, 1);
            dc = Mathf.Clamp(dc, -1, 1);

            if (df != deltaFila || dc != deltaCol)
            {
                ResetSeleccion("❌ Dirección inválida");
                return;
            }
        }

        // Construir palabra RESPETANDO ORDEN DEL USUARIO
        string formada = "";
        foreach (var c in casillasSeleccionadas)
            formada += c.letraDeEsteBoton;

        string correcta = bancoDePreguntas[indicePreguntaActual].respuestaCorrecta.Trim().ToUpper();

        Debug.Log($"FORMADA: {formada}");
        Debug.Log($"CORRECTA: {correcta}");

        // ✅ ACEPTAR normal o invertida
        string invertida = Invertir(formada);


        if (formada == correcta || invertida == correcta)
        {
            bloqueado = true;

            foreach (var c in casillasSeleccionadas)
                c.MarcarCorrecta();

            StartCoroutine(SiguientePreguntaConDelay());

            return;
        }
        else if (formada.Length >= correcta.Length)
        {
            ResetSeleccion("❌ Incorrecta");
        }
    }

    string Invertir(string s)
    {
        char[] arr = s.ToCharArray();
        System.Array.Reverse(arr);
        return new string(arr);
    }

    void ResetSeleccion(string mensaje)
    {
        Debug.Log(mensaje);

        foreach (var c in casillasSeleccionadas)
            c.Resetear();

        casillasSeleccionadas.Clear();
    }
    public void PasarSiguientePregunta()
    {
        indicePreguntaActual++;

        if (indicePreguntaActual < bancoDePreguntas.Length)
        {
            MostrarPregunta();
        }
        else
        {
            Debug.Log("Juego terminado");

            txtPreguntaDisplay.text = "¡Completaste el juego! ";

            bloqueado = true; // 🔒 bloquear interacción
            corriendoTiempo = false; // ⏹ detener tiempo
        }
    }

    private bool bloqueado = false;

    public IEnumerator SiguientePreguntaConDelay()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("➡️ CAMBIANDO PREGUNTA");

        foreach (var c in casillasSeleccionadas)
            c.Resetear();

        casillasSeleccionadas.Clear();

        indicePreguntaActual++;

        // 🔥 reiniciar tiempo aquí
        tiempoRestante = TIEMPO_MAX;
        corriendoTiempo = true;

        bloqueado = false;

        MostrarPregunta();
    }
    public bool PuedeInteractuar()
    {
        return !bloqueado;
    }
    private float tiempoRestante = 10f;
    private float TIEMPO_MAX = 20f;
    private bool corriendoTiempo = false;

    public Image barraTiempo;
    public TextMeshProUGUI txtTiempo;
    void Update()
    {
        if (!corriendoTiempo) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante < 0f)
            tiempoRestante = 0f;

        // 🔥 barra
        if (barraTiempo != null)
        {
            barraTiempo.fillAmount = tiempoRestante / TIEMPO_MAX;
        }

        // 🔥 texto del reloj
        if (txtTiempo != null)
        {
            txtTiempo.text = Mathf.CeilToInt(tiempoRestante).ToString();
        }

        // 🔥 tiempo terminado
        if (tiempoRestante <= 0f)
        {
            corriendoTiempo = false;

            ResetSeleccion("⏰ Tiempo agotado");

            bloqueado = true;
            StartCoroutine(SiguientePreguntaConDelay());
        }
    }
}
