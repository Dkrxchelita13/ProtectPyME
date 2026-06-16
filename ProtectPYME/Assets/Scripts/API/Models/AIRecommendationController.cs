using UnityEngine;
using UnityEngine.SceneManagement;

public class AIRecommendationController : MonoBehaviour
{
    private int recommendedScenario;

    public void LoadRecommendation(
        AIRiskResponse response)
    {
        recommendedScenario =
            response.recommended_scenario;
    }

    public void OnPracticeButton()
    {
        switch (recommendedScenario)
        {
            case 1:
                SceneManager.LoadScene("Scenario1");
                break;

            case 2:
                SceneManager.LoadScene("Scenario2");
                break;

            case 3:
                SceneManager.LoadScene("Scenario3");
                break;

            case 4:
                SceneManager.LoadScene("Scenario4");
                break;
        }
    }
}