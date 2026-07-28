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

    private float seguridadInicial = 0f;
    private int contadorErroresPalabra = 0;
    
    // 🔥 NUEVO: Contador para exigir que todas se contesten bien
    private int respuestasCorrectas = 0;

    void Start()
    {
        Time.timeScale = 1f;
        gamificacion = GetComponent<GamificacionController>();

        if (gamificacion != null)
        {
            seguridadInicial = gamificacion.ObtenerSeguridadActual();
            gamificacion.progresoSeguridad = seguridadInicial;
        }

        indicePreguntaActual = 0;
        respuestasCorrectas = 0;
        bloqueado = false;
        corriendoTiempo = false;
        casillasSeleccionadas.Clear();

        ActualizarBarraSeguridadVisual();

        string token = APIManager.Instance != null ? APIManager.Instance.GetToken() : "";
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
        if (bancoDePreguntas == null || bancoDePreguntas.Length == 0)
        {
            if (txtPreguntaDisplay != null) 
                txtPreguntaDisplay.text = "No hay preguntas disponibles.";
            return;
        }

        if (indicePreguntaActual < bancoDePreguntas.Length)
        {
            tiempoRestante = TIEMPO_MAX;
            corriendoTiempo = true;
            txtPreguntaDisplay.text = bancoDePreguntas[indicePreguntaActual].textoPregunta;
            
            var generador = GetComponent<GeneradorSopa>();
            if (generador != null)
            {
                generador.GenerarLetras(bancoDePreguntas[indicePreguntaActual].respuestaCorrecta);
            }
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

        if (seleccionada && !casillasSeleccionadas.Contains(casilla)) 
            casillasSeleccionadas.Add(casilla);
        else 
            casillasSeleccionadas.Remove(casilla);

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

            // 🔥 Cuenta la respuesta como válida
            respuestasCorrectas++;

            if (gamificacion != null)
            {
                gamificacion.ReproducirAcierto();
                gamificacion.SumarPuntos(10);
                gamificacion.ModificarSeguridad(1f);
                ActualizarBarraSeguridadVisual();
            }

            foreach (var c in casillasSeleccionadas) c.MarcarCorrecta();
            
            StartCoroutine(SiguientePreguntaConDelay());
        }
        else if (formada.Length >= correcta.Length)
        {
            bloqueado = true;
            contadorErroresPalabra++;

            if (gamificacion != null) gamificacion.ReproducirError();

            if (contadorErroresPalabra >= 2)
            {
                contadorErroresPalabra = 0;
                if (gamificacion != null) gamificacion.QuitarVida(); 
            }

            if (gamificacion != null && gamificacion.ObtenerVidas() > 0)
            {
                StartCoroutine(FeedbackErrorConDelay("❌ Incorrecta"));
            }
            else
            {
                DetenerJuegoPorGameOver();
            }
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

    private IEnumerator TerminarJuegoYSincronizar()
    {
        if (barraTiempo != null && barraTiempo2 != null)
        {
            barraTiempo.fillAmount = 0f;
            barraTiempo2.fillAmount = 0f;
        }

        txtPreguntaDisplay.text = "Guardando progreso...";

        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        float seguridadLocalGanada = ObtenerSeguridadPersistente();

        // 🔥 LOGICA ESTRICTA: Solo gana si contestó TODAS bien y sigue vivo
        bool ganoElJuego = (gamificacion != null && gamificacion.ObtenerVidas() > 0 && respuestasCorrectas == bancoDePreguntas.Length);

        // Si falló (ya sea por vidas o por saltarse preguntas por tiempo), pierde el progreso
        if (!ganoElJuego)
        {
            seguridadLocalGanada = seguridadInicial;
            if (gamificacion != null) gamificacion.progresoSeguridad = seguridadInicial;
        }

        PlayerPrefs.SetFloat(claveSeguridad, seguridadLocalGanada);
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.nivelSeguridad = seguridadLocalGanada;
        }
        PlayerPrefs.Save();

        if (APIManager.Instance != null && gamificacion != null)
        {
            yield return StartCoroutine(APIManager.Instance.SendScore(gamificacion.ObtenerPuntos()));
        }

        bool redCompletada = false;
        float tiempoEspera = 0f;
        float tiempoLimiteRed = 5f; 

        if (APIManager.Instance != null)
        {
            StartCoroutine(APIManager.Instance.GetAnalytics((json) =>
            {
                if (json != "ERROR" && json != "NO_TOKEN")
                {
                    AnalyticsData data = JsonUtility.FromJson<AnalyticsData>(json);
                    float seguridadServidor = data.awareness_score;
                    float seguridadFinal = Mathf.Max(seguridadServidor, seguridadLocalGanada);

                    if (!ganoElJuego)
                    {
                        seguridadFinal = seguridadInicial;
                    }

                    if (GameManagerGlobal.instancia != null)
                    {
                        GameManagerGlobal.instancia.nivelSeguridad = seguridadFinal;
                    }

                    if (gamificacion != null)
                    {
                        gamificacion.progresoSeguridad = seguridadFinal;
                    }

                    PlayerPrefs.SetFloat(claveSeguridad, seguridadFinal);
                    PlayerPrefs.Save();
                }
                redCompletada = true; 
            }));

            while (!redCompletada && tiempoEspera < tiempoLimiteRed)
            {
                tiempoEspera += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        ActualizarBarraSeguridadVisual();

        // 3. Desplegar pantalla correspondiente
        if (!ganoElJuego)
        {
            if (gamificacion != null) gamificacion.ReproducirDerrota();
            txtPreguntaDisplay.text = "¡Juego Terminado!";
            if (gamificacion != null && gamificacion.canvasGameOver != null)
            {
                gamificacion.canvasGameOver.SetActive(true);
            }
        }
        else
        {
            if (gamificacion != null) gamificacion.ReproducirVictoria();
            
            txtPreguntaDisplay.text = "¡Completaste el juego!";
            if (canvasGanador != null)
            {
                if (txtPuntosFinal != null) txtPuntosFinal.text = gamificacion.ObtenerPuntos().ToString();
                if (txtVidasFinal != null) txtVidasFinal.text = gamificacion.ObtenerVidas().ToString();

                float segFinal = ObtenerSeguridadPersistente();
                if (txtSeguridadFinal != null) txtSeguridadFinal.text = Mathf.RoundToInt(segFinal).ToString() + "%";

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

        if (gamificacion != null && gamificacion.ObtenerVidas() <= 0)
        {
            yield break;
        }

        foreach (var c in casillasSeleccionadas) c.Resetear();
        casillasSeleccionadas.Clear();

        indicePreguntaActual++;
        bloqueado = false;
        cambiandoPregunta = false;

        if (indicePreguntaActual >= bancoDePreguntas.Length)
        {
            bloqueado = true;
            corriendoTiempo = false;
            txtPreguntaDisplay.text = "Guardando progreso...";
            StartCoroutine(TerminarJuegoYSincronizar());
            yield break;
        }

        tiempoRestante = TIEMPO_MAX;
        corriendoTiempo = true;
        if (gamificacion != null) gamificacion.ReiniciarCronometro();
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

        // 🔥 TIMEOUT: Resta vida y SALTA a la siguiente palabra
        if (tiempoRestante <= 0f)
        {
            corriendoTiempo = false;
            bloqueado = true;
            ResetSeleccion("⏰ Tiempo agotado");

            if (gamificacion != null)
            {
                gamificacion.ReproducirError();
                contadorErroresPalabra = 0; 
                gamificacion.QuitarVida(); 

                if (gamificacion.ObtenerVidas() > 0)
                {
                    // Como el tiempo se acabó, obligamos al jugador a avanzar perdiendo esa palabra
                    StartCoroutine(SiguientePreguntaConDelay());
                }
                else
                {
                    DetenerJuegoPorGameOver();
                }
            }
        }
    }

    private IEnumerator FeedbackErrorConDelay(string mensaje)
    {
        foreach (var c in casillasSeleccionadas) c.MarcarIncorrecta();
        yield return new WaitForSeconds(0.5f);
        foreach (var c in casillasSeleccionadas) c.Resetear();

        casillasSeleccionadas.Clear();
        if (gamificacion != null && gamificacion.ObtenerVidas() > 0) bloqueado = false;
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
        if (gamificacion != null && gamificacion.progresoSeguridad > 0)
            return gamificacion.progresoSeguridad;

        if (GameManagerGlobal.instancia != null) 
            return GameManagerGlobal.instancia.nivelSeguridad;

        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        return PlayerPrefs.GetFloat(claveSeguridad, 0f);
    }
}
