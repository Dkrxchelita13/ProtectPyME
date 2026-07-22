using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CrosswordController : MonoBehaviour
{
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
    public float tiempoRestante = 90f;
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

    public Image barraSeguridad;

    // ➕ AGREGADO: Referencia al controlador de gamificación
    private GamificacionController gamificacion;
    private HashSet<CrosswordWordData> palabrasPuntuadas = new HashSet<CrosswordWordData>();

    void Start()
    {
        // ➕ AGREGADO: Obtener el GamificacionController de este objeto
        gamificacion = GetComponent<GamificacionController>();

        Debug.Log("🧹 LIMPIANDO OBJETOS EXTRA");

        if (gamificacion != null && barraSeguridad != null)
        {
            barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;
        }

        var extras = GameObject.FindObjectsOfType<SpriteRenderer>();

        foreach (var obj in extras)
        {
            if (obj.name.Contains("Clone"))
            {
                Destroy(obj.gameObject);
            }
        }

        // ➕ MODIFICADO: Usar los parámetros de AIState igual que en Sopa de Letras
        string token = APIManager.Instance.GetToken();
        if (string.IsNullOrEmpty(token)) return;

        StartCoroutine(APIManager.Instance.GetCrossword(AIState.RecommendedTraining, AIState.RiskLevel, OnData));
    }

    void Update()
    {
        if (juegoTerminado) return;

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
            barraTiempo.fillAmount = tiempoRestante / 90f;
            barraTiempo2.fillAmount = tiempoRestante / 90f;
        }

        if (tiempoRestante <= 0)
        {
            if (gamificacion != null)
            {
                gamificacion.QuitarVida();

                if (gamificacion.ObtenerVidas() <= 0)
                {
                    Debug.Log("💀 Sin vidas. Sincronizando y terminando el juego.");
                    juegoTerminado = true;
                    // 🔥 Llamamos a la sincronización en versión DERROTA
                    StartCoroutine(TerminarJuegoYSincronizar(false));
                }
                else
                {
                    tiempoRestante = 90f; // Reiniciamos el tiempo
                    Debug.Log($"💔 Tiempo agotado. Te quedan {gamificacion.ObtenerVidas()} vidas.");
                }
            }
        }
    }

    void OnData(string json)
    {
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

        model = generator.Generate(words);

        ShowClues();
        CreateGrid();
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
                    gamificacion.RegistrarError();
                }
            }
        }

        foreach (var word in model.words)
        {
            if (validator.ValidateWord(model, word))
            {
                MarkWord(word); //
                
                // 🔥 NUEVA LÓGICA DE GAMIFICACIÓN: Dar puntos por palabra correcta
                if (gamificacion != null && !palabrasPuntuadas.Contains(word))
                {
                    gamificacion.SumarPuntos(10); // 10 puntos, igual que la sopa de letras
                    gamificacion.ModificarSeguridad(1f); // 1% de seguridad por palabra (puedes ajustarlo)
                    if (barraSeguridad != null)
                    {
                        barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;
                    }
                    palabrasPuntuadas.Add(word); // La marcamos para no volver a dar puntos por esta
                    Debug.Log("🌟 ¡Palabra del crucigrama completada! +10 puntos y +15% seguridad");
                }
            }
        }

        if (AllWordsCompleted())
        {
            Debug.Log("🎉 GANASTE. Sincronizando y terminando el juego.");
            juegoTerminado = true;

            CrosswordInput input = FindObjectOfType<CrosswordInput>();
            if (input != null) input.enabled = false;

            // 🔥 Llamamos a la sincronización en versión VICTORIA
            StartCoroutine(TerminarJuegoYSincronizar(true));
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

    // 🔥 NUEVO: Sincronización de datos al terminar la partida (Ganar o Perder)
    private IEnumerator TerminarJuegoYSincronizar(bool victoria)
    {
        // 1. Guardar y asegurar la seguridad local ganada
        float seguridadLocalGanada = gamificacion != null ? gamificacion.progresoSeguridad : 0f;
        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        PlayerPrefs.SetFloat(claveSeguridad, seguridadLocalGanada);
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.nivelSeguridad = seguridadLocalGanada;
        }
        PlayerPrefs.Save();

        // 2. Enviar los puntos del minijuego al servidor
        if (gamificacion != null)
        {
            yield return StartCoroutine(APIManager.Instance.SendScore(gamificacion.ObtenerPuntos()));
        }

        // 3. Consultar las analíticas globales y cruzar datos (Mantenemos el valor más alto)
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
                
                Debug.Log($"🛡️ Sincronización Crucigrama | Servidor: {seguridadServidor}% | Local: {seguridadLocalGanada}% | Final: {seguridadFinal}%");
            }
            redCompletada = true; 
        }));

        yield return new WaitUntil(() => redCompletada == true);

        // Actualizamos visualmente por si acaso
        if (barraSeguridad != null && gamificacion != null) barraSeguridad.fillAmount = gamificacion.progresoSeguridad / 100f;

        // 4. Mostrar los Canvas correspondientes
        if (victoria)
        {
            if (gamificacion != null)
            {
                if (txtPuntosFinal != null) txtPuntosFinal.text = gamificacion.ObtenerPuntos().ToString();
                if (txtVidasFinal != null) txtVidasFinal.text = gamificacion.ObtenerVidas().ToString();
                if (txtSeguridadFinal != null) txtSeguridadFinal.text = gamificacion.progresoSeguridad.ToString("0") + "%";
            }
            if (canvasGanador != null) canvasGanador.SetActive(true);
        }
        else
        {
            if (gamificacion != null && gamificacion.canvasGameOver != null)
            {
                gamificacion.canvasGameOver.SetActive(true);
            }
            else
            {
                Perder(); // Backup en caso de no tener el canvas en Gamificacion
            }
        }
        yield return new WaitForEndOfFrame();
        Time.timeScale = 0f;
    }
}