using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PilotProfileEntryInstaller
{
    private const string ProfileSceneName = "MiPerfil";
    private const string PilotSceneName = "PilotoAcademico";
    private const string ButtonName = "btn_piloto_academico";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private static void TryInstall(Scene scene)
    {
        if (scene.name != ProfileSceneName ||
            GameObject.Find(ButtonName) != null)
        {
            return;
        }

        Transform parent = ResolveButtonParent();

        if (parent == null)
        {
            Debug.LogWarning(
                "Piloto: no se encontro contenedor para crear la entrada en MiPerfil."
            );
            return;
        }

        Button button = CreatePilotButton(parent);
        button.onClick.AddListener(() => SceneManager.LoadScene(PilotSceneName));
    }

    private static Transform ResolveButtonParent()
    {
        GameObject buttonContainer = GameObject.Find("botonesMiPerfil");

        if (buttonContainer != null)
        {
            return buttonContainer.transform;
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private static Button CreatePilotButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(ButtonName);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.localScale = Vector3.one;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minWidth = 300f;
        layout.preferredWidth = 420f;
        layout.minHeight = 72f;
        layout.preferredHeight = 86f;
        layout.flexibleWidth = 1f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 0.86f, 0.12f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(1f, 0.93f, 0.38f);
        colors.pressedColor = new Color(0.89f, 0.72f, 0.08f);
        button.colors = colors;

        GameObject textObject = new GameObject("Text");
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.SetParent(buttonRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = "PILOTO ACADEMICO";
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.07f, 0.25f, 0.42f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;

        return button;
    }
}
