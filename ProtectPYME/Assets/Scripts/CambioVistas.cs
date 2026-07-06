using UnityEngine;

public class CambioVistas : MonoBehaviour
{
    [Header("Vistas")]
    public GameObject scrollViewProgreso;
    public GameObject scrollViewRanking;
    public GameObject scrollViewPerfil;

    void Start()
    {
        MostrarProgreso();
    }

    public void MostrarProgreso()
    {
        if (scrollViewProgreso != null)
            scrollViewProgreso.SetActive(true);

        if (scrollViewRanking != null)
            scrollViewRanking.SetActive(false);

        if (scrollViewPerfil != null)
            scrollViewPerfil.SetActive(false);
    }

    public void MostrarRanking()
    {
        if (scrollViewProgreso != null)
            scrollViewProgreso.SetActive(false);

        if (scrollViewRanking != null)
            scrollViewRanking.SetActive(true);

        if (scrollViewPerfil != null)
            scrollViewPerfil.SetActive(false);
    }

    public void MostrarPerfil()
    {
        if (scrollViewProgreso != null)
            scrollViewProgreso.SetActive(false);

        if (scrollViewRanking != null)
            scrollViewRanking.SetActive(false);

        if (scrollViewPerfil != null)
            scrollViewPerfil.SetActive(true);
    }
}