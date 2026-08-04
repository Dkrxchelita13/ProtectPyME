using System;

[Serializable]
public class ConceptFeedbackResponse
{
    public string concept_id;
    public string term;
    public float mastery_score;
    public string mastery_level;
    public int session_attempts;
    public int session_correct;
    public int session_incorrect;
    public string status;
    public string message;
    public string recommendation;
}

[Serializable]
public class MinigameFeedbackResponse
{
    public string session_id;
    public string topic;
    public string risk;
    public string minigame;

    public float accuracy;
    public int points_earned;
    public int total_attempts;
    public int correct_attempts;
    public int incorrect_attempts;

    public string performance_level;
    public string title;
    public string message;
    public string next_step;

    public ConceptFeedbackResponse[] strengths;
    public ConceptFeedbackResponse[] reinforcement;

    public string[] recommended_concept_ids;
    public string recommended_topic;
    public string recommended_minigame;

    public string generated_at;
}
