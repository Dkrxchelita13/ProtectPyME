using UnityEngine;
using System.Linq;

public class CrosswordInput : MonoBehaviour
{
    public AudioSource fuenteAudio;

    public AudioClip sonidoSeleccion;
    public AudioClip sonidoEscribir;

    public static CrosswordInput Instance;

    private CellWorld selectedCell;


    private TouchScreenKeyboard keyboard;
    private bool keyboardOpened = false;

    void Awake()
    {
        Instance = this;
    }

    public void SelectCell(CellWorld cell)
    {
        CrosswordController controller =
            FindObjectOfType<CrosswordController>();

        foreach (CellWorld c in controller.GetAllCells())
        {
            c.Deselect();
        }

        selectedCell = cell;
        //fuenteAudio.PlayOneShot(sonidoSeleccion);

        HighlightWord(cell);

        Debug.Log("CELDA SELECCIONADA");

        #if UNITY_ANDROID || UNITY_IOS
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
        #endif
    }
    void HighlightWord(CellWorld startCell)
    {
        CrosswordController controller =
            FindObjectOfType<CrosswordController>();

        int dx = startCell.GetDirX();
        int dy = startCell.GetDirY();

        int x = startCell.GetX();
        int y = startCell.GetY();

        while (true)
        {
            CellWorld cell =
                controller.GetCell(x, y);

            if (cell == null)
                break;

            cell.Select();

            x += dx;
            y += dy;
        }
    }

    void Update()
    {
        DetectClick();
        DetectKeyboard();
        ReadMobileKeyboard();
    }

    void DetectClick()
{
    if (Input.GetMouseButtonDown(0))
    {
        // Convertimos la posición a coordenadas del mundo
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Forzamos un vector 2D para la posición del clic
        Vector2 mousePos2D = new Vector2(worldPos.x, worldPos.y);

        Debug.Log("CLICK EN COORDENADAS MUNDO: " + mousePos2D);

        // Lanzamos un rayo infinito hacia el fondo en esa coordenada exacta
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider != null)
        {
            Debug.Log("¡TOCÓ ALGO!: " + hit.collider.name);

            CellWorld cell = hit.collider.GetComponentInParent<CellWorld>();

            if (cell != null)
            {
                Debug.Log("CLICK DETECTADO EN CELDA");
                SelectCell(cell);
            }
            else
            {
                Debug.Log("El objeto tocado no tiene CellWorld en sus componentes.");
            }
        }
        else
        {
            Debug.Log("El Raycast no chocó con ningún Collider2D en esa posición.");
        }
    }
}

void DetectKeyboard()
{
    if (selectedCell == null) return;

    // 1. Detectar si quieres borrar
    if (Input.GetKeyDown(KeyCode.Backspace))
    {
        selectedCell.ClearLetter();

        int prevX = selectedCell.GetX();
        int prevY = selectedCell.GetY();
        bool horizontal = selectedCell.GetDirX() == 1;

        if (horizontal) prevX--;
        else prevY--;

        CrosswordController controller = FindObjectOfType<CrosswordController>();
        CellWorld prevCell = controller.GetCell(prevX, prevY);

        if (prevCell != null)
        {
            selectedCell = prevCell;
        }
        return;
    }

    // 2. DETECCIÓN ROBUSTA: Capturar la letra del teclado usando la interfaz OnGUI/Event
}
void ReadMobileKeyboard()
{
#if UNITY_ANDROID || UNITY_IOS

    if (!keyboardOpened)
        return;

    if (keyboard == null)
        return;

    if (selectedCell == null)
        return;

    if (keyboard.status == TouchScreenKeyboard.Status.Done)
    {
        keyboardOpened = false;

        string texto = keyboard.text.Trim().ToUpper();

        if (texto.Length > 0)
        {

            FillWord(texto);
            texto = texto.Replace(" ", "");

            texto = new string(
                texto
                .Where(char.IsLetter)
                .ToArray()
            );

            CrosswordController controller =
                FindObjectOfType<CrosswordController>();

            int nextX =
                selectedCell.GetX() + selectedCell.GetDirX();

            int nextY =
                selectedCell.GetY() + selectedCell.GetDirY();

            CellWorld nextCell =
                controller.GetCell(nextX, nextY);

            if (nextCell != null)
            {
                SelectCell(nextCell);
            }
        }
    }

#endif

    }
    void FillWord(string word)
    {
        CrosswordController controller =
            FindObjectOfType<CrosswordController>();

        CellWorld currentCell = selectedCell;

        foreach (char c in word)
        {
            if (currentCell == null)
                break;

            if (!char.IsLetter(c))
                continue;

            currentCell.SetLetter(
                char.ToUpper(c).ToString()
            );

            int nextX =
                currentCell.GetX() +
                currentCell.GetDirX();

            int nextY =
                currentCell.GetY() +
                currentCell.GetDirY();

            currentCell =
                controller.GetCell(
                    nextX,
                    nextY
                );
        }

        if (currentCell != null)
        {
            SelectCell(currentCell);
        }
    }
    // Usamos OnGUI para capturar la letra real directamente antes de que Unity la filtre
    void OnGUI()
{
    // Si no hay celda seleccionada o no es un evento de teclado, no hacemos nada
    if (selectedCell == null || Event.current == null || !Event.current.isKey) return;

    // Solo actuamos en el momento en que se presiona la tecla (KeyDown)
    if (Event.current.type == EventType.KeyDown)
    {
        char caracter = Event.current.character;

        // Validamos que sea una letra válida (A-Z)
        if (char.IsLetter(caracter))
        {
            string letraMayuscula = caracter.ToString().ToUpper();
            
            Debug.Log("🎯 ¡AHORA SÍ DETECTADA!: " + letraMayuscula);

            // Pasamos la letra a la celda
            selectedCell.SetLetter(letraMayuscula);

            // Avanzar a la siguiente celda automáticamente
            CrosswordController controller = FindObjectOfType<CrosswordController>();

            int nextX = selectedCell.GetX() + selectedCell.GetDirX();
            int nextY = selectedCell.GetY() + selectedCell.GetDirY();
            
            CellWorld nextCell = controller.GetCell(nextX, nextY);

            if (nextCell != null)
            {
                selectedCell = nextCell;
            }
        }
    }
}
}