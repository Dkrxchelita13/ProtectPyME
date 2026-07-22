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
public class CrosswordBackend { public string clue; public string answer; }

[System.Serializable]
public class CrosswordList { public CrosswordBackend[] items; }

public class PreguntasController : MonoBehaviour
{
    [Header("Panel Ganador")]
    public TextMeshProUGUI txtPuntosFinal;
    public TextMeshProUGUI txtVidasFinal;
    public TextMeshProUGUI txtSeguridadFinal;
    public GameObject canvasGanador;
    public TextMeshProUGUI txtPreguntaDisplay;
    public Pregunta[] bancoDePreguntas;

    [Header("Barras de Progreso")]
    public Image barraSeguridad;
    public Image barraTiempo;
    public Image barraTiempo2;
    public TextMeshProUGUI txtTiempo;
    public TextMeshProUGUI txtTiempo2;

    private int indicePreguntaActual = 0;
    private List<CasillaController> casillasSeleccionadas = new List<CasillaController>();
    private GamificacionController gamificacion;

    private float tiempoRestante = 10f;
    private float TIEMPO_MAX = 20f;
    private bool corriendoTiempo = false;
    private bool bloqueado = false;
    private bool cambiandoPregunta = false;
    private bool perfectGame = true;

    void Start()
    {
        gamificacion = GetComponent<GamificacionController>();

        if (gamificacion != null)
        {
            gamificacion.progresoSeguridad = gamificacion.ObtenerSeguridadActual();
        }

        indicePreguntaActual = 0;
        bloqueado = false;
        corriendoTiempo = false;
        perfectGame = true;
        casillasSeleccionadas.Clear();

        ActualizarBarraSeguridadVisual();

        string token = APIManager.Instance.GetToken();
        if (string.IsNullOrEmpty(token)) return;

        StartCoroutine(APIManager.Instance.GetWords(AIState.RecommendedTraining, AIState.RiskLevel, OnCrosswordLoaded));
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
        if (bancoDePreguntas == null || bancoDePreguntas.Length == 0) return;

        if (indicePreguntaActual < bancoDePreguntas.Length)
        {
            tiempoRestante = TIEMPO_MAX;
            corriendoTiempo = true;
            txtPreguntaDisplay.text = bancoDePreguntas[indicePreguntaActual].textoPregunta;
            GetComponent<GeneradorSopa>().GenerarLetras(bancoDePreguntas[indicePreguntaActual].respuestaCorrecta);
        }
    }

    public void ActualizarBarraSeguridadVisual()
    {
        if (barraSeguridad != null && gamificacion != null)
        {
            barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;
        }
    }

    public void AgregarLetra(string letra, CasillaController casilla, bool seleccionada)
    {
        if (bloqueado || indicePreguntaActual >= bancoDePreguntas.Length) return;

        if (seleccionada && !casillasSeleccionadas.Contains(casilla)) casillasSeleccionadas.Add(casilla);
        else casillasSeleccionadas.Remove(casilla);

        ValidarSeleccion();
    }
    
