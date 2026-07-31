using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if GOOGLE_SIGN_IN
using Google;
#endif

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
    [Header("Estructura de Carga")]
    public GameObject objetoBarraRaiz;
    public Image imagenRelleno;
    public TMP_Text textoPorcentaje;

    [Header("Visibilidad de Password")]
    public Button botonMostrarOcultar;
    public Sprite iconoOjoAbierto;
    public Sprite iconoOjoCerrado;

    private bool esVisible = false;

    public Button botonLogin;
    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public TMP_Text txtStatus;

    [Header("Google Sign-In")]
    public Button botonGoogleLogin;
    public string googleWebClientId;

    private string apiUrl = "https://protectpyme.onrender.com/login";
    private bool autenticacionEnProceso;

    public void OnLoginClicked()
    {
        if (autenticacionEnProceso)
        {
            return;
        }

        if (txtStatus != null)
        {
            txtStatus.text = "";
        }

        if (inputEmail == null || inputPassword == null)
        {
            if (txtStatus != null)
            {
                txtStatus.text = "Campos de login no configurados.";
            }

            return;
        }

        if (string.IsNullOrEmpty(inputEmail.text.Trim())
            || string.IsNullOrEmpty(inputPassword.text.Trim()))
        {
            if (txtStatus != null)
            {
                txtStatus.text = "Por favor, llene todos los campos.";
            }

            return;
        }

        autenticacionEnProceso = true;
        StartCoroutine(Login());
    }

    public void OnGoogleLoginClicked()
    {
        if (autenticacionEnProceso)
        {
            return;
        }

        if (txtStatus != null)
        {
            txtStatus.text = "";
        }

        autenticacionEnProceso = true;
        SetGoogleButtonInteractable(false);

#if GOOGLE_SIGN_IN
        if (string.IsNullOrEmpty(googleWebClientId)
            || googleWebClientId.Trim().Length == 0)
        {
            ShowGoogleLoginError("Configure el Web Client ID de Google.");
            return;
        }

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = googleWebClientId,
            RequestIdToken = true,
            RequestEmail = true
        };

        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                ShowGoogleLoginError("Inicio con Google cancelado.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Error Google Sign-In: " + task.Exception);
                ShowGoogleLoginError("No se pudo iniciar sesion con Google.");
                return;
            }

            string idToken = task.Result != null ? task.Result.IdToken : "";

            if (string.IsNullOrEmpty(idToken))
            {
                ShowGoogleLoginError("Google no devolvio un id_token valido.");
                return;
            }

            StartCoroutine(SendGoogleTokenToBackend(idToken));
        });
#else
        ShowGoogleLoginError("Google Sign-In no esta instalado o no esta habilitado.");
        Debug.LogWarning(
            "Importe Google Sign-In for Unity y agregue GOOGLE_SIGN_IN en Scripting Define Symbols."
        );
#endif
    }

#if GOOGLE_SIGN_IN
    private IEnumerator SendGoogleTokenToBackend(string idToken)
    {
        if (APIManager.Instance == null)
        {
            ShowGoogleLoginError("APIManager no esta disponible.");
            yield break;
        }

        bool loginCorrecto = false;

        yield return StartCoroutine(APIManager.Instance.LoginWithGoogle(
            idToken,
            result =>
            {
                loginCorrecto = result == "OK";
            }
        ));

        if (loginCorrecto)
        {
            if (txtStatus != null)
            {
                txtStatus.text = "Login con Google exitoso";
            }

            PrepararSesionLimpia();
            yield return StartCoroutine(ContinuarDespuesDeAutenticacion());
        }
        else
        {
            ShowGoogleLoginError("No se pudo validar Google con el servidor.");
        }
    }
#endif

    private void ShowGoogleLoginError(string message)
    {
        if (txtStatus != null)
        {
            txtStatus.text = message;
        }

        autenticacionEnProceso = false;
        SetGoogleButtonInteractable(true);
    }

    private void SetGoogleButtonInteractable(bool interactable)
    {
        if (botonGoogleLogin != null)
        {
            botonGoogleLogin.interactable = interactable;
        }
    }

    private IEnumerator Login()
    {
        if (imagenRelleno != null)
        {
            imagenRelleno.fillAmount = 0f;
        }

        if (textoPorcentaje != null)
        {
            textoPorcentaje.text = "0%";
        }

        if (objetoBarraRaiz != null)
        {
            objetoBarraRaiz.SetActive(true);
        }

        if (botonLogin != null)
        {
            botonLogin.interactable = false;
        }

        LoginRequest data = new LoginRequest
        {
            email = inputEmail.text,
            password = inputPassword.text
        };

        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            float progresoSimulado = 0f;

            while (!operation.isDone)
            {
                if (progresoSimulado < 0.9f)
                {
                    progresoSimulado += Time.deltaTime * 0.2f;
                }

                ActualizarProgresoLogin(progresoSimulado);
                yield return null;
            }

            while (progresoSimulado < 1f)
            {
                progresoSimulado = Mathf.MoveTowards(
                    progresoSimulado,
                    1f,
                    Time.deltaTime * 3f
                );

                ActualizarProgresoLogin(progresoSimulado);
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            if (objetoBarraRaiz != null)
            {
                objetoBarraRaiz.SetActive(false);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                LoginResponse response =
                    JsonUtility.FromJson<LoginResponse>(
                        request.downloadHandler.text
                    );

                if (response == null || string.IsNullOrEmpty(response.access_token))
                {
                    MostrarErrorPostAutenticacion(
                        "Respuesta de login invalida."
                    );
                    yield break;
                }

                if (txtStatus != null)
                {
                    txtStatus.text = "Login exitoso";
                }

                PrepararSesionLimpia();
                PlayerPrefs.SetString("token", response.access_token);
                PlayerPrefs.Save();

                if (APIManager.Instance != null)
                {
                    APIManager.Instance.SetToken(response.access_token);
                }

                yield return StartCoroutine(ContinuarDespuesDeAutenticacion());
            }
            else
            {
                autenticacionEnProceso = false;

                if (botonLogin != null)
                {
                    botonLogin.interactable = true;
                }

                ManejarError(request);
            }
        }
    }

    private void ActualizarProgresoLogin(float progreso)
    {
        if (imagenRelleno != null)
        {
            imagenRelleno.fillAmount = progreso;
        }

        if (textoPorcentaje != null)
        {
            textoPorcentaje.text = (progreso * 100).ToString("F0") + "%";
        }
    }

    private IEnumerator ContinuarDespuesDeAutenticacion()
    {
        if (APIManager.Instance == null)
        {
            MostrarErrorPostAutenticacion("APIManager no esta disponible.");
            yield break;
        }

        if (txtStatus != null)
        {
            txtStatus.text = "Verificando encuesta diagnostica...";
        }

        SurveyStatusResponse surveyStatus = null;
        string error = "";

        yield return StartCoroutine(APIManager.Instance.GetSurveyStatus(
            response => surveyStatus = response,
            requestError => error = requestError
        ));

        if (!string.IsNullOrEmpty(error))
        {
            if (error == "NO_TOKEN" || error.StartsWith("HTTP_401"))
            {
                MostrarErrorPostAutenticacion(
                    "Sesion expirada. Inicia sesion nuevamente."
                );
            }
            else
            {
                MostrarErrorPostAutenticacion(
                    "No se pudo verificar la encuesta: " + error
                );
            }

            yield break;
        }

        if (surveyStatus == null)
        {
            MostrarErrorPostAutenticacion(
                "Respuesta invalida al verificar la encuesta."
            );
            yield break;
        }

        if (surveyStatus.has_submitted)
        {
            AIState.SurveyCompleted = true;
            AIState.SurveyInitialRisk = surveyStatus.initial_risk;
            AIState.SurveyPrimaryWeakness = surveyStatus.primary_weakness;
            AIState.SurveyTotalRiskScore = 0;

            autenticacionEnProceso = false;
            SceneManager.LoadScene("MenuPrincipal");
        }
        else
        {
            AIState.SurveyCompleted = false;
            AIState.SurveyInitialRisk = "";
            AIState.SurveyPrimaryWeakness = "";
            AIState.SurveyTotalRiskScore = 0;

            autenticacionEnProceso = false;
            SceneManager.LoadScene("Encuesta");
        }
    }

    private void MostrarErrorPostAutenticacion(string message)
    {
        if (txtStatus != null)
        {
            txtStatus.text = message;
        }

        autenticacionEnProceso = false;

        if (botonLogin != null)
        {
            botonLogin.interactable = true;
        }

        SetGoogleButtonInteractable(true);

        if (objetoBarraRaiz != null)
        {
            objetoBarraRaiz.SetActive(false);
        }
    }

    private void ManejarError(UnityWebRequest req)
    {
        if (txtStatus == null)
        {
            return;
        }

        if (req.responseCode == 401)
        {
            txtStatus.text = "Correo o contrasena incorrectos";
        }
        else if (req.result == UnityWebRequest.Result.ConnectionError)
        {
            txtStatus.text = "Error de conexion";
        }
        else
        {
            txtStatus.text = "Error en el servidor. Intente mas tarde.";
        }
    }

    public void AlternarVisibilidadPassword()
    {
        esVisible = !esVisible;

        if (esVisible)
        {
            inputPassword.contentType = TMP_InputField.ContentType.Standard;

            if (botonMostrarOcultar != null && iconoOjoCerrado != null)
            {
                botonMostrarOcultar.image.sprite = iconoOjoCerrado;
            }
        }
        else
        {
            inputPassword.contentType = TMP_InputField.ContentType.Password;

            if (botonMostrarOcultar != null && iconoOjoAbierto != null)
            {
                botonMostrarOcultar.image.sprite = iconoOjoAbierto;
            }
        }

        inputPassword.ForceLabelUpdate();
    }

    private void PrepararSesionLimpia()
    {
        string email = inputEmail != null ? inputEmail.text.Trim() : "";
        string usuarioActual = !string.IsNullOrEmpty(email)
            ? email
            : "usuario_google";

        PlayerPrefs.SetString("UsuarioActual", usuarioActual);
        PlayerPrefs.Save();

        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.CargarDatosUsuarioActual();
        }

        Debug.Log("Sesion iniciada para: " + usuarioActual);
    }
}
