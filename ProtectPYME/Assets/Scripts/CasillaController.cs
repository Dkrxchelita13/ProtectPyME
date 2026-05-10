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

        GetComponent<Image>().color = seleccionada ? Color.yellow : Color.white;

        controlador.AgregarLetra(letraDeEsteBoton, this, seleccionada);
    }
    public void MarcarCorrecta()
    {
        GetComponent<Image>().color = Color.green;
    }

    public void Resetear()
    {
        seleccionada = false;
        GetComponent<Image>().color = Color.white;
    }
}