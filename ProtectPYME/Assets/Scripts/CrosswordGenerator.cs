using System.Collections.Generic;

public class CrosswordGenerator
{
    public CrosswordModel Generate(List<CrosswordWordData> words)
    {
        CrosswordModel model = new CrosswordModel();

        int size = 12;
        model.width = size;
        model.height = size;
        model.grid = new CellData[size, size];
        model.words = words;

        // Inicializar grid
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                model.grid[x, y] = new CellData();
                model.grid[x, y].isBlocked = true;
                model.grid[x, y].currentLetter = "";
            }
        }

        // Primera palabra al centro
        var first = words[0];
        first.startX = size / 2 - first.answer.Length / 2;
        first.startY = size / 2;
        first.isHorizontal = true;

        PlaceWord(model, first);

        // Las demás palabras (simple)
        for (int i = 1; i < words.Count; i++)
        {
            words[i].startX = 0;
            words[i].startY = i + 2;
            words[i].isHorizontal = true;

            PlaceWord(model, words[i]);
        }

        return model;
    }

    void PlaceWord(CrosswordModel model, CrosswordWordData word)
    {
        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            if (x < 0 || x >= model.width || y < 0 || y >= model.height)
            {
                UnityEngine.Debug.LogError($"❌ Fuera de rango: {x},{y}");
                continue;
            }

            if (model.grid[x, y] == null)
            {
                UnityEngine.Debug.LogError($"❌ Celda NULL en {x},{y}");
                continue;
            }

            model.grid[x, y].isBlocked = false;
            model.grid[x, y].correctLetter = word.answer[i];
        }
    }
}