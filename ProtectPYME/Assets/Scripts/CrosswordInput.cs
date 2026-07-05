using UnityEngine;

public class CrosswordInput : MonoBehaviour
{
    public AudioSource fuenteAudio;

    public AudioClip sonidoSeleccion;
    public AudioClip sonidoEscribir;

    public static CrosswordInput Instance;

    private CellWorld selectedCell;

#if UNITY_ANDROID || UNITY_IOS
    private TouchScreenKeyboard keyboard;
    private string mobileKeyboardText = "";
    private bool keyboardOpened = false;
#endif

    void Awake()
    {
        Instance = this;
    }

    public void SelectCell(CellWorld cell)
    {
        SelectCell(cell, true);
    }

    private void SelectCell(CellWorld cell, bool openKeyboard)
    {
        if (cell == null)
        {
            return;
        }

        CrosswordController controller = FindObjectOfType<CrosswordController>();

        if (controller == null)
        {
            return;
        }

        foreach (CellWorld c in controller.GetAllCells())
        {
            c.Deselect();
        }

        selectedCell = cell;
        HighlightWord(cell, controller);

        Debug.Log("CELDA SELECCIONADA");

        if (openKeyboard)
        {
            OpenMobileKeyboard();
        }
    }

    private void HighlightWord(CellWorld startCell, CrosswordController controller)
    {
        int dx = startCell.GetDirX();
        int dy = startCell.GetDirY();
        int x = startCell.GetX();
        int y = startCell.GetY();

        while (true)
        {
            CellWorld cell = controller.GetCell(x, y);

            if (cell == null)
            {
                break;
            }

            cell.Select();

            x += dx;
            y += dy;
        }
    }

    void Update()
    {
        DetectClick();
        DetectBackspace();
        ReadMobileKeyboard();
    }

    private void DetectClick()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (Camera.main == null)
        {
            return;
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(worldPos.x, worldPos.y);

        Debug.Log("CLICK EN COORDENADAS MUNDO: " + mousePos2D);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider == null)
        {
            Debug.Log("El Raycast no choco con ningun Collider2D en esa posicion.");
            return;
        }

        Debug.Log("TOCO ALGO: " + hit.collider.name);

        CellWorld cell = hit.collider.GetComponentInParent<CellWorld>();

        if (cell == null)
        {
            Debug.Log("El objeto tocado no tiene CellWorld en sus componentes.");
            return;
        }

        Debug.Log("CLICK DETECTADO EN CELDA");
        SelectCell(cell);
    }

    private void DetectBackspace()
    {
        if (selectedCell == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            HandleBackspace();
        }
    }

    private void ReadMobileKeyboard()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!keyboardOpened || keyboard == null || selectedCell == null)
        {
            return;
        }

        if (keyboard.status == TouchScreenKeyboard.Status.Canceled ||
            keyboard.status == TouchScreenKeyboard.Status.LostFocus)
        {
            keyboardOpened = false;
            return;
        }

        ProcessMobileKeyboardText(NormalizeKeyboardText(keyboard.text ?? ""));

        if (keyboard.status == TouchScreenKeyboard.Status.Done)
        {
            keyboardOpened = false;
        }
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    private void OpenMobileKeyboard()
    {
        mobileKeyboardText = "";
        keyboard = TouchScreenKeyboard.Open(
            "",
            TouchScreenKeyboardType.Default,
            false,
            false,
            false,
            false,
            "Escribe una letra"
        );

        keyboardOpened = true;
    }

    private void ProcessMobileKeyboardText(string currentText)
    {
        if (currentText == mobileKeyboardText)
        {
            return;
        }

        int commonPrefix = GetCommonPrefixLength(mobileKeyboardText, currentText);

        for (int i = commonPrefix; i < mobileKeyboardText.Length; i++)
        {
            HandleBackspace();
        }

        for (int i = commonPrefix; i < currentText.Length; i++)
        {
            HandleTypedCharacter(currentText[i]);
        }

        mobileKeyboardText = currentText;
    }

    private string NormalizeKeyboardText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        char[] letters = new char[text.Length];
        int count = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsLetter(text[i]))
            {
                continue;
            }

            letters[count] = char.ToUpperInvariant(text[i]);
            count++;
        }

        return new string(letters, 0, count);
    }

    private int GetCommonPrefixLength(string previousText, string currentText)
    {
        int length = Mathf.Min(previousText.Length, currentText.Length);

        for (int i = 0; i < length; i++)
        {
            if (previousText[i] != currentText[i])
            {
                return i;
            }
        }

        return length;
    }
#else
    private void OpenMobileKeyboard()
    {
    }
#endif

    private void HandleTypedCharacter(char character)
    {
        if (selectedCell == null || !char.IsLetter(character))
        {
            return;
        }

        string letter = char.ToUpperInvariant(character).ToString();

        Debug.Log("LETRA DETECTADA: " + letter);

        selectedCell.SetLetter(letter);
        PlaySound(sonidoEscribir);
        MoveSelection(1);
    }

    private void HandleBackspace()
    {
        if (selectedCell == null)
        {
            return;
        }

        if (HasLetter(selectedCell))
        {
            selectedCell.ClearLetter();
            MoveSelection(-1);
            return;
        }

        if (MoveSelection(-1))
        {
            selectedCell.ClearLetter();
        }
    }

    private bool MoveSelection(int direction)
    {
        if (selectedCell == null)
        {
            return false;
        }

        CrosswordController controller = FindObjectOfType<CrosswordController>();

        if (controller == null)
        {
            return false;
        }

        int nextX = selectedCell.GetX() + selectedCell.GetDirX() * direction;
        int nextY = selectedCell.GetY() + selectedCell.GetDirY() * direction;
        CellWorld nextCell = controller.GetCell(nextX, nextY);

        if (nextCell == null)
        {
            return false;
        }

        SelectCell(nextCell, false);
        return true;
    }

    private bool HasLetter(CellWorld cell)
    {
        CrosswordController controller = FindObjectOfType<CrosswordController>();

        if (controller == null || controller.GetModel() == null)
        {
            return false;
        }

        int x = cell.GetX();
        int y = cell.GetY();
        CrosswordModel model = controller.GetModel();

        if (x < 0 || y < 0 || x >= model.width || y >= model.height)
        {
            return false;
        }

        return !string.IsNullOrEmpty(model.grid[x, y].currentLetter);
    }

    private void PlaySound(AudioClip clip)
    {
        if (fuenteAudio != null && clip != null)
        {
            fuenteAudio.PlayOneShot(clip);
        }
    }

    void OnGUI()
    {
        if (selectedCell == null || Event.current == null || !Event.current.isKey)
        {
            return;
        }

        if (Event.current.type != EventType.KeyDown)
        {
            return;
        }

        char character = Event.current.character;

        if (!char.IsLetter(character))
        {
            return;
        }

        HandleTypedCharacter(character);
        Event.current.Use();
    }
}
