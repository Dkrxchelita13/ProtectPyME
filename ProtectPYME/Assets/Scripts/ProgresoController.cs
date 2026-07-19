using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;




public class ProgresoController : MonoBehaviour
{
    [Header("Referencias de UI (Estadísticas/Progreso)")]
    public TextMeshProUGUI txtInsignias;
    public TextMeshProUGUI txtPuntajeTotal;
    public TextMeshProUGUI txtSeguridadPromedio;
    public TextMeshProUGUI txtPartidasJugadas;
    public TextMeshProUGUI txtVidasMax;

    public TextMeshProUGUI txtRiskLevel;
    [Header("IA")]

    public TextMeshProUGUI txtAreaVulnerable;

    public TextMeshProUGUI txtRecomendacion;
    public TextMeshProUGUI txtEscenarioSugerido;
    private int recommendedScenario;

    [Header("Lista Maestra de Niveles (Menú Selección)")]
    public Button[] botonesNiveles;

    void Start()
    {
        // Al iniciar cualquiera de las dos escenas, solicita los datos al servidor
        CargarYMostrarProgreso();
    }

    public void CargarYMostrarProgreso()
    {
        StartCoroutine(APIManager.Instance.GetAnalytics(ProcesarAnalytics));

        void ProcesarAnalytics(string json)
        {
            if (json == "ERROR" || json == "NO_TOKEN")
            {
                Debug.LogError("❌ No se pudo cargar analytics");
                BloquearNivelesPorDefecto();
                return;
            }

            AnalyticsData data = JsonUtility.FromJson<AnalyticsData>(json);

            // 1. ASIGNACIÓN DE TEXTOS (Solo si existen en la escena actual)

            if (txtPuntajeTotal != null)
                txtPuntajeTotal.text = data.total_points.ToString();

            if (txtSeguridadPromedio != null)
                txtSeguridadPromedio.text = data.accuracy.ToString("F0") + "%";

            if (txtPartidasJugadas != null)
                txtPartidasJugadas.text =
                    Mathf.RoundToInt(data.awareness_score).ToString();

            //if (txtVidasMax != null)
            // txtVidasMax.text = "0";

            Debug.Log("✅ Analytics cargados con éxito");

            // 2. CONTROL DE BOTONES (Solo si fueron asignados en la escena actual)
            if (botonesNiveles != null && botonesNiveles.Length > 0)
            {
                ControlarDesbloqueoDeNiveles(data);
            }
        }

        StartCoroutine(
            APIManager.Instance.GetBadges(
                cantidad =>
                {
                    if (txtInsignias != null)
                        txtInsignias.text = cantidad.ToString();
                }
            )
        );


        StartCoroutine(
            APIManager.Instance.GetAIRisk(
                OnRiskLoaded,
                error =>
                {
                    Debug.LogError(
                        "Error IA: " + error
                    );
                }
            )
        );

    }

    void ControlarDesbloqueoDeNiveles(AnalyticsData data)
    {
            if (botonesNiveles == null || botonesNiveles.Length == 0) return;

            // Obtener el nivel máximo alcanzado guardado (1 por defecto)
            int progresoInicial = PlayerPrefs.GetInt("ProgresoNIvelInicial", 1);

            // Opcional: Si deseas que los datos del Backend (AnalyticsData) también desbloqueen niveles,
            // puedes actualizar 'nivelAlcanzado' según el progreso recibido del servidor.

            for (int i = 0; i < botonesNiveles.Length; i++)
            {
                // El botón del nivel (i + 1) se desbloquea si el nivel alcanzado es mayor o igual a ese número
                bool estaDesbloqueado = (progresoInicial >= (i + 1));

                botonesNiveles[i].interactable = estaDesbloqueado;
                SetBotonVisual(botonesNiveles[i], estaDesbloqueado);
            }
    }

    void SetBotonVisual(Button boton, bool estaDesbloqueado)
    {
        // Buscamos los componentes decorativos por su nombre dentro de este botón específico
        Transform capaOpacidad = boton.transform.Find("Opacidad");
        Transform iconoCandado = boton.transform.Find("Candado");

        if (estaDesbloqueado)
        {
            boton.GetComponent<Image>().color = Color.white;
            if (capaOpacidad != null) capaOpacidad.gameObject.SetActive(false); // Quita la sombra
            if (iconoCandado != null) iconoCandado.gameObject.SetActive(false); // Quita el candado

            // Forzar a que los textos hijos (como el número) recuperen brillo completo
            foreach (Transform hijo in boton.transform)
            {
                var txt = hijo.GetComponent<TextMeshProUGUI>();
                if (txt != null) txt.alpha = 1.0f;
            }
        }
        else
        {
            boton.GetComponent<Image>().color = Color.white;
            if (capaOpacidad != null) capaOpacidad.gameObject.SetActive(true); // Pone la sombra
            if (iconoCandado != null) iconoCandado.gameObject.SetActive(true); // Pone el candado

            // Opacar el texto del número
            foreach (Transform hijo in boton.transform)
            {
                var txt = hijo.GetComponent<TextMeshProUGUI>();
                if (txt != null) txt.alpha = 0.3f;
            }
        }
    }

    void BloquearNivelesPorDefecto()
    {
        if (botonesNiveles == null || botonesNiveles.Length == 0) return;

        botonesNiveles[0].interactable = true;
        SetBotonVisual(botonesNiveles[0], true);

        for (int i = 1; i < botonesNiveles.Length; i++)
        {
            botonesNiveles[i].interactable = false;
            SetBotonVisual(botonesNiveles[i], false);
        }
    }


    private void OnRiskLoaded(
        AIRiskResponse data
    )
    {
        if (txtRiskLevel != null)
        {
            txtRiskLevel.text =
                data.risk_level;
        }

        if (txtEscenarioSugerido != null)
        {
            txtEscenarioSugerido.text =
                "Escenario " +
                data.recommended_scenario;
        }

        if (txtAreaVulnerable != null)
        {
            txtAreaVulnerable.text =
                data.recommended_training;
        }

        if (txtRecomendacion != null)
        {
            txtRecomendacion.text =
                data.message;
        }

        recommendedScenario =
            data.recommended_scenario;

        Debug.Log(
            "🤖 Riesgo: " +
            data.risk_level
        );

        Debug.Log(
            "📚 Área vulnerable: " +
            data.recommended_training
        );

        Debug.Log(
            "💡 Recomendación: " +
            data.message
        );
    }

    public void PracticarEscenarioIA()
    {
        Debug.Log(
            "🚀 Escenario recomendado: " +
            recommendedScenario
        );

        switch (recommendedScenario)
        {
            case 1:
                SceneManager.LoadScene("Escenario");
                break;

            case 2:
                SceneManager.LoadScene("Escenario2_Acceso");
                break;

            case 3:
                SceneManager.LoadScene("Escenario 3 (USB sospechoso)");
                break;

            default:
                Debug.LogWarning(
                    "Escenario no configurado"
                );
                break;
        }
    }
}
