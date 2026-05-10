using UnityEngine;

public class CambioVistas : MonoBehaviour
{
    public GameObject scrollViewProgreso;
    public GameObject scrollViewRanking;

    public void MostrarProgreso()
    {
        scrollViewProgreso.SetActive(true);
        scrollViewRanking.SetActive(false);
    }

    public void MostrarRanking()
    {
        scrollViewProgreso.SetActive(false);
        scrollViewRanking.SetActive(true);
    }
}
