using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instancia;

    public AudioSource source;

    public AudioClip clickBtn;
    public AudioClip pregunta;
    public AudioClip error;
    public AudioClip correcto;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReproducirClick()
    {
        source.PlayOneShot(clickBtn);
    }

    public void ReproducirPregunta()
    {
        source.PlayOneShot(pregunta);
    }

    public void ReproducirError()
    {
        source.PlayOneShot(error);
    }

    public void ReproducirCorrecto()
    {
        source.PlayOneShot(correcto);
    }
}