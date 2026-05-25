using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    private static string escenaAnterior;
    public GameObject panelPausa;
    public GameObject CanvasJuegoTerminado;
    public AudioSource SonidoBoton;
    public AudioClip Boton;

    public void CambiarEscena(string nombreEscena)
    {
        //Guardamos el nombre de la escena actual antes de irnos
        escenaAnterior = SceneManager.GetActiveScene().name;
        //Cargamos la nueva
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego..."); //Para ver en la consola que el botón funciona
        Application.Quit();
    }

    public void PausarJuego()
    {
        panelPausa.SetActive(true); // Muestra el menú
        Time.timeScale = 0f;        // Congela el tiempo del juego
    }

    public void ContinuarJuego()
    {
        panelPausa.SetActive(false); // Esconde el menú
        Time.timeScale = 1f;         // Reanuda el tiempo normal
    }

    public void JugarDeNuevo()
    {

        //Desactivamos el Canvas
        if (CanvasJuegoTerminado != null)
        {
            CanvasJuegoTerminado.SetActive(false);
        }

        //Correr el tiempo si se pausa el juego cuando pierde
        Time.timeScale = 1f;

        //Reiniciamos la escena
        string nombreEscena = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(nombreEscena);
    }
}


    

    

