using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    private static string escenaAnterior;
    public GameObject panelPausa;
    public GameObject CanvasJuegoTerminado;
    
    [Header("Panel de Confirmación")]
    public GameObject panelConfirmacionSalir; 

    public AudioSource SonidoBoton;
    public AudioClip Boton;

    // 🔥 Variables para la "Fotografía" inicial
    private float seguridadAntesDeJugar;
    private string claveSeguridad;
    
    private int vidasAntesDeJugar;
    private string claveVidas;
    private bool fotoTomada = false;

    void Awake()
    {
        TomarFotografia();
    }

    public void TomarFotografia()
    {
        // Tomamos la foto SOLAMENTE UNA VEZ
        if (GameManagerGlobal.instancia != null && !fotoTomada)
        {
            // Seguridad
            claveSeguridad = GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente");
            seguridadAntesDeJugar = PlayerPrefs.GetFloat(claveSeguridad, GameManagerGlobal.instancia.nivelSeguridad);
            
            // Vidas (¡Corregido a "Vidas" con base en tu GamificacionController!)
            claveVidas = GameManagerGlobal.instancia.ObtenerClaveUsuario("Vidas"); 
            vidasAntesDeJugar = PlayerPrefs.GetInt(claveVidas, GameManagerGlobal.instancia.vidas);
            
            fotoTomada = true;
            Debug.Log($"📸 FOTO TOMADA AL INICIAR: Seguridad={seguridadAntesDeJugar}%, Vidas={vidasAntesDeJugar}");
        }
    }

    public void CambiarEscena(string nombreEscena)
    {
        escenaAnterior = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    public void PausarJuego()
    {
        // Si el script estaba apagado y apenas despertó, aseguramos que tome la foto (por si acaso)
        TomarFotografia(); 
        if (panelPausa != null) panelPausa.SetActive(true); 
        Time.timeScale = 0f;        
    }

    public void PausarJuegoSimple()
    {
        if (panelPausa != null) panelPausa.SetActive(true); 
        Time.timeScale = 0f;        
    }
    
    public void ContinuarJuego()
    {
        if (panelPausa != null) panelPausa.SetActive(false); 
        Time.timeScale = 1f;         
    }

    public void JugarDeNuevo()
    {
        if (CanvasJuegoTerminado != null) CanvasJuegoTerminado.SetActive(false);
        Time.timeScale = 1f;
        string nombreEscena = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(nombreEscena);
    }

    // ==========================================
    // ALERTA DE SALIDA Y RESTAURACIÓN DE DATOS
    // ==========================================

    public void MostrarAlertaSalir()
    {
        if (panelConfirmacionSalir != null && panelPausa != null)
        {
            panelConfirmacionSalir.SetActive(true);
            panelPausa.SetActive(false);
        }
    }

    public void CancelarSalida()
    {
        if (panelConfirmacionSalir != null && panelPausa != null)
        {
            panelConfirmacionSalir.SetActive(false);
            panelPausa.SetActive(true);
        }
    }

    public void ConfirmarSalidaAbortada(string nombreEscena)
    {
        // Restauramos los datos exactos del inicio
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.nivelSeguridad = seguridadAntesDeJugar;
            PlayerPrefs.SetFloat(claveSeguridad, seguridadAntesDeJugar);
            
            GameManagerGlobal.instancia.vidas = vidasAntesDeJugar;
            PlayerPrefs.SetInt(claveVidas, vidasAntesDeJugar);
            
            PlayerPrefs.Save();
            Debug.Log($"🔄 RESTAURACIÓN EXITOSA: Seguridad regresó a {seguridadAntesDeJugar}%, Vidas regresaron a {vidasAntesDeJugar}");
        }

        Time.timeScale = 1f;
        // Cargamos la escena que escribas en el Inspector
        SceneManager.LoadScene(nombreEscena); 
    }
}