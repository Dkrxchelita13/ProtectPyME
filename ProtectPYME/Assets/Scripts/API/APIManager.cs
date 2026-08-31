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

    private const string PendingSessionCompletionPrefsKey =
        "PendingMinigameCompletionSessionIds";

    private string baseUrl = "https://protectpyme.onrender.com";
    private string token;
    private bool surveyStatusRequestInProgress;
    private bool surveySubmitRequestInProgress;
    private bool pilotRequestInProgress;
    private bool minigameLessonRequestInProgress;
    private bool minigameSessionRequestInProgress;
    private readonly HashSet<string> minigameAttemptRequestsInProgress =
        new HashSet<string>();
    private readonly Dictionary<string, MinigameAttemptRequest> failedMinigameAttempts =
        new Dictionary<string, MinigameAttemptRequest>();
    private readonly HashSet<string> sessionCompletionRequestsInProgress =
        new HashSet<string>();
    private readonly HashSet<string> completedSessionIds =
        new HashSet<string>();
    private readonly HashSet<string> pendingSessionCompletionRetries =
        new HashSet<string>();
    private readonly Dictionary<string, Coroutine> sessionCompletionRetryCoroutines =
        new Dictionary<string, Coroutine>();
    private readonly float[] sessionCompletionRetryDelays = { 2f, 5f, 10f };
    private readonly HashSet<string> feedbackRequestsInProgress =
        new HashSet<string>();
    private readonly HashSet<string> feedbackLoadedSessionIds =
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
            LoadPendingMinigameSessionCompletions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartPendingMinigameSessionCompletionRetries();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            StartPendingMinigameSessionCompletionRetries();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            StartPendingMinigameSessionCompletionRetries();
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
        StartPendingMinigameSessionCompletionRetries();
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
            StartPendingMinigameSessionCompletionRetries();

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
            StartPendingMinigameSessionCompletionRetries();

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
    public IEnumerator SendDecision(int scenarioId, string choice, int responseTime)
    {
        yield return StartCoroutine(
            SendDecision(scenarioId, choice, responseTime, null)
        );
    }

    public IEnumerator SendDecision(
        int scenarioId,
        string choice,
        int responseTime,
        Action<DecisionRequestResult> onComplete
    )
    {
        string url = baseUrl + "/decisions/";
        string endpoint = "/decisions/";

        if (string.IsNullOrEmpty(token))
        {
            DecisionRequestResult noTokenResult =
                DecisionRequestResult.Failure(endpoint, 0, "NO_TOKEN", "");
            Debug.LogError(
                "Decision POST failed endpoint="
                + endpoint
                + " responseCode=0 error=NO_TOKEN body="
            );
            onComplete?.Invoke(noTokenResult);
            yield break;
        }

        string json = JsonUtility.ToJson(
            new DecisionData(scenarioId, choice, responseTime)
        );
        Debug.Log("JSON decision: " + json);
        DecisionRequestResult result;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 20;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            string responseBody = request.downloadHandler != null
                ? request.downloadHandler.text
                : "";
            string safeBody = SanitizeLogBody(responseBody);

            if (request.result == UnityWebRequest.Result.Success)
            {
                result = DecisionRequestResult.Success(
                    endpoint,
                    request.responseCode,
                    safeBody
                );
                Debug.Log(
                    "Decision enviada al backend endpoint="
                    + endpoint
                    + " responseCode="
                    + request.responseCode
                );
            }
            else
            {
                result = DecisionRequestResult.Failure(
                    endpoint,
                    request.responseCode,
                    request.error,
                    safeBody
                );
                Debug.LogError(
                    "Decision POST failed endpoint="
                    + endpoint
                    + " responseCode="
                    + request.responseCode
                    + " error="
                    + SafeLogValue(request.error)
                    + " body="
                    + safeBody
                );
            }
        }

        onComplete?.Invoke(result);
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
                string safeBody = SafeResponseBody(request);
                error = BuildSafeRequestError(request, safeBody);
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

                if (request.responseCode == 409 &&
                    SafeResponseBody(request).Contains("Attempt already exists"))
                {
                    error = "";
                }
            }
        }

        minigameAttemptRequestsInProgress.Remove(requestKey);

        if (string.IsNullOrEmpty(error))
        {
            failedMinigameAttempts.Remove(requestKey);
            onSuccess?.Invoke(parsedResponse);
        }
        else
        {
            failedMinigameAttempts[requestKey] = payload;
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

    public bool HasFailedAttemptsForSession(string sessionId)
    {
        return GetFailedAttemptsForSession(sessionId).Count > 0;
    }

    private List<MinigameAttemptRequest> GetFailedAttemptsForSession(
        string sessionId
    )
    {
        List<MinigameAttemptRequest> attempts =
            new List<MinigameAttemptRequest>();

        if (string.IsNullOrEmpty(sessionId))
        {
            return attempts;
        }

        string prefix = sessionId + "|";

        foreach (KeyValuePair<string, MinigameAttemptRequest> entry
            in failedMinigameAttempts)
        {
            if (!string.IsNullOrEmpty(entry.Key) &&
                entry.Key.StartsWith(prefix, StringComparison.Ordinal) &&
                entry.Value != null)
            {
                attempts.Add(entry.Value);
            }
        }

        return attempts;
    }

    private IEnumerator RetryFailedAttemptsForSession(
        string sessionId,
        float timeoutSeconds
    )
    {
        float startedAt = Time.realtimeSinceStartup;

        while (HasFailedAttemptsForSession(sessionId))
        {
            List<MinigameAttemptRequest> attempts =
                GetFailedAttemptsForSession(sessionId);

            for (int i = 0; i < attempts.Count; i++)
            {
                if (Time.realtimeSinceStartup - startedAt >= timeoutSeconds)
                {
                    yield break;
                }

                yield return StartCoroutine(
                    RecordMinigameAttempt(
                        attempts[i],
                        (_) => { },
                        (_) => { }
                    )
                );
            }

            yield return null;
        }
    }

    public IEnumerator CompleteMinigameSessionWhenReady(
        string sessionId,
        float timeoutSeconds,
        Action<MinigameSessionSummaryResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            CompleteMinigameSessionWhenReady(
                sessionId,
                timeoutSeconds,
                (result) =>
                {
                    if (result != null && result.success)
                    {
                        onSuccess?.Invoke(result.summary);
                    }
                    else
                    {
                        onError?.Invoke(
                            result != null
                                ? result.error
                                : "Session completion: resultado vacio."
                        );
                    }
                }
            )
        );
    }

    public IEnumerator CompleteMinigameSessionWhenReady(
        string sessionId,
        float timeoutSeconds,
        Action<MinigameSessionCompleteResult> onComplete
    )
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            MinigameSessionCompleteResult result =
                BuildMinigameCompleteResult(
                    sessionId,
                    false,
                    0,
                    "Session completion: session_id vacio.",
                    ""
                );
            LogMinigameCompleteResult(result);
            onComplete?.Invoke(result);
            yield break;
        }

        float safeTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
        float startedAt = Time.realtimeSinceStartup;

        while (HasPendingAttemptsForSession(sessionId))
        {
            if (Time.realtimeSinceStartup - startedAt >= safeTimeoutSeconds)
            {
                MinigameSessionCompleteResult result =
                    BuildMinigameCompleteResult(
                        sessionId,
                        false,
                        0,
                        "Session completion: timeout esperando intentos pendientes.",
                        ""
                    );
                LogMinigameCompleteResult(result);
                onComplete?.Invoke(result);
                yield break;
            }

            yield return null;
        }

        float remainingSeconds =
            safeTimeoutSeconds - (Time.realtimeSinceStartup - startedAt);

        if (HasFailedAttemptsForSession(sessionId) && remainingSeconds > 0f)
        {
            yield return StartCoroutine(
                RetryFailedAttemptsForSession(sessionId, remainingSeconds)
            );
        }

        if (HasFailedAttemptsForSession(sessionId))
        {
            MinigameSessionCompleteResult result =
                BuildMinigameCompleteResult(
                    sessionId,
                    false,
                    0,
                    "Session completion: hay intentos sin confirmar.",
                    ""
                );
            LogMinigameCompleteResult(result);
            onComplete?.Invoke(result);
            yield break;
        }

        yield return StartCoroutine(CompleteMinigameSession(sessionId, onComplete));
    }

    public IEnumerator CompleteMinigameSession(
        string sessionId,
        Action<MinigameSessionSummaryResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            CompleteMinigameSession(
                sessionId,
                (result) =>
                {
                    if (result != null && result.success)
                    {
                        onSuccess?.Invoke(result.summary);
                    }
                    else
                    {
                        onError?.Invoke(
                            result != null
                                ? result.error
                                : "Session completion: resultado vacio."
                        );
                    }
                }
            )
        );
    }

    public IEnumerator CompleteMinigameSession(
        string sessionId,
        Action<MinigameSessionCompleteResult> onComplete
    )
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            MinigameSessionCompleteResult result =
                BuildMinigameCompleteResult(
                    sessionId,
                    false,
                    0,
                    "Session completion: session_id vacio.",
                    ""
                );
            LogMinigameCompleteResult(result);
            onComplete?.Invoke(result);
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            MinigameSessionCompleteResult result =
                BuildMinigameCompleteResult(
                    sessionId,
                    false,
                    0,
                    "NO_TOKEN",
                    "",
                    queuedForRetry: true
                );
            LogMinigameCompleteResult(result);
            QueueMinigameSessionCompletionRetry(sessionId);
            onComplete?.Invoke(result);
            yield break;
        }

        if (completedSessionIds.Contains(sessionId))
        {
            bool hasMatchingSummary = MinigameLessonState.HasLastSummary &&
                ValuesMatch(MinigameLessonState.LastSummary.session_id, sessionId);
            MinigameSessionCompleteResult result =
                BuildMinigameCompleteResult(
                    sessionId,
                    hasMatchingSummary,
                    0,
                    hasMatchingSummary
                        ? ""
                        : "Session completion: cierre duplicado sin resumen local.",
                    "",
                    summary: hasMatchingSummary
                        ? MinigameLessonState.LastSummary
                        : null
                );
            LogMinigameCompleteResult(result);
            onComplete?.Invoke(result);
            yield break;
        }

        if (sessionCompletionRequestsInProgress.Contains(sessionId))
        {
            MinigameSessionCompleteResult result =
                BuildMinigameCompleteResult(
                    sessionId,
                    false,
                    0,
                    "Session completion: cierre en curso.",
                    ""
                );
            LogMinigameCompleteResult(result);
            onComplete?.Invoke(result);
            yield break;
        }

        sessionCompletionRequestsInProgress.Add(sessionId);

        MinigameSessionSummaryResponse parsedResponse = null;
        string error = "";
        string safeBody = "";
        long responseCode = 0;

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
            responseCode = request.responseCode;
            safeBody = SafeResponseBody(request);

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

        MinigameSessionCompleteResult completeResult =
            BuildMinigameCompleteResult(
                sessionId,
                string.IsNullOrEmpty(error),
                responseCode,
                error,
                safeBody,
                summary: parsedResponse
            );
        LogMinigameCompleteResult(completeResult);

        if (completeResult.success)
        {
            completedSessionIds.Add(sessionId);
            RemovePendingMinigameSessionCompletion(sessionId);
            MinigameLessonState.SetLastSummary(parsedResponse);
            onComplete?.Invoke(completeResult);

            if (ShouldFetchFeedbackForSession(sessionId))
            {
                StartCoroutine(
                    GetMinigameSessionFeedback(
                        sessionId,
                        (feedback) =>
                        {
                            Debug.Log(
                                "Feedback recibido id=" +
                                feedback.session_id +
                                " level=" +
                                feedback.performance_level +
                                " recommended=" +
                                feedback.recommended_minigame
                            );
                        },
                        (message) =>
                        {
                            Debug.LogWarning(
                                "Feedback no disponible id=" +
                                sessionId +
                                ": " +
                                message
                            );
                        }
                    )
                );
            }
            else
            {
                Debug.Log("Feedback: omitido porque el flujo es legacy");
            }
        }
        else
        {
            completeResult.queuedForRetry = true;
            QueueMinigameSessionCompletionRetry(sessionId);
            onComplete?.Invoke(completeResult);
        }
    }

    public bool HasPendingMinigameSessionCompletion(string sessionId)
    {
        return !string.IsNullOrEmpty(sessionId) &&
            pendingSessionCompletionRetries.Contains(sessionId);
    }

    public void QueueMinigameSessionCompletionRetry(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        bool added = pendingSessionCompletionRetries.Add(sessionId);

        if (added)
        {
            SavePendingMinigameSessionCompletions();
            Debug.Log(
                "Session completion: retry encolado id=" + sessionId
            );
        }

        StartMinigameSessionCompletionRetry(sessionId);
    }

    private void StartPendingMinigameSessionCompletionRetries()
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        List<string> pendingIds =
            new List<string>(pendingSessionCompletionRetries);

        for (int i = 0; i < pendingIds.Count; i++)
        {
            StartMinigameSessionCompletionRetry(pendingIds[i]);
        }
    }

    private void StartMinigameSessionCompletionRetry(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) ||
            string.IsNullOrEmpty(token) ||
            !pendingSessionCompletionRetries.Contains(sessionId) ||
            sessionCompletionRetryCoroutines.ContainsKey(sessionId))
        {
            return;
        }

        sessionCompletionRetryCoroutines[sessionId] =
            StartCoroutine(RetryMinigameSessionCompletion(sessionId));
    }

    private IEnumerator RetryMinigameSessionCompletion(string sessionId)
    {
        for (int i = 0; i < sessionCompletionRetryDelays.Length; i++)
        {
            if (string.IsNullOrEmpty(token) ||
                !pendingSessionCompletionRetries.Contains(sessionId))
            {
                break;
            }

            yield return new WaitForSecondsRealtime(
                sessionCompletionRetryDelays[i]
            );

            if (string.IsNullOrEmpty(token) ||
                !pendingSessionCompletionRetries.Contains(sessionId))
            {
                break;
            }

            MinigameSessionCompleteResult retryResult = null;

            yield return StartCoroutine(
                CompleteMinigameSession(
                    sessionId,
                    (result) =>
                    {
                        retryResult = result;
                    }
                )
            );

            if (retryResult != null && retryResult.success)
            {
                break;
            }
        }

        sessionCompletionRetryCoroutines.Remove(sessionId);
    }

    private void RemovePendingMinigameSessionCompletion(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        if (pendingSessionCompletionRetries.Remove(sessionId))
        {
            SavePendingMinigameSessionCompletions();
            Debug.Log(
                "Session completion: retry resuelto id=" + sessionId
            );
        }
    }

    private void LoadPendingMinigameSessionCompletions()
    {
        pendingSessionCompletionRetries.Clear();

        string raw = PlayerPrefs.GetString(
            PendingSessionCompletionPrefsKey,
            ""
        );

        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        string[] sessionIds = raw.Split(
            new char[] { ',' },
            StringSplitOptions.RemoveEmptyEntries
        );

        for (int i = 0; i < sessionIds.Length; i++)
        {
            string sessionId = sessionIds[i].Trim();

            if (!string.IsNullOrEmpty(sessionId))
            {
                pendingSessionCompletionRetries.Add(sessionId);
            }
        }
    }

    private void SavePendingMinigameSessionCompletions()
    {
        StringBuilder builder = new StringBuilder();

        foreach (string sessionId in pendingSessionCompletionRetries)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(",");
            }

            builder.Append(sessionId);
        }

        if (builder.Length == 0)
        {
            PlayerPrefs.DeleteKey(PendingSessionCompletionPrefsKey);
        }
        else
        {
            PlayerPrefs.SetString(
                PendingSessionCompletionPrefsKey,
                builder.ToString()
            );
        }

        PlayerPrefs.Save();
    }

    public IEnumerator GetMinigameSessionFeedback(
        string sessionId,
        Action<MinigameFeedbackResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            onError?.Invoke("Feedback: session_id vacio.");
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        if (feedbackRequestsInProgress.Contains(sessionId))
        {
            Debug.LogWarning(
                "Feedback: solicitud duplicada/en curso ignorada id=" +
                sessionId
            );
            yield break;
        }

        if (feedbackLoadedSessionIds.Contains(sessionId))
        {
            if (MinigameLessonState.HasLastFeedback &&
                ValuesMatch(MinigameLessonState.LastFeedback.session_id, sessionId))
            {
                onSuccess?.Invoke(MinigameLessonState.LastFeedback);
            }
            else
            {
                Debug.LogWarning(
                    "Feedback: solicitud duplicada/en curso ignorada id=" +
                    sessionId
                );
            }

            yield break;
        }

        feedbackRequestsInProgress.Add(sessionId);

        MinigameFeedbackResponse parsedResponse = null;
        string error = "";

        using (UnityWebRequest request =
            UnityWebRequest.Get(
                baseUrl +
                "/minigames/session/" +
                UnityWebRequest.EscapeURL(sessionId) +
                "/feedback"
            ))
        {
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
                        JsonUtility.FromJson<MinigameFeedbackResponse>(
                            request.downloadHandler.text
                        );
                    NormalizeMinigameFeedback(parsedResponse);
                }
                catch (Exception exception)
                {
                    error =
                        "La respuesta de feedback no se pudo leer: " +
                        exception.Message;
                }

                if (string.IsNullOrEmpty(error))
                {
                    error = ValidateMinigameFeedback(
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

        feedbackRequestsInProgress.Remove(sessionId);

        if (string.IsNullOrEmpty(error))
        {
            MinigameLessonState.SetLastFeedback(parsedResponse);
            feedbackLoadedSessionIds.Add(sessionId);
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

            string rawRecommendedTraining = response.recommended_training;
            string effectiveRecommendedTraining =
                AIState.ResolvePlayableTopic(
                    rawRecommendedTraining,
                    response.recommended_scenario,
                    AIState.SurveyPrimaryWeakness
                );

            if (!AIState.IsPlayableTopic(rawRecommendedTraining))
            {
                Debug.LogWarning(
                    "Topic IA no jugable '"
                    + SafeLogValue(rawRecommendedTraining)
                    + "'; usando fallback '"
                    + effectiveRecommendedTraining
                    + "'"
                );
            }

            response.recommended_training = effectiveRecommendedTraining;

            // NUEVO
            AIState.RawRecommendedTraining =
                rawRecommendedTraining;
            AIState.RecommendedTraining =
                effectiveRecommendedTraining;
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
            AIState.RecommendationLoaded = true;

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

    public IEnumerator GetPilotConsent(
        Action<PilotConsentResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/consent",
                "GET",
                null,
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator AcceptPilotConsent(
        Action<PilotConsentResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/consent",
                "POST",
                new PilotConsentAcceptRequest(),
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator RevokePilotConsent(
        Action<PilotConsentResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/consent/revoke",
                "POST",
                null,
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator GetPilotAssessmentStatus(
        Action<PilotAssessmentStatusResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/assessment/status",
                "GET",
                null,
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator StartPilotAssessment(
        string phase,
        Action<PilotAssessmentStartResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/assessment/start",
                "POST",
                new PilotAssessmentStartRequest(phase),
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator SendPilotAssessmentAnswer(
        string assessmentId,
        PilotAssessmentAnswerRequest payload,
        Action<PilotAssessmentAnswerResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(assessmentId))
        {
            onError?.Invoke("assessment_id vacio.");
            yield break;
        }

        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/assessment/" +
                UnityWebRequest.EscapeURL(assessmentId) +
                "/answer",
                "POST",
                payload,
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator CompletePilotAssessment(
        string assessmentId,
        Action<PilotAssessmentResultItem> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(assessmentId))
        {
            onError?.Invoke("assessment_id vacio.");
            yield break;
        }

        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/assessment/" +
                UnityWebRequest.EscapeURL(assessmentId) +
                "/complete",
                "POST",
                null,
                onSuccess,
                onError
            )
        );
    }

    public IEnumerator GetPilotAssessmentResults(
        Action<PilotAssessmentResultsResponse> onSuccess,
        Action<string> onError
    )
    {
        yield return StartCoroutine(
            SendAuthenticatedJsonRequest(
                "/pilot/assessment/results",
                "GET",
                null,
                onSuccess,
                onError
            )
        );
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

    private bool ShouldFetchFeedbackForSession(string sessionId)
    {
        return MinigameLessonState.HasValidSession &&
            ValuesMatch(MinigameLessonState.SessionId, sessionId);
    }

    private void NormalizeMinigameFeedback(MinigameFeedbackResponse response)
    {
        if (response == null)
        {
            return;
        }

        if (response.strengths == null)
        {
            response.strengths = Array.Empty<ConceptFeedbackResponse>();
        }

        if (response.reinforcement == null)
        {
            response.reinforcement = Array.Empty<ConceptFeedbackResponse>();
        }

        if (response.recommended_concept_ids == null)
        {
            response.recommended_concept_ids = Array.Empty<string>();
        }
    }

    private string ValidateMinigameFeedback(
        MinigameFeedbackResponse feedback,
        string expectedSessionId
    )
    {
        if (feedback == null)
        {
            return "Feedback: respuesta vacia.";
        }

        if (!ValuesMatch(feedback.session_id, expectedSessionId))
        {
            return "Feedback: session_id no coincide.";
        }

        if (string.IsNullOrEmpty(feedback.topic))
        {
            return "Feedback: topic vacio.";
        }

        if (string.IsNullOrEmpty(feedback.risk))
        {
            return "Feedback: risk vacio.";
        }

        if (string.IsNullOrEmpty(feedback.minigame))
        {
            return "Feedback: minigame vacio.";
        }

        if (feedback.accuracy < 0f || feedback.accuracy > 100f)
        {
            return "Feedback: accuracy invalida.";
        }

        if (feedback.total_attempts < 0 ||
            feedback.correct_attempts < 0 ||
            feedback.incorrect_attempts < 0)
        {
            return "Feedback: contadores invalidos.";
        }

        if (feedback.correct_attempts + feedback.incorrect_attempts >
            feedback.total_attempts)
        {
            return "Feedback: total_attempts no coincide.";
        }

        if (!IsValidPerformanceLevel(feedback.performance_level))
        {
            return "Feedback: performance_level invalido.";
        }

        if (string.IsNullOrEmpty(feedback.title))
        {
            return "Feedback: title vacio.";
        }

        if (string.IsNullOrEmpty(feedback.message))
        {
            return "Feedback: message vacio.";
        }

        if (string.IsNullOrEmpty(feedback.next_step))
        {
            return "Feedback: next_step vacio.";
        }

        if (feedback.strengths == null)
        {
            return "Feedback: strengths no debe ser null.";
        }

        if (feedback.reinforcement == null)
        {
            return "Feedback: reinforcement no debe ser null.";
        }

        if (feedback.recommended_concept_ids == null)
        {
            return "Feedback: recommended_concept_ids no debe ser null.";
        }

        for (int i = 0; i < feedback.strengths.Length; i++)
        {
            string conceptError = ValidateConceptFeedback(feedback.strengths[i]);

            if (!string.IsNullOrEmpty(conceptError))
            {
                return conceptError;
            }
        }

        for (int i = 0; i < feedback.reinforcement.Length; i++)
        {
            string conceptError =
                ValidateConceptFeedback(feedback.reinforcement[i]);

            if (!string.IsNullOrEmpty(conceptError))
            {
                return conceptError;
            }
        }

        return "";
    }

    private string ValidateConceptFeedback(ConceptFeedbackResponse concept)
    {
        if (concept == null)
        {
            return "Feedback: concepto vacio.";
        }

        if (string.IsNullOrEmpty(concept.concept_id))
        {
            return "Feedback: concept_id vacio.";
        }

        if (string.IsNullOrEmpty(concept.term))
        {
            return "Feedback: term vacio.";
        }

        if (concept.mastery_score < 0f || concept.mastery_score > 100f)
        {
            return "Feedback: mastery_score invalido.";
        }

        if (concept.session_attempts < 0 ||
            concept.session_correct < 0 ||
            concept.session_incorrect < 0)
        {
            return "Feedback: contadores de concepto invalidos.";
        }

        if (!IsValidConceptFeedbackStatus(concept.status))
        {
            return "Feedback: status de concepto invalido.";
        }

        if (string.IsNullOrEmpty(concept.message))
        {
            return "Feedback: message de concepto vacio.";
        }

        if (string.IsNullOrEmpty(concept.recommendation))
        {
            return "Feedback: recommendation de concepto vacia.";
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

    private bool IsValidPerformanceLevel(string performanceLevel)
    {
        string value = (performanceLevel ?? "").Trim().ToLowerInvariant();
        return value == "sin_evidencia"
            || value == "excelente"
            || value == "buen_progreso"
            || value == "en_desarrollo"
            || value == "necesita_refuerzo";
    }

    private bool IsValidConceptFeedbackStatus(string status)
    {
        string value = (status ?? "").Trim().ToLowerInvariant();
        return value == "fortaleza"
            || value == "avance"
            || value == "refuerzo"
            || value == "dificultad_puntual";
    }

    private bool ValuesMatch(string left, string right)
    {
        return string.Equals(
            (left ?? "").Trim(),
            (right ?? "").Trim(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private MinigameSessionCompleteResult BuildMinigameCompleteResult(
        string sessionId,
        bool success,
        long responseCode,
        string error,
        string body,
        bool queuedForRetry = false,
        MinigameSessionSummaryResponse summary = null
    )
    {
        return new MinigameSessionCompleteResult
        {
            success = success,
            responseCode = responseCode,
            error = error ?? "",
            body = body ?? "",
            sessionId = sessionId ?? "",
            queuedForRetry = queuedForRetry,
            summary = summary
        };
    }

    private void LogMinigameCompleteResult(MinigameSessionCompleteResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("[MINIGAME COMPLETE] result=null");
            return;
        }

        Debug.Log(
            "[MINIGAME COMPLETE] session=" +
            result.sessionId +
            " responseCode=" +
            result.responseCode +
            " success=" +
            result.success
        );

        if (!result.success && !string.IsNullOrEmpty(result.body))
        {
            Debug.LogWarning("[MINIGAME COMPLETE] body=" + result.body);
        }
    }

    private string SafeResponseBody(UnityWebRequest request)
    {
        string body = request != null && request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (string.IsNullOrEmpty(body))
        {
            return "";
        }

        const int maxLength = 500;
        string safeBody = body.Replace("\r", " ").Replace("\n", " ");

        if (safeBody.Length > maxLength)
        {
            safeBody = safeBody.Substring(0, maxLength) + "...";
        }

        return safeBody;
    }

    private string BuildSafeRequestError(UnityWebRequest request, string safeBody)
    {
        long responseCode = request != null ? request.responseCode : 0;

        if (!string.IsNullOrEmpty(safeBody))
        {
            return "HTTP_" + responseCode + ": " + safeBody;
        }

        if (request != null && !string.IsNullOrEmpty(request.error))
        {
            return "HTTP_" + responseCode + ": " + request.error;
        }

        return "HTTP_" + responseCode;
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

    private IEnumerator SendAuthenticatedJsonRequest<T>(
        string path,
        string method,
        object payload,
        Action<T> onSuccess,
        Action<string> onError
    ) where T : class
    {
        if (pilotRequestInProgress)
        {
            onError?.Invoke("Ya hay una solicitud del piloto en curso.");
            yield break;
        }

        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("NO_TOKEN");
            yield break;
        }

        pilotRequestInProgress = true;
        T parsedResponse = null;
        string error = "";

        using (UnityWebRequest request =
            new UnityWebRequest(baseUrl + path, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 20;

            if (payload != null)
            {
                string json = JsonUtility.ToJson(payload);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            string responseBody = request.downloadHandler != null
                ? request.downloadHandler.text
                : "";

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    parsedResponse =
                        JsonUtility.FromJson<T>(responseBody);
                    NormalizePilotStatusNulls(responseBody, parsedResponse);
                }
                catch (Exception exception)
                {
                    error =
                        "La respuesta del piloto no se pudo leer: " +
                        exception.Message;
                }
            }
            else
            {
                error = BuildRequestError(request);
            }
        }

        pilotRequestInProgress = false;

        if (string.IsNullOrEmpty(error))
        {
            onSuccess?.Invoke(parsedResponse);
        }
        else
        {
            onError?.Invoke(error);
        }
    }

    private void NormalizePilotStatusNulls<T>(
        string responseBody,
        T parsedResponse
    ) where T : class
    {
        PilotAssessmentStatusResponse status =
            parsedResponse as PilotAssessmentStatusResponse;

        if (status == null || !IsPilotStatusBody(responseBody))
        {
            return;
        }

        if (JsonHasNullField(responseBody, "pre"))
        {
            status.pre = null;
        }

        if (JsonHasNullField(responseBody, "post"))
        {
            status.post = null;
        }
    }

    private bool IsPilotStatusBody(string responseBody)
    {
        return !string.IsNullOrEmpty(responseBody) &&
            responseBody.IndexOf(
                "\"intervention_progress\"",
                StringComparison.Ordinal
            ) >= 0;
    }

    private bool JsonHasNullField(string json, string fieldName)
    {
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        string compact = json
            .Replace(" ", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", "");
        return compact.IndexOf(
            "\"" + fieldName + "\":null",
            StringComparison.Ordinal
        ) >= 0;
    }

    private string SafeLogValue(string value)
    {
        return string.IsNullOrEmpty(value) ? "<vacio>" : value;
    }

    private string SanitizeLogBody(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        string compact = value.Replace("\r", " ").Replace("\n", " ");

        if (compact.Length > 500)
        {
            compact = compact.Substring(0, 500) + "...";
        }

        return compact;
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

public class DecisionRequestResult
{
    public bool success;
    public long response_code;
    public string error;
    public string body;
    public string endpoint;

    public static DecisionRequestResult Success(
        string endpoint,
        long responseCode,
        string body
    )
    {
        return new DecisionRequestResult
        {
            success = true,
            response_code = responseCode,
            error = "",
            body = body,
            endpoint = endpoint
        };
    }

    public static DecisionRequestResult Failure(
        string endpoint,
        long responseCode,
        string error,
        string body
    )
    {
        return new DecisionRequestResult
        {
            success = false,
            response_code = responseCode,
            error = error,
            body = body,
            endpoint = endpoint
        };
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
