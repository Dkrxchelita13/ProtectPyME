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
        // 🟢 Cargar vidas correctamente al iniciar la escena
        ActualizarTextoVidas();

        CargarYMostrarProgreso();
    }

    // 🟢 Método auxiliar para obtener y pintar las vidas del usuario actual
    private void ActualizarTextoVidas()
    {
        if (txtVidasMax == null) return;

        int vidasActuales = 3;

        if (GameManagerGlobal.instancia != null)
        {
            // 1. Prioridad: Vidas en memoria RAM del GameManager Global
            vidasActuales = GameManagerGlobal.instancia.vidas;
        }
        else
        {
            // 2. Si no hay GameManager, leemos del disco usando la clave del usuario activo
            string usuarioActivo = PlayerPrefs.GetString("UsuarioActual", "default_user");
            string claveVidas = $"{usuarioActivo}_Vidas";
            vidasActuales = PlayerPrefs.GetInt(claveVidas, 3);
        }

        txtVidasMax.text = vidasActuales.ToString();
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

            // 1. ASIGNACIÓN DE TEXTOS
            if (txtPuntajeTotal != null)
                txtPuntajeTotal.text = data.total_points.ToString();

            if (txtPartidasJugadas != null)
                txtPartidasJugadas.text =
                    Mathf.RoundToInt(data.awareness_score).ToString();

            // 🟢 Obtener la clave dinámica del usuario para la seguridad persistente
            string usuarioActivo = PlayerPrefs.GetString("UsuarioActual", "default_user");
            string claveSeguridad = $"{usuarioActivo}_SeguridadPersistente";

            int seguridadServidor = Mathf.RoundToInt(data.awareness_score);
            int seguridadLocal = (GameManagerGlobal.instancia != null) 
                ? Mathf.RoundToInt(GameManagerGlobal.instancia.nivelSeguridad) 
                : Mathf.RoundToInt(PlayerPrefs.GetFloat(claveSeguridad, 0f));

            // Tomamos el valor más alto
            int seguridadDefinitiva = Mathf.Max(seguridadServidor, seguridadLocal);

            if (GameManagerGlobal.instancia != null)
            {
                GameManagerGlobal.instancia.nivelSeguridad = seguridadDefinitiva;
            }

            if (txtSeguridadPromedio != null)
            {
                txtSeguridadPromedio.text = seguridadDefinitiva.ToString() + "%";
            }

            // 🟢 2. Sincronizamos las vidas actualizadas en la UI
            ActualizarTextoVidas();

            // 3. CONTROL DE BOTONES (Pasándole la seguridad definitiva)
            if (botonesNiveles != null && botonesNiveles.Length > 0)
            {
                ControlarDesbloqueoDeNiveles(data, seguridadDefinitiva);
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

    void ControlarDesbloqueoDeNiveles(AnalyticsData data, float seguridadCalculada)
    {
        if (botonesNiveles == null || botonesNiveles.Length == 0) return;

        int puntajeMinimoRequerido = 60;
        float seguridadMinimaRequerida = 60f;

        int puntajeActual = data.total_points;
        // 🟢 Usamos la seguridad calculada (el valor más alto entre servidor y local)
        float seguridadActual = seguridadCalculada; 

        // El Nivel Inicial (0) SIEMPRE está desbloqueado
        botonesNiveles[0].interactable = true;
        SetBotonVisual(botonesNiveles[0], true);

        int nivelAptitud = 1; 

        // 2. Nivel Intermedio (1)
        if (botonesNiveles.Length > 1)
        {
            bool puedeDesbloquearIntermedio = (puntajeActual >= puntajeMinimoRequerido) && 
                                             (seguridadActual >= seguridadMinimaRequerida);

            botonesNiveles[1].interactable = puedeDesbloquearIntermedio;
            SetBotonVisual(botonesNiveles[1], puedeDesbloquearIntermedio);

            if (puedeDesbloquearIntermedio)
            {
                nivelAptitud = 2; // Tiene acceso hasta el nivel 2 o más
            }
        }

        // 3. Resto de niveles bloqueados...
        for (int i = 2; i < botonesNiveles.Length; i++)
        {
            botonesNiveles[i].interactable = false;
            SetBotonVisual(botonesNiveles[i], false);
        }

        // 🟢 CLAVE: Guardamos en PlayerPrefs el nivel calculado según sus datos reales del backend
        int nivelActualGuardado = PlayerPrefs.GetInt("NivelAlcanzado", 1);
        int nivelDefinitivo = Mathf.Max(nivelActualGuardado, nivelAptitud);
        
        PlayerPrefs.SetInt("NivelAlcanzado", nivelDefinitivo);
        PlayerPrefs.Save();

        // 🟢 Notificamos al controlador de botones y candados que se actualice
        BotonesProgresoController controladorBotones = FindObjectOfType<BotonesProgresoController>();
        if (controladorBotones != null)
        {
            controladorBotones.ActualizarProgreso();
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
