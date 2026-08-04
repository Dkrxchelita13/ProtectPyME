using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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
        {
            scrollViewProgreso.SetActive(true);
            StartCoroutine(ResetProgressScrollNextFrame());
        }

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

    private IEnumerator ResetProgressScrollNextFrame()
    {
        yield return null;

        if (scrollViewProgreso == null || !scrollViewProgreso.activeInHierarchy)
        {
            yield break;
        }

        ScrollRect scrollRect =
            scrollViewProgreso.GetComponentInChildren<ScrollRect>(true);

        if (scrollRect == null)
        {
            yield break;
        }

        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.StopMovement();
        scrollRect.horizontalNormalizedPosition = 0f;
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
