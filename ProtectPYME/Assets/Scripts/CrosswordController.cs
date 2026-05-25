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

    public GameObject numeroPrefab;

    private CrosswordModel model;
    public CrosswordModel GetModel()
    {
        return model;
    }
    private CrosswordValidator validator = new CrosswordValidator();
    private CrosswordGenerator generator = new CrosswordGenerator();

    [SerializeField]
    private Vector2 gridOffset;
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

                // Posición del tilemap
                Vector3Int tilePos = new Vector3Int(x, -y, 0);
                Vector3 worldPos =
                    tilemap.GetCellCenterWorld(tilePos);

                worldPos += (Vector3)gridOffset;

                // CREAR CELDA
                GameObject cell = Instantiate(cellPrefab);

                cell.transform.SetParent(gridParent, true);

                cell.transform.position = worldPos;

                CellWorld cellWorld =
                    cell.GetComponent<CellWorld>();
                
                

                if (cellWorld != null)
                {
                    cellWorld.Init(x, y, this);

                    Debug.Log($"✅ CELDA CREADA [{x},{y}]");
                }
                foreach (var word in model.words)
                {
                    for (int i = 0; i < word.answer.Length; i++)
                    {
                        int wx =
                            word.startX +
                            (word.isHorizontal ? i : 0);

                        int wy =
                            word.startY +
                            (word.isHorizontal ? 0 : i);

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

                // CREAR NÚMERO SOLO SI ES INICIO DE PALABRA
                foreach (var word in model.words)
                {
                    if (word.startX == x &&
                        word.startY == y)
                    {
                        GameObject numero =
                            Instantiate(numeroPrefab);

                        numero.transform.SetParent(
                            gridParent,
                            true
                        );

                        numero.transform.position =
                            worldPos +
                            new Vector3(-0.42f, 0.42f, -2);

                        TextMesh txt =
                            numero.GetComponent<TextMesh>();

                        txt.text =
                            (model.words.IndexOf(word) + 1)
                            .ToString();
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
            else if (letter[0] ==
                model.grid[x, y].correctLetter)
            {
                currentCell.SetCorrect();
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
    public CellWorld GetCell(int x, int y)
    {
        foreach (Transform child in gridParent)
        {
            CellWorld cell = child.GetComponent<CellWorld>();

            if (cell != null &&
                cell.IsPosition(x, y))
            {
                return cell;
            }
        }

        return null;
    }
}