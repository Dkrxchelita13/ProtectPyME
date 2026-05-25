using UnityEngine;

public class CrosswordInput : MonoBehaviour
{
    public static CrosswordInput Instance;

    private CellWorld selectedCell;

    void Awake()
    {
        Instance = this;
    }

    public void SelectCell(CellWorld cell)
    {
        if (selectedCell != null)
        {
            selectedCell.Deselect();
        }

        selectedCell = cell;

        selectedCell.Select();

        Debug.Log("CELDA SELECCIONADA");
    }

    void Update()
    {
        DetectClick();
        DetectKeyboard();
    }

    void DetectClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Debug.Log("CLICK EN: " + mousePos);

            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null)
            {
                Debug.Log("TOCO: " + hit.name);

                CellWorld cell = hit.GetComponentInParent<CellWorld>();

                if (cell != null)
                {
                    Debug.Log("CLICK DETECTADO EN CELDA");
                    SelectCell(cell);
                }
                else
                {
                    Debug.Log("NO TIENE CellWorld");
                }
            }
            else
            {
                Debug.Log("NO HIT");
            }
        }
    }

    void DetectKeyboard()
    {
        if (selectedCell == null) return;

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            selectedCell.ClearLetter();

            int prevX = selectedCell.GetX();
            int prevY = selectedCell.GetY();

            bool horizontal =
                selectedCell.GetDirX() == 1;

            if (horizontal)
            {
                prevX--;
            }
            else
            {
                prevY--;
            }

            CrosswordController controller =
                FindObjectOfType<CrosswordController>();

            CellWorld prevCell =
                controller.GetCell(prevX, prevY);

            if (prevCell != null)
            {
                selectedCell = prevCell;
            }

            return;
        }

        string input = Input.inputString.ToUpper();

        if (!string.IsNullOrEmpty(input) && char.IsLetter(input[0]))
        {
            Debug.Log("TECLA: " + input);
            selectedCell.SetLetter(input[0].ToString());

            CrosswordController controller =
                FindObjectOfType<CrosswordController>();

            int nextX =
                selectedCell.GetX() +
                selectedCell.GetDirX();

            int nextY =
                selectedCell.GetY() +
                selectedCell.GetDirY();
            CellWorld nextCell =
                controller.GetCell(nextX, nextY);

            if (nextCell != null)
            {
                selectedCell = nextCell;
            }
        }
    }
}