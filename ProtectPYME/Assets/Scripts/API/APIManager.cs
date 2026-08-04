using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using System;


public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

    private string baseUrl = "https://protectpyme.onrender.com";
    private string token;
    private bool surveyStatusRequestInProgress;
    private bool surveySubmitRequestInProgress;
    private bool minigameLessonRequestInProgress;
    private bool minigameSessionRequestInProgress;
    private readonly HashSet<string> minigameAttemptRequestsInProgress =
        new HashSet<string>();
    private readonly HashSet<string> sessionCompletionRequestsInProgress =
        new HashSet<string>();
    private readonly HashSet<string> completedSessionIds =
        new HashSet<string>();



    public IEnumerator Register(
        string nombre,
        string email,
        string password,
        TMP_Text mensajeUI
    )
    {
        RegisterRequest req = new RegisterRequest();

        req.name = nombre;
        req.email = email;
        req.password = password;

        string json = JsonUtility.ToJson(req);

        UnityWebRequest request =
            new UnityWebRequest(
                baseUrl + "/users/",
                "POST"
            );

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(json);

        request.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result ==
            UnityWebRequest.Result.Success)
        {
            mensajeUI.text =
                "Usuario registrado correctamente";

            Debug.Log(
                "Registro exitoso: "
                + request.downloadHandler.text
            );
        }
        else
        {
            mensajeUI.text =
                "Error al registrar usuario";

            Debug.LogError(
                request.downloadHandler.text
            );
        }
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            token = PlayerPrefs.GetString("token", "");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // 🔥 ENVIAR SCORE
    public IEnumerator SendScore(int score)
    {
        string url = baseUrl + "/leaderboard/score?score=" + score;

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("🏆 Score enviado correctamente");
        }
        else
        {
            Debug.LogError("❌ Error al enviar score: " + request.error);
        }
    }
    //  FUERA DEL AWAKE
    public string GetToken()
    {
        return token;
    }

    public void SetToken(string newToken)
    {
        token = newToken;
    }

    // LOGIN
    public IEnumerator Login(string email, string password, System.Action<string> callback)
    {
        string url = baseUrl + "/login";

        string json = JsonUtility.ToJson(new LoginData(email, password));

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            TokenResponse res = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
            token = res.access_token;

            // 🔥 guardar token
            PlayerPrefs.SetString("token", token);
            PlayerPrefs.Save();

            callback?.Invoke("OK");
        }
        else
        {
            callback?.Invoke("ERROR");
        }
    }

    public IEnumerator LoginWithGoogle(string googleIdToken, System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(googleIdToken))
        {
            Debug.LogError("Google id_token vacio");
            callback?.Invoke("ERROR");
            yield break;
        }

        string url = baseUrl + "/auth/google";
        string json = JsonUtility.ToJson(new GoogleLoginData(googleIdToken));

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            TokenResponse res = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
            token = res.access_token;

            PlayerPrefs.SetString("token", token);
            PlayerPrefs.Save();

            callback?.Invoke("OK");
        }
        else
        {
            Debug.LogError("Error en login Google: " + request.error);
            Debug.LogError("Respuesta: " + request.downloadHandler.text);
            callback?.Invoke("ERROR");
        }
    }

    // GET SCENARIOS
    public IEnumerator GetScenarios(System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token → usuario no autenticado");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url = baseUrl + "/scenarios";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Datos recibidos del backend");
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Error API: " + request.error);
            Debug.LogError("Respuesta: " + request.downloadHandler.text);

            callback?.Invoke("ERROR");
        }
    }
    // 🔥 GET QUIZ
    public IEnumerator GetQuiz(
        string topic,
        string risk,
        System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token → usuario no autenticado");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url =
            baseUrl +
            "/minigames/quiz?topic=" +
            UnityWebRequest.EscapeURL(topic) +
            "&risk=" +
            UnityWebRequest.EscapeURL(risk);

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Quiz recibido del backend");
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Error quiz: " + request.error);
            callback?.Invoke("ERROR");
        }
    }
    // 🔥 SEND DECISION (AQUÍ VA DENTRO)
    public IEnumerator SendDecision(int scenarioId, string choice, int responseTime)
    {
        string url = baseUrl + "/decisions/";

        string json = JsonUtility.ToJson(
            new DecisionData(scenarioId, choice, responseTime)
        );
        Debug.Log("📤 JSON decision: " + json);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Decision enviada al backend");
        }
        else
        {
            Debug.LogError("❌ Error al enviar decision: " + request.error);
        }
    }
    // 🔥 GET WORDS (SOPA DE LETRAS)
    public IEnumerator GetWords(
        string topic,
        string risk,
        System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url =
            baseUrl +
            "/minigames/wordsearch?topic=" +
            UnityWebRequest.EscapeURL(topic) +
            "&risk=" +
            UnityWebRequest.EscapeURL(risk);

        UnityWebRequest request =
            UnityWebRequest.Get(url);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        if (request.result ==
            UnityWebRequest.Result.Success)
        {
            Debug.Log(
                "✅ Sopa adaptativa recibida"
            );

            callback?.Invoke(
                request.downloadHandler.text
            );
        }
        else
        {
            Debug.LogError(request.error);

            callback?.Invoke("ERROR");
        }
    }
    public IEnumerator GetCrossword(
        string topic,
        string risk,
        System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url =
            baseUrl +
            "/minigames/crossword?topic=" +
            UnityWebRequest.EscapeURL(topic) +
            "&risk=" +
            UnityWebRequest.EscapeURL(risk);

        UnityWebRequest request =
            UnityWebRequest.Get(url);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        if (request.result ==
            UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Crossword adaptativo");

            callback?.Invoke(
                request.downloadHandler.text
            );
        }
        else
        {
            Debug.LogError(request.error);

            callback?.Invoke("ERROR");
        }
    }

    public IEnumerator CreateMinigameSession(
        string topic,
        string risk,
        string minigame,
        Action<MinigameSessionResponse> onSuccess,
        Action<string> onError
    )
    {
        if (minigameSessionRequestInProgress)
        {
            Debug.LogWarning(
                "Minigame session: se ignoro una solicitud duplicada mientras la sesion esta cargando."
            );
            yield break;
        }

        string parameterError = ValidateMinigameSessionParameters(
            topic,
            risk,
            minigame
        );

        if (!string.IsNullOrEmpty(parameterError))
        {
            onError?.Invoke(parameterError);
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        MinigameSessionRequest payload =
            new MinigameSessionRequest(topic, risk, minigame);
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        minigameSessionRequestInProgress = true;

        MinigameSessionResponse parsedResponse = null;
        string error = "";

        using (UnityWebRequest request =
            new UnityWebRequest(baseUrl + "/minigames/session", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + token
            );

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    parsedResponse =
                        JsonUtility.FromJson<MinigameSessionResponse>(
                            request.downloadHandler.text
                        );
                }
                catch (Exception exception)
                {
                    error =
                        "La respuesta de la sesion no se pudo leer: " +
                        exception.Message;
                }

                if (string.IsNullOrEmpty(error))
                {
                    error = ValidateMinigameSessionResponse(
                        parsedResponse,
                        topic,
                        risk,
                        minigame
                    );
                }
            }
            else if (request.responseCode == 401)
            {
                error = "HTTP_401: Sesion expirada. Inicia sesion nuevamente.";
            }
            else
            {
                error = BuildRequestError(request);
            }
        }

        minigameSessionRequestInProgress = false;

        if (string.IsNullOrEmpty(error))
        {
            onSuccess?.Invoke(parsedResponse);
        }
        else
        {
            onError?.Invoke(error);
        }
    }

    public IEnumerator RecordMinigameAttempt(
        MinigameAttemptRequest payload,
        Action<MinigameAttemptResponse> onSuccess,
        Action<string> onError
    )
    {
        string validationError = ValidateMinigameAttemptRequest(payload);

        if (!string.IsNullOrEmpty(validationError))
        {
            onError?.Invoke(validationError);
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        string requestKey = BuildMinigameAttemptKey(payload);

        if (minigameAttemptRequestsInProgress.Contains(requestKey))
        {
            Debug.LogWarning(
                "Attempt: envio duplicado ignorado para item=" + payload.item_id
            );
            yield break;
        }

        minigameAttemptRequestsInProgress.Add(requestKey);

        MinigameAttemptResponse parsedResponse = null;
        string error = "";
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
            new UnityWebRequest(baseUrl + "/minigames/attempts", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + token
            );

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    parsedResponse =
                        JsonUtility.FromJson<MinigameAttemptResponse>(
                            request.downloadHandler.text
                        );
                }
                catch (Exception exception)
                {
                    error =
                        "La respuesta del intento no se pudo leer: " +
                        exception.Message;
                }
            }
            else
            {
                error = BuildRequestError(request);
            }
        }

        minigameAttemptRequestsInProgress.Remove(requestKey);

        if (string.IsNullOrEmpty(error))
        {
            onSuccess?.Invoke(parsedResponse);
        }
        else
        {
            onError?.Invoke(error);
        }
    }

    public bool HasPendingAttemptsForSession(string sessionId)
    {
        return GetPendingAttemptsCount(sessionId) > 0;
    }

    public int GetPendingAttemptsCount(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return 0;
        }

        string prefix = sessionId + "|";
        int count = 0;

        foreach (string requestKey in minigameAttemptRequestsInProgress)
        {
            if (!string.IsNullOrEmpty(requestKey) &&
                requestKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    public IEnumerator CompleteMinigameSessionWhenReady(
        string sessionId,
        float timeoutSeconds,
        Action<MinigameSessionSummaryResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            onError?.Invoke("Session completion: session_id vacio.");
            yield break;
        }

        float safeTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
        float startedAt = Time.realtimeSinceStartup;

        while (HasPendingAttemptsForSession(sessionId))
        {
            if (Time.realtimeSinceStartup - startedAt >= safeTimeoutSeconds)
            {
                onError?.Invoke(
                    "Session completion: timeout esperando intentos pendientes."
                );
                yield break;
            }

            yield return null;
        }

        yield return StartCoroutine(
            CompleteMinigameSession(sessionId, onSuccess, onError)
        );
    }

    public IEnumerator CompleteMinigameSession(
        string sessionId,
        Action<MinigameSessionSummaryResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            onError?.Invoke("Session completion: session_id vacio.");
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        if (completedSessionIds.Contains(sessionId))
        {
            Debug.LogWarning(
                "Session completion: cierre duplicado ignorado id=" + sessionId
            );
            yield break;
        }

        if (sessionCompletionRequestsInProgress.Contains(sessionId))
        {
            Debug.LogWarning(
                "Session completion: cierre en curso ignorado id=" + sessionId
            );
            yield break;
        }

        sessionCompletionRequestsInProgress.Add(sessionId);

        MinigameSessionSummaryResponse parsedResponse = null;
        string error = "";

        using (UnityWebRequest request =
            new UnityWebRequest(
                baseUrl +
                "/minigames/session/" +
                UnityWebRequest.EscapeURL(sessionId) +
                "/complete",
                "POST"
            ))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + token
            );

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    parsedResponse =
                        JsonUtility.FromJson<MinigameSessionSummaryResponse>(
                            request.downloadHandler.text
                        );
                }
                catch (Exception exception)
                {
                    error =
                        "La respuesta del cierre no se pudo leer: " +
                        exception.Message;
                }

                if (string.IsNullOrEmpty(error))
                {
                    error = ValidateMinigameSessionSummary(
                        parsedResponse,
                        sessionId
                    );
                }
            }
            else
            {
                error = BuildRequestError(request);
            }
        }

        sessionCompletionRequestsInProgress.Remove(sessionId);

        if (string.IsNullOrEmpty(error))
        {
            completedSessionIds.Add(sessionId);
            MinigameLessonState.SetLastSummary(parsedResponse);
            onSuccess?.Invoke(parsedResponse);
        }
        else
        {
            onError?.Invoke(error);
        }
    }

    public IEnumerator GetMinigameLesson(
        string topic,
        string risk,
        string minigame,
        Action<MinigameLessonResponse> onSuccess,
        Action<string> onError
    )
    {
        if (minigameLessonRequestInProgress)
        {
            Debug.LogWarning(
                "Lesson: se ignoro una solicitud duplicada mientras la leccion esta cargando."
            );
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        string safeTopic = topic ?? "";
        string safeRisk = risk ?? "";
        string safeMinigame = minigame ?? "";

        string url =
            baseUrl +
            "/minigames/lesson?topic=" +
            UnityWebRequest.EscapeURL(safeTopic) +
            "&risk=" +
            UnityWebRequest.EscapeURL(safeRisk) +
            "&minigame=" +
            UnityWebRequest.EscapeURL(safeMinigame);

        minigameLessonRequestInProgress = true;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + token
            );

            yield return request.SendWebRequest();

            FinishMinigameLessonRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MinigameLessonResponse response = null;

                try
                {
                    response =
                        JsonUtility.FromJson<MinigameLessonResponse>(
                            request.downloadHandler.text
                        );
                }
                catch (Exception exception)
                {
                    onError?.Invoke(
                        "La respuesta de la leccion no se pudo leer: " +
                        exception.Message
                    );
                    yield break;
                }

                string validationError = ValidateMinigameLesson(response);

                if (!string.IsNullOrEmpty(validationError))
                {
                    onError?.Invoke(validationError);
                    yield break;
                }

                onSuccess?.Invoke(response);
            }
            else
            {
                if (request.responseCode == 401)
                {
                    onError?.Invoke(
                        "HTTP_401: Sesion expirada. Inicia sesion nuevamente."
                    );
                }
                else
                {
                    onError?.Invoke(BuildRequestError(request));
                }
            }
        }
    }

    // 🔥 GET ANALYTICS
    public IEnumerator GetAnalytics(System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url = baseUrl + "/users/me/analytics";

        UnityWebRequest request = UnityWebRequest.Get(url);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Analytics recibidos");
            Debug.Log(request.downloadHandler.text);

            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError(
                "❌ Error analytics: " + request.error
            );

            callback?.Invoke("ERROR");
        }
    }
    // 🔥 GET LEADERBOARD
    public IEnumerator GetLeaderboard(
        System.Action<string> callback
    )
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");

            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url = baseUrl + "/leaderboard/";

        UnityWebRequest request =
            UnityWebRequest.Get(url);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        if (request.result ==
            UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Leaderboard recibido");

            callback?.Invoke(
                request.downloadHandler.text
            );
        }
        else
        {
            Debug.LogError(
                "❌ Error leaderboard: " +
                request.error
            );

            callback?.Invoke("ERROR");
        }
    }

    public IEnumerator GetBadges(System.Action<int> callback)
    {
        string url = baseUrl + "/users/me/badges";

        UnityWebRequest request =
            UnityWebRequest.Get(url);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            callback(0);
        }
        else
        {
            string json =
                request.downloadHandler.text;

            json = "{\"badges\":" + json + "}";

            BadgeList badgeList =
                JsonUtility.FromJson<BadgeList>(json);

            callback(badgeList.badges.Length);
        }
    }

    public IEnumerator GetAIRisk(
        Action<AIRiskResponse> onSuccess,
        Action<string> onError
    )
    {
        string url = $"{baseUrl}/ai/risk/me";

        UnityWebRequest request = UnityWebRequest.Get(url);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AIRiskResponse response =
                JsonUtility.FromJson<AIRiskResponse>(
                    request.downloadHandler.text
                );

            // NUEVO
            AIState.RecommendedTraining =
                response.recommended_training;
            AIState.RecommendedScenario =
                response.recommended_scenario;
            AIState.RiskLevel =
                NormalizeRiskLevel(response.risk_level);
            AIState.RiskSource =
                response.risk_source;
            AIState.BehavioralDecisions =
                response.behavioral_decisions;
            AIState.MinBehavioralDecisions =
                response.min_behavioral_decisions > 0
                    ? response.min_behavioral_decisions
                    : 3;
            AIState.SufficientBehavioralData =
                response.sufficient_behavioral_data;

            Debug.Log(
                "Tema recomendado IA: "
                + AIState.RecommendedTraining
            );

            Debug.Log(
                "Nivel de riesgo IA: " +
                AIState.RiskLevel
            );

            Debug.Log(
                "Fuente de riesgo: " +
                AIState.RiskSource
            );

            Debug.Log(
                "Decisiones conductuales: " +
                AIState.BehavioralDecisions +
                "/" +
                AIState.MinBehavioralDecisions
            );

            onSuccess?.Invoke(response);
        }
        else
        {
            onError?.Invoke(request.error);
        }
    }

    public IEnumerator GetSurveyStatus(
        Action<SurveyStatusResponse> onSuccess,
        Action<string> onError
    )
    {
        if (surveyStatusRequestInProgress)
        {
            onError?.Invoke("Ya hay una consulta de encuesta en curso.");
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        surveyStatusRequestInProgress = true;

        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/survey/status"))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + token
            );

            yield return request.SendWebRequest();

            surveyStatusRequestInProgress = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                SurveyStatusResponse response =
                    JsonUtility.FromJson<SurveyStatusResponse>(
                        request.downloadHandler.text
                    );

                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(BuildRequestError(request));
            }
        }
    }

    public IEnumerator SubmitSurvey(
        SurveySubmitRequest payload,
        Action<SurveySubmitResponse> onSuccess,
        Action<string> onError
    )
    {
        if (surveySubmitRequestInProgress)
        {
            onError?.Invoke("Ya hay un envio de encuesta en curso.");
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        surveySubmitRequestInProgress = true;

        using (UnityWebRequest request =
            new UnityWebRequest(baseUrl + "/survey/submit", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + token
            );

            yield return request.SendWebRequest();

            surveySubmitRequestInProgress = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                SurveySubmitResponse response =
                    JsonUtility.FromJson<SurveySubmitResponse>(
                        request.downloadHandler.text
                    );

                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(BuildRequestError(request));
            }
        }
    }

    private string ValidateMinigameSessionParameters(
        string topic,
        string risk,
        string minigame
    )
    {
        if (!IsValidTopic(topic))
        {
            return "Topic de minijuego invalido.";
        }

        if (!IsValidRisk(risk))
        {
            return "Risk de minijuego invalido.";
        }

        if (!IsValidMinigame(minigame))
        {
            return "Minigame invalido.";
        }

        return "";
    }

    private string ValidateMinigameSessionResponse(
        MinigameSessionResponse response,
        string expectedTopic,
        string expectedRisk,
        string expectedMinigame
    )
    {
        if (response == null)
        {
            return "La respuesta de la sesion esta vacia.";
        }

        if (string.IsNullOrEmpty(response.session_id))
        {
            return "La sesion no incluye session_id.";
        }

        if (!ValuesMatch(response.topic, expectedTopic) ||
            !ValuesMatch(response.risk, expectedRisk) ||
            !ValuesMatch(response.minigame, expectedMinigame))
        {
            return "La sesion no coincide con el minijuego solicitado.";
        }

        if (response.items == null || response.items.Length == 0)
        {
            return "La sesion no incluye items.";
        }

        string lessonError = ValidateMinigameLesson(response.lesson);

        if (!string.IsNullOrEmpty(lessonError))
        {
            return lessonError;
        }

        for (int i = 0; i < response.items.Length; i++)
        {
            MinigameSessionItem item = response.items[i];

            if (item == null)
            {
                return "La sesion incluye un item vacio.";
            }

            if (string.IsNullOrEmpty(item.item_id))
            {
                return "Cada item de sesion debe incluir item_id.";
            }

            if (item.concept_ids == null || item.concept_ids.Length == 0)
            {
                return "Cada item de sesion debe incluir concept_ids.";
            }

            if (!ValuesMatch(item.difficulty, expectedRisk))
            {
                return "La dificultad de un item no coincide con el riesgo.";
            }

            string itemTypeError = ValidateMinigameSessionItemType(
                item,
                expectedMinigame
            );

            if (!string.IsNullOrEmpty(itemTypeError))
            {
                return itemTypeError;
            }

            for (int conceptIndex = 0;
                conceptIndex < item.concept_ids.Length;
                conceptIndex++)
            {
                if (string.IsNullOrEmpty(item.concept_ids[conceptIndex]))
                {
                    return "La sesion incluye un concept_id vacio.";
                }
            }
        }

        return "";
    }

    private string ValidateMinigameAttemptRequest(MinigameAttemptRequest request)
    {
        if (request == null)
        {
            return "Attempt: request vacio.";
        }

        if (string.IsNullOrEmpty(request.session_id))
        {
            return "Attempt: session_id vacio.";
        }

        if (string.IsNullOrEmpty(request.item_id))
        {
            return "Attempt: item_id vacio.";
        }

        if (request.response_time_ms < 0)
        {
            return "Attempt: response_time_ms invalido.";
        }

        if (request.attempt_number < 1)
        {
            return "Attempt: attempt_number invalido.";
        }

        return "";
    }

    private string ValidateMinigameSessionSummary(
        MinigameSessionSummaryResponse summary,
        string expectedSessionId
    )
    {
        if (summary == null)
        {
            return "Session completion: resumen vacio.";
        }

        if (!ValuesMatch(summary.session_id, expectedSessionId))
        {
            return "Session completion: session_id no coincide.";
        }

        if (!ValuesMatch(summary.status, "completed"))
        {
            return "Session completion: status invalido.";
        }

        if (summary.total_items < 0 ||
            summary.attempted_items < 0 ||
            summary.total_attempts < 0)
        {
            return "Session completion: contadores invalidos.";
        }

        if (summary.accuracy < 0f || summary.accuracy > 100f)
        {
            return "Session completion: accuracy invalida.";
        }

        return "";
    }

    private string BuildMinigameAttemptKey(MinigameAttemptRequest request)
    {
        return request.session_id
            + "|"
            + request.item_id
            + "|"
            + request.attempt_number;
    }

    private string ValidateMinigameSessionItemType(
        MinigameSessionItem item,
        string expectedMinigame
    )
    {
        if (ValuesMatch(expectedMinigame, "quiz"))
        {
            if (string.IsNullOrEmpty(item.question))
            {
                return "Cada item de quiz debe incluir question.";
            }

            if (item.options == null || item.options.Length == 0)
            {
                return "Cada item de quiz debe incluir options.";
            }

            if (item.correct_option < 0 ||
                item.correct_option >= item.options.Length)
            {
                return "El correct_option de quiz esta fuera de rango.";
            }

            return "";
        }

        if (string.IsNullOrEmpty(item.clue))
        {
            return "Cada item de sopa o crucigrama debe incluir clue.";
        }

        if (string.IsNullOrEmpty(item.answer_text))
        {
            return "Cada item de sopa o crucigrama debe incluir answer_text.";
        }

        if (item.correct_option != -1)
        {
            return "Sopa y crucigrama deben usar correct_option = -1.";
        }

        return "";
    }

    private bool IsValidTopic(string topic)
    {
        string value = (topic ?? "").Trim().ToLowerInvariant();
        return value == "phishing"
            || value == "passwords"
            || value == "malware"
            || value == "wifi";
    }

    private bool IsValidRisk(string risk)
    {
        string value = (risk ?? "").Trim().ToLowerInvariant();
        return value == "alto"
            || value == "medio"
            || value == "bajo";
    }

    private bool IsValidMinigame(string minigame)
    {
        string value = (minigame ?? "").Trim().ToLowerInvariant();
        return value == "quiz"
            || value == "wordsearch"
            || value == "crossword";
    }

    private bool ValuesMatch(string left, string right)
    {
        return string.Equals(
            (left ?? "").Trim(),
            (right ?? "").Trim(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private string BuildRequestError(UnityWebRequest request)
    {
        string body = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!string.IsNullOrEmpty(body))
        {
            return "HTTP_" + request.responseCode + ": " + body;
        }

        if (!string.IsNullOrEmpty(request.error))
        {
            return "HTTP_" + request.responseCode + ": " + request.error;
        }

        return "HTTP_" + request.responseCode;
    }

    private void FinishMinigameLessonRequest()
    {
        minigameLessonRequestInProgress = false;
    }

    private string ValidateMinigameLesson(MinigameLessonResponse response)
    {
        if (response == null)
        {
            return "La respuesta de la leccion esta vacia.";
        }

        if (string.IsNullOrEmpty(response.title))
        {
            return "La leccion no incluye titulo.";
        }

        if (string.IsNullOrEmpty(response.explanation))
        {
            return "La leccion no incluye explicacion.";
        }

        if (string.IsNullOrEmpty(response.learning_objective))
        {
            return "La leccion no incluye objetivo de aprendizaje.";
        }

        if (string.IsNullOrEmpty(response.recommended_action))
        {
            return "La leccion no incluye accion recomendada.";
        }

        if (response.tips == null)
        {
            return "La leccion no incluye recomendaciones.";
        }

        if (response.tips.Length != 3)
        {
            return "La leccion debe incluir exactamente 3 recomendaciones.";
        }

        if (string.IsNullOrEmpty(response.minigame))
        {
            return "La leccion no incluye minijuego.";
        }

        if (response.key_concepts == null)
        {
            return "La leccion no incluye conceptos clave.";
        }

        if (response.key_concepts.Length < 2 || response.key_concepts.Length > 4)
        {
            return "La leccion debe incluir entre 2 y 4 conceptos clave.";
        }

        for (int i = 0; i < response.key_concepts.Length; i++)
        {
            LessonConcept concept = response.key_concepts[i];

            if (concept == null ||
                string.IsNullOrEmpty(concept.term) ||
                string.IsNullOrEmpty(concept.definition) ||
                string.IsNullOrEmpty(concept.why_it_matters) ||
                string.IsNullOrEmpty(concept.example))
            {
                return "Cada concepto clave debe estar completo.";
            }
        }

        if (response.practical_example == null)
        {
            return "La leccion no incluye ejemplo practico.";
        }

        if (string.IsNullOrEmpty(response.practical_example.title))
        {
            return "El ejemplo practico no incluye titulo.";
        }

        if (response.practical_example.steps == null ||
            response.practical_example.steps.Length < 3 ||
            response.practical_example.steps.Length > 5)
        {
            return "El ejemplo practico debe incluir entre 3 y 5 pasos.";
        }

        if (response.common_mistake == null)
        {
            return "La leccion no incluye error frecuente.";
        }

        if (string.IsNullOrEmpty(response.common_mistake.title) ||
            string.IsNullOrEmpty(response.common_mistake.explanation))
        {
            return "El error frecuente no esta completo.";
        }

        if (response.quick_check == null)
        {
            return "La leccion no incluye comprobacion rapida.";
        }

        if (string.IsNullOrEmpty(response.quick_check.question) ||
            string.IsNullOrEmpty(response.quick_check.explanation))
        {
            return "La comprobacion rapida no esta completa.";
        }

        if (response.quick_check.options == null ||
            response.quick_check.options.Length != 3)
        {
            return "La comprobacion rapida debe incluir exactamente 3 opciones.";
        }

        if (response.quick_check.correct_option < 0 ||
            response.quick_check.correct_option > 2)
        {
            return "La comprobacion rapida tiene una respuesta correcta invalida.";
        }

        if (string.IsNullOrEmpty(response.visual_key))
        {
            return "La leccion no incluye clave visual.";
        }

        return "";
    }

    private string NormalizeRiskLevel(string risk)
    {
        if (string.IsNullOrEmpty(risk))
            return "alto";

        risk = risk.Trim().ToUpper();

        switch (risk)
        {
            case "ALTO":
                return "alto";

            case "MEDIO":
                return "medio";

            case "BAJO":
                return "bajo";

            default:
                return "alto";
        }
    }

}
    
    [System.Serializable]
public class LoginData
{
    public string email;
    public string password;

    public LoginData(string e, string p)
    {
        email = e;
        password = p;
    }
}

[System.Serializable]
public class TokenResponse
{
    public string access_token;
    public string token_type;
}

[System.Serializable]
public class GoogleLoginData
{
    public string id_token;

    public GoogleLoginData(string token)
    {
        id_token = token;
    }
}

[System.Serializable]
public class DecisionData
{
    public int scenario_id;
    public string choice;
    public int response_time;

    public DecisionData(int id, string c, int time)
    {
        scenario_id = id;
        choice = c;
        response_time = time;
    }
}
[System.Serializable]
public class QuizQuestion
{
    public string question;
    public string[] options;
    public int answer;
}

[System.Serializable]
public class QuizList
{
    public QuizQuestion[] items;
}

[System.Serializable]
public class WordsearchData
{
    public string clue;
    public string answer;
}

[System.Serializable]
public class WordsearchList
{
    public WordsearchData[] items;
}

[System.Serializable]
public class WordList
{
    public WordsearchData[] items;
}
[System.Serializable]
public class AnalyticsData
{
    public string level;

    public int total_points;

    public float accuracy;
    public float risk_index;
    public float awareness_score;
    public bool high_risk_user;
    public string most_failed_category;
    public int decisions_last_7_days;
}
[System.Serializable]
public class BadgeData
{
    public int id;
    public string name;
    public string description;
    public string icon;
}
[System.Serializable]
public class BadgeList
{
    public BadgeData[] badges;
}
[System.Serializable]
public class LeaderboardUser
{
    public int rank;
    public int id;
    public string name;
    public int total_points;
}

[System.Serializable]
public class LeaderboardList
{
    public LeaderboardUser[] items;
}

[System.Serializable]
public class RegisterRequest
{
    public string name;
    public string email;
    public string password;
}

[System.Serializable]
public class RegisterResponse
{
    public int id;
    public string name;
    public string email;
}
