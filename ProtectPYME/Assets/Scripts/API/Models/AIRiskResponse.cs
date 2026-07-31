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

    public string risk_source;

    public int behavioral_decisions;

    public int min_behavioral_decisions;

    public bool sufficient_behavioral_data;
}