    void ValidarSeleccion()
    {
        if (bloqueado || casillasSeleccionadas.Count <= 1) return;

        int deltaFila = Mathf.Clamp(casillasSeleccionadas[1].fila - casillasSeleccionadas[0].fila, -1, 1);
        int deltaCol = Mathf.Clamp(casillasSeleccionadas[1].columna - casillasSeleccionadas[0].columna, -1, 1);

        for (int i = 1; i < casillasSeleccionadas.Count; i++)
        {
            int df = Mathf.Clamp(casillasSeleccionadas[i].fila - casillasSeleccionadas[i - 1].fila, -1, 1);
            int dc = Mathf.Clamp(casillasSeleccionadas[i].columna - casillasSeleccionadas[i - 1].columna, -1, 1);

            if (df != deltaFila || dc != deltaCol)
            {
                ResetSeleccion("❌ Dirección inválida");
                return;
            }
        }

        string formada = "";
        foreach (var c in casillasSeleccionadas) formada += c.letraDeEsteBoton;

        string correcta = bancoDePreguntas[indicePreguntaActual].respuestaCorrecta.Trim().ToUpper();
        string invertida = Invertir(formada);

        if (formada == correcta || invertida == correcta)
        {
            if (cambiandoPregunta) return;
            cambiandoPregunta = true;
            bloqueado = true;

            // 🔥 ACIERTO: Solo suma puntos para el backend, no altera arbitrariamente la seguridad local
            gamificacion.SumarPuntos(10);
            gamificacion.ModificarSeguridad(1f);
            ActualizarBarraSeguridadVisual();

            foreach (var c in casillasSeleccionadas) c.MarcarCorrecta();
            StartCoroutine(SiguientePreguntaConDelay());
        }
        else if (formada.Length >= correcta.Length)
        {
            bloqueado = true; 
            perfectGame = false;
            gamificacion.RegistrarError(); 
            
            StartCoroutine(FeedbackErrorConDelay("❌ Incorrecta"));
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
        foreach (var c in casillasSeleccionadas) c.Resetear();
        casillasSeleccionadas.Clear();
    }
    
    public void PasarSiguientePregunta()
    {
        indicePreguntaActual++;

        if (indicePreguntaActual < bancoDePreguntas.Length)
        {
            gamificacion.ReiniciarCronometro();
            MostrarPregunta();
        }
        else
        {
            bloqueado = true;
            corriendoTiempo = false;
            txtPreguntaDisplay.text = "Guardando progreso...";
            StartCoroutine(TerminarJuegoYSincronizar());
        }
    }

    private IEnumerator TerminarJuegoYSincronizar()
    {
        if (barraTiempo != null && barraTiempo2 != null)
        {
            barraTiempo.fillAmount = 0f;
            barraTiempo2.fillAmount = 0f;
        }

        txtPreguntaDisplay.text = "Guardando progreso...";

        // 1. Obtener y asegurar la seguridad local ganada (72%)
        float seguridadLocalGanada = ObtenerSeguridadPersistente();
        
        PlayerPrefs.SetFloat("SeguridadPersistente", seguridadLocalGanada);
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.nivelSeguridad = seguridadLocalGanada;
        }
        PlayerPrefs.Save();

        // 2. Enviar los puntos del minijuego al backend (leaderboard/ranking)
        yield return StartCoroutine(APIManager.Instance.SendScore(gamificacion.puntajeObtenido));

        // 3. Consultar las analíticas globales
        bool redCompletada = false;
        yield return StartCoroutine(APIManager.Instance.GetAnalytics((json) =>
        {
            if (json != "ERROR" && json != "NO_TOKEN")
            {
                AnalyticsData data = JsonUtility.FromJson<AnalyticsData>(json);
                
                float seguridadServidor = data.awareness_score;

                // 🟢 PROTECCIÓN: Mantenemos el 72% local ya que el servidor no bajó por malas decisiones
                float seguridadFinal = Mathf.Max(seguridadServidor, seguridadLocalGanada);

                if (GameManagerGlobal.instancia != null)
                {
                    GameManagerGlobal.instancia.nivelSeguridad = seguridadFinal;
                }

                if (gamificacion != null)
                {
                    gamificacion.progresoSeguridad = seguridadFinal;
                }

                PlayerPrefs.SetFloat("SeguridadPersistente", seguridadFinal);
                PlayerPrefs.Save();
                
                Debug.Log($"🛡️ Sincronización Minijuego | Servidor: {seguridadServidor}% | Local Ganado: {seguridadLocalGanada}% | Aplicado: {seguridadFinal}%");
            }
            redCompletada = true; 
        }));

        yield return new WaitUntil(() => redCompletada == true);

        ActualizarBarraSeguridadVisual();

        if (gamificacion.ObtenerVidasActuales() <= 0 || !perfectGame)
        {
            txtPreguntaDisplay.text = "¡Juego Terminado!";
            if (gamificacion.canvasGameOver != null)
            {
                gamificacion.canvasGameOver.SetActive(true);
            }
        }
        else
        {
            txtPreguntaDisplay.text = "¡Completaste el juego!";
            
            if (canvasGanador != null)
            {
                txtPuntosFinal.text = gamificacion.puntajeObtenido.ToString();
                txtVidasFinal.text = gamificacion.ObtenerVidasActuales().ToString();

                float segFinal = ObtenerSeguridadPersistente();
                txtSeguridadFinal.text = Mathf.RoundToInt(segFinal).ToString() + "%";

                canvasGanador.SetActive(true); 
            }
        }

        yield return new WaitForEndOfFrame();
        Time.timeScale = 0f; 
    }

