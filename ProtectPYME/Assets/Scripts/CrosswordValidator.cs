using System.Linq;

public class CrosswordValidator
{
    public bool ValidateWord(CrosswordModel model, CrosswordWordData word)
    {
        string formed = "";

        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            formed += model.grid[x, y].currentLetter;
        }

        return formed.ToUpper() == word.answer.ToUpper();
    }
}