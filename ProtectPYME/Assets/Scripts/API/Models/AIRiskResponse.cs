using System;

[Serializable]
public class AIRiskResponse
{
    public int user_id;

    public string risk_level;

    public float probability;

    public string recommended_training;

    public int recommended_scenario;

    public string message;
}