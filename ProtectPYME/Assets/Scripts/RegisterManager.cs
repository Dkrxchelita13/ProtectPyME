using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class RegisterManager : MonoBehaviour
{
    [Header("Estructura de Carga")]
    public GameObject objetoBarraRaiz; 
    public Image imagenRelleno; 
    public TMP_Text textoPorcentaje;
    public Button botonRegistrar; // Añadido para desactivarlo durante la carga

    [Header("Visibilidad de Contraseña")]
    public Button botonMostrarOcultar; // El botón con el icono del ojo
    public Sprite iconoOjoAbierto;     // Sprite del ojo abierto
    public Sprite iconoOjoCerrado;    // Sprite del ojo con una línea (oculto)
    
    private bool esVisible = false;

    [Header("Campos de Formulario")]
    public TMP_InputField inputNombre;
    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public TMP_InputField inputConfirmPassword;

    public TMP_Text txtMensaje;

    [SerializeField] private string nombreEscenaLogin = "Login";

    public void Registrar()
    {
        if (txtMensaje != null) txtMensaje.text = "";

        // Validaciones mejoradas con Trim() para evitar puros espacios en blanco
        if (string.IsNullOrEmpty(inputNombre.text.Trim()))
        {
            txtMensaje.text = "Ingresa un nombre";
            return;
        }

        if (string.IsNullOrEmpty(inputEmail.text.Trim()))
        {
            txtMensaje.text = "Ingresa un correo";
            return;
        }

        if (string.IsNullOrEmpty(inputPassword.text.Trim()))
        {
            txtMensaje.text = "Ingresa una contraseña";
            return;
        }

        if (inputPassword.text != inputConfirmPassword.text)
        {
            txtMensaje.text = "Las contraseñas no coinciden";
            return;
        }

        // Iniciamos la rutina que maneja la barra visual y la petición
        StartCoroutine(ProcesoRegistro());
    }

    IEnumerator ProcesoRegistro()
{
    // 1. Inicializar UI de Carga
    imagenRelleno.fillAmount = 0f;
    if(textoPorcentaje != null) textoPorcentaje.text = "0%";
    
    // OCULTAMOS el texto temporalmente para que si APIManager responde rápido, el usuario no vea el mensaje antes de tiempo.
    if (txtMensaje != null) txtMensaje.gameObject.SetActive(false);

    objetoBarraRaiz.SetActive(true); 
    if(botonRegistrar != null) botonRegistrar.interactable = false;

    // 2. Lanzar la petición HTTP al APIManager
    Coroutine apiCall = StartCoroutine(
        APIManager.Instance.Register(
            inputNombre.text,
            inputEmail.text,
            inputPassword.text,
            txtMensaje
        )
    );

    float progresoSimulado = 0f;

    // 3. Bucle de carga simulada (hasta el 90% máximo mientras procesa)
    while (progresoSimulado < 0.9f)
    {
        if (APIManager.Instance != null && !requestEstaActiva(apiCall)) 
        {
            break; 
        }

        progresoSimulado += Time.deltaTime * 0.2f; 
        imagenRelleno.fillAmount = progresoSimulado;

        if (textoPorcentaje != null)
            textoPorcentaje.text = (progresoSimulado * 100).ToString("F0") + "%";

        yield return null;
    }

    // Aseguramos que la petición del APIManager terminó por completo
    yield return apiCall;

    // 4. Llenado rápido e imperativo al 100% 
    while (progresoSimulado < 1f)
    {
        progresoSimulado = Mathf.MoveTowards(progresoSimulado, 1f, Time.deltaTime * 4f);
        imagenRelleno.fillAmount = progresoSimulado;
        if (textoPorcentaje != null) 
            textoPorcentaje.text = (progresoSimulado * 100).ToString("F0") + "%";
        yield return null;
    }

    yield return new WaitForSeconds(0.3f);

    // 5. Restaurar UI y MOSTRAR el mensaje final
    objetoBarraRaiz.SetActive(false);
    if(botonRegistrar != null) botonRegistrar.interactable = true;

    // Volvemos a activar el texto en pantalla AHORA que la barra llegó al 100%
    if (txtMensaje != null) txtMensaje.gameObject.SetActive(true);

    if(txtMensaje != null && txtMensaje.text.Contains("correctamente"))
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nombreEscenaLogin);
    }

    }

    public void AlternarVisibilidadPassword()
    {
        esVisible = !esVisible;

        if (esVisible)
        {
            // Ambos campos se vuelven texto plano
            inputPassword.contentType = TMP_InputField.ContentType.Standard;
            inputConfirmPassword.contentType = TMP_InputField.ContentType.Standard;
            
            if (botonMostrarOcultar != null && iconoOjoCerrado != null)
                botonMostrarOcultar.image.sprite = iconoOjoCerrado;
        }
        else
        {
            // Ambos campos vuelven a ocultarse
            inputPassword.contentType = TMP_InputField.ContentType.Password;
            inputConfirmPassword.contentType = TMP_InputField.ContentType.Password;
            
            if (botonMostrarOcultar != null && iconoOjoAbierto != null)
                botonMostrarOcultar.image.sprite = iconoOjoAbierto;
        }

        // Forzamos el refresco visual de ambos inputs
        inputPassword.ForceLabelUpdate();
        inputConfirmPassword.ForceLabelUpdate();
    }

    public void Volver()
    {
        SceneManager.LoadScene("Inicio");
    }

    private bool requestEstaActiva(Coroutine coroutine)
{
    return true; 
}
}