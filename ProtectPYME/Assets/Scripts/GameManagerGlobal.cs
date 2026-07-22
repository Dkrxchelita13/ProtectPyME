using UnityEngine;

public class GameManagerGlobal : MonoBehaviour
{
    public static GameManagerGlobal instancia;

    public int vidas = 3; 
    public float nivelSeguridad = 0f;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
            
            // Cargar datos al arrancar la app
            CargarDatosUsuarioActual();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🟢 Método para generar la llave personalizada de PlayerPrefs
    public string ObtenerClaveUsuario(string nombreLlave)
    {
        string usuario = PlayerPrefs.GetString("UsuarioActual", "default_user");
        return $"{usuario}_{nombreLlave}"; // Ej: "usuario@mail.com_Vidas"
    }

    // 🟢 Método para recargar vidas y seguridad del usuario activo
    public void CargarDatosUsuarioActual()
    {
        string claveSeguridad = ObtenerClaveUsuario("SeguridadPersistente");
        string claveVidas = ObtenerClaveUsuario("Vidas");

        nivelSeguridad = PlayerPrefs.GetFloat(claveSeguridad, 0f);
        vidas = PlayerPrefs.GetInt(claveVidas, 3); // Si es usuario nuevo, empieza con 3 vidas

        Debug.Log($"⚙️ Datos cargados para [{PlayerPrefs.GetString("UsuarioActual")}]: Vidas={vidas}, Seguridad={nivelSeguridad}%");
    }

    public void PerderVida()
    {
        vidas--;
        if (vidas < 0) vidas = 0;

        // Guardamos en la llave propia del usuario
        PlayerPrefs.SetInt(ObtenerClaveUsuario("Vidas"), vidas);
        PlayerPrefs.Save();
        
        Debug.Log("Vida perdida. Vidas restantes: " + vidas);
    }

    public void GanarVida()
    {
        if (vidas < 3) 
        {
            vidas++;
            
            // Guardamos en la llave propia del usuario
            PlayerPrefs.SetInt(ObtenerClaveUsuario("Vidas"), vidas);
            PlayerPrefs.Save();
            
            Debug.Log("¡Vida recuperada! Vidas restantes: " + vidas);
        }
    }

    public void SetSeguridadServidor(float nuevoValor)
    {
        nivelSeguridad = Mathf.Clamp(nuevoValor, 0f, 100f);
    }
}