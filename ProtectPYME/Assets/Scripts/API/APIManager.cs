using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
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
            onError?.Invoke("Ya hay una consulta de leccion en curso.");
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

            minigameLessonRequestInProgress = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                MinigameLessonResponse response =
                    JsonUtility.FromJson<MinigameLessonResponse>(
                        request.downloadHandler.text
                    );

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
                string.IsNullOrEmpty(concept.definition))
            {
                return "Cada concepto clave debe incluir termino y definicion.";
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
