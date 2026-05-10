using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Tilemaps;

public class CrosswordController : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform gridParent;
    public TextMeshProUGUI cluesText;

    public Tilemap tilemap;
    public TileBase tileBlanco;

    private CrosswordModel model;
    private CrosswordValidator validator = new CrosswordValidator();
    private CrosswordGenerator generator = new CrosswordGenerator();

    void Start()
    {
        Debug.Log("🧹 LIMPIANDO OBJETOS EXTRA");

        var extras = GameObject.FindObjectsOfType<SpriteRenderer>();

        foreach (var obj in extras)
        {
            if (obj.name.Contains("Clone"))
            {
                Destroy(obj.gameObject);
            }
        }
        StartCoroutine(APIManager.Instance.GetCrossword(OnData));


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
        Debug.Log("🚀 USANDO TILEMAP REAL");

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;

        int width = model.grid.GetLength(0);
        int height = model.grid.GetLength(1);

        

        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--) // 🔥 IMPORTANTE (orden visual)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                TileBase tile = tilemap.GetTile(tilePos);


                Debug.Log("Tile en " + tilePos + ": " + tile);
                Debug.Log("CREANDO CELDA en " + tilePos);

                if (tile == null)
                    continue;

                int gridX = x - bounds.xMin;
                int gridY = y - bounds.yMin;

                if (gridY >= height)
                    continue;

                var cellData = model.grid[gridX, gridY];

                if (cellData == null || cellData.correctLetter == '\0')
                {
                    continue;
                }

                GameObject cell = Instantiate(cellPrefab);

                Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);

                cell.transform.position = worldPos;
                cell.transform.SetParent(gridParent, true);

                var cellWorld = cell.GetComponent<CellWorld>();
                if (cellWorld != null)
                {
                    cellWorld.Init(gridX, gridY, this);
                }

            }
        }
    }
    public void OnLetterChanged(int x, int y, string letter)
    {
        model.grid[x, y].currentLetter = letter;

        foreach (var word in model.words)
        {
            if (validator.ValidateWord(model, word))
            {
                MarkWord(word);
            }
        }

        // 👇 AGREGA ESTO AL FINAL
        if (AllWordsCompleted())
        {
            Debug.Log("🎉 GANASTE");
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
    // 👇 AQUÍ VA
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

    void ShowClues()
    {
        cluesText.text = "HORIZONTALES:\n";

        foreach (var word in model.words)
        {
            if (word.isHorizontal)
                cluesText.text += "- " + word.clue + "\n";
        }

        cluesText.text += "\nVERTICALES:\n";

        foreach (var word in model.words)
        {
            if (!word.isHorizontal)
                cluesText.text += "- " + word.clue + "\n";
        }
    }
}