    public IEnumerator SiguientePreguntaConDelay()
    {
        corriendoTiempo = false;
        yield return new WaitForSeconds(1f);

        foreach (var c in casillasSeleccionadas) c.Resetear();
        casillasSeleccionadas.Clear();

        indicePreguntaActual++;
        bloqueado = false;
        cambiandoPregunta = false;

        if (indicePreguntaActual >= bancoDePreguntas.Length)
        {
            PasarSiguientePregunta();
            yield break;
        }

        tiempoRestante = TIEMPO_MAX;
        corriendoTiempo = true;
        MostrarPregunta();
    }
    
    public bool PuedeInteractuar() { return !bloqueado; }

    void Update()
    {
        if (!corriendoTiempo) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante < 0f) tiempoRestante = 0f;

        if (barraTiempo != null && barraTiempo2 != null)
        {
            barraTiempo.fillAmount = tiempoRestante / TIEMPO_MAX;
            barraTiempo2.fillAmount = tiempoRestante / TIEMPO_MAX;
        }

        if (txtTiempo != null && txtTiempo2 != null)
        {
            txtTiempo.text = Mathf.CeilToInt(tiempoRestante).ToString();
            txtTiempo2.text = Mathf.CeilToInt(tiempoRestante).ToString();
        }

        if (tiempoRestante <= 0f)
        {
            corriendoTiempo = false;
            perfectGame = false;
            gamificacion.RegistrarError();

            ResetSeleccion("⏰ Tiempo agotado");
            bloqueado = true;
            StartCoroutine(SiguientePreguntaConDelay());
        }
    }

    private IEnumerator FeedbackErrorConDelay(string mensaje)
    {
        foreach (var c in casillasSeleccionadas) c.MarcarIncorrecta();
        yield return new WaitForSeconds(0.5f);
        foreach (var c in casillasSeleccionadas) c.Resetear();
        
        casillasSeleccionadas.Clear();
        if (gamificacion.ObtenerVidasActuales() > 0) bloqueado = false;
    }

    public void DetenerJuegoPorGameOver()
    {
        bloqueado = true;
        corriendoTiempo = false;

        foreach (var c in casillasSeleccionadas) if (c != null) c.Resetear();
        casillasSeleccionadas.Clear();

        txtPreguntaDisplay.text = "Guardando progreso...";
        StartCoroutine(TerminarJuegoYSincronizar());
    }

    private float ObtenerSeguridadPersistente()
    {
        // 1. Si el controlador del minijuego tiene un valor actual válido, usar ese
        if (gamificacion != null && gamificacion.progresoSeguridad > 0)
            return gamificacion.progresoSeguridad;

        // 2. Si GameManagerGlobal existe, tomar su valor en memoria RAM
        if (GameManagerGlobal.instancia != null) 
            return GameManagerGlobal.instancia.nivelSeguridad;
            
        // 3. Si todo lo anterior falla, leer directamente del almacenamiento local (disco)
        return PlayerPrefs.GetFloat("SeguridadPersistente", 0f);
    }
}