using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarPerfilController : MonoBehaviour
{
    // Definimos las secciones posibles
    public enum SeccionAvatar { Base, Cabello, Accesorio }
    private SeccionAvatar seccionActual = SeccionAvatar.Base;

    //private const string AvatarBaseKey = "avatar_base";
    //private const string AvatarCabelloKey = "avatar_cabello";
    //private const string AvatarAccesorioKey = "avatar_accesorio";

    [Header("Paneles")]
    public GameObject contenedorPerfil;
    public GameObject panelPersonalizarAvatar;

    [Header("Avatar principal (Perfil)")]
    public Image imgAvatarPrincipalBase;
    public Image imgAvatarPrincipalCabello;
    public Image imgAvatarPrincipalAccesorio;

    [Header("Foto Circular de Perfil")]
    public ActualizadorAvatarFoto fotoPerfilCircular;

    [Header("Preview por capas (Editor)")]
    public Image imgPreviewBase;
    public Image imgPreviewCabello;
    public Image imgPreviewAccesorio;

    [Header("Sprites por categoría")]
    public Sprite[] bases;
    public Sprite[] cabellos;
    public Sprite[] accesorios;

    [Header("Texto de Sección Activa")]
    public TextMeshProUGUI txtSeccionActual; // Muestra "Base", "Cabello", etc.
    public TextMeshProUGUI txtEstadoAvatar;

    private int baseIndex;
    private int cabelloIndex;
    private int accesorioIndex;

    void Start()
    {
        CargarSeleccionGuardada();
        MostrarPerfil();
        ActualizarPreview();
        ActualizarAvatarPrincipal();
        CambiarSeccion(0); // Inicia en Base por defecto
    }

    public void AbrirPersonalizacion()
    {
        if (contenedorPerfil != null) contenedorPerfil.SetActive(false);
        if (panelPersonalizarAvatar != null) panelPersonalizarAvatar.SetActive(true);
        
        CambiarSeccion(0); // Reinicia a la primera pestaña al abrir
        ActualizarPreview();
    }

    public void MostrarPerfil()
    {
        if (contenedorPerfil != null) contenedorPerfil.SetActive(true);
        if (panelPersonalizarAvatar != null) panelPersonalizarAvatar.SetActive(false);
    }

    // --- SISTEMA DE PESTAÑAS ---
    // Vincula este método a tus 3 botones de categoría (puedes pasarle un entero en el Inspector)
    // 0 = Base, 1 = Cabello, 2 = Accesorio
    public void CambiarSeccion(int nuevaSeccion)
    {
        seccionActual = (SeccionAvatar)nuevaSeccion;

        if (txtSeccionActual != null)
        {
            txtSeccionActual.text = seccionActual.ToString().ToUpper();
        }
    }

    // --- BOTONES ÚNICOS DE NAVEGACIÓN ---
    public void BotonSiguiente()
    {
        NavegarSeccion(1);
    }

    public void BotonAnterior()
    {
        NavegarSeccion(-1);
    }

    private void NavegarSeccion(int direccion)
    {
        switch (seccionActual)
        {
            case SeccionAvatar.Base:
                CambiarIndice(ref baseIndex, bases, direccion, "base");
                break;
            case SeccionAvatar.Cabello:
                CambiarIndice(ref cabelloIndex, cabellos, direccion, "cabello");
                break;
            case SeccionAvatar.Accesorio:
                CambiarIndice(ref accesorioIndex, accesorios, direccion, "accesorio");
                break;
        }
    }

    // --- GUARDADO Y LÓGICA INTERNA ---
    public void GuardarAvatar()
    {
        NormalizarIndices();

        // 🔄 MODIFICADO: Ahora usamos las claves dinámicas
        PlayerPrefs.SetInt(ObtenerClaveAvatar("base"), baseIndex);
        PlayerPrefs.SetInt(ObtenerClaveAvatar("cabello"), cabelloIndex);
        PlayerPrefs.SetInt(ObtenerClaveAvatar("accesorio"), accesorioIndex);
        PlayerPrefs.Save();

        ActualizarAvatarPrincipal();
        MostrarPerfil();

        // Refresca el objeto circular inmediatamente al guardar
        if (fotoPerfilCircular != null)
        {
            fotoPerfilCircular.ActualizarFoto();
        }

        if (txtEstadoAvatar != null)
            txtEstadoAvatar.text = "¡Avatar guardado!";
    }

    private void CargarSeleccionGuardada()
    {
        // 🔄 MODIFICADO: Ahora leemos de las claves dinámicas
        baseIndex = PlayerPrefs.GetInt(ObtenerClaveAvatar("base"), 0);
        cabelloIndex = PlayerPrefs.GetInt(ObtenerClaveAvatar("cabello"), 0);
        accesorioIndex = PlayerPrefs.GetInt(ObtenerClaveAvatar("accesorio"), 0);
        NormalizarIndices();
    }

    private void NormalizarIndices()
    {
        baseIndex = NormalizarIndice(baseIndex, bases);
        cabelloIndex = NormalizarIndice(cabelloIndex, cabellos);
        accesorioIndex = NormalizarIndice(accesorioIndex, accesorios);
    }

    private int NormalizarIndice(int index, Sprite[] sprites)
    {
        if (!TieneSprites(sprites)) return 0;
        if (index < 0) return 0;
        if (index >= sprites.Length) return index % sprites.Length;
        return index;
    }

    private void CambiarIndice(ref int index, Sprite[] sprites, int direccion, string nombre)
    {
        if (!TieneSprites(sprites)) return;

        index += direccion;

        if (index >= sprites.Length) index = 0;
        else if (index < 0) index = sprites.Length - 1;

        ActualizarPreview();
    }

    private void ActualizarPreview()
    {
        NormalizarIndices();
        ActualizarCapaPreview(imgPreviewBase, bases, baseIndex, "base");
        ActualizarCapaPreview(imgPreviewCabello, cabellos, cabelloIndex, "cabello");
        ActualizarCapaPreview(imgPreviewAccesorio, accesorios, accesorioIndex, "accesorio");
    }

    private void ActualizarAvatarPrincipal()
    {
        ActualizarCapaPreview(imgAvatarPrincipalBase, bases, baseIndex, "base principal");
        ActualizarCapaPreview(imgAvatarPrincipalCabello, cabellos, cabelloIndex, "cabello principal");
        ActualizarCapaPreview(imgAvatarPrincipalAccesorio, accesorios, accesorioIndex, "accesorio principal");
    }

    private void ActualizarCapaPreview(Image image, Sprite[] sprites, int index, string nombre)
    {
        if (image == null) return;

        if (!TieneSprites(sprites))
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = sprites[index];
        image.enabled = image.sprite != null;
    }

    private bool TieneSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
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