using System;

[Serializable]
public class MinigameSessionSummaryResponse
{
    public string session_id;
    public string status;
    public string topic;
    public string risk;
    public string minigame;
    public int total_items;
    public int attempted_items;
    public int total_attempts;
    public int correct_attempts;
    public int incorrect_attempts;
    public int points_earned;
    public float accuracy;
    public int total_response_time_ms;
    public string started_at;
    public string completed_at;
}
