using System;

[Serializable]
public class MinigameAttemptRequest
{
    public string session_id;
    public string item_id;
    public bool correct;
    public int response_time_ms;
    public int attempt_number;
    public int points_delta;
}

[Serializable]
public class MinigameAttemptResponse
{
    public int id;
    public string session_id;
    public string item_id;
    public string[] concept_ids;
    public string difficulty;
    public bool correct;
    public int response_time_ms;
    public int attempt_number;
    public int points_delta;
    public string created_at;
}
