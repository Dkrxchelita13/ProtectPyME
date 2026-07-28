using UnityEngine;
using UnityEngine.UI;

public class ActualizadorAvatarFoto : MonoBehaviour
{
    // Las mismas llaves de guardado que usas en el creador
    //private const string AvatarBaseKey = "avatar_base";
    //private const string AvatarCabelloKey = "avatar_cabello";
    //private const string AvatarAccesorioKey = "avatar_accesorio";

    [Header("Capas de la foto de perfil (Hijos del Mask)")]
    public Image imgBase;
    public Image imgCabello;
    public Image imgAccesorio;

    [Header("Imagen por Defecto (Antes de Personalizar)")]
    public Sprite spritePorDefecto; // <--- Tu ícono de usuario / silueta genérica

    [Header("Banco de Sprites (Los mismos del editor)")]
    public Sprite[] bases;
    public Sprite[] cabellos;
    public Sprite[] accesorios;

    private void OnEnable()
    {
        // Se ejecuta automáticamente cada vez que el menú o foto de perfil aparece
        ActualizarFoto();
    }

    public void ActualizarFoto()
    {
        // Generamos las llaves dinámicas del usuario actual
        string claveBase = ObtenerClaveAvatar("base");
        string claveCabello = ObtenerClaveAvatar("cabello");
        string claveAccesorio = ObtenerClaveAvatar("accesorio");

        // 1. EVALUAR SI EL JUGADOR YA TIENE UN AVATAR GUARDADO
        if (!PlayerPrefs.HasKey(claveBase))
        {
            // --- CASO A: EL JUGADOR ES NUEVO / NUNCA HA GUARDADO UN AVATAR ---
            
            // A. En la capa base ponemos el ícono genérico
            if (imgBase != null)
            {
                imgBase.sprite = spritePorDefecto;
                imgBase.enabled = spritePorDefecto != null;
            }

            // B. Desactivamos las capas superiores para que no estorben ni se vean vacías
            if (imgCabello != null) imgCabello.enabled = false;
            if (imgAccesorio != null) imgAccesorio.enabled = false;
        }
        else
        {
            // --- CASO B: EL JUGADOR YA PERSONALIZÓ SU AVATAR ---
            
            // A. Cargar los números guardados en PlayerPrefs con las llaves dinámicas
            int baseIndex = PlayerPrefs.GetInt(claveBase, 0);
            int cabelloIndex = PlayerPrefs.GetInt(claveCabello, 0);
            int accesorioIndex = PlayerPrefs.GetInt(claveAccesorio, 0);

            // B. Asignar los sprites seleccionados a cada capa
            AsignarSprite(imgBase, bases, baseIndex);
            AsignarSprite(imgCabello, cabellos, cabelloIndex);
            AsignarSprite(imgAccesorio, accesorios, accesorioIndex);
        }
    }

    private void AsignarSprite(Image image, Sprite[] sprites, int index)
    {
        if (image == null) return;

        if (sprites != null && sprites.Length > 0 && index >= 0 && index < sprites.Length)
        {
            image.sprite = sprites[index];
            image.enabled = image.sprite != null;
        }
        else
        {
            image.sprite = null;
            image.enabled = false;
        }
    }

    private string ObtenerClaveAvatar(string parte)
    {
        if (GameManagerGlobal.instancia != null)
        {
            return GameManagerGlobal.instancia.ObtenerClaveUsuario("avatar_" + parte);
        }
        
        // Respaldo por si se prueba la escena sin pasar por el Login
        string usuario = PlayerPrefs.GetString("UsuarioActual", "default_user");
        return $"{usuario}_avatar_{parte}";
    }
}