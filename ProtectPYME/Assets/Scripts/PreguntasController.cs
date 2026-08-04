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
    private float itemStartedAt;
    private bool currentItemAttemptRecorded;
    private bool usingSessionItems;
    private bool sessionCompletionScheduled;

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
    private bool firstQuestionStarted = false;
    private GeneradorSopa generadorSopa;

    private float seguridadInicial = 0f;
    private int contadorErroresPalabra = 0;
    
    // 🔥 NUEVO: Contador para exigir que todas se contesten bien
    private int respuestasCorrectas = 0;

    void Start()
    {
        Time.timeScale = 1f;
        gamificacion = GetComponent<GamificacionController>();
        generadorSopa = GetComponent<GeneradorSopa>();

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

        if (TryLoadWordsearchItemsFromSession())
        {
            StartCoroutine(StartFirstQuestionWhenGridReady());
            return;
        }

        Debug.Log("Wordsearch: usando endpoint legacy");

        string token = APIManager.Instance != null ? APIManager.Instance.GetToken() : "";
        if (string.IsNullOrEmpty(token)) return;

        StartCoroutine(APIManager.Instance.GetWords(AIState.RecommendedTraining, AIState.RiskLevel, OnCrosswordLoaded));
    }

    private bool TryLoadWordsearchItemsFromSession()
    {
        if (MinigameLessonState.Session != null &&
            !IsSessionForMinigame("wordsearch"))
        {
            Debug.LogWarning(
                "Wordsearch: la sesion pertenece a otro minijuego; usando endpoint legacy."
            );
            return false;
        }

        if (!MinigameLessonState.HasValidSession ||
            !IsSessionForMinigame("wordsearch"))
        {
            return false;
        }

        Pregunta[] sessionQuestions;

        if (!BuildWordsearchItemsFromSession(out sessionQuestions))
        {
            bancoDePreguntas = null;
            Debug.LogWarning(
                "Wordsearch: la sesion no cumple el contrato; usando endpoint legacy."
            );
            return false;
        }

        bancoDePreguntas = sessionQuestions;
        usingSessionItems = true;

        Debug.Log(
            "Wordsearch: usando "
            + bancoDePreguntas.Length
            + " items de sesion "
            + MinigameLessonState.SessionId
        );

        return true;
    }

    private bool BuildWordsearchItemsFromSession(out Pregunta[] questions)
    {
        questions = null;
        MinigameSessionItem[] items = MinigameLessonState.GetItems();

        if (items == null || items.Length == 0)
        {
            return false;
        }

        Pregunta[] result = new Pregunta[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            MinigameSessionItem item = items[i];

            if (item == null ||
                string.IsNullOrEmpty(item.clue) ||
                string.IsNullOrEmpty(item.answer_text) ||
                item.correct_option != -1)
            {
                return false;
            }

            result[i] = new Pregunta
            {
                textoPregunta = item.clue,
                respuestaCorrecta = item.answer_text
            };
        }

        questions = result;
        return true;
    }

    private bool IsSessionForMinigame(string minigame)
    {
        return MinigameLessonState.Session != null &&
            string.Equals(
                MinigameLessonState.Session.minigame,
                minigame,
                System.StringComparison.OrdinalIgnoreCase
            );
    }

    private IEnumerator StartFirstQuestionWhenGridReady()
    {
        if (firstQuestionStarted)
        {
            yield break;
        }

        yield return null;

        if (!EnsureWordsearchGridReady())
        {
            if (txtPreguntaDisplay != null)
            {
                txtPreguntaDisplay.text = "No se pudo preparar la sopa de letras.";
            }
            yield break;
        }

        firstQuestionStarted = true;
        MostrarPregunta();
        Debug.Log("Wordsearch: primera pregunta iniciada");
    }

    private bool EnsureWordsearchGridReady()
    {
        if (generadorSopa == null)
        {
            generadorSopa = GetComponent<GeneradorSopa>();
        }

        if (generadorSopa == null)
        {
            Debug.LogError("Wordsearch: no se encontro GeneradorSopa.");
            return false;
        }

        return generadorSopa.EnsureGridInitialized() &&
            generadorSopa.IsGridReady;
    }

    void OnCrosswordLoaded(string json)
    {
        usingSessionItems = false;
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
        StartCoroutine(StartFirstQuestionWhenGridReady());
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
            if (!EnsureWordsearchGridReady())
            {
                bloqueado = true;
                corriendoTiempo = false;
                Debug.LogError("Wordsearch: grid no listo; no se inicia la pregunta.");
                return;
            }

            if (string.IsNullOrEmpty(bancoDePreguntas[indicePreguntaActual].respuestaCorrecta))
            {
                bloqueado = true;
                corriendoTiempo = false;
                Debug.LogError("Wordsearch: respuesta vacia; no se inicia la pregunta.");
                return;
            }

            tiempoRestante = TIEMPO_MAX;
            itemStartedAt = Time.realtimeSinceStartup;
            currentItemAttemptRecorded = false;
            corriendoTiempo = true;
            txtPreguntaDisplay.text = bancoDePreguntas[indicePreguntaActual].textoPregunta;

            if (generadorSopa != null)
            {
                generadorSopa.GenerarLetras(bancoDePreguntas[indicePreguntaActual].respuestaCorrecta);
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
            int puntosAntes = ObtenerPuntosActuales();
            respuestasCorrectas++;

            if (gamificacion != null)
            {
                gamificacion.ReproducirAcierto();
                gamificacion.SumarPuntos(10);
                gamificacion.ModificarSeguridad(1f);
                ActualizarBarraSeguridadVisual();
            }

            foreach (var c in casillasSeleccionadas) c.MarcarCorrecta();
            RegistrarIntentoActual(true, ObtenerPuntosActuales() - puntosAntes);
            
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
        ScheduleSessionCompletion();
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
                BeginFeedbackUiForCurrentSession(gamificacion.canvasGameOver);
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
                BeginFeedbackUiForCurrentSession(canvasGanador);
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
                int puntosAntesTimeout = ObtenerPuntosActuales();

            if (gamificacion != null)
            {
                gamificacion.ReproducirError();
                contadorErroresPalabra = 0; 
                    gamificacion.QuitarVida();
                    RegistrarIntentoActual(
                        false,
                        ObtenerPuntosActuales() - puntosAntesTimeout
                    );

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
            RegistrarIntentoActual(false, ObtenerPuntosActuales() - puntosAntesTimeout);
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

    private void BeginFeedbackUiForCurrentSession(GameObject finalPanel)
    {
        if (!usingSessionItems || !MinigameLessonState.HasValidSession)
        {
            Debug.Log("Feedback UI: omitido porque el flujo es legacy");
            return;
        }

        if (finalPanel == null)
        {
            return;
        }

        MinigameFeedbackPresenter presenter =
            MinigameFeedbackPresenter.AttachOrGet(finalPanel.transform);

        if (presenter != null)
        {
            presenter.BeginWaitingForFeedback(MinigameLessonState.SessionId);
        }
    }

    private void RegistrarIntentoActual(bool correcto, int pointsDelta)
    {
        if (currentItemAttemptRecorded)
        {
            return;
        }

        MinigameSessionItem item = ObtenerItemActualSesion();

        if (item == null)
        {
            Debug.LogWarning("Attempt: omitido porque el flujo es legacy");
            return;
        }

        currentItemAttemptRecorded = true;
        EnviarIntento(item.item_id, correcto, pointsDelta);
    }

    private MinigameSessionItem ObtenerItemActualSesion()
    {
        if (!usingSessionItems ||
            !MinigameLessonState.HasValidSession ||
            !IsSessionForMinigame("wordsearch"))
        {
            return null;
        }

        MinigameSessionItem[] items = MinigameLessonState.GetItems();

        if (items == null ||
            indicePreguntaActual < 0 ||
            indicePreguntaActual >= items.Length ||
            items[indicePreguntaActual] == null ||
            string.IsNullOrEmpty(items[indicePreguntaActual].item_id))
        {
            return null;
        }

        return items[indicePreguntaActual];
    }

    private void EnviarIntento(string itemId, bool correcto, int pointsDelta)
    {
        if (APIManager.Instance == null)
        {
            Debug.LogWarning("Attempt no registrado item=" + itemId + ": APIManager no disponible");
            return;
        }

        int responseTimeMs = CalcularResponseTimeMs();

        MinigameAttemptRequest request = new MinigameAttemptRequest
        {
            session_id = MinigameLessonState.SessionId,
            item_id = itemId,
            correct = correcto,
            response_time_ms = responseTimeMs,
            attempt_number = 1,
            points_delta = pointsDelta
        };

        StartCoroutine(APIManager.Instance.RecordMinigameAttempt(
            request,
            (_) =>
            {
                Debug.Log("Attempt registrado item=" + itemId);
            },
            (mensaje) =>
            {
                Debug.LogWarning("Attempt no registrado item=" + itemId + ": " + mensaje);
            }
        ));
    }

    private int CalcularResponseTimeMs()
    {
        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                (Time.realtimeSinceStartup - itemStartedAt) * 1000f
            )
        );
    }

    private int ObtenerPuntosActuales()
    {
        return gamificacion != null ? gamificacion.ObtenerPuntos() : 0;
    }

    private void ScheduleSessionCompletion()
    {
        if (sessionCompletionScheduled)
        {
            return;
        }

        if (!usingSessionItems ||
            !MinigameLessonState.HasValidSession ||
            !IsSessionForMinigame("wordsearch") ||
            string.IsNullOrEmpty(MinigameLessonState.SessionId))
        {
            Debug.LogWarning("Session completion: omitido porque el flujo es legacy");
            return;
        }

        if (APIManager.Instance == null)
        {
            Debug.LogWarning("Session completion: APIManager no disponible");
            return;
        }

        sessionCompletionScheduled = true;
        string sessionId = MinigameLessonState.SessionId;

        APIManager.Instance.StartCoroutine(
            APIManager.Instance.CompleteMinigameSessionWhenReady(
                sessionId,
                12f,
                (summary) =>
                {
                    Debug.Log(
                        "Session completada id=" + summary.session_id +
                        " accuracy=" + summary.accuracy + "% " +
                        "attempts=" + summary.total_attempts
                    );
                },
                (mensaje) =>
                {
                    sessionCompletionScheduled = false;
                    Debug.LogWarning(
                        "Session no completada id=" + sessionId + ": " + mensaje
                    );
                }
            )
        );
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
