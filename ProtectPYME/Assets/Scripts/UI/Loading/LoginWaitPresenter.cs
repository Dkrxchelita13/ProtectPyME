using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginWaitPresenter : MonoBehaviour
{
    [System.Serializable]
    public struct WaitMessage
    {
        public string status;
        public string tip;
    }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private RectTransform loadingIndicator;
    [SerializeField] private RectTransform mascotAnchor;

    [Header("Messages")]
    [SerializeField] private WaitMessage loginMessage =
        new WaitMessage
        {
            status = "Verificando tu acceso...",
            tip = "Estamos preparando tu espacio."
        };

    [SerializeField] private WaitMessage surveyMessage =
        new WaitMessage
        {
            status = "Preparando tu experiencia...",
            tip = "Tu progreso está casi listo."
        };

    [Header("Timing")]
    [SerializeField] private float defaultDelaySeconds = 0.7f;

    private Coroutine delayedShowCoroutine;
    private int requestVersion;

    public WaitMessage LoginMessage => loginMessage;
    public WaitMessage SurveyMessage => surveyMessage;

    private void Awake()
    {
        ConfigureAsNonBlocking();
        Hide();

        if (loadingIndicator != null)
        {
            loadingIndicator.gameObject.SetActive(true);
        }

        if (mascotAnchor != null)
        {
            mascotAnchor.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CancelDelayedShow();
    }

    public void ShowDelayed(WaitMessage message)
    {
        ShowDelayed(message, defaultDelaySeconds);
    }

    public void ShowDelayed(WaitMessage message, float delaySeconds)
    {
        CancelDelayedShow();

        requestVersion++;
        int version = requestVersion;
        ApplyMessage(message);

        delayedShowCoroutine = StartCoroutine(
            ShowAfterDelay(version, Mathf.Max(0f, delaySeconds))
        );
    }

    public void UpdateState(WaitMessage message)
    {
        ApplyMessage(message);
    }

    public void Hide()
    {
        CancelDelayedShow();
        requestVersion++;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private IEnumerator ShowAfterDelay(int version, float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        delayedShowCoroutine = null;

        if (version != requestVersion || panelRoot == null)
        {
            yield break;
        }

        panelRoot.SetActive(true);
    }

    private void ApplyMessage(WaitMessage message)
    {
        if (statusText != null)
        {
            statusText.text = message.status;
        }

        if (tipText != null)
        {
            tipText.text = message.tip;
        }
    }

    private void CancelDelayedShow()
    {
        if (delayedShowCoroutine != null)
        {
            StopCoroutine(delayedShowCoroutine);
            delayedShowCoroutine = null;
        }
    }

    private void ConfigureAsNonBlocking()
    {
        if (panelRoot == null)
        {
            return;
        }

        Graphic[] graphics = panelRoot.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }
}
