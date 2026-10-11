using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PilotInterventionGate
{
    public const string ScenarioAction = "Scenario";
    public const string MinigameAction = "Minigame";
    public const string InterventionMenuAction = "InterventionMenu";

    private const string PilotSceneName = "PilotoAcademico";
    private const float CacheTtlSeconds = 45f;

    private static readonly string[] ScenarioScenes =
    {
        "Escenario",
        "Escenario2",
        "Escenario2_Acceso",
        "Escenario 3 (USB sospechoso)",
        "Escenario 4",
        "Escenario 5",
        "Escenario 6",
        "Scenario1",
        "Scenario2",
        "Scenario3",
        "Scenario4",
    };

    private static readonly string[] MinigameScenes =
    {
        "MenuMinijuegos",
        "LeccionMinijuego",
        "Kahoot",
        "SopaLetras",
        "Crucigrama",
    };

    private static readonly string[] InterventionMenuScenes =
    {
        "MenuNiveles",
        "MenuNivelInicial",
        "MenuNivelIntermedio",
    };

    private static PilotAssessmentStatusResponse cachedStatus;
    private static float cachedAt = -9999f;
    private static bool statusRequestInProgress;

    private static GameObject modalRoot;
    private static TextMeshProUGUI modalTitle;
    private static TextMeshProUGUI modalBody;
    private static Button primaryButton;
    private static Button secondaryButton;
    private static TextMeshProUGUI primaryText;
    private static TextMeshProUGUI secondaryText;

    public static bool IsProtectedScene(string sceneName)
    {
        return ContainsScene(ScenarioScenes, sceneName) ||
            ContainsScene(MinigameScenes, sceneName) ||
            ContainsScene(InterventionMenuScenes, sceneName);
    }

    public static void TryLoadScene(MonoBehaviour owner, string sceneName)
    {
        if (!IsProtectedScene(sceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
            return;
        }

        TryRun(
            owner,
            ResolveActionForScene(sceneName),
            () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(sceneName);
            }
        );
    }

    public static void TryRun(
        MonoBehaviour owner,
        string actionName,
        Action onAllowed,
        Action onCancelled = null
    )
    {
        if (owner == null)
        {
            LogStatusRequestFailed();
            return;
        }

        owner.StartCoroutine(
            VerifyAndRun(owner, actionName, onAllowed, onCancelled, false)
        );
    }

    public static void RefreshStatusCacheIfPossible(MonoBehaviour owner)
    {
        if (owner == null ||
            APIManager.Instance == null ||
            statusRequestInProgress ||
            HasUsableCache())
        {
            return;
        }

        owner.StartCoroutine(RefreshStatusCacheSilently(owner));
    }

    public static void CacheStatus(PilotAssessmentStatusResponse status)
    {
        if (status == null)
        {
            return;
        }

        cachedStatus = status;
        cachedAt = Time.realtimeSinceStartup;
    }

    public static void InvalidateCachedStatus()
    {
        cachedStatus = null;
        cachedAt = -9999f;
    }

    private static IEnumerator VerifyAndRun(
        MonoBehaviour owner,
        string actionName,
        Action onAllowed,
        Action onCancelled,
        bool forceRefresh
    )
    {
        if (!forceRefresh && HasUsableCache())
        {
            EvaluateStatus(owner, cachedStatus, actionName, onAllowed, onCancelled);
            yield break;
        }

        if (statusRequestInProgress)
        {
            ShowCheckingStatus();
            while (statusRequestInProgress)
            {
                yield return null;
            }

            if (!forceRefresh && HasUsableCache())
            {
                EvaluateStatus(owner, cachedStatus, actionName, onAllowed, onCancelled);
                yield break;
            }
        }

        if (APIManager.Instance == null)
        {
            LogStatusRequestFailed();
            ShowStatusRequestFailed(owner, actionName, onAllowed, onCancelled);
            yield break;
        }

        statusRequestInProgress = true;
        ShowCheckingStatus();

        PilotAssessmentStatusResponse status = null;
        string error = "";

        yield return owner.StartCoroutine(
            APIManager.Instance.GetPilotAssessmentStatus(
                response => status = response,
                message => error = message
            )
        );

        statusRequestInProgress = false;

        if (!string.IsNullOrEmpty(error) || status == null)
        {
            InvalidateCachedStatus();
            LogStatusRequestFailed();
            ShowStatusRequestFailed(owner, actionName, onAllowed, onCancelled);
            yield break;
        }

        CacheStatus(status);
        EvaluateStatus(owner, status, actionName, onAllowed, onCancelled);
    }

    private static IEnumerator RefreshStatusCacheSilently(MonoBehaviour owner)
    {
        if (APIManager.Instance == null || statusRequestInProgress)
        {
            yield break;
        }

        statusRequestInProgress = true;

        PilotAssessmentStatusResponse status = null;
        string error = "";

        yield return owner.StartCoroutine(
            APIManager.Instance.GetPilotAssessmentStatus(
                response => status = response,
                message => error = message
            )
        );

        statusRequestInProgress = false;

        if (string.IsNullOrEmpty(error) && status != null)
        {
            CacheStatus(status);
        }
        else
        {
            LogStatusRequestFailed();
        }
    }

    private static void EvaluateStatus(
        MonoBehaviour owner,
        PilotAssessmentStatusResponse status,
        string actionName,
        Action onAllowed,
        Action onCancelled
    )
    {
        if (status == null)
        {
            LogStatusRequestFailed();
            ShowStatusRequestFailed(owner, actionName, onAllowed, onCancelled);
            return;
        }

        PilotStatusViewState viewState =
            PilotController.ResolveStatusViewState(status);

        if (!status.consent_active ||
            viewState == PilotStatusViewState.ConsentRequired)
        {
            HideModal();
            LogGate(viewState, actionName, "allowed");
            onAllowed?.Invoke();
            return;
        }

        if (viewState == PilotStatusViewState.InterventionPending ||
            viewState == PilotStatusViewState.PostAvailable ||
            viewState == PilotStatusViewState.PostStarted ||
            viewState == PilotStatusViewState.AllCompleted)
        {
            HideModal();
            LogGate(viewState, actionName, "allowed");
            onAllowed?.Invoke();
            return;
        }

        if (viewState == PilotStatusViewState.PrePending ||
            viewState == PilotStatusViewState.PreStarted)
        {
            LogGate(viewState, actionName, "blocked");
            ShowPreRequired(owner, viewState, onCancelled);
            return;
        }

        LogGate(viewState, actionName, "blocked");
        ShowInvalidPilotState(owner, actionName, onAllowed, onCancelled);
    }

    private static bool HasUsableCache()
    {
        return cachedStatus != null &&
            Time.realtimeSinceStartup - cachedAt <= CacheTtlSeconds;
    }

    private static bool ContainsScene(string[] scenes, string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        for (int i = 0; i < scenes.Length; i++)
        {
            if (string.Equals(
                scenes[i],
                sceneName,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveActionForScene(string sceneName)
    {
        if (ContainsScene(ScenarioScenes, sceneName))
        {
            return ScenarioAction;
        }

        if (ContainsScene(MinigameScenes, sceneName))
        {
            return MinigameAction;
        }

        return InterventionMenuAction;
    }

    private static void ShowCheckingStatus()
    {
        EnsureModal();
        ConfigureModal(
            "Verificando piloto",
            "Estamos verificando el estado del Piloto Academico antes de abrir la actividad.",
            "",
            null,
            "",
            null
        );
    }

    private static void ShowPreRequired(
        MonoBehaviour owner,
        PilotStatusViewState viewState,
        Action onCancelled
    )
    {
        EnsureModal();

        string primaryLabel = viewState == PilotStatusViewState.PreStarted
            ? "CONTINUAR EVALUACION INICIAL"
            : "IR A EVALUACION INICIAL";

        ConfigureModal(
            "Evaluacion inicial pendiente",
            "Primero completa la evaluacion inicial del Piloto Academico. " +
            "Las actividades realizadas antes de esta evaluacion no forman parte " +
            "de la intervencion.",
            primaryLabel,
            () =>
            {
                HideModal();
                SceneManager.LoadScene(PilotSceneName);
            },
            "CANCELAR",
            () =>
            {
                HideModal();
                onCancelled?.Invoke();
            }
        );
    }

    private static void ShowStatusRequestFailed(
        MonoBehaviour owner,
        string actionName,
        Action onAllowed,
        Action onCancelled
    )
    {
        EnsureModal();
        ConfigureModal(
            "No se pudo verificar el piloto",
            "No fue posible verificar el estado del Piloto Academico. " +
            "Comprueba tu conexion e intentalo nuevamente.",
            "REINTENTAR",
            () =>
            {
                if (owner != null)
                {
                    owner.StartCoroutine(
                        VerifyAndRun(
                            owner,
                            actionName,
                            onAllowed,
                            onCancelled,
                            true
                        )
                    );
                }
            },
            "VOLVER",
            () =>
            {
                HideModal();
                onCancelled?.Invoke();
            }
        );
    }

    private static void ShowInvalidPilotState(
        MonoBehaviour owner,
        string actionName,
        Action onAllowed,
        Action onCancelled
    )
    {
        EnsureModal();
        ConfigureModal(
            "Estado del piloto no reconocido",
            "Actualiza el estado del Piloto Academico antes de continuar.",
            "REINTENTAR",
            () =>
            {
                InvalidateCachedStatus();
                if (owner != null)
                {
                    owner.StartCoroutine(
                        VerifyAndRun(
                            owner,
                            actionName,
                            onAllowed,
                            onCancelled,
                            true
                        )
                    );
                }
            },
            "VOLVER",
            () =>
            {
                HideModal();
                onCancelled?.Invoke();
            }
        );
    }

    private static void ConfigureModal(
        string title,
        string body,
        string primaryLabel,
        UnityEngine.Events.UnityAction primaryAction,
        string secondaryLabel,
        UnityEngine.Events.UnityAction secondaryAction
    )
    {
        modalRoot.SetActive(true);
        modalTitle.text = title;
        modalBody.text = body;

        ConfigureButton(primaryButton, primaryText, primaryLabel, primaryAction);
        ConfigureButton(secondaryButton, secondaryText, secondaryLabel, secondaryAction);
    }

    private static void ConfigureButton(
        Button button,
        TextMeshProUGUI label,
        string text,
        UnityEngine.Events.UnityAction action
    )
    {
        bool visible = !string.IsNullOrEmpty(text) && action != null;
        button.gameObject.SetActive(visible);
        button.onClick.RemoveAllListeners();

        if (!visible)
        {
            return;
        }

        label.text = text;
        button.onClick.AddListener(action);
    }

    private static void HideModal()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private static void EnsureModal()
    {
        if (modalRoot != null)
        {
            return;
        }

        EnsureEventSystem();

        modalRoot = new GameObject("PilotInterventionGateModal");
        UnityEngine.Object.DontDestroyOnLoad(modalRoot);

        Canvas canvas = modalRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        modalRoot.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = modalRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform overlay = CreateRect(
            "Overlay",
            modalRoot.transform,
            Vector2.zero,
            Vector2.one
        );
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;

        Image overlayImage = overlay.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.62f);

        RectTransform panel = CreateRect(
            "Panel",
            overlay,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f)
        );
        panel.sizeDelta = new Vector2(980f, 520f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.18f, 0.31f, 0.98f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 46, 46);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        modalTitle = CreateText("Title", panel, "", 42, FontStyles.Bold);
        modalBody = CreateText("Body", panel, "", 30, FontStyles.Normal);

        RectTransform buttonRow = CreateRect(
            "Buttons",
            panel,
            Vector2.zero,
            Vector2.one
        );
        LayoutElement buttonRowLayout = buttonRow.gameObject.AddComponent<LayoutElement>();
        buttonRowLayout.minHeight = 92f;
        buttonRowLayout.preferredHeight = 104f;

        HorizontalLayoutGroup buttonLayout =
            buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 22f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = true;

        primaryButton = CreateButton(
            "PrimaryButton",
            "",
            buttonRow,
            new Color(1f, 0.86f, 0.12f),
            new Color(0.06f, 0.18f, 0.30f)
        );
        primaryText = primaryButton.GetComponentInChildren<TextMeshProUGUI>();

        secondaryButton = CreateButton(
            "SecondaryButton",
            "",
            buttonRow,
            new Color(0.85f, 0.91f, 0.97f),
            new Color(0.06f, 0.18f, 0.30f)
        );
        secondaryText = secondaryButton.GetComponentInChildren<TextMeshProUGUI>();

        modalRoot.SetActive(false);
    }

    private static RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax
    )
    {
        GameObject gameObject = new GameObject(name);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        FontStyles style
    )
    {
        GameObject textObject = new GameObject(name);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.minHeight = fontSize + 18f;
        layout.flexibleWidth = 1f;

        return label;
    }

    private static Button CreateButton(
        string name,
        string label,
        Transform parent,
        Color background,
        Color textColor
    )
    {
        RectTransform buttonRect = CreateRect(name, parent, Vector2.zero, Vector2.one);

        LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 78f;
        layout.preferredHeight = 88f;
        layout.flexibleWidth = 1f;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = background;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = new Color(1f, 0.93f, 0.38f);
        colors.pressedColor = new Color(0.89f, 0.72f, 0.08f);
        colors.disabledColor = new Color(0.55f, 0.57f, 0.58f);
        button.colors = colors;

        TextMeshProUGUI buttonText =
            CreateText("Text", buttonRect, label, 28, FontStyles.Bold);
        buttonText.color = textColor;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, -10f);

        return button;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        UnityEngine.Object.DontDestroyOnLoad(eventSystem);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void LogGate(
        PilotStatusViewState status,
        string actionName,
        string result
    )
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[PilotGate] status=" +
            FormatStatusName(status) +
            " action=" +
            SafeActionName(actionName) +
            " " +
            result
        );
#endif
    }

    private static void LogStatusRequestFailed()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning("[PilotGate] status request failed");
#endif
    }

    private static string FormatStatusName(PilotStatusViewState status)
    {
        string name = status.ToString();
        StringBuilder builder = new StringBuilder(name.Length + 4);

        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            if (i > 0 && char.IsUpper(current))
            {
                builder.Append('_');
            }

            builder.Append(char.ToUpperInvariant(current));
        }

        return builder.ToString();
    }

    private static string SafeActionName(string actionName)
    {
        return string.IsNullOrEmpty(actionName)
            ? "Unknown"
            : actionName.Replace(" ", "");
    }
}
