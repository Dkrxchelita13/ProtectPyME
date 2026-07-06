using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerfilUsuarioController : MonoBehaviour
{
    [Header("UI Perfil")]
    public Image imgAvatar;
    public TextMeshProUGUI txtNombreUsuario;
    public TextMeshProUGUI txtTituloUsuario;
    public TextMeshProUGUI txtCorreoUsuario;

    void Start()
    {
        CargarDatosPerfil();
    }

    public void CargarDatosPerfil()
    {
        string nombre = PlayerPrefs.GetString("user_name", "Usuario ProtectPyME");
        string correo = PlayerPrefs.GetString("user_email", "usuario@protectpyme.com");

        if (txtNombreUsuario != null)
            txtNombreUsuario.text = nombre;

        if (txtCorreoUsuario != null)
            txtCorreoUsuario.text = correo;

        if (txtTituloUsuario != null)
            txtTituloUsuario.text = "Defensor PYME";

        if (imgAvatar != null && imgAvatar.sprite == null)
        {
            Debug.Log("Perfil: avatar pendiente de asignar por diseño.");
        }
    }
}