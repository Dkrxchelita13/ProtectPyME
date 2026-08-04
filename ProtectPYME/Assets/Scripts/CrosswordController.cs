using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CrosswordController : MonoBehaviour
{
    private float itemStartedAt;
    private bool currentItemAttemptRecorded;
    private bool usingSessionItems;
    private bool sessionCompletionScheduled;

    public GameObject cellPrefab;
    public Transform gridParent;
    public TextMeshProUGUI cluesText;

    public Tilemap tilemap;
    public TileBase tileBlanco;

    public GameObject numeroPrefab;

    private CrosswordModel model;
    public CrosswordModel GetModel()
    {
        return model;
    }
    private CrosswordValidator validator = new CrosswordValidator();
    private CrosswordGenerator generator = new CrosswordGenerator();

    private List<CellWorld> spawnedCells = new List<CellWorld>();

    [SerializeField]
    private Vector2 gridOffset;
    public List<CellWorld> GetAllCells()
    {
        return spawnedCells;
    }

    [Header("Timer")]
    public float tiempoRestante = 30f;
    public TextMeshProUGUI txtTimer;
    public Image barraTiempo;
    public TextMeshProUGUI txtTimer2;
    public Image barraTiempo2;   

    private bool juegoTerminado = false;

    [Header("Paneles Fin de Juego")]
    public GameObject canvasGanador;
    public GameObject canvasFinJuego;

    // ➕ AGREGADO: Referencias de los textos para mostrar Puntos y Vidas al ganar
    [Header("UI Resultados Ganador")]
    public TextMeshProUGUI txtPuntosFinal;
    public TextMeshProUGUI txtVidasFinal;
    public TextMeshProUGUI txtSeguridadFinal;

    public int indicePreguntaActual = 0;
    public Image barraSeguridad;
    private float seguridadInicial;

    // ➕ AGREGADO: Referencia al controlador de gamificación
    private GamificacionController gamificacion;
    private HashSet<CrosswordWordData> palabrasPuntuadas = new HashSet<CrosswordWordData>();
    private bool crosswordInitialized = false;

    void Start()
    {
        // ➕ AGREGADO: Obtener el GamificacionController de este objeto
        gamificacion = GetComponent<GamificacionController>();

        Debug.Log("🧹 LIMPIANDO OBJETOS EXTRA");

        if (gamificacion != null && barraSeguridad != null)
        {
            seguridadInicial = gamificacion.ObtenerSeguridadActual();

            if (barraSeguridad != null)
            {
                barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;
            }
        }

        var extras = GameObject.FindObjectsOfType<SpriteRenderer>();

        foreach (var obj in extras)
        {
            if (obj.name.Contains("Clone"))
            {
                Destroy(obj.gameObject);
            }
        }

        StartCoroutine(InitializeCrosswordFlow());
    }

    void Update()
    {
        if (juegoTerminado) return;

        if (!crosswordInitialized) return;

        if (gamificacion != null && gamificacion.ObtenerVidas() <= 0)
        {
            Debug.Log("💀 Sin vidas por errores. Sincronizando y terminando el juego.");
            juegoTerminado = true;
            StartCoroutine(TerminarJuegoYSincronizar(false));
            return; 
        }

        tiempoRestante -= Time.deltaTime;

        if (txtTimer != null && txtTimer2 != null)
        {
            txtTimer.text = Mathf.Ceil(tiempoRestante).ToString();
            txtTimer2.text = Mathf.Ceil(tiempoRestante).ToString();
        }

        if (barraTiempo != null && barraTiempo2 != null)
        {
            barraTiempo.fillAmount = tiempoRestante / 30f;
            barraTiempo2.fillAmount = tiempoRestante / 30f;
        }

        if (tiempoRestante <= 0)
        {
            int puntosAntesTimeout = ObtenerPuntosActuales();

            if (model != null && model.words != null && indicePreguntaActual < model.words.Count)
            {
                CrosswordWordData palabraActual = model.words[indicePreguntaActual];

                // Si no contestó correctamente la palabra actual al terminar el tiempo
                if (!palabrasPuntuadas.Contains(palabraActual))
                {
                    if (gamificacion != null)
                    {
                        gamificacion.ReproducirError();
                        gamificacion.QuitarVida();
                        RegistrarIntentoPalabra(
                            palabraActual,
                            false,
                            ObtenerPuntosActuales() - puntosAntesTimeout
                        );
                        Debug.Log($"⏳ Tiempo agotado. Palabra incorrecta/vacía. Vidas restantes: {gamificacion.ObtenerVidas()}");

                        if (gamificacion.ObtenerVidas() <= 0)
                        {
                            juegoTerminado = true;
                            StartCoroutine(TerminarJuegoYSincronizar(false));
                            return;
                        }
                    }
                    RegistrarIntentoPalabra(
                        palabraActual,
                        false,
                        ObtenerPuntosActuales() - puntosAntesTimeout
                    );
                }
            }
            AvanzarSiguientePregunta();
        }
    }

    private IEnumerator InitializeCrosswordFlow()
    {
        if (TryLoadCrosswordItemsFromSession())
        {
            yield break;
        }

        Debug.Log("Crossword: usando endpoint legacy");

        if (APIManager.Instance == null)
        {
            Debug.Log("Crossword: esperando APIManager para endpoint legacy");
            float startedAt = Time.realtimeSinceStartup;

            while (
                APIManager.Instance == null &&
                Time.realtimeSinceStartup - startedAt < 3f
            )
            {
                yield return null;
            }

            if (APIManager.Instance == null)
            {
                Debug.LogError(
                    "Crossword: APIManager no disponible despues del tiempo de espera"
                );
                yield break;
            }
        }

        string token = APIManager.Instance.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Crossword: no hay token para endpoint legacy.");
            yield break;
        }

        bool legacyCompleted = false;
        string legacyJson = "";

        StartCoroutine(APIManager.Instance.GetCrossword(
            AIState.RecommendedTraining,
            AIState.RiskLevel,
            (json) =>
            {
                legacyJson = json;
                legacyCompleted = true;
            }
        ));

        while (!legacyCompleted)
        {
            yield return null;
        }

        Debug.Log("Crossword: datos legacy recibidos");
        OnData(legacyJson);
    }

    private bool TryLoadCrosswordItemsFromSession()
    {
        if (MinigameLessonState.Session != null &&
            !IsSessionForMinigame("crossword"))
        {
            Debug.LogWarning(
                "Crossword: la sesion pertenece a otro minijuego; usando endpoint legacy."
            );
            return false;
        }

        if (!MinigameLessonState.HasValidSession ||
            !IsSessionForMinigame("crossword"))
        {
            return false;
        }

        List<CrosswordWordData> sessionWords;

        if (!BuildCrosswordItemsFromSession(out sessionWords))
        {
            Debug.LogWarning(
                "Crossword: la sesion no cumple el contrato; usando endpoint legacy."
            );
            return false;
        }

        Debug.Log(
            "Crossword: usando "
            + sessionWords.Count
            + " items de sesion "
            + MinigameLessonState.SessionId
        );

        usingSessionItems = true;
        InitializeGameFromWords(sessionWords);
        return true;
    }

    private bool BuildCrosswordItemsFromSession(out List<CrosswordWordData> words)
    {
        words = null;
        MinigameSessionItem[] items = MinigameLessonState.GetItems();

        if (items == null || items.Length == 0)
        {
            return false;
        }

        List<CrosswordWordData> result = new List<CrosswordWordData>();

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

            result.Add(new CrosswordWordData
            {
                clue = item.clue,
                answer = item.answer_text
            });
        }

        words = result;
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

    void OnData(string json)
    {
        usingSessionItems = false;
        Debug.Log("📦 JSON recibido: " + json);

        List<CrosswordWordData> words;

        if (json == "ERROR" || json == "NO_TOKEN")
        {
            Debug.Log("⚠️ USANDO OFFLINE");
            words = GetOffline();
        }
        else
        {
            words = new List<CrosswordWordData>(
                JsonHelper.FromJson<CrosswordWordData>(json)
            );
        }

        Debug.Log("🧠 Cantidad de palabras: " + words.Count);

        foreach (var w in words)
        {
            Debug.Log("➡️ " + w.clue + " | " + w.answer);
        }

        InitializeGameFromWords(words);
    }

    private void InitializeGameFromWords(List<CrosswordWordData> words)
    {
        if (crosswordInitialized)
        {
            Debug.LogWarning("Crossword: inicializacion duplicada ignorada.");
            return;
        }

        if (words == null || words.Count == 0)
        {
            Debug.LogError("Crossword: no hay palabras validas para inicializar.");
            return;
        }

        model = generator.Generate(words);

        if (model == null || model.words == null || model.words.Count == 0)
        {
            Debug.LogError("Crossword: el generador no produjo un modelo valido.");
            return;
        }

        ShowClues();
        CreateGrid();
        crosswordInitialized = true;
        Debug.Log("Crossword: inicializado con " + words.Count + " palabras");
    }

    void CreateGrid()
    {
        Debug.Log("🚀 CREANDO GRID DESDE MODEL");

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        int width = model.width;
        int height = model.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CellData cellData = model.grid[x, y];

                if (cellData == null)
                    continue;

                if (cellData.correctLetter == '\0')
                    continue;

                Vector3Int tilePos = new Vector3Int(x, -y, 0);
                Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);

                worldPos += (Vector3)gridOffset;

                GameObject cell = Instantiate(cellPrefab);

                CellWorld cellWorld = cell.GetComponent<CellWorld>();

                spawnedCells.Add(cellWorld);

                cell.transform.SetParent(gridParent, true);
                cell.transform.position = worldPos;

                if (cellWorld != null)
                {
                    cellWorld.Init(x, y, this);
                    Debug.Log($"✅ CELDA CREADA [{x},{y}]");
                }

                foreach (var word in model.words)
                {
                    for (int i = 0; i < word.answer.Length; i++)
                    {
                        int wx = word.startX + (word.isHorizontal ? i : 0);
                        int wy = word.startY + (word.isHorizontal ? 0 : i);

                        if (wx == x && wy == y)
                        {
                            if (word.isHorizontal)
                            {
                                cellWorld.SetDirection(1, 0);
                            }
                            else
                            {
                                cellWorld.SetDirection(0, 1);
                            }
                        }
                    }
                }

                foreach (var word in model.words)
                {
                    if (word.startX == x && word.startY == y)
                    {
                        GameObject numero = Instantiate(numeroPrefab);

                        numero.transform.SetParent(gridParent, true);

                        numero.transform.position = worldPos + new Vector3(-0.42f, 0.42f, -2);

                        TextMesh txt = numero.GetComponent<TextMesh>();

                        txt.text = (model.words.IndexOf(word) + 1).ToString();
                    }
                }
            }
        }
        indicePreguntaActual = 0;
        tiempoRestante = 30f;

        BloquearTodasLasCasillas();
        if (model != null && model.words != null && model.words.Count > 0)
        {
            DesbloquearCasillasPalabra(model.words[indicePreguntaActual]);
            itemStartedAt = Time.realtimeSinceStartup;
            currentItemAttemptRecorded = false;
        }
    }

    public void OnLetterChanged(int x, int y, string letter)
    {
        model.grid[x, y].currentLetter = letter;

        CellWorld currentCell = GetCell(x, y);

        if (currentCell != null)
        {
            if (string.IsNullOrEmpty(letter))
            {
                currentCell.SetNormal();
            }
            else if (letter[0] == model.grid[x, y].correctLetter)
            {
                currentCell.SetCorrect();
                currentCell.LockCell();
            }
            else
            {
                currentCell.SetWrong();
                
                // ➕ AGREGADO: Quitar vida si la letra escrita es incorrecta
                if (gamificacion != null)
                {
                    gamificacion.ReproducirError();
                    gamificacion.RegistrarError();
                }
            }
        }

        foreach (var word in model.words)
        {
            if (validator.ValidateWord(model, word))
            {
                MarkWord(word); //
                
                if (gamificacion != null && !palabrasPuntuadas.Contains(word))
                {
                    int puntosAntes = ObtenerPuntosActuales();
                    gamificacion.ReproducirAcierto();
                    gamificacion.SumarPuntos(10); // 10 puntos, igual que la sopa de letras
                    gamificacion.ModificarSeguridad(1f); // 1% de seguridad por palabra (puedes ajustarlo)
                    if (barraSeguridad != null)
                    {
                        barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;
                    }
                    palabrasPuntuadas.Add(word); // La marcamos para no volver a dar puntos por esta
                    RegistrarIntentoPalabra(
                        word,
                        true,
                        ObtenerPuntosActuales() - puntosAntes
                    );
                    Debug.Log("🌟 ¡Palabra del crucigrama completada! +10 puntos y +1% seguridad");

                    if (model.words.IndexOf(word) == indicePreguntaActual)
                    {
                        AvanzarSiguientePregunta();
                    }
                }
            }
        }

        if (AllWordsCompleted() && !juegoTerminado)
        {
            Debug.Log("🎉 GANASTE. Sincronizando y terminando el juego.");
            juegoTerminado = true;

            CrosswordInput input = FindObjectOfType<CrosswordInput>();
            if (input != null) input.enabled = false;

            StartCoroutine(TerminarJuegoYSincronizar(true));
        }
    }

    public void AvanzarSiguientePregunta()
    {
        if (juegoTerminado) return;
        BloquearTodasLasCasillas();

        indicePreguntaActual++;

        if (model != null && indicePreguntaActual < model.words.Count)
        {
            tiempoRestante = 30f;
            itemStartedAt = Time.realtimeSinceStartup;
            currentItemAttemptRecorded = false;
            DesbloquearCasillasPalabra(model.words[indicePreguntaActual]);
            Debug.Log($"➡️ Turno de la pregunta {indicePreguntaActual + 1}");
        }
        else
        {
            Debug.Log("🏁 Se acabaron las palabras del crucigrama.");
            juegoTerminado = true;

            if (AllWordsCompleted())
            {
                StartCoroutine(TerminarJuegoYSincronizar(true));
            }
            else
            {
                StartCoroutine(TerminarJuegoYSincronizar(false));
            }
        }
    }

    private void BloquearTodasLasCasillas()
    {
        foreach (CellWorld cell in spawnedCells)
        {
            if (cell != null)
            {
                cell.LockCell();
            }
        }
    }

    private void DesbloquearCasillasPalabra(CrosswordWordData word)
    {
        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            CellWorld cell = GetCell(x, y);
            if (cell != null)
            {
                cell.UnlockCell();
            }
        }
    }

    void MarkWord(CrosswordWordData word)
    {
        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            foreach (Transform cell in gridParent)
            {
                CellWorld cw = cell.GetComponent<CellWorld>();

                if (cw != null && cw.IsPosition(x, y))
                {
                    cw.SetCorrect();
                }
            }
        }
    }

    bool AllWordsCompleted()
    {
        foreach (var word in model.words)
        {
            if (!validator.ValidateWord(model, word))
                return false;
        }
        return true;
    }

    private void RegistrarIntentoPalabra(
        CrosswordWordData word,
        bool correcto,
        int pointsDelta
    )
    {
        if (currentItemAttemptRecorded)
        {
            return;
        }

        MinigameSessionItem item = ObtenerItemSesion(word);

        if (item == null)
        {
            Debug.LogWarning("Attempt: omitido porque el flujo es legacy");
            return;
        }

        currentItemAttemptRecorded = true;
        EnviarIntento(item.item_id, correcto, pointsDelta);
    }

    private MinigameSessionItem ObtenerItemSesion(CrosswordWordData word)
    {
        if (!usingSessionItems ||
            !MinigameLessonState.HasValidSession ||
            !IsSessionForMinigame("crossword") ||
            model == null ||
            model.words == null)
        {
            return null;
        }

        int itemIndex = model.words.IndexOf(word);
        MinigameSessionItem[] items = MinigameLessonState.GetItems();

        if (items == null ||
            itemIndex < 0 ||
            itemIndex >= items.Length ||
            items[itemIndex] == null ||
            string.IsNullOrEmpty(items[itemIndex].item_id))
        {
            return null;
        }

        return items[itemIndex];
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
            !IsSessionForMinigame("crossword") ||
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

    List<CrosswordWordData> GetOffline()
    {
        return new List<CrosswordWordData>
        {
            new CrosswordWordData { clue="Ataque", answer="PHISHING"},
            new CrosswordWordData { clue="Malware", answer="MALWARE"}
        };
    }

    void Perder()
    {
        juegoTerminado = true;

        Debug.Log("💀 GAME OVER");

        if (canvasFinJuego != null)
        {
            canvasFinJuego.SetActive(true);
        }

        CrosswordInput input = FindObjectOfType<CrosswordInput>();

        if (input != null)
        {
            input.enabled = false;
        }
    }

    void ShowClues()
    {
        cluesText.text = "HORIZONTALES:\n";

        foreach (var word in model.words)
        {
            if (word.isHorizontal)
            {
                int numeroPalabra = model.words.IndexOf(word) + 1;
                cluesText.text += $"{numeroPalabra}. {word.clue}\n";
            }
        }

        cluesText.text += "\nVERTICALES:\n";

        foreach (var word in model.words)
        {
            if (!word.isHorizontal)
            {
                int numeroPalabra = model.words.IndexOf(word) + 1;
                cluesText.text += $"{numeroPalabra}. {word.clue}\n";
            }
        }
    }

    public CellWorld GetCell(int x, int y)
    {
        foreach (Transform child in gridParent)
        {
            CellWorld cell = child.GetComponent<CellWorld>();

            if (cell != null && cell.IsPosition(x, y))
            {
                return cell;
            }
        }

        return null;
    }

    // ✏️ REEMPLAZAR tu corrutina TerminarJuegoYSincronizar por esta versión:
    private IEnumerator TerminarJuegoYSincronizar(bool victoria)
    {
        ScheduleSessionCompletion();

        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        // 🔴 1. GESTIÓN DE SEGURIDAD (Diferenciada según victoria o derrota)
        if (victoria)
        {
            // 🟢 SI GANÓ: Sincroniza y guarda el aumento de seguridad local
            float seguridadLocalGanada = gamificacion != null ? gamificacion.progresoSeguridad : 0f;
            
            PlayerPrefs.SetFloat(claveSeguridad, seguridadLocalGanada);
            if (GameManagerGlobal.instancia != null)
            {
                GameManagerGlobal.instancia.nivelSeguridad = seguridadLocalGanada;
            }
            PlayerPrefs.Save();

            bool redCompletada = false;
            yield return StartCoroutine(APIManager.Instance.GetAnalytics((json) =>
            {
                if (json != "ERROR" && json != "NO_TOKEN")
                {
                    AnalyticsData data = JsonUtility.FromJson<AnalyticsData>(json);
                    float seguridadServidor = data.awareness_score;
                    float seguridadFinal = Mathf.Max(seguridadServidor, seguridadLocalGanada);

                    if (GameManagerGlobal.instancia != null) GameManagerGlobal.instancia.nivelSeguridad = seguridadFinal;
                    if (gamificacion != null) gamificacion.progresoSeguridad = seguridadFinal;

                    PlayerPrefs.SetFloat(claveSeguridad, seguridadFinal);
                    PlayerPrefs.Save();
                }
                redCompletada = true; 
            }));

            yield return new WaitUntil(() => redCompletada == true);
        }
        else
        {
            // 🔴 SI PERDIÓ: Revierte la seguridad al valor inicial y descarta el aumento
            if (GameManagerGlobal.instancia != null)
            {
                GameManagerGlobal.instancia.nivelSeguridad = seguridadInicial;
            }
            
            PlayerPrefs.SetFloat(claveSeguridad, seguridadInicial);
            PlayerPrefs.Save();
            
            if (gamificacion != null) gamificacion.progresoSeguridad = seguridadInicial;
            
            Debug.Log($"❌ Derrota: La seguridad vuelve a su valor inicial ({seguridadInicial}%).");
        }

        // ⭐ 2. GESTIÓN DE PUNTOS (SE GUARDAN SIEMPRE, GANE O PIERDA)
        if (gamificacion != null)
        {
            int puntosAcumulados = gamificacion.ObtenerPuntos();

            // Enviamos los puntos de esta sesión al backend
            yield return StartCoroutine(APIManager.Instance.SendScore(puntosAcumulados));

            // Acumulamos los puntos en el progreso global local
            if (GameManagerGlobal.instancia != null)
            {
                GameManagerGlobal.instancia.puntuacion += puntosAcumulados;
                
                string clavePuntos = GameManagerGlobal.instancia.ObtenerClaveUsuario("Puntuacion");
                PlayerPrefs.SetInt(clavePuntos, GameManagerGlobal.instancia.puntuacion);
                PlayerPrefs.Save();
            }

            Debug.Log($"⭐ Puntos conservados tras la partida ({puntosAcumulados} pts).");
        }

        if (barraSeguridad != null && gamificacion != null) 
            barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;

        // 📺 3. PANTALLAS FINALES
        if (victoria)
        {
            if (gamificacion != null)
            {
                gamificacion.ReproducirVictoria();

                if (txtPuntosFinal != null) txtPuntosFinal.text = gamificacion.ObtenerPuntos().ToString();
                if (txtVidasFinal != null) txtVidasFinal.text = gamificacion.ObtenerVidas().ToString();
                if (txtSeguridadFinal != null) txtSeguridadFinal.text = gamificacion.progresoSeguridad.ToString("0") + "%";
            }
            if (canvasGanador != null)
            {
                canvasGanador.SetActive(true);
                BeginFeedbackUiForCurrentSession(canvasGanador);
            }
        }
        else
        {
            if (gamificacion != null && gamificacion.canvasGameOver != null)
            {
                gamificacion.ReproducirDerrota();
                gamificacion.canvasGameOver.SetActive(true);
                BeginFeedbackUiForCurrentSession(gamificacion.canvasGameOver);
            }
            else
            {
                Perder();
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

    public void DetenerJuegoPorGameOver()
    {
        if (juegoTerminado) return;
        
        juegoTerminado = true;
        Debug.Log("💀 Game Over detectado desde GamificacionController. Sincronizando...");
        StartCoroutine(TerminarJuegoYSincronizar(false));
    }
}
