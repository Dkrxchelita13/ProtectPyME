using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ProgresoController : MonoBehaviour
{
    [Header("Referencias de UI")]
    public TextMeshProUGUI txtPuntajeTotal;
    public TextMeshProUGUI txtSeguridadPromedio;
    public TextMeshProUGUI txtPartidasJugadas;
    public TextMeshProUGUI txtVidasMax;

    void Start()
    {
        // Al iniciar la escena, cargamos y mostramos los datos
        CargarYMostrarProgreso();
    }

    public void CargarYMostrarProgreso()
    {
        StartCoroutine(
            APIManager.Instance.GetAnalytics(
                ProcesarAnalytics
            )
        );
        void ProcesarAnalytics(string json)
        {
            if (json == "ERROR" || json == "NO_TOKEN")
            {
                Debug.LogError("❌ No se pudo cargar analytics");
                return;
            }

            AnalyticsData data =
                JsonUtility.FromJson<AnalyticsData>(json);

            // Awareness Score
            if (txtPuntajeTotal != null)
            {
                txtPuntajeTotal.text =
                    data.awareness_score.ToString("F0");
            }

            // Accuracy
            if (txtSeguridadPromedio != null)
            {
                txtSeguridadPromedio.text =
                    data.accuracy.ToString("F0") + "%";
            }

            // Decisiones últimos 7 días
            if (txtPartidasJugadas != null)
            {
                txtPartidasJugadas.text =
                    "Decisiones: " +
                    data.decisions_last_7_days;
            }

            // Risk Index
            if (txtVidasMax != null)
            {
                txtVidasMax.text =
                    data.risk_index.ToString("F0");
            }

            Debug.Log("✅ Analytics mostrados");
        }

    }
}