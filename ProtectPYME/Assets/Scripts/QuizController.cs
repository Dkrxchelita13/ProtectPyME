using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizController : MonoBehaviour
{
    private float itemStartedAt;
    private bool currentItemAttemptRecorded;
    private bool usingSessionItems;
    private bool sessionCompletionScheduled;

    private bool usandoBackend = false;

    [System.Serializable]
    public class Pregunta
    {
        public string enunciado;
        public string[] opciones;
        public int indiceCorrecto;
    }

    [Header("Referencias de UI")]
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI[] textosOpciones;
    public TextMeshProUGUI textoTimer;
    public TextMeshProUGUI textoTimer2;
    public Image barraTiempo;
    public Image barraTiempo2;
    public Image[] imagenesBotones;
    public GameObject[] iconosResultado;

    [Header("Sprites de Feedback")]
    public Sprite spritePalomita;
    public Sprite spriteTache;

    [Header("Colores de Feedback")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    private Color colorOriginalBotones;

    [Header("Configuración")]
    public Pregunta[] bancoDePreguntas;
    public float tiempoPorPregunta = 15f;

    [Header("Progreso y Seguridad")]
    public Image barraSeguridad; // Solo necesitamos la seguridad aquí, vidas y puntos los maneja GamificacionController

    [Header("Pantallas Finales")]
    public GameObject canvasGanador;
    public TextMeshProUGUI textoPuntosFinal;
    public TextMeshProUGUI textoVidasFinal;
    public TextMeshProUGUI textoSeguridadFinal;

    private int preguntaActual = 0;
    private float tiempoRestante;
    private bool juegoActivo = true;

    // 🔥 Conexión con el ecosistema
    private GamificacionController gamificacion;
    private float seguridadInicial = 0f;
    private int respuestasCorrectas = 0;

    void Start()
    {
        Time.timeScale = 1f;
        
        // 🔗 Obtener el controlador central
        gamificacion = GetComponent<GamificacionController>();
        if (gamificacion != null)
        {
            // Jalamos la seguridad global del usuario
            seguridadInicial = gamificacion.ObtenerSeguridadActual();
            gamificacion.progresoSeguridad = seguridadInicial;
        }

        respuestasCorrectas = 0;
        preguntaActual = 0;
        juegoActivo = false;

        // Mostrar la seguridad jalada inmediatamente en pantalla
        ActualizarBarraSeguridadVisual();

        if (imagenesBotones.Length > 0)
            colorOriginalBotones = imagenesBotones[0].color;

        if (TryLoadQuizItemsFromSession())
        {
            CargarPregunta();
            return;
        }

        Debug.Log("Quiz: usando endpoint legacy");

        // Intentar jalar preguntas del backend
        if (APIManager.Instance != null)
        {
            StartCoroutine(
                APIManager.Instance.GetQuiz(
                    AIState.RecommendedTraining,
                    AIState.RiskLevel,
                    OnQuizLoaded
                )
            );
        }
        else
        {
            CargarPregunta(); // Si no hay API, carga local
        }
    }

    private bool TryLoadQuizItemsFromSession()
    {
        if (MinigameLessonState.Session != null &&
            !IsSessionForMinigame("quiz"))
        {
            Debug.LogWarning(
                "Quiz: la sesion pertenece a otro minijuego; usando endpoint legacy."
            );
            return false;
        }

        if (!MinigameLessonState.HasValidSession ||
            !IsSessionForMinigame("quiz"))
        {
            return false;
        }

        Pregunta[] sessionQuestions;

        if (!BuildQuizItemsFromSession(out sessionQuestions))
        {
            bancoDePreguntas = null;
            Debug.LogWarning(
                "Quiz: la sesion no cumple el contrato; usando endpoint legacy."
            );
            return false;
        }

        bancoDePreguntas = sessionQuestions;
        usandoBackend = true;
        usingSessionItems = true;

        Debug.Log(
            "Quiz: usando "
            + bancoDePreguntas.Length
            + " items de sesion "
            + MinigameLessonState.SessionId
        );

        return true;
    }

    private bool BuildQuizItemsFromSession(out Pregunta[] questions)
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
                string.IsNullOrEmpty(item.question) ||
                item.options == null ||
                item.options.Length == 0 ||
                item.correct_option < 0 ||
                item.correct_option >= item.options.Length)
            {
                return false;
            }

            result[i] = new Pregunta
            {
                enunciado = item.question,
                opciones = item.options,
                indiceCorrecto = item.correct_option
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

    void OnQuizLoaded(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "ERROR")
        {
            Debug.Log("⚠️ OFFLINE → usando banco local");
            usandoBackend = false;
            CargarPregunta();
            return;
        }

        Debug.Log("✅ QUIZ DESDE BACKEND");
        string fixedJson = "{\"items\":" + json + "}";
        QuizBackendList data = JsonUtility.FromJson<QuizBackendList>(fixedJson);

        if (data == null || data.items == null || data.items.Length == 0)
        {
            Debug.Log("⚠️ Backend vacío → usando banco local");
            usandoBackend = false;
            CargarPregunta();
            return;
        }

        usandoBackend = true;
        usingSessionItems = false;
        bancoDePreguntas = new Pregunta[data.items.Length];

        for (int i = 0; i < data.items.Length; i++)
        {
            bancoDePreguntas[i] = new Pregunta
            {
                enunciado = data.items[i].question ?? "Pregunta vacía",
                opciones = data.items[i].options ?? new string[] { "N/A" },
                indiceCorrecto = data.items[i].answer
            };
        }

        CargarPregunta();
    }

    [System.Serializable]
    public class QuizBackend { public string question; public string[] options; public int answer; }
    [System.Serializable]
    public class QuizBackendList { public QuizBackend[] items; }

    void Update()
    {
        if (juegoActivo)
        {
            ActualizarTemporizador();
        }
    }

    void CargarPregunta() 
    {
        if (bancoDePreguntas == null || bancoDePreguntas.Length == 0) return;

        if (preguntaActual < bancoDePreguntas.Length) 
        {
            RestablecerBotones();

            textoPregunta.text = bancoDePreguntas[preguntaActual].enunciado;
            for (int i = 0; i < textosOpciones.Length; i++)
            {
                if (i < bancoDePreguntas[preguntaActual].opciones.Length)
                    textosOpciones[i].text = bancoDePreguntas[preguntaActual].opciones[i];
                else
                    textosOpciones[i].text = "";
            }

            tiempoRestante = tiempoPorPregunta;
            itemStartedAt = Time.realtimeSinceStartup;
            currentItemAttemptRecorded = false;
            if (gamificacion != null) gamificacion.ReiniciarCronometro();
            juegoActivo = true;
        } 
        else 
        {
            juegoActivo = false;
            textoPregunta.text = "Guardando progreso...";
            StartCoroutine(TerminarJuegoYSincronizar());
        }
    }

    void RestablecerBotones() 
    {
        for (int i = 0; i < imagenesBotones.Length; i++) {
            imagenesBotones[i].color = colorOriginalBotones; 
            iconosResultado[i].SetActive(false); 
        }
    }

    void ActualizarTemporizador() 
    {
        tiempoRestante -= Time.deltaTime;
        
        if (barraTiempo != null && barraTiempo2 != null) {
            barraTiempo.fillAmount = tiempoRestante / tiempoPorPregunta;
            barraTiempo2.fillAmount = tiempoRestante / tiempoPorPregunta;
        }

        if (textoTimer != null && textoTimer2 != null) {
            textoTimer.text = Mathf.Ceil(tiempoRestante).ToString();
            textoTimer2.text = Mathf.Ceil(tiempoRestante).ToString();
        }

        // 🔥 TIMEOUT: Resta vida y te obliga a saltar a la siguiente pregunta
        if (tiempoRestante <= 0) 
        {
            juegoActivo = false;
            int puntosAntes = ObtenerPuntosActuales();
            if (gamificacion != null) 
            {
                gamificacion.ReproducirError();
                gamificacion.QuitarVida(); // Esto actualiza la UI de vidas automáticamente
                
                RegistrarIntentoActual(false, ObtenerPuntosActuales() - puntosAntes);

                if (gamificacion.ObtenerVidas() <= 0)
                {
                    StartCoroutine(TerminarJuegoYSincronizar());
                    return;
                }
            }
            RegistrarIntentoActual(false, ObtenerPuntosActuales() - puntosAntes);
            Invoke("SiguientePregunta", 2f);
        }
    }

    public void Responder(int indiceSeleccionado) 
    {
        if (!juegoActivo) return;
        juegoActivo = false;
        int puntosAntes = ObtenerPuntosActuales();

        int indiceCorrecto = bancoDePreguntas[preguntaActual].indiceCorrecto;
        Image imagenIcono = iconosResultado[indiceSeleccionado].GetComponent<Image>();

        if (indiceSeleccionado == indiceCorrecto) 
        {
            // 🟢 ACIERTO
            respuestasCorrectas++;
            if (gamificacion != null)
            {
                gamificacion.ReproducirAcierto();
                gamificacion.SumarPuntos(10); // Esto actualiza la UI del puntaje automáticamente
                gamificacion.ModificarSeguridad(1f);
                ActualizarBarraSeguridadVisual();
            }

            imagenesBotones[indiceSeleccionado].color = colorCorrecto;
            imagenIcono.sprite = spritePalomita;
        } 
        else 
        {
            // 🔴 ERROR: En Kahoot, 1 error = 1 vida menos
            if (gamificacion != null) 
            {
                gamificacion.ReproducirError();
                gamificacion.QuitarVida(); // Esto actualiza la UI de vidas automáticamente
            }

            imagenesBotones[indiceSeleccionado].color = colorIncorrecto;
            imagenIcono.sprite = spriteTache;
        }

        iconosResultado[indiceSeleccionado].SetActive(true);
        RegistrarIntentoActual(
            indiceSeleccionado == indiceCorrecto,
            ObtenerPuntosActuales() - puntosAntes
        );

        if (gamificacion != null && gamificacion.ObtenerVidas() <= 0)
        {
            StartCoroutine(TerminarJuegoYSincronizar());
        }
        else
        {
            Invoke("SiguientePregunta", 2f);
        }
    }

    // 🛡️ Actualiza visualmente el fill de la barra de seguridad local
    void ActualizarBarraSeguridadVisual()
    {
        if (barraSeguridad != null && gamificacion != null)
        {
            barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;
        }
    }

    void SiguientePregunta() 
    {
        preguntaActual++;
        CargarPregunta();
    }

    private IEnumerator TerminarJuegoYSincronizar()
    {
        juegoActivo = false;
        ScheduleSessionCompletion();
        if (barraTiempo != null && barraTiempo2 != null)
        {
            barraTiempo.fillAmount = 0f;
            barraTiempo2.fillAmount = 0f;
        }

        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        float seguridadLocalGanada = ObtenerSeguridadPersistente();

        // 🏆 LOGICA ESTRICTA: Solo gana si contestó TODAS bien y sigue vivo
        bool ganoElJuego = (gamificacion != null && gamificacion.ObtenerVidas() > 0 && respuestasCorrectas == bancoDePreguntas.Length);

        if (!ganoElJuego)
        {
            seguridadLocalGanada = seguridadInicial;
            if (gamificacion != null)
            {
                gamificacion.progresoSeguridad = seguridadInicial;
                gamificacion.ReproducirDerrota();
            }
        }
        else
        {
            if (gamificacion != null) gamificacion.ReproducirVictoria();
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
                        seguridadFinal = seguridadInicial;

                    if (GameManagerGlobal.instancia != null)
                        GameManagerGlobal.instancia.nivelSeguridad = seguridadFinal;

                    if (gamificacion != null)
                        gamificacion.progresoSeguridad = seguridadFinal;

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
            textoPregunta.text = "¡Juego Terminado!";
            if (gamificacion != null && gamificacion.canvasGameOver != null)
            {
                gamificacion.canvasGameOver.SetActive(true);
                BeginFeedbackUiForCurrentSession(gamificacion.canvasGameOver);
            }
        }
        else
        {
            textoPregunta.text = "¡Completaste el juego!";
            if (canvasGanador != null)
            {
                if (textoPuntosFinal != null) textoPuntosFinal.text = gamificacion.ObtenerPuntos().ToString();
                if (textoVidasFinal != null) textoVidasFinal.text = gamificacion.ObtenerVidas().ToString();

                float segFinal = ObtenerSeguridadPersistente();
                if (textoSeguridadFinal != null) textoSeguridadFinal.text = Mathf.RoundToInt(segFinal).ToString() + "%";

                canvasGanador.SetActive(true);
                BeginFeedbackUiForCurrentSession(canvasGanador);
            }
        }

        yield return new WaitForEndOfFrame();
        Time.timeScale = 0f;
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
            !IsSessionForMinigame("quiz"))
        {
            return null;
        }

        MinigameSessionItem[] items = MinigameLessonState.GetItems();

        if (items == null ||
            preguntaActual < 0 ||
            preguntaActual >= items.Length ||
            items[preguntaActual] == null ||
            string.IsNullOrEmpty(items[preguntaActual].item_id))
        {
            return null;
        }

        return items[preguntaActual];
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
            !IsSessionForMinigame("quiz") ||
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
