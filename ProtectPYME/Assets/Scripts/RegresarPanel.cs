using UnityEngine;

public class RegresarPanel : MonoBehaviour
{
    [Header("Panel que se va a ocultar")]
    public GameObject panelActual;

    [Header("Panel al que quieres regresar")]
    public GameObject panelAnterior;

    public void Regresar()
    {
        if (panelActual != null)
        {
            panelActual.SetActive(false);
        }

        if (panelAnterior != null)
        {
            panelAnterior.SetActive(true);
        }
    }
}