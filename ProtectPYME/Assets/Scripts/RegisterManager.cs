using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RegisterManager : MonoBehaviour
{
    public TMP_InputField inputNombre;
    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public TMP_InputField inputConfirmPassword;

    public TMP_Text txtMensaje;

    public void Registrar()
    {
        if (inputNombre.text == "")
        {
            txtMensaje.text = "Ingresa un nombre";
            return;
        }

        if (inputEmail.text == "")
        {
            txtMensaje.text = "Ingresa un correo";
            return;
        }

        if (inputPassword.text == "")
        {
            txtMensaje.text = "Ingresa una contraseña";
            return;
        }

        if (inputPassword.text != inputConfirmPassword.text)
        {
            txtMensaje.text = "Las contraseñas no coinciden";
            return;
        }

        StartCoroutine(
            APIManager.Instance.Register(
                inputNombre.text,
                inputEmail.text,
                inputPassword.text,
                txtMensaje
            )
        );
    }

    public void Volver()
    {
        SceneManager.LoadScene("Login");
    }
}