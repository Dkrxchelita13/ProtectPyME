using UnityEngine;
using TMPro;
using System.Collections;

public class LeaderboardController : MonoBehaviour
{
    public Transform contenido;
    public GameObject filaPrefab;



    void Start()
    {
        Debug.Log("LeaderboardController iniciado en: " + gameObject.name);

        StartCoroutine(
            APIManager.Instance.GetLeaderboard(
                ProcesarLeaderboard
            )
        );
    }

    void ProcesarLeaderboard(string json)
    {
        if (json == "ERROR" ||
            json == "NO_TOKEN")
        {
            Debug.LogError("❌ Error leaderboard");
            return;
        }

        LeaderboardUser[] users =
            JsonHelper.FromJson<LeaderboardUser>(
                json
            );

        // limpiar anteriores
        foreach (Transform child in contenido)
        {
            if (child.name != "HeaderRanking")
            {
                Destroy(child.gameObject);
            }
        }

        foreach (LeaderboardUser user in users)
        {
            GameObject fila =
                Instantiate(
                    filaPrefab,
                    contenido
                );

            TMP_Text[] textos =
                fila.GetComponentsInChildren<TMP_Text>();

            textos[0].text =
                "#" + user.rank;

            string nombre = user.name;

            if (nombre.Contains("@"))
            {
                nombre = nombre.Split('@')[0];
            }

            textos[1].text = nombre;

            textos[2].text =
                user.total_points.ToString();
        }

        Debug.Log("✅ Leaderboard cargado");
    }
}