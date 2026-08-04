using System;

[Serializable]
public class MinigameSessionRequest
{
    public string topic;
    public string risk;
    public string minigame;

    public MinigameSessionRequest(string topic, string risk, string minigame)
    {
        this.topic = topic;
        this.risk = risk;
        this.minigame = minigame;
    }
}

[Serializable]
public class MinigameSessionItem
{
    public string item_id;
    public string[] concept_ids;
    public string difficulty;
    public string question;
    public string[] options;
    public string answer_text;
    public int correct_option;
    public string clue;
}

[Serializable]
public class MinigameSessionResponse
{
    public string session_id;
    public string topic;
    public string risk;
    public string minigame;
    public MinigameLessonResponse lesson;
    public MinigameSessionItem[] items;
}
