using UnityEngine;

public class CrosswordValidator
{
    public bool ValidateWord(CrosswordModel model, CrosswordWordData word)
    {
        string formed = "";

        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            // 🔥 VALIDAR RANGO
            if (x < 0 || x >= model.width || y < 0 || y >= model.height)
            {
                Debug.LogWarning($"FUERA DE RANGO -> {word.answer} ({x},{y})");
                return false;
            }

            // 🔥 VALIDAR NULL
            if (model.grid[x, y] == null)
            {
                Debug.LogWarning($"CELDA NULL -> ({x},{y})");
                return false;
            }

            formed += model.grid[x, y].currentLetter;
        }

        return formed.ToUpper() == word.answer.ToUpper();
    }
}