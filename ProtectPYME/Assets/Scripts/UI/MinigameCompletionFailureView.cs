using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinigameCompletionFailureView : MonoBehaviour
{
    private const float VisibleSeconds = 4f;

    private CanvasGroup canvasGroup;
    private TextMeshProUGUI messageText;
    private Coroutine hideCoroutine;

    public static MinigameCompletionFailureView ShowSyncPending(
        Transform source,
        string message
    )
    {
        Canvas canvas = ResolveCanvas(source);

        if (canvas == null)
        {
            Debug.LogWarning("Session completion: no hay Canvas para aviso.");
            return null;
        }

        MinigameCompletionFailureView existing =
            canvas.GetComponentInChildren<MinigameCompletionFailureView>(true);

        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.SetMessage(message);
            existing.RestartHideTimer();
            return existing;
        }

        GameObject toast = new GameObject(
            "MinigameCompletionSyncNotice",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image)
        );
        toast.transform.SetParent(canvas.transform, false);

        MinigameCompletionFailureView view =
            toast.AddComponent<MinigameCompletionFailureView>();
        view.BuildContent(message);
        view.RestartHideTimer();
        return view;
    }

    private static Canvas ResolveCanvas(Transform source)
    {
        Canvas canvas = source != null
            ? source.GetComponentInParent<Canvas>()
            : null;

        if (canvas != null)
        {
            return canvas;
        }

        return FindObjectOfType<Canvas>();
    }

    private void BuildContent(string message)
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 84f);
        rect.sizeDelta = new Vector2(720f, 72f);

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image image = GetComponent<Image>();
        image.color = new Color(0.05f, 0.10f, 0.16f, 0.88f);
        image.raycastTarget = false;

        GameObject labelObject = new GameObject(
            "Message",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        labelObject.transform.SetParent(transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);

        messageText = labelObject.GetComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.fontSize = 24f;
        messageText.enableWordWrapping = true;
        messageText.raycastTarget = false;
        SetMessage(message);
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = string.IsNullOrEmpty(message)
                ? "El progreso se sincronizara automaticamente."
                : message;
        }
    }

    private void RestartHideTimer()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(VisibleSeconds);
        hideCoroutine = null;

        if (this != null)
        {
            Destroy(gameObject);
        }
    }
}
