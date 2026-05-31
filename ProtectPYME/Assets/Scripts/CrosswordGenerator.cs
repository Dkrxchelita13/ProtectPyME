using System.Collections.Generic;
using UnityEngine;

public class CrosswordGenerator
{
    public CrosswordModel Generate(List<CrosswordWordData> words)
    {
        CrosswordModel model = new CrosswordModel();

        int size = 11;

        model.width = size;
        model.height = size;
        model.grid = new CellData[size, size];
        model.words = words;

        // =====================
        // CREAR GRID
        // =====================

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                model.grid[x, y] = new CellData();
                model.grid[x, y].isBlocked = true;
                model.grid[x, y].currentLetter = "";
            }
        }

        // =====================
        // PRIMERA PALABRA
        // =====================

        CrosswordWordData first = words[0];

        first.isHorizontal = true;
        first.startX = 0;
        first.startY = 0;

        PlaceWord(model, first);

        // =====================
        // RESTO DE PALABRAS
        // =====================

        for (int w = 1; w < words.Count; w++)
        {
            CrosswordWordData current = words[w];

            bool placed = false;

            // buscar cruces con TODAS las palabras anteriores
            for (int prev = 0; prev < w; prev++)
            {
                CrosswordWordData other = words[prev];

                for (int i = 0; i < current.answer.Length; i++)
                {
                    char currentLetter = current.answer[i];

                    for (int j = 0; j < other.answer.Length; j++)
                    {
                        char otherLetter = other.answer[j];

                        if (currentLetter == otherLetter)
                        {
                            int crossX = other.startX + (other.isHorizontal ? j : 0);
                            int crossY = other.startY + (other.isHorizontal ? 0 : j);

                            current.isHorizontal = !other.isHorizontal;

                            current.startX =
                                crossX - (current.isHorizontal ? i : 0);

                            current.startY =
                                crossY - (current.isHorizontal ? 0 : i);

                            if (CanPlaceWord(model, current))
                            {
                                PlaceWord(model, current);

                                placed = true;
                                break;
                            }
                        }
                    }

                    if (placed)
                        break;
                }

                if (placed)
                    break;
            }

            // fallback si no encontró cruce
            if (!placed)
            {
                current.isHorizontal = true;
                current.startX = 1;
                current.startY = 1 + w;

                if (CanPlaceWord(model, current))
                {
                    PlaceWord(model, current);
                }
            }
        }

        return model;
    }

    bool CanPlaceWord(CrosswordModel model, CrosswordWordData word)
    {
        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            // fuera del grid
            if (x < 0 || x >= model.width || y < 0 || y >= model.height)
                return false;

            // ya existe letra diferente
            if (!model.grid[x, y].isBlocked &&
                model.grid[x, y].correctLetter != word.answer[i])
            {
                return false;
            }
        }

        return true;
    }

    void PlaceWord(CrosswordModel model, CrosswordWordData word)
    {
        for (int i = 0; i < word.answer.Length; i++)
        {
            int x = word.startX + (word.isHorizontal ? i : 0);
            int y = word.startY + (word.isHorizontal ? 0 : i);

            model.grid[x, y].isBlocked = false;
            model.grid[x, y].correctLetter = word.answer[i];
            model.grid[x, y].isHorizontal = word.isHorizontal;
        }
    }
}