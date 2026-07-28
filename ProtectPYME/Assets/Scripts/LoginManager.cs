using UnityEngine;

using TMPro;

using UnityEngine.Networking;

using UnityEngine.SceneManagement;

using System.Collections;

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



    [Header("Visibilidad de Contraseña")]

    public Button botonMostrarOcultar; // El botón con el icono del ojo

    public Sprite iconoOjoAbierto;     // Sprite del ojo abierto

    public Sprite iconoOjoCerrado;    // Sprite del ojo con una línea (oculto)

   

    private bool esVisible = false;



    public Button botonLogin;

    public TMP_InputField inputEmail;

    public TMP_InputField inputPassword;

    public TMP_Text txtStatus;



    [Header("Google Sign-In")]

    public Button botonGoogleLogin;

    public string googleWebClientId;



    private string apiUrl = "https://protectpyme.onrender.com/login";



    public void OnLoginClicked()

    {

        if (txtStatus != null) txtStatus.text = "";



        // 2. Validamos si el email o la contraseña están vacíos (usando Trim() para ignorar espacios en blanco)

        if (string.IsNullOrEmpty(inputEmail.text.Trim()) || string.IsNullOrEmpty(inputPassword.text.Trim()))

        {

            if (txtStatus != null)

            {

                txtStatus.text = "Por favor, llene todos los campos.";

            }

            return; // Detiene la ejecución aquí y NO inicia la corrutina de login

        }



        // 3. Si ambos campos tienen texto, procedemos con el login normal

        StartCoroutine(Login());

    }



    public void OnGoogleLoginClicked()

    {

        if (txtStatus != null) txtStatus.text = "";



        SetGoogleButtonInteractable(false);



#if GOOGLE_SIGN_IN

        if (googleWebClientId == null || googleWebClientId.Trim().Length == 0)

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

                ShowGoogleLoginError("No se pudo iniciar sesión con Google.");

                return;

            }



            string idToken = task.Result != null ? task.Result.IdToken : "";



            if (string.IsNullOrEmpty(idToken))

            {

                ShowGoogleLoginError("Google no devolvió un id_token válido.");

                return;

            }



            StartCoroutine(SendGoogleTokenToBackend(idToken));

        });

#else

        ShowGoogleLoginError("Google Sign-In no está instalado o no está habilitado.");

        Debug.LogWarning("Importe Google Sign-In for Unity y agregue GOOGLE_SIGN_IN en Scripting Define Symbols.");

#endif

    }



#if GOOGLE_SIGN_IN

    IEnumerator SendGoogleTokenToBackend(string idToken)

    {

        if (APIManager.Instance == null)

        {

            ShowGoogleLoginError("APIManager no está disponible.");

            yield break;

        }



        yield return StartCoroutine(APIManager.Instance.LoginWithGoogle(idToken, result =>

        {

            if (result == "OK")

            {

                if (txtStatus != null) txtStatus.text = "Login con Google exitoso";

                PrepararSesionLimpia();

                SceneManager.LoadScene("Encuesta");

            }

            else

            {

                ShowGoogleLoginError("No se pudo validar Google con el servidor.");

            }

        }));

    }

#endif



    void ShowGoogleLoginError(string message)

    {

        if (txtStatus != null) txtStatus.text = message;

        SetGoogleButtonInteractable(true);

    }



    void SetGoogleButtonInteractable(bool interactable)

    {

        if (botonGoogleLogin != null)

        {

            botonGoogleLogin.interactable = interactable;

        }

    }



    IEnumerator Login()

    {



        imagenRelleno.fillAmount = 0f;

        if(textoPorcentaje != null) textoPorcentaje.text = "0%";

       

        objetoBarraRaiz.SetActive(true);

        botonLogin.interactable = false;



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

       



        UnityWebRequestAsyncOperation operation = request.SendWebRequest();



        float progresoSimulado = 0f;



        // Bucle mientras la operación no termine

        while (!operation.isDone)

        {

            // Simulamos una subida constante pero progresiva.

            // Se detendrá cerca del 90% para esperar la respuesta real del servidor si tarda mucho.

            if (progresoSimulado < 0.9f)

            {

                progresoSimulado += Time.deltaTime * 0.2f; // Ajusta el 0.2f para cambiar la velocidad de subida inicial

            }



            imagenRelleno.fillAmount = progresoSimulado;



            if (textoPorcentaje != null)

                textoPorcentaje.text = (progresoSimulado * 100).ToString("F0") + "%";



            yield return null;

        }



        // Cuando sale del bucle es porque el servidor ya respondió.

        // Llenamos rápidamente la barra del porcentaje actual hasta el 100% de manera fluida.

        while (progresoSimulado < 1f)

        {

            progresoSimulado = Mathf.MoveTowards(progresoSimulado, 1f, Time.deltaTime * 3f);

            imagenRelleno.fillAmount = progresoSimulado;

            if (textoPorcentaje != null)

                textoPorcentaje.text = (progresoSimulado * 100).ToString("F0") + "%";

            yield return null;

        }



        yield return new WaitForSeconds(0.4f);



        // 3. Respuesta recibida: Ocultamos TODO el grupo de carga

        objetoBarraRaiz.SetActive(false);

        botonLogin.interactable = true;



        if (request.result == UnityWebRequest.Result.Success)

        {

            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            txtStatus.text = "Login exitoso";

            PrepararSesionLimpia();

            PlayerPrefs.SetString("token", response.access_token);

            PlayerPrefs.Save();

            APIManager.Instance.SetToken(response.access_token);

            SceneManager.LoadScene("Encuesta");

        }

        else

        {

            ManejarError(request);

        }

    }



    void ManejarError(UnityWebRequest req)

    {

        if (req.responseCode == 401) txtStatus.text = "Correo o contraseña incorrectos";

        else if (req.result == UnityWebRequest.Result.ConnectionError) txtStatus.text = "Error de conexión";

        else txtStatus.text = "Error en el servidor. Intente más tarde.";

    }



    public void AlternarVisibilidadPassword()

    {

        esVisible = !esVisible; // Cambiamos el estado



        if (esVisible)

        {

            // Cambiamos a texto normal para que se vea la contraseña

            inputPassword.contentType = TMP_InputField.ContentType.Standard;

           

            // Cambiamos el icono al del ojo cerrado (para indicar que al dar clic se ocultará)

            if (botonMostrarOcultar != null && iconoOjoCerrado != null)

                botonMostrarOcultar.image.sprite = iconoOjoCerrado;

        }

        else

        {

            // Volvemos a ponerlo en modo Password

            inputPassword.contentType = TMP_InputField.ContentType.Password;

           

            // Cambiamos el icono al del ojo abierto

            if (botonMostrarOcultar != null && iconoOjoAbierto != null)

                botonMostrarOcultar.image.sprite = iconoOjoAbierto;

        }



        // ¡IMPORTANTE! Forzamos a TextMeshPro a refrescar el texto en pantalla

        inputPassword.ForceLabelUpdate();

    }



    private void PrepararSesionLimpia()

    {

        // 1. Identificamos al usuario que está iniciando sesión

        string usuarioActual = inputEmail != null ? inputEmail.text.Trim() : "usuario_google";

        PlayerPrefs.SetString("UsuarioActual", usuarioActual);

        PlayerPrefs.Save();



        // 2. Si el GameManagerGlobal ya existe, le decimos que recargue los datos para este usuario

        if (GameManagerGlobal.instancia != null)

        {

            GameManagerGlobal.instancia.CargarDatosUsuarioActual();

        }



        Debug.Log($"🧼 Sesión iniciada para: {usuarioActual}");

    }

}

