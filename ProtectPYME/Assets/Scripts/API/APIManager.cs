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
            topic;

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
            topic;

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
            topic;

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

            Debug.Log(
                "Tema recomendado IA: "
                + AIState.RecommendedTraining
            );

            onSuccess?.Invoke(response);
        }
        else
        {
            onError?.Invoke(request.error);
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