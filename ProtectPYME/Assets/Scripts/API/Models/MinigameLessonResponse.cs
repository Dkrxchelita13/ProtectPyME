using System;

[Serializable]
public class LessonConcept
{
    public string term;
    public string definition;
    public string why_it_matters;
    public string example;
}

[Serializable]
public class LessonPracticalExample
{
    public string title;
    public string[] steps;
}

[Serializable]
public class LessonCommonMistake
{
    public string title;
    public string explanation;
}

[Serializable]
public class LessonQuickCheck
{
    public string question;
    public string[] options;
    public int correct_option;
    public string explanation;
}

[Serializable]
public class MinigameLessonResponse
{
    public string topic;
    public string risk;
    public string minigame;
    public string title;
    public string vulnerability;
    public string learning_objective;
    public string explanation;
    public string[] tips;
    public string recommended_action;
    public LessonConcept[] key_concepts;
    public LessonPracticalExample practical_example;
    public LessonCommonMistake common_mistake;
    public LessonQuickCheck quick_check;
    public string visual_key;
}
