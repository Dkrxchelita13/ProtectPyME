using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CasillaController : MonoBehaviour
{
    public string letraDeEsteBoton;
    public TextMeshProUGUI miTexto;

    // 🔥 POSICIÓN EN LA MATRIZ
    public int fila;
    public int columna;
    public Color colorIncorrecto = Color.red;

    private PreguntasController controlador;
    private bool seleccionada = false;

    public void EstablecerControlador(PreguntasController ctrl)
    {
        controlador = ctrl;
    }

    public void OnClick()
    {
        if (controlador == null) return;

        // 🚫 evitar clicks si está bloqueado
        if (!controlador.PuedeInteractuar()) return;

        seleccionada = !seleccionada;

        GetComponent<Image>().color =
            seleccionada ? new Color(1f, 1f, 0f, 1f) : Color.white;
        controlador.AgregarLetra(letraDeEsteBoton, this, seleccionada);
    }
    public void MarcarCorrecta()
    {
        GetComponent<Image>().color = new Color(0f, 1f, 0f, 1f);
        GetComponent<Button>().interactable = false;
    }

    public void Resetear()
    {
        seleccionada = false;

        GetComponent<Image>().color = Color.white;

        GetComponent<Button>().interactable = true;
    }

     public void MarcarIncorrecta()
    {
        // Si usas UnityEngine.UI.Image en tus botones:
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = colorIncorrecto;
        }
    }
}