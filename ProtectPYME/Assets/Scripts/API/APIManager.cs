using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

    private string baseUrl = "http://172.20.32.1:8000";
    private string token;

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
    public IEnumerator GetQuiz(System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token → usuario no autenticado");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url = baseUrl + "/minigames/quiz";

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
    public IEnumerator SendDecision(int scenarioId, string choice)
    {
        string url = baseUrl + "/decisions/";

        string json = JsonUtility.ToJson(new DecisionData(scenarioId, choice));

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
    public IEnumerator GetWords(System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url = baseUrl + "/minigames/wordsearch";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Palabras recibidas");
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Error words: " + request.error);
            callback?.Invoke("ERROR");
        }
    }
    public IEnumerator GetCrossword(System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ No hay token");
            callback?.Invoke("NO_TOKEN");
            yield break;
        }

        string url = baseUrl + "/minigames/crossword";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Crossword recibido");
            Debug.Log("📦 JSON: " + request.downloadHandler.text);
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Error crossword: " + request.error);
            Debug.LogError("Respuesta: " + request.downloadHandler.text);
            callback?.Invoke("ERROR");
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

    public DecisionData(int id, string c)
    {
        scenario_id = id;
        choice = c;
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
public class WordData
{
    public string word;
}

[System.Serializable]
public class WordList
{
    public WordData[] items;
}