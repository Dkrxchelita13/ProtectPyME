using System;

[Serializable]
public class CrosswordWordData
{
    public string clue;
    public string answer;

    public int startX;
    public int startY;
    public bool isHorizontal;
}