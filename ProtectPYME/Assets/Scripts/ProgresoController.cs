using System.Collections;
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

    [Header("Resultado Diagnostico")]
    [SerializeField] private Image imagenBotonPracticar;
    [SerializeField] private Sprite spriteBotonPracticar;
    [SerializeField] private Sprite spriteBotonContinuar;

    private int recommendedScenario;
    private bool recommendationReady;
    private Button botonPracticar;
    private Coroutine progressLayoutCoroutine;
    private bool progressLayoutRebuildPending;
    private const float ProgressContentHorizontalPadding = 0f;



    [Header("Lista Maestra de Niveles (Menú Selección)")]

    public Button[] botonesNiveles;



    void Start()

    {

        recommendationReady = false;
        SetPracticeButtonInteractable(false);

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



        if (DebeMostrarResultadoDiagnostico())

        {

            ShowSurveyDiagnosticResult();

        }

        AIState.SurveyResultPending = false;

        SetPracticeButtonLoadingState();

        if (APIManager.Instance == null)

        {

            SetPracticeButtonUnavailableState();
            Debug.LogWarning("Error IA: APIManager no disponible");
            return;

        }

        StartCoroutine(

            APIManager.Instance.GetAIRisk(

                OnRiskLoaded,

                error =>

                {

                    SetPracticeButtonUnavailableState();

                    Debug.LogWarning(

                        "Error IA: " + error

                    );

                }

            )

        );

    }



    void ControlarDesbloqueoDeNiveles(AnalyticsData data, float seguridadCalculada)

    {

        if (botonesNiveles == null || botonesNiveles.Length == 0) return;



        int puntajeMinimoRequerido = 100;

        float seguridadMinimaRequerida = 85f;



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





    private bool DebeMostrarResultadoDiagnostico()

    {

        return AIState.SurveyResultPending && AIState.SurveyCompleted;

    }



    private void ShowSurveyDiagnosticResult()

    {

        string weakness = NormalizeSurveyWeakness(AIState.SurveyPrimaryWeakness);

        string risk = NormalizeSurveyRisk(AIState.SurveyInitialRisk);



        if (txtRiskLevel != null)

        {

            txtRiskLevel.text = risk;

        }



        if (txtAreaVulnerable != null)

        {

            txtAreaVulnerable.text =

                GetSurveyAreaDisplayName(weakness);

        }



        if (txtRecomendacion != null)

        {

            txtRecomendacion.text = GetSurveyRecommendation(weakness, risk);

        }



        if (txtEscenarioSugerido != null)

        {

            txtEscenarioSugerido.text =

                GetSurveySuggestedPractice(weakness);

        }



        if (imagenBotonPracticar != null && spriteBotonContinuar != null)

        {

            imagenBotonPracticar.sprite = spriteBotonContinuar;

        }

        recommendationReady = true;
        SetPracticeButtonInteractable(true);

        ScheduleProgressLayoutRebuild();

    }



    private string NormalizeSurveyWeakness(string weakness)

    {

        if (string.IsNullOrEmpty(weakness))

        {

            return "";

        }



        string normalized = weakness.Trim().ToLower();



        switch (normalized)

        {

            case "phishing":

            case "passwords":

            case "malware":

            case "none":

                return normalized;



            default:

                return "";

        }

    }



    private string NormalizeSurveyRisk(string risk)

    {

        if (string.IsNullOrEmpty(risk))

        {

            return "NO DISPONIBLE";

        }



        string normalized = risk.Trim().ToUpper();



        switch (normalized)

        {

            case "ALTO":

            case "MEDIO":

            case "BAJO":

                return normalized;



            default:

                return "NO DISPONIBLE";

        }

    }



    private string GetSurveyAreaDisplayName(string weakness)

    {

        switch (weakness)

        {

            case "phishing":

                return "Phishing e ingenieria social";



            case "passwords":

                return "Contrasenas y proteccion de cuentas";



            case "malware":

                return "Malware y dispositivos USB";



            case "none":

                return "No se detecto un area critica";



            default:

                return "Area de mejora general";

        }

    }



    private string GetSurveySuggestedPractice(string weakness)

    {

        switch (weakness)

        {

            case "phishing":

                return "Escenario 1: correo fraudulento";



            case "passwords":

                return "Escenario 2: contrasenas y acceso";



            case "malware":

                return "Escenario 3: USB sospechoso";



            case "none":

                return "Ruta general de capacitacion";



            default:

                return "Capacitacion general";

        }

    }



    private string GetSurveyRecommendation(string weakness, string risk)

    {

        if (weakness == "none")

        {

            return "Tu diagnostico inicial no detecto un area critica. "

                + "Continuaras con una ruta general para fortalecer y comprobar "

                + "tus conocimientos mediante situaciones practicas.";

        }



        switch (weakness)

        {

            case "phishing":

                return GetPhishingRecommendation(risk);



            case "passwords":

                return GetPasswordRecommendation(risk);



            case "malware":

                return GetMalwareRecommendation(risk);



            default:

                return "Tu diagnostico inicial esta listo. Revisa la practica "

                    + "sugerida para fortalecer tus conocimientos de seguridad.";

        }

    }



    private string GetPhishingRecommendation(string risk)

    {

        switch (risk)

        {

            case "ALTO":

                return "Tus respuestas indican que necesitas reforzar la "

                    + "identificacion de remitentes, enlaces y mensajes urgentes "

                    + "antes de interactuar con ellos.";



            case "MEDIO":

                return "Reconoces algunas senales de phishing, pero debes revisar "

                    + "con mayor atencion el remitente, el dominio y los enlaces.";



            case "BAJO":

                return "Demuestras buenas practicas iniciales frente al phishing. "

                    + "Continua fortaleciendo la verificacion de mensajes y enlaces.";



            default:

                return GetGeneralSurveyRecommendation();

        }

    }



    private string GetPasswordRecommendation(string risk)

    {

        switch (risk)

        {

            case "ALTO":

                return "Necesitas reforzar el uso de contrasenas unicas, extensas "

                    + "y dificiles de predecir, ademas del uso de mecanismos "

                    + "adicionales de autenticacion.";



            case "MEDIO":

                return "Conoces algunas practicas de proteccion de cuentas, pero "

                    + "debes mejorar la creacion y administracion de contrasenas.";



            case "BAJO":

                return "Demuestras buenas practicas iniciales para proteger tus "

                    + "cuentas. Continua utilizando credenciales unicas y "

                    + "autenticacion adicional.";



            default:

                return GetGeneralSurveyRecommendation();

        }

    }



    private string GetMalwareRecommendation(string risk)

    {

        switch (risk)

        {

            case "ALTO":

                return "Necesitas reforzar la prevencion de malware y evitar "

                    + "conectar dispositivos USB desconocidos a los equipos de "

                    + "la empresa.";



            case "MEDIO":

                return "Identificas algunos riesgos de dispositivos externos, "

                    + "pero debes fortalecer la respuesta ante memorias USB "

                    + "desconocidas.";



            case "BAJO":

                return "Demuestras buenas practicas iniciales frente a dispositivos "

                    + "USB y malware. Continua aplicando medidas preventivas.";



            default:

                return GetGeneralSurveyRecommendation();

        }

    }



    private string GetGeneralSurveyRecommendation()

    {

        return "Tu diagnostico inicial esta listo. Continua con la ruta "

            + "recomendada para fortalecer tus conocimientos de ciberseguridad.";

    }



    private void RestaurarSpriteBotonPracticar()

    {

        if (imagenBotonPracticar != null && spriteBotonPracticar != null)

        {

            imagenBotonPracticar.sprite = spriteBotonPracticar;

        }

    }

    private void SetPracticeButtonLoadingState()

    {

        recommendationReady = false;
        recommendedScenario = 0;
        RestaurarSpriteBotonPracticar();
        SetPracticeButtonInteractable(false);

        if (txtEscenarioSugerido != null)

        {

            txtEscenarioSugerido.text = "CARGANDO...";

        }

        ScheduleProgressLayoutRebuild();

    }

    private void SetPracticeButtonUnavailableState()

    {

        recommendationReady = false;
        recommendedScenario = 0;
        RestaurarSpriteBotonPracticar();
        SetPracticeButtonInteractable(false);

        if (txtEscenarioSugerido != null)

        {

            txtEscenarioSugerido.text = "NO DISPONIBLE";

        }

        ScheduleProgressLayoutRebuild();

    }

    private void SetPracticeButtonInteractable(bool interactable)

    {

        Button practiceButton = ResolvePracticeButton();

        if (practiceButton != null)

        {

            practiceButton.interactable = interactable;

        }

    }

    private Button ResolvePracticeButton()

    {

        if (botonPracticar != null)

        {

            return botonPracticar;

        }

        if (imagenBotonPracticar == null)

        {

            return null;

        }

        botonPracticar = imagenBotonPracticar.GetComponentInParent<Button>(true);

        return botonPracticar;

    }



    private string ResolveRiskSource(AIRiskResponse data)

    {

        string source = data.risk_source;

        if (!string.IsNullOrEmpty(source))

        {

            source = source.Trim().ToLower();

            if (source == "survey" || source == "random_forest")

            {

                return source;

            }

        }



        int minDecisions = data.min_behavioral_decisions;

        if (data.sufficient_behavioral_data

            || data.behavioral_decisions >= minDecisions)

        {

            return "random_forest";

        }



        return "survey";

    }



    private int GetMinBehavioralDecisions(AIRiskResponse data)

    {

        return data.min_behavioral_decisions > 0

            ? data.min_behavioral_decisions

            : 3;

    }



    private string GetTrainingDisplayName(string training)

    {

        if (string.IsNullOrEmpty(training))

        {

            return "Area de mejora general";

        }



        switch (training.Trim().ToLower())

        {

            case "phishing":

                return "Phishing e ingenieria social";



            case "passwords":

                return "Contrasenas y proteccion de cuentas";



            case "malware":

                return "Malware y dispositivos USB";



            case "wifi":

            case "network":

                return "Redes WiFi y conexiones inseguras";



            case "general":

                return "Capacitacion general";



            case "none":

                return "No se detecto un area critica";



            default:

                return "Area de mejora general";

        }

    }



    private string BuildRiskRecommendationText(

        AIRiskResponse data,

        string riskSource

    )

    {

        string message = string.IsNullOrEmpty(data.message)

            ? "Revisa la practica sugerida para fortalecer tus conocimientos."

            : data.message;

        int minDecisions = GetMinBehavioralDecisions(data);



        if (riskSource == "survey")

        {

            return message

                + "\n\nFuente: encuesta ("

                + data.behavioral_decisions

                + "/"

                + minDecisions

                + " decisiones).";

        }



        return message

            + "\n\nFuente: evaluacion conductual ("

            + data.behavioral_decisions

            + " decisiones).";

    }



    private void OnRiskLoaded(

        AIRiskResponse data

    )

    {

        string riskSource = ResolveRiskSource(data);

        string risk = NormalizeSurveyRisk(data.risk_level);

        string area = GetTrainingDisplayName(data.recommended_training);

        string recommendation = BuildRiskRecommendationText(

            data,

            riskSource

        );

        if (txtRiskLevel != null)

        {

            txtRiskLevel.text = risk;

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

                area;

        }



        if (txtRecomendacion != null)

        {

            txtRecomendacion.text =

                recommendation;

        }



        recommendedScenario =

            data.recommended_scenario;

        recommendationReady =

            AIState.IsValidPracticeScenario(recommendedScenario);

        SetPracticeButtonInteractable(recommendationReady);

        if (!recommendationReady)

        {

            Debug.LogWarning(

                "Practicar: escenario recomendado no disponible para MiPerfil"

            );

        }



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

        ScheduleProgressLayoutRebuild();

    }

    private void ScheduleProgressLayoutRebuild()
    {
        if (!this || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (progressLayoutRebuildPending && progressLayoutCoroutine != null)
        {
            StopCoroutine(progressLayoutCoroutine);
        }

        progressLayoutRebuildPending = true;
        progressLayoutCoroutine = StartCoroutine(RebuildProgressLayoutCoroutine());
    }

    private IEnumerator RebuildProgressLayoutCoroutine()
    {
        yield return null;

        if (!this || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            FinishProgressLayoutRebuild();
            yield break;
        }

        ScrollRect scrollRect = ResolveProgressScrollRect();

        if (scrollRect == null || scrollRect.content == null)
        {
            Debug.LogWarning("MiPerfil layout: rebuild cancelado porque el ScrollRect ya no esta disponible.");
            FinishProgressLayoutRebuild();
            yield break;
        }

        RectTransform contentRect = scrollRect.content;
        RectTransform resultadosRect = ResolveResultsContainer();
        RectTransform contenidoBtnRect = ResolveButtonContainer();
        RectTransform contenidoIARect =
            GetCommonAncestor(resultadosRect, contenidoBtnRect);

        if (contenidoIARect == null || !IsAncestorOrSelf(contentRect, contenidoIARect))
        {
            Debug.LogError("MiPerfil layout: el content del ScrollRect no contiene el bloque IA esperado.");
            FinishProgressLayoutRebuild();
            yield break;
        }

        RebuildProgressLayoutPass(
            scrollRect,
            contentRect,
            resultadosRect,
            contenidoIARect,
            contenidoBtnRect
        );

        yield return null;

        if (!this || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            FinishProgressLayoutRebuild();
            yield break;
        }

        RebuildProgressLayoutPass(
            scrollRect,
            contentRect,
            resultadosRect,
            contenidoIARect,
            contenidoBtnRect
        );

        Debug.Log(
            $"MiPerfil layout: content={GetRectHeight(contentRect)} "
            + $"ia={GetRectHeight(contenidoIARect)} "
            + $"results={GetRectHeight(resultadosRect)} "
            + $"button={GetRectHeight(contenidoBtnRect)}"
        );

        FinishProgressLayoutRebuild();
    }

    private void RebuildProgressLayoutPass(
        ScrollRect scrollRect,
        RectTransform contentRect,
        RectTransform resultadosRect,
        RectTransform contenidoIARect,
        RectTransform contenidoBtnRect
    )
    {
        ConfigureProgressScrollRect(scrollRect, contentRect);
        ConfigureRootContent(contentRect);
        ConfigureDynamicVerticalGroup(resultadosRect);
        ConfigureDynamicVerticalGroup(contenidoIARect);

        ApplyPreferredTextHeight(txtEscenarioSugerido, 10f);
        ApplyPreferredTextHeight(txtAreaVulnerable, 10f);
        ApplyPreferredTextHeight(txtRecomendacion, 28f);

        ApplyDirectChildrenPreferredHeight(resultadosRect, 0f);
        ApplyDirectChildrenPreferredHeight(
            resultadosRect != null ? resultadosRect.parent as RectTransform : null,
            0f
        );
        ApplyButtonContainerHeight(contenidoBtnRect, 30f, 50f);

        DisableFitterIfChildOfLayoutGroup(contenidoIARect);
        ApplyDirectChildrenPreferredHeight(contenidoIARect, 0f);

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        scrollRect.StopMovement();
        scrollRect.horizontalNormalizedPosition = 0f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ConfigureProgressScrollRect(
        ScrollRect scrollRect,
        RectTransform contentRect
    )
    {
        if (scrollRect == null || contentRect == null)
        {
            return;
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        contentRect.anchorMin = new Vector2(0f, contentRect.anchorMin.y);
        contentRect.anchorMax = new Vector2(1f, contentRect.anchorMax.y);
        contentRect.pivot = new Vector2(0.5f, contentRect.pivot.y);
        contentRect.anchoredPosition =
            new Vector2(0f, contentRect.anchoredPosition.y);
        contentRect.offsetMin =
            new Vector2(ProgressContentHorizontalPadding, contentRect.offsetMin.y);
        contentRect.offsetMax =
            new Vector2(-ProgressContentHorizontalPadding, contentRect.offsetMax.y);
        contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
    }

    private void FinishProgressLayoutRebuild()
    {
        progressLayoutRebuildPending = false;
        progressLayoutCoroutine = null;
    }

    private void OnDisable()
    {
        if (progressLayoutCoroutine != null)
        {
            StopCoroutine(progressLayoutCoroutine);
            progressLayoutCoroutine = null;
        }

        progressLayoutRebuildPending = false;
    }

    private ScrollRect ResolveProgressScrollRect()
    {
        if (txtRecomendacion != null)
        {
            return txtRecomendacion.GetComponentInParent<ScrollRect>(true);
        }

        if (txtAreaVulnerable != null)
        {
            return txtAreaVulnerable.GetComponentInParent<ScrollRect>(true);
        }

        if (txtEscenarioSugerido != null)
        {
            return txtEscenarioSugerido.GetComponentInParent<ScrollRect>(true);
        }

        return imagenBotonPracticar != null
            ? imagenBotonPracticar.GetComponentInParent<ScrollRect>(true)
            : null;
    }

    private RectTransform ResolveResultsContainer()
    {
        return GetCommonAncestor(
            GetTextRect(txtEscenarioSugerido),
            GetTextRect(txtAreaVulnerable),
            GetTextRect(txtRecomendacion)
        );
    }

    private RectTransform ResolveButtonContainer()
    {
        return imagenBotonPracticar != null
            ? imagenBotonPracticar.rectTransform.parent as RectTransform
            : null;
    }

    private void ConfigureRootContent(RectTransform contentRect)
    {
        if (contentRect == null)
        {
            return;
        }

        ContentSizeFitter rootFitter =
            contentRect.GetComponent<ContentSizeFitter>();

        if (rootFitter != null)
        {
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        VerticalLayoutGroup rootLayout =
            contentRect.GetComponent<VerticalLayoutGroup>();

        if (rootLayout == null)
        {
            return;
        }

        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.padding.bottom = Mathf.Max(rootLayout.padding.bottom, 50);
    }

    private void ConfigureDynamicVerticalGroup(RectTransform container)
    {
        if (container == null)
        {
            return;
        }

        VerticalLayoutGroup layoutGroup =
            container.GetComponent<VerticalLayoutGroup>();

        if (layoutGroup == null)
        {
            return;
        }

        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
    }

    private float ApplyPreferredTextHeight(
        TMP_Text textComponent,
        float extraPadding
    )
    {
        if (textComponent == null || textComponent.rectTransform == null)
        {
            return 0f;
        }

        RectTransform textRect = textComponent.rectTransform;
        textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
        textRect.anchorMax = new Vector2(1f, textRect.anchorMax.y);
        textRect.offsetMin = new Vector2(0f, textRect.offsetMin.y);
        textRect.offsetMax = new Vector2(0f, textRect.offsetMax.y);
        float availableWidth = textRect.rect.width;

        if (availableWidth <= 10f && textRect.parent is RectTransform parentRect)
        {
            availableWidth =
                parentRect.rect.width - GetHorizontalPadding(parentRect);
        }

        availableWidth = Mathf.Max(10f, availableWidth - 8f);

        Vector2 preferred =
            textComponent.GetPreferredValues(
                textComponent.text,
                availableWidth,
                Mathf.Infinity
            );

        LayoutElement layoutElement =
            textComponent.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement =
                textComponent.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = false;
        layoutElement.minHeight = 0;
        layoutElement.preferredHeight = Mathf.Ceil(preferred.y + extraPadding);
        layoutElement.flexibleHeight = 0;

        textComponent.richText = false;
        textComponent.enableWordWrapping = true;
        textComponent.overflowMode = TextOverflowModes.Overflow;

        return layoutElement.preferredHeight;
    }

    private float ApplyDirectChildrenPreferredHeight(
        RectTransform container,
        float extraPadding
    )
    {
        if (container == null)
        {
            return 0f;
        }

        HorizontalOrVerticalLayoutGroup layoutGroup =
            container.GetComponent<HorizontalOrVerticalLayoutGroup>();

        float calculatedHeight = extraPadding;
        int activeChildren = 0;

        if (layoutGroup != null)
        {
            calculatedHeight += layoutGroup.padding.top;
            calculatedHeight += layoutGroup.padding.bottom;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child =
                container.GetChild(i) as RectTransform;

            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            calculatedHeight += GetPreferredOrRectHeight(child);
            activeChildren++;
        }

        if (layoutGroup != null && activeChildren > 1)
        {
            calculatedHeight += layoutGroup.spacing * (activeChildren - 1);
        }

        return ApplyLayoutElementHeight(container, calculatedHeight);
    }

    private float ApplyButtonContainerHeight(
        RectTransform buttonContainer,
        float topMargin,
        float bottomMargin
    )
    {
        if (buttonContainer == null)
        {
            return 0f;
        }

        VerticalLayoutGroup layoutGroup =
            buttonContainer.GetComponent<VerticalLayoutGroup>();

        if (layoutGroup != null)
        {
            layoutGroup.padding.top = Mathf.RoundToInt(topMargin);
            layoutGroup.padding.bottom = Mathf.RoundToInt(bottomMargin);
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
        }

        RectTransform buttonRect = imagenBotonPracticar != null
            ? imagenBotonPracticar.rectTransform
            : null;

        if (buttonRect != null)
        {
            buttonRect.anchorMin = new Vector2(0.5f, buttonRect.anchorMin.y);
            buttonRect.anchorMax = new Vector2(0.5f, buttonRect.anchorMax.y);
            buttonRect.anchoredPosition =
                new Vector2(0f, buttonRect.anchoredPosition.y);
        }

        float buttonHeight = GetPreferredOrRectHeight(buttonRect);
        float preferredHeight = buttonHeight + topMargin + bottomMargin;

        return ApplyLayoutElementHeight(buttonContainer, preferredHeight);
    }

    private float ApplyLayoutElementHeight(RectTransform rectTransform, float height)
    {
        if (rectTransform == null)
        {
            return 0f;
        }

        LayoutElement layoutElement =
            rectTransform.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement =
                rectTransform.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = false;
        layoutElement.minHeight = 0;
        layoutElement.preferredHeight = Mathf.Ceil(height);
        layoutElement.flexibleHeight = 0;

        return layoutElement.preferredHeight;
    }

    private void DisableFitterIfChildOfLayoutGroup(RectTransform rectTransform)
    {
        if (rectTransform == null || rectTransform.parent == null)
        {
            return;
        }

        if (rectTransform.parent.GetComponent<LayoutGroup>() == null)
        {
            return;
        }

        ContentSizeFitter fitter =
            rectTransform.GetComponent<ContentSizeFitter>();

        if (fitter != null && fitter.enabled)
        {
            fitter.enabled = false;
        }
    }

    private float GetPreferredOrRectHeight(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return 0f;
        }

        float preferredHeight = LayoutUtility.GetPreferredHeight(rectTransform);

        if (preferredHeight > 0f)
        {
            return preferredHeight;
        }

        return Mathf.Max(0f, rectTransform.rect.height);
    }

    private float GetHorizontalPadding(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return 0f;
        }

        HorizontalOrVerticalLayoutGroup layoutGroup =
            rectTransform.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (layoutGroup == null)
        {
            return 0f;
        }

        return layoutGroup.padding.left + layoutGroup.padding.right;
    }

    private RectTransform GetTextRect(TMP_Text textComponent)
    {
        return textComponent != null
            ? textComponent.rectTransform
            : null;
    }

    private RectTransform GetCommonAncestor(
        RectTransform first,
        RectTransform second,
        RectTransform third
    )
    {
        return GetCommonAncestor(GetCommonAncestor(first, second), third);
    }

    private RectTransform GetCommonAncestor(
        RectTransform first,
        RectTransform second
    )
    {
        if (first == null)
        {
            return second;
        }

        if (second == null)
        {
            return first;
        }

        for (Transform candidate = first; candidate != null; candidate = candidate.parent)
        {
            if (IsAncestorOrSelf(candidate, second))
            {
                return candidate as RectTransform;
            }
        }

        return null;
    }

    private bool IsAncestorOrSelf(Transform ancestor, Transform child)
    {
        for (Transform current = child; current != null; current = current.parent)
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    private float GetRectHeight(RectTransform rectTransform)
    {
        return rectTransform != null
            ? rectTransform.rect.height
            : 0f;
    }

    public void PracticarEscenarioIA()

    {

        if (AIState.SurveyResultPending && !recommendationReady)

        {

            Debug.LogWarning(

                "Practicar: recomendación todavía no disponible"

            );

            return;

        }

        AIState.SurveyResultPending = false;

        if (!recommendationReady ||
            !AIState.IsValidPracticeScenario(recommendedScenario))

        {

            Debug.LogWarning(

                "Practicar: recomendación todavía no disponible"

            );

            return;

        }

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
