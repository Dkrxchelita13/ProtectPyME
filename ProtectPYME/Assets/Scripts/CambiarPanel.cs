using UnityEngine;

public class CambiarPanel : MonoBehaviour
{
    [Header("Panel actual que se va a ocultar")]
    public GameObject panelActual;

    [Header("Panel que se va a mostrar")]
    public GameObject panelSiguiente;

    public void Cambiar()
    {
        // Oculta el panel actual
        if (panelActual != null)
        {
            panelActual.SetActive(false);
        }

        // Muestra el siguiente panel
        if (panelSiguiente != null)
        {
            panelSiguiente.SetActive(true);
        }
    }

    public void Regresar()
    {
        // Muestra el panel anterior
        if (panelSiguiente != null)
        {
            panelSiguiente.SetActive(false);
        }

        // Regresa al panel actual
        if (panelActual != null)
        {
            panelActual.SetActive(true);
        }
    }
}