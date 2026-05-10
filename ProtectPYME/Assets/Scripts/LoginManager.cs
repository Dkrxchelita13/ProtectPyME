using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

[System.Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string access_token;
    public string token_type;
}

public class LoginManager : MonoBehaviour
{
    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public TMP_Text txtStatus;

    private string apiUrl = "http://172.20.32.1:8000/login"; // dchttp://192.168.1.72:8000

    public void OnLoginClicked()
    {
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        LoginRequest data = new LoginRequest
        {
            email = inputEmail.text,
            password = inputPassword.text
        };

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            
            Debug.Log("TOKEN: " + response.access_token);
            
            txtStatus.text = "Login exitoso ✅";

            // 🔥 guardar token
            PlayerPrefs.SetString("token", response.access_token);
            PlayerPrefs.Save(); // importante


            // 🔥 pasar token al APIManager
            APIManager.Instance.SetToken(response.access_token);

            // 👉 cambiar escena
            SceneManager.LoadScene("MenuPrincipal"); // 👈 usa tu nombre real
        }
        else
        {
            txtStatus.text = "Error: " + request.error;
        }
    }
}

