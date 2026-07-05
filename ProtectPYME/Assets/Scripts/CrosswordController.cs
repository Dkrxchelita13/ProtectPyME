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
    private List<Transform> spawnedGridItems = new List<Transform>();

    [SerializeField]
    private Vector2 gridOffset;

    [Header("Crossword Layout")]
    [SerializeField]
    private RectTransform crosswordContainer;

    [SerializeField]
    private float crosswordPadding = 24f;

    [SerializeField]
    private bool autoScaleCrossword = true;

    [Header("Clue Layout")]
    [SerializeField]
    private ScrollRect cluesScrollRect;

    [SerializeField]
    private RectTransform cluesViewport;

    [SerializeField]
    private RectTransform clueFooter;

    [SerializeField]
    private float maxClueTextHeight = 420f;

    [SerializeField]
    private float clueFooterSpacing = 16f;

    [SerializeField]
    private float clueHorizontalPadding = 18f;

    [SerializeField]
    private bool createScrollRectIfMissing = true;

    public List<CellWorld> GetAllCells()
    {
        return spawnedCells;
    }

    [Header("Timer")]
    public float tiempoRestante = 90f;

    [Header("Sistema de Vidas")]
    public int vidas = 3;

    public TextMeshProUGUI txtTimer;
    public Image barraTiempo;

    public TextMeshProUGUI txtTimer2;
    public Image barraTiempo2;

    private bool juegoTerminado = false;
    public GameObject canvasGanador;
    public GameObject canvasFinJuego;

    [Header("Pantalla Ganador")]
    public TextMeshProUGUI txtPuntos;
    public TextMeshProUGUI txtVidas;
    public TextMeshProUGUI txtSeguridad;

    void Start()
    {
        Debug.Log("LIMPIANDO OBJETOS EXTRA");

        var extras = GameObject.FindObjectsOfType<SpriteRenderer>();

        foreach (var obj in extras)
        {
            if (obj.name.Contains("Clone"))
            {
                Destroy(obj.gameObject);
            }
        }

        StartCoroutine(
        APIManager.Instance.GetCrossword(
            AIState.RecommendedTraining,
            AIState.RiskLevel,
            OnData
        )
        );
    }

    void Update()
    {
        if (juegoTerminado)
            return;

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
            Perder();
        }
    }

    void OnData(string json)
    {
        Debug.Log("JSON recibido: " + json);

        List<CrosswordWordData> words;

        if (json == "ERROR" || json == "NO_TOKEN")
        {
            Debug.Log("USANDO OFFLINE");
            words = GetOffline();
        }
        else
        {
            words = new List<CrosswordWordData>(
                JsonHelper.FromJson<CrosswordWordData>(json)
            );
        }

        Debug.Log("Cantidad de palabras: " + words.Count);

        foreach (var w in words)
        {
            Debug.Log("Palabra: " + w.clue + " | " + w.answer);
        }

        model = generator.Generate(words);

        ShowClues();
        CreateGrid();
    }

    void CreateGrid()
    {
        Debug.Log("CREANDO GRID DESDE MODEL");

        ClearGrid();

        if (model == null || model.grid == null)
        {
            return;
        }

        CrosswordBounds bounds = CalculateOccupiedBounds();

        if (!bounds.HasCells)
        {
            return;
        }

        for (int y = bounds.MinY; y <= bounds.MaxY; y++)
        {
            for (int x = bounds.MinX; x <= bounds.MaxX; x++)
            {
                CellData cellData = model.grid[x, y];

                if (cellData == null)
                    continue;

                if (cellData.correctLetter == '\0')
                    continue;

                Vector3 worldPos = GetCellWorldPosition(x, y, bounds);
                CreateCell(x, y, worldPos);
                CreateNumberIfNeeded(x, y, worldPos);
            }
        }

        FitCrosswordInsideContainer();
    }

    private void ClearGrid()
    {
        spawnedCells.Clear();
        spawnedGridItems.Clear();

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
    }

    private Vector3 GetCellWorldPosition(int x, int y, CrosswordBounds bounds)
    {
        int visualX = x - bounds.MinX;
        int visualY = y - bounds.MinY;
        Vector3Int tilePos = new Vector3Int(visualX, -visualY, 0);
        Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);
        return worldPos + (Vector3)gridOffset;
    }

    private void CreateCell(int x, int y, Vector3 worldPos)
    {
        GameObject cell = Instantiate(cellPrefab);
        CellWorld cellWorld = cell.GetComponent<CellWorld>();

        cell.transform.SetParent(gridParent, true);
        cell.transform.position = worldPos;
        spawnedGridItems.Add(cell.transform);

        if (cellWorld == null)
        {
            return;
        }

        spawnedCells.Add(cellWorld);
        cellWorld.Init(x, y, this);
        SetCellDirection(cellWorld, x, y);

        Debug.Log("CELDA CREADA [" + x + "," + y + "]");
    }

    private void SetCellDirection(CellWorld cellWorld, int x, int y)
    {
        foreach (var word in model.words)
        {
            for (int i = 0; i < word.answer.Length; i++)
            {
                int wx = word.startX + (word.isHorizontal ? i : 0);
                int wy = word.startY + (word.isHorizontal ? 0 : i);

                if (wx != x || wy != y)
                {
                    continue;
                }

                if (word.isHorizontal)
                {
                    cellWorld.SetDirection(1, 0);
                }
                else
                {
                    cellWorld.SetDirection(0, 1);
                }

                return;
            }
        }
    }

    private void CreateNumberIfNeeded(int x, int y, Vector3 worldPos)
    {
        foreach (var word in model.words)
        {
            if (word.startX != x || word.startY != y)
            {
                continue;
            }

            GameObject numero = Instantiate(numeroPrefab);

            numero.transform.SetParent(gridParent, true);
            numero.transform.position = worldPos + new Vector3(-0.42f, 0.42f, -2);
            spawnedGridItems.Add(numero.transform);

            TextMesh txt = numero.GetComponent<TextMesh>();

            if (txt != null)
            {
                txt.text = (model.words.IndexOf(word) + 1).ToString();
            }
        }
    }

    private CrosswordBounds CalculateOccupiedBounds()
    {
        CrosswordBounds bounds = new CrosswordBounds();

        for (int y = 0; y < model.height; y++)
        {
            for (int x = 0; x < model.width; x++)
            {
                CellData cellData = model.grid[x, y];

                if (cellData == null || cellData.correctLetter == '\0')
                {
                    continue;
                }

                bounds.Include(x, y);
            }
        }

        return bounds;
    }

    private void FitCrosswordInsideContainer()
    {
        RectTransform container = ResolveCrosswordContainer();

        if (container == null || spawnedGridItems.Count == 0)
        {
            return;
        }

        Bounds contentBounds;

        if (!TryGetSpawnedItemsBounds(out contentBounds))
        {
            return;
        }

        Bounds containerBounds = GetRectTransformWorldBounds(container);
        float padding = Mathf.Max(0f, crosswordPadding);
        float availableWidth = Mathf.Max(0.01f, containerBounds.size.x - padding * 2f);
        float availableHeight = Mathf.Max(0.01f, containerBounds.size.y - padding * 2f);
        float scale = 1f;

        if (autoScaleCrossword)
        {
            float widthScale = availableWidth / Mathf.Max(0.01f, contentBounds.size.x);
            float heightScale = availableHeight / Mathf.Max(0.01f, contentBounds.size.y);
            scale = Mathf.Min(1f, widthScale, heightScale);
        }

        if (scale < 1f)
        {
            ScaleSpawnedItems(contentBounds.center, scale);
            TryGetSpawnedItemsBounds(out contentBounds);
        }

        Vector3 delta = containerBounds.center - contentBounds.center;
        delta.z = 0f;
        MoveSpawnedItems(delta);

        if (TryGetSpawnedItemsBounds(out contentBounds))
        {
            MoveSpawnedItems(CalculateClampDelta(contentBounds, containerBounds, padding));
        }
    }

    private RectTransform ResolveCrosswordContainer()
    {
        if (crosswordContainer != null)
        {
            return crosswordContainer;
        }

        if (gridParent != null && gridParent.parent is RectTransform parentRect)
        {
            return parentRect;
        }

        RectTransform gridRect = gridParent as RectTransform;

        if (gridRect != null)
        {
            return gridRect;
        }

        Debug.LogWarning(
            "No hay RectTransform asignado para contener el crucigrama. " +
            "Asigna crosswordContainer o usa un gridParent RectTransform para evitar solapamientos."
        );

        return null;
    }

    private bool TryGetSpawnedItemsBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < spawnedGridItems.Count; i++)
        {
            Transform item = spawnedGridItems[i];

            if (item == null)
            {
                continue;
            }

            Renderer renderer = item.GetComponentInChildren<Renderer>();

            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private Bounds GetRectTransformWorldBounds(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Bounds bounds = new Bounds(corners[0], Vector3.zero);

        for (int i = 1; i < corners.Length; i++)
        {
            bounds.Encapsulate(corners[i]);
        }

        return bounds;
    }

    private void ScaleSpawnedItems(Vector3 pivot, float scale)
    {
        for (int i = 0; i < spawnedGridItems.Count; i++)
        {
            Transform item = spawnedGridItems[i];

            if (item == null)
            {
                continue;
            }

            Vector3 direction = item.position - pivot;
            item.position = pivot + direction * scale;
            item.localScale *= scale;
        }
    }

    private void MoveSpawnedItems(Vector3 delta)
    {
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        for (int i = 0; i < spawnedGridItems.Count; i++)
        {
            Transform item = spawnedGridItems[i];

            if (item != null)
            {
                item.position += delta;
            }
        }
    }

    private Vector3 CalculateClampDelta(Bounds contentBounds, Bounds containerBounds, float padding)
    {
        float minX = containerBounds.min.x + padding;
        float maxX = containerBounds.max.x - padding;
        float minY = containerBounds.min.y + padding;
        float maxY = containerBounds.max.y - padding;
        Vector3 delta = Vector3.zero;

        if (contentBounds.min.x < minX)
        {
            delta.x += minX - contentBounds.min.x;
        }
        else if (contentBounds.max.x > maxX)
        {
            delta.x -= contentBounds.max.x - maxX;
        }

        if (contentBounds.min.y < minY)
        {
            delta.y += minY - contentBounds.min.y;
        }
        else if (contentBounds.max.y > maxY)
        {
            delta.y -= contentBounds.max.y - maxY;
        }

        return delta;
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
            }
        }

        foreach (var word in model.words)
        {
            if (validator.ValidateWord(model, word))
            {
                MarkWord(word);
            }
        }

        if (AllWordsCompleted())
        {
            Debug.Log("GANASTE");

            juegoTerminado = true;
            canvasGanador.SetActive(true);

            int score = 10;
            txtVidas.text = vidas.ToString();

            float seguridad = (tiempoRestante / 90f) * 100f;

            if (txtPuntos != null)
            {
                txtPuntos.text = score.ToString();
            }

            if (txtVidas != null)
            {
                txtVidas.text = vidas.ToString();
            }

            if (txtSeguridad != null)
            {
                txtSeguridad.text = seguridad.ToString("F0");
            }

            StartCoroutine(APIManager.Instance.SendScore(score));

            CrosswordInput input = FindObjectOfType<CrosswordInput>();

            if (input != null)
            {
                input.enabled = false;
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

    List<CrosswordWordData> GetOffline()
    {
        return new List<CrosswordWordData>
        {
            new CrosswordWordData { clue = "Ataque", answer = "PHISHING" },
            new CrosswordWordData { clue = "Malware", answer = "MALWARE" }
        };
    }

    void Perder()
    {
        juegoTerminado = true;

        Debug.Log("GAME OVER");

        canvasFinJuego.SetActive(true);

        CrosswordInput input = FindObjectOfType<CrosswordInput>();

        if (input != null)
        {
            input.enabled = false;
        }
    }

    void ShowClues()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("HORIZONTALES:");

        foreach (var word in model.words)
        {
            if (word.isHorizontal)
            {
                int numeroPalabra = model.words.IndexOf(word) + 1;
                builder.AppendLine(numeroPalabra + ". " + word.clue);
            }
        }

        builder.AppendLine();
        builder.AppendLine("VERTICALES:");

        foreach (var word in model.words)
        {
            if (!word.isHorizontal)
            {
                int numeroPalabra = model.words.IndexOf(word) + 1;
                builder.AppendLine(numeroPalabra + ". " + word.clue);
            }
        }

        cluesText.text = builder.ToString();
        ConfigureCluePanelLayout();
    }

    private void ConfigureCluePanelLayout()
    {
        if (cluesText == null)
        {
            return;
        }

        ScrollRect scrollRect = ResolveCluesScrollRect();

        cluesText.enableWordWrapping = true;
        cluesText.overflowMode = TextOverflowModes.Overflow;
        ClampHorizontalTextMargins();
        cluesText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        RectTransform textRect = cluesText.rectTransform;
        float preferredHeight = Mathf.Max(cluesText.preferredHeight, 1f);

        if (scrollRect == null)
        {
            textRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Min(preferredHeight, maxClueTextHeight)
            );

            return;
        }

        RectTransform viewport = ResolveCluesViewport(scrollRect);
        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();

        ClampClueScrollHeight(scrollRectTransform);

        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = viewport;
        scrollRect.content = textRect;

        float horizontalPadding = Mathf.Max(0f, clueHorizontalPadding);

        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.offsetMin = new Vector2(horizontalPadding, textRect.offsetMin.y);
        textRect.offsetMax = new Vector2(-horizontalPadding, textRect.offsetMax.y);
        cluesText.ForceMeshUpdate();
        preferredHeight = Mathf.Max(cluesText.preferredHeight, 1f);
        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(preferredHeight, viewport.rect.height)
        );

        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClampHorizontalTextMargins()
    {
        Vector4 margin = cluesText.margin;
        margin.x = Mathf.Max(0f, margin.x);
        margin.z = Mathf.Max(0f, margin.z);
        cluesText.margin = margin;
    }

    private ScrollRect ResolveCluesScrollRect()
    {
        if (cluesScrollRect != null)
        {
            return cluesScrollRect;
        }

        cluesScrollRect = cluesText.GetComponentInParent<ScrollRect>();

        if (cluesScrollRect != null || !createScrollRectIfMissing)
        {
            return cluesScrollRect;
        }

        return CreateRuntimeClueScrollRect();
    }

    private ScrollRect CreateRuntimeClueScrollRect()
    {
        RectTransform textRect = cluesText.rectTransform;
        RectTransform originalParent = textRect.parent as RectTransform;

        if (originalParent == null)
        {
            return null;
        }

        GameObject scrollObject = new GameObject(
            "CluesScrollRect",
            typeof(RectTransform),
            typeof(ScrollRect)
        );

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(originalParent, false);
        scrollRectTransform.SetSiblingIndex(textRect.GetSiblingIndex());
        CopyRectTransform(textRect, scrollRectTransform);

        GameObject viewportObject = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(RectMask2D)
        );

        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(scrollRectTransform, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        viewport.pivot = new Vector2(0.5f, 0.5f);

        textRect.SetParent(viewport, false);

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = textRect;
        cluesViewport = viewport;
        cluesScrollRect = scrollRect;

        return scrollRect;
    }

    private void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private RectTransform ResolveCluesViewport(ScrollRect scrollRect)
    {
        if (cluesViewport != null)
        {
            EnsureViewportMask(cluesViewport);
            return cluesViewport;
        }

        if (scrollRect.viewport != null)
        {
            cluesViewport = scrollRect.viewport;
            EnsureViewportMask(cluesViewport);
            return cluesViewport;
        }

        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        cluesViewport = scrollRectTransform;
        EnsureViewportMask(cluesViewport);
        return cluesViewport;
    }

    private void EnsureViewportMask(RectTransform viewport)
    {
        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }
    }

    private void ClampClueScrollHeight(RectTransform scrollRectTransform)
    {
        if (scrollRectTransform == null)
        {
            return;
        }

        float currentHeight = scrollRectTransform.rect.height;
        float targetHeight = Mathf.Min(currentHeight, Mathf.Max(1f, maxClueTextHeight));

        RectTransform footer = ResolveClueFooter();

        if (footer != null)
        {
            RectTransform parent = scrollRectTransform.parent as RectTransform;

            if (parent != null)
            {
                float currentTop = GetLocalMaxY(parent, scrollRectTransform);
                float footerTop = GetLocalMaxY(parent, footer);
                float availableHeight = Mathf.Max(1f, currentTop - footerTop - clueFooterSpacing);
                targetHeight = Mathf.Min(targetHeight, availableHeight);
            }
        }

        float topBefore = GetLocalMaxY(scrollRectTransform.parent as RectTransform, scrollRectTransform);
        scrollRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        RectTransform parentAfterResize = scrollRectTransform.parent as RectTransform;

        if (parentAfterResize != null)
        {
            float topAfter = GetLocalMaxY(parentAfterResize, scrollRectTransform);
            scrollRectTransform.anchoredPosition += new Vector2(0f, topBefore - topAfter);
        }
    }

    private RectTransform ResolveClueFooter()
    {
        if (clueFooter != null)
        {
            return clueFooter;
        }

        if (barraTiempo2 != null)
        {
            return barraTiempo2.rectTransform;
        }

        if (barraTiempo != null)
        {
            return barraTiempo.rectTransform;
        }

        return null;
    }

    private float GetLocalMaxY(RectTransform parent, RectTransform child)
    {
        if (parent == null || child == null)
        {
            return 0f;
        }

        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(corners[i]);
            maxY = Mathf.Max(maxY, local.y);
        }

        return maxY;
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

    private struct CrosswordBounds
    {
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
        public bool HasCells;

        public void Include(int x, int y)
        {
            if (!HasCells)
            {
                MinX = x;
                MaxX = x;
                MinY = y;
                MaxY = y;
                HasCells = true;
                return;
            }

            MinX = Mathf.Min(MinX, x);
            MaxX = Mathf.Max(MaxX, x);
            MinY = Mathf.Min(MinY, y);
            MaxY = Mathf.Max(MaxY, y);
        }
    }
}
