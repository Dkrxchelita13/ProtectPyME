using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PilotProfileEntryInstaller : MonoBehaviour
{
    [Header("Configuración del Botón")]
    [SerializeField] private GameObject buttonPrefab; 
    [SerializeField] private string profileSceneName = "MiPerfil";
    [SerializeField] private string pilotSceneName = "PilotoAcademico";
    [SerializeField] private string buttonContainerName = "botonesMiPerfil";

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private void TryInstall(Scene scene)
    {
        if (scene.name != profileSceneName || GameObject.Find("btn_piloto_academico") != null)
        {
            return;
        }

        Transform parent = ResolveButtonParent();

        if (parent == null)
        {
            Debug.LogWarning("Piloto: no se encontro contenedor para crear la entrada en MiPerfil.");
            return;
        }

        if (buttonPrefab != null)
        {
            GameObject newButton = Instantiate(buttonPrefab, parent);
            newButton.name = "btn_piloto_academico";

            Button btnComponent = newButton.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.onClick.AddListener(() => SceneManager.LoadScene(pilotSceneName));
            }
        }
        else
        {
            Debug.LogError("No se ha asignado el prefab del botón en el Inspector.");
        }
    }

    private Transform ResolveButtonParent()
    {
        GameObject buttonContainer = GameObject.Find(buttonContainerName);
        if (buttonContainer != null) return buttonContainer.transform;

        Canvas canvas = FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }
}