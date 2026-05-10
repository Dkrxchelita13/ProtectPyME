using UnityEngine;

public class GameManagerGlobal : MonoBehaviour
{
    public static GameManagerGlobal instancia;

    public int vidas = 3;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instancia != this)
        {
            Destroy(gameObject);
        }
    }

    public void PerderVida()
    {
        vidas--;

        if (vidas <= 0)
        {
            Debug.Log("GAME OVER");
        }
    }
}