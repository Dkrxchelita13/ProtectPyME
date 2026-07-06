using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarPerfilController : MonoBehaviour
{
    private const string AvatarBaseKey = "avatar_base";
    private const string AvatarCabelloKey = "avatar_cabello";
    private const string AvatarAccesorioKey = "avatar_accesorio";

    [Header("Paneles")]
    public GameObject contenedorPerfil;
    public GameObject panelPersonalizarAvatar;

    [Header("Avatar principal")]
    public Image imgAvatarPrincipal;

    [Header("Preview por capas")]
    public Image imgPreviewBase;
    public Image imgPreviewCabello;
    public Image imgPreviewAccesorio;

    [Header("Sprites temporales / diseno")]
    public Sprite[] bases;
    public Sprite[] cabellos;
    public Sprite[] accesorios;

    [Header("Texto opcional")]
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
    }

    public void AbrirPersonalizacion()
    {
        if (contenedorPerfil != null)
            contenedorPerfil.SetActive(false);

        if (panelPersonalizarAvatar != null)
            panelPersonalizarAvatar.SetActive(true);

        ActualizarPreview();
    }

    public void MostrarPerfil()
    {
        if (contenedorPerfil != null)
            contenedorPerfil.SetActive(true);

        if (panelPersonalizarAvatar != null)
            panelPersonalizarAvatar.SetActive(false);
    }

    public void SiguienteBase()
    {
        CambiarIndice(ref baseIndex, bases, 1, "base");
    }

    public void AnteriorBase()
    {
        CambiarIndice(ref baseIndex, bases, -1, "base");
    }

    public void SiguienteCabello()
    {
        CambiarIndice(ref cabelloIndex, cabellos, 1, "cabello");
    }

    public void AnteriorCabello()
    {
        CambiarIndice(ref cabelloIndex, cabellos, -1, "cabello");
    }

    public void SiguienteAccesorio()
    {
        CambiarIndice(ref accesorioIndex, accesorios, 1, "accesorio");
    }

    public void AnteriorAccesorio()
    {
        CambiarIndice(ref accesorioIndex, accesorios, -1, "accesorio");
    }

    public void GuardarAvatar()
    {
        NormalizarIndices();

        PlayerPrefs.SetInt(AvatarBaseKey, baseIndex);
        PlayerPrefs.SetInt(AvatarCabelloKey, cabelloIndex);
        PlayerPrefs.SetInt(AvatarAccesorioKey, accesorioIndex);
        PlayerPrefs.Save();

        ActualizarAvatarPrincipal();
        MostrarPerfil();

        if (txtEstadoAvatar != null)
            txtEstadoAvatar.text = "Avatar guardado";

        Debug.Log("Avatar guardado");
    }

    private void CargarSeleccionGuardada()
    {
        baseIndex = PlayerPrefs.GetInt(AvatarBaseKey, 0);
        cabelloIndex = PlayerPrefs.GetInt(AvatarCabelloKey, 0);
        accesorioIndex = PlayerPrefs.GetInt(AvatarAccesorioKey, 0);

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
        if (!TieneSprites(sprites))
            return 0;

        if (index < 0)
            return 0;

        if (index >= sprites.Length)
            return index % sprites.Length;

        return index;
    }

    private void CambiarIndice(ref int index, Sprite[] sprites, int direccion, string nombre)
    {
        if (!TieneSprites(sprites))
        {
            Debug.Log("No hay sprites de " + nombre + " asignados");
            return;
        }

        index += direccion;

        if (index >= sprites.Length)
            index = 0;
        else if (index < 0)
            index = sprites.Length - 1;

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
        // El avatar principal conserva una representacion simple hasta que diseno agregue capas finales.
        if (imgAvatarPrincipal != null && bases != null && bases.Length > 0)
        {
            imgAvatarPrincipal.sprite = bases[baseIndex];
        }
        else
        {
            Debug.Log("No hay sprites de base asignados para el avatar principal");
        }
    }

    private void ActualizarCapaPreview(Image image, Sprite[] sprites, int index, string nombre)
    {
        if (image == null)
            return;

        if (!TieneSprites(sprites))
        {
            image.sprite = null;
            image.enabled = false;
            Debug.Log("No hay sprites de " + nombre + " asignados");
            return;
        }

        image.sprite = sprites[index];
        image.enabled = image.sprite != null;
    }

    private bool TieneSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
    }
}
