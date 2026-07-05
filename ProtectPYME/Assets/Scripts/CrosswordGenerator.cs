using System;
using System.Collections.Generic;
using UnityEngine;

public class CrosswordGenerator
{
    private const int DefaultBoardSize = 30;
    private const int MaxSearchNodes = 60000;
    private const int MaxConnectedCandidatesPerWord = 36;
    private const int MaxFallbackCandidatesPerWord = 10;

    public int BoardWidth { get; private set; }
    public int BoardHeight { get; private set; }

    private int visitedNodes;
    private List<Placement> bestPlacements;
    private LayoutScore bestScore;

    public CrosswordGenerator()
        : this(DefaultBoardSize, DefaultBoardSize)
    {
    }

    public CrosswordGenerator(int boardSize)
        : this(boardSize, boardSize)
    {
    }

    public CrosswordGenerator(int boardWidth, int boardHeight)
    {
        BoardWidth = Math.Max(1, boardWidth);
        BoardHeight = Math.Max(1, boardHeight);
    }

    public CrosswordModel Generate(List<CrosswordWordData> words)
    {
        List<WordEntry> entries = BuildWordEntries(words);

        if (entries.Count == 0)
        {
            return CreateEmptyModel();
        }

        bestPlacements = null;
        bestScore = null;
        visitedNodes = 0;

        List<WordEntry> searchOrder = BuildSearchOrder(entries);
        SearchFromEverySeed(searchOrder);

        if (bestPlacements == null || bestPlacements.Count == 0)
        {
            Debug.LogWarning("No fue posible generar un crucigrama con las palabras recibidas.");
            return CreateEmptyModel();
        }

        if (bestPlacements.Count < entries.Count)
        {
            Debug.LogWarning(
                "El crucigrama se genero parcialmente. Palabras colocadas: " +
                bestPlacements.Count + "/" + entries.Count
            );
        }

        return BuildModel(bestPlacements);
    }

    private List<WordEntry> BuildWordEntries(List<CrosswordWordData> words)
    {
        List<WordEntry> entries = new List<WordEntry>();

        if (words == null)
        {
            return entries;
        }

        for (int i = 0; i < words.Count; i++)
        {
            CrosswordWordData word = words[i];

            if (word == null)
            {
                continue;
            }

            string answer = NormalizeAnswer(word.answer);

            if (string.IsNullOrEmpty(answer))
            {
                Debug.LogWarning("Se omitio una palabra vacia en el crucigrama.");
                continue;
            }

            if (answer.Length > BoardWidth && answer.Length > BoardHeight)
            {
                Debug.LogWarning("La palabra no cabe en el tablero configurado: " + answer);
                continue;
            }

            word.answer = answer;
            word.startX = 0;
            word.startY = 0;
            word.isHorizontal = true;

            entries.Add(new WordEntry(word, i, answer));
        }

        ComputeSharedLetterPotential(entries);
        return entries;
    }

    private string NormalizeAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return string.Empty;
        }

        char[] buffer = new char[answer.Length];
        int count = 0;

        for (int i = 0; i < answer.Length; i++)
        {
            char c = answer[i];

            if (char.IsLetterOrDigit(c))
            {
                buffer[count] = char.ToUpperInvariant(c);
                count++;
            }
        }

        return new string(buffer, 0, count);
    }

    private void ComputeSharedLetterPotential(List<WordEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            int potential = 0;

            for (int j = 0; j < entries.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                potential += CountSharedLetters(entries[i].Answer, entries[j].Answer);
            }

            entries[i].SharedLetterPotential = potential;
        }
    }

    private int CountSharedLetters(string a, string b)
    {
        int count = 0;

        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                if (a[i] == b[j])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private List<WordEntry> BuildSearchOrder(List<WordEntry> entries)
    {
        List<WordEntry> ordered = new List<WordEntry>(entries);

        ordered.Sort((a, b) =>
        {
            int byPotential = b.SharedLetterPotential.CompareTo(a.SharedLetterPotential);
            if (byPotential != 0) return byPotential;

            int byLength = b.Answer.Length.CompareTo(a.Answer.Length);
            if (byLength != 0) return byLength;

            return a.OriginalIndex.CompareTo(b.OriginalIndex);
        });

        return ordered;
    }

    private void SearchFromEverySeed(List<WordEntry> searchOrder)
    {
        for (int i = 0; i < searchOrder.Count && visitedNodes < MaxSearchNodes; i++)
        {
            WordEntry seed = searchOrder[i];
            List<Candidate> seedCandidates = CreateSeedCandidates(seed);

            for (int c = 0; c < seedCandidates.Count && visitedNodes < MaxSearchNodes; c++)
            {
                BoardState state = new BoardState(BoardWidth, BoardHeight);
                Candidate seedCandidate = seedCandidates[c];

                ApplyPlacement(state, seedCandidate.ToPlacement());

                List<WordEntry> remaining = new List<WordEntry>(searchOrder);
                remaining.Remove(seed);

                Backtrack(state, remaining);
            }
        }
    }

    private List<Candidate> CreateSeedCandidates(WordEntry seed)
    {
        List<Candidate> candidates = new List<Candidate>();

        AddCenteredSeedCandidate(candidates, seed, true);
        AddCenteredSeedCandidate(candidates, seed, false);

        candidates.Sort(CompareCandidates);
        return candidates;
    }

    private void AddCenteredSeedCandidate(List<Candidate> candidates, WordEntry seed, bool horizontal)
    {
        int length = seed.Answer.Length;

        if (horizontal && length > BoardWidth)
        {
            return;
        }

        if (!horizontal && length > BoardHeight)
        {
            return;
        }

        int x = horizontal ? (BoardWidth - length) / 2 : BoardWidth / 2;
        int y = horizontal ? BoardHeight / 2 : (BoardHeight - length) / 2;

        Placement placement = new Placement(seed, x, y, horizontal);
        candidates.Add(new Candidate(placement, 0, null));
    }

    private void Backtrack(BoardState state, List<WordEntry> remaining)
    {
        visitedNodes++;
        SaveIfBest(state);

        if (remaining.Count == 0 || visitedNodes >= MaxSearchNodes)
        {
            return;
        }

        WordOptions next = SelectNextWord(state, remaining);

        if (next == null || next.Candidates.Count == 0)
        {
            return;
        }

        remaining.Remove(next.Word);

        for (int i = 0; i < next.Candidates.Count && visitedNodes < MaxSearchNodes; i++)
        {
            Placement placement = next.Candidates[i].ToPlacement();
            ApplyPlacement(state, placement);
            Backtrack(state, remaining);
            UndoPlacement(state, placement);
        }

        remaining.Add(next.Word);
    }

    private WordOptions SelectNextWord(BoardState state, List<WordEntry> remaining)
    {
        List<WordOptions> options = new List<WordOptions>();
        bool anyConnected = false;

        for (int i = 0; i < remaining.Count; i++)
        {
            List<Candidate> candidates = GetCandidates(state, remaining[i]);

            if (candidates.Count == 0)
            {
                continue;
            }

            WordOptions wordOptions = new WordOptions(remaining[i], candidates);
            options.Add(wordOptions);

            if (wordOptions.HasConnectedCandidate)
            {
                anyConnected = true;
            }
        }

        if (options.Count == 0)
        {
            return null;
        }

        options.Sort((a, b) =>
        {
            if (anyConnected)
            {
                int byConnection = b.HasConnectedCandidate.CompareTo(a.HasConnectedCandidate);
                if (byConnection != 0) return byConnection;
            }

            int byCandidateCount = a.Candidates.Count.CompareTo(b.Candidates.Count);
            if (byCandidateCount != 0) return byCandidateCount;

            int byBestIntersection = b.BestIntersectionCount.CompareTo(a.BestIntersectionCount);
            if (byBestIntersection != 0) return byBestIntersection;

            int byPotential = b.Word.SharedLetterPotential.CompareTo(a.Word.SharedLetterPotential);
            if (byPotential != 0) return byPotential;

            int byLength = b.Word.Answer.Length.CompareTo(a.Word.Answer.Length);
            if (byLength != 0) return byLength;

            return a.Word.OriginalIndex.CompareTo(b.Word.OriginalIndex);
        });

        return options[0];
    }

    private List<Candidate> GetCandidates(BoardState state, WordEntry word)
    {
        List<Candidate> connected = BuildConnectedCandidates(state, word);

        if (connected.Count > 0)
        {
            connected.Sort(CompareCandidates);
            TrimCandidates(connected, MaxConnectedCandidatesPerWord);
            return connected;
        }

        List<Candidate> fallback = BuildFallbackCandidates(state, word);
        fallback.Sort(CompareCandidates);
        TrimCandidates(fallback, MaxFallbackCandidatesPerWord);
        return fallback;
    }

    private List<Candidate> BuildConnectedCandidates(BoardState state, WordEntry word)
    {
        List<Candidate> candidates = new List<Candidate>();
        HashSet<string> seen = new HashSet<string>();

        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                BoardCell cell = state.Grid[x, y];

                if (cell.IsEmpty)
                {
                    continue;
                }

                for (int i = 0; i < word.Answer.Length; i++)
                {
                    if (word.Answer[i] != cell.Letter)
                    {
                        continue;
                    }

                    if (cell.HasVertical && !cell.HasHorizontal)
                    {
                        AddCandidateIfValid(state, word, x - i, y, true, candidates, seen);
                    }

                    if (cell.HasHorizontal && !cell.HasVertical)
                    {
                        AddCandidateIfValid(state, word, x, y - i, false, candidates, seen);
                    }
                }
            }
        }

        return candidates;
    }

    private List<Candidate> BuildFallbackCandidates(BoardState state, WordEntry word)
    {
        List<Candidate> candidates = new List<Candidate>();
        HashSet<string> seen = new HashSet<string>();

        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                AddCandidateIfValid(state, word, x, y, true, candidates, seen);
                AddCandidateIfValid(state, word, x, y, false, candidates, seen);
            }
        }

        candidates.RemoveAll(candidate => candidate.Intersections > 0);
        return candidates;
    }

    private void AddCandidateIfValid(
        BoardState state,
        WordEntry word,
        int x,
        int y,
        bool horizontal,
        List<Candidate> candidates,
        HashSet<string> seen)
    {
        Placement placement = new Placement(word, x, y, horizontal);

        if (!CanPlaceWord(state, placement))
        {
            return;
        }

        string key = x + ":" + y + ":" + horizontal;

        if (!seen.Add(key))
        {
            return;
        }

        int intersections = CountIntersections(state, placement);
        ApplyPlacement(state, placement);
        LayoutScore score = EvaluateState(state);
        UndoPlacement(state, placement);

        candidates.Add(new Candidate(placement, intersections, score));
    }

    private bool CanPlaceWord(BoardState state, Placement placement)
    {
        int length = placement.Word.Answer.Length;

        if (!IsInside(placement.StartX, placement.StartY))
        {
            return false;
        }

        int endX = placement.StartX + (placement.Horizontal ? length - 1 : 0);
        int endY = placement.StartY + (placement.Horizontal ? 0 : length - 1);

        if (!IsInside(endX, endY))
        {
            return false;
        }

        int beforeX = placement.StartX - (placement.Horizontal ? 1 : 0);
        int beforeY = placement.StartY - (placement.Horizontal ? 0 : 1);
        int afterX = placement.StartX + (placement.Horizontal ? length : 0);
        int afterY = placement.StartY + (placement.Horizontal ? 0 : length);

        if (!IsEmpty(state, beforeX, beforeY) || !IsEmpty(state, afterX, afterY))
        {
            return false;
        }

        for (int i = 0; i < length; i++)
        {
            int x = placement.StartX + (placement.Horizontal ? i : 0);
            int y = placement.StartY + (placement.Horizontal ? 0 : i);
            BoardCell cell = state.Grid[x, y];
            char expected = placement.Word.Answer[i];

            if (!cell.IsEmpty)
            {
                if (cell.Letter != expected)
                {
                    return false;
                }

                if (placement.Horizontal && cell.HasHorizontal)
                {
                    return false;
                }

                if (!placement.Horizontal && cell.HasVertical)
                {
                    return false;
                }

                continue;
            }

            if (placement.Horizontal)
            {
                if (!IsEmpty(state, x, y - 1) || !IsEmpty(state, x, y + 1))
                {
                    return false;
                }
            }
            else
            {
                if (!IsEmpty(state, x - 1, y) || !IsEmpty(state, x + 1, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < BoardWidth && y < BoardHeight;
    }

    private bool IsEmpty(BoardState state, int x, int y)
    {
        if (!IsInside(x, y))
        {
            return true;
        }

        return state.Grid[x, y].IsEmpty;
    }

    private void ApplyPlacement(BoardState state, Placement placement)
    {
        state.Placements.Add(placement);

        for (int i = 0; i < placement.Word.Answer.Length; i++)
        {
            int x = placement.StartX + (placement.Horizontal ? i : 0);
            int y = placement.StartY + (placement.Horizontal ? 0 : i);
            BoardCell cell = state.Grid[x, y];

            cell.Letter = placement.Word.Answer[i];

            if (placement.Horizontal)
            {
                cell.HorizontalCount++;
            }
            else
            {
                cell.VerticalCount++;
            }
        }
    }

    private void UndoPlacement(BoardState state, Placement placement)
    {
        state.Placements.RemoveAt(state.Placements.Count - 1);

        for (int i = 0; i < placement.Word.Answer.Length; i++)
        {
            int x = placement.StartX + (placement.Horizontal ? i : 0);
            int y = placement.StartY + (placement.Horizontal ? 0 : i);
            BoardCell cell = state.Grid[x, y];

            if (placement.Horizontal)
            {
                cell.HorizontalCount--;
            }
            else
            {
                cell.VerticalCount--;
            }

            if (cell.IsEmpty)
            {
                cell.Letter = '\0';
            }
        }
    }

    private int CountIntersections(BoardState state, Placement placement)
    {
        int count = 0;

        for (int i = 0; i < placement.Word.Answer.Length; i++)
        {
            int x = placement.StartX + (placement.Horizontal ? i : 0);
            int y = placement.StartY + (placement.Horizontal ? 0 : i);
            BoardCell cell = state.Grid[x, y];

            if (!cell.IsEmpty)
            {
                count++;
            }
        }

        return count;
    }

    private void SaveIfBest(BoardState state)
    {
        LayoutScore score = EvaluateState(state);

        if (!score.IsBetterThan(bestScore))
        {
            return;
        }

        bestScore = score;
        bestPlacements = ClonePlacements(state.Placements);
    }

    private LayoutScore EvaluateState(BoardState state)
    {
        Bounds bounds = CalculateBounds(state);
        int totalIntersections = 0;
        int centerDistance = 0;

        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                BoardCell cell = state.Grid[x, y];

                if (cell.IsEmpty)
                {
                    continue;
                }

                if (cell.HasHorizontal && cell.HasVertical)
                {
                    totalIntersections++;
                }

                centerDistance += Math.Abs(x - BoardWidth / 2) + Math.Abs(y - BoardHeight / 2);
            }
        }

        int isolatedWords = CountIsolatedWords(state);
        int connectedComponents = CountConnectedComponents(state);

        return new LayoutScore(
            state.Placements.Count,
            totalIntersections,
            isolatedWords,
            connectedComponents,
            bounds.Area,
            bounds.Width + bounds.Height,
            centerDistance,
            Math.Abs(bounds.Width - bounds.Height)
        );
    }

    private int CountIsolatedWords(BoardState state)
    {
        int isolated = 0;

        for (int i = 0; i < state.Placements.Count; i++)
        {
            Placement placement = state.Placements[i];
            bool hasIntersection = false;

            for (int c = 0; c < placement.Word.Answer.Length; c++)
            {
                int x = placement.StartX + (placement.Horizontal ? c : 0);
                int y = placement.StartY + (placement.Horizontal ? 0 : c);
                BoardCell cell = state.Grid[x, y];

                if (cell.HasHorizontal && cell.HasVertical)
                {
                    hasIntersection = true;
                    break;
                }
            }

            if (!hasIntersection)
            {
                isolated++;
            }
        }

        return isolated;
    }

    private int CountConnectedComponents(BoardState state)
    {
        int count = state.Placements.Count;

        if (count == 0)
        {
            return 0;
        }

        DisjointSet set = new DisjointSet(count);

        for (int a = 0; a < count; a++)
        {
            for (int b = a + 1; b < count; b++)
            {
                if (PlacementsShareCell(state.Placements[a], state.Placements[b]))
                {
                    set.Union(a, b);
                }
            }
        }

        return set.CountRoots();
    }

    private bool PlacementsShareCell(Placement a, Placement b)
    {
        for (int i = 0; i < a.Word.Answer.Length; i++)
        {
            int ax = a.StartX + (a.Horizontal ? i : 0);
            int ay = a.StartY + (a.Horizontal ? 0 : i);

            for (int j = 0; j < b.Word.Answer.Length; j++)
            {
                int bx = b.StartX + (b.Horizontal ? j : 0);
                int by = b.StartY + (b.Horizontal ? 0 : j);

                if (ax == bx && ay == by)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Bounds CalculateBounds(BoardState state)
    {
        if (state.Placements.Count == 0)
        {
            return new Bounds(0, 0, 0, 0);
        }

        int minX = BoardWidth;
        int minY = BoardHeight;
        int maxX = 0;
        int maxY = 0;

        for (int i = 0; i < state.Placements.Count; i++)
        {
            Placement placement = state.Placements[i];
            int endX = placement.StartX + (placement.Horizontal ? placement.Word.Answer.Length - 1 : 0);
            int endY = placement.StartY + (placement.Horizontal ? 0 : placement.Word.Answer.Length - 1);

            minX = Math.Min(minX, placement.StartX);
            minY = Math.Min(minY, placement.StartY);
            maxX = Math.Max(maxX, endX);
            maxY = Math.Max(maxY, endY);
        }

        return new Bounds(minX, minY, maxX, maxY);
    }

    private List<Placement> ClonePlacements(List<Placement> placements)
    {
        List<Placement> clone = new List<Placement>();

        for (int i = 0; i < placements.Count; i++)
        {
            clone.Add(placements[i].Clone());
        }

        return clone;
    }

    private int CompareCandidates(Candidate a, Candidate b)
    {
        int byIntersections = b.Intersections.CompareTo(a.Intersections);
        if (byIntersections != 0) return byIntersections;

        if (a.Score != null && b.Score != null)
        {
            int byScore = b.Score.CompareTo(a.Score);
            if (byScore != 0) return byScore;
        }

        int byY = a.StartY.CompareTo(b.StartY);
        if (byY != 0) return byY;

        int byX = a.StartX.CompareTo(b.StartX);
        if (byX != 0) return byX;

        return b.Horizontal.CompareTo(a.Horizontal);
    }

    private void TrimCandidates(List<Candidate> candidates, int maxCount)
    {
        if (candidates.Count <= maxCount)
        {
            return;
        }

        candidates.RemoveRange(maxCount, candidates.Count - maxCount);
    }

    private CrosswordModel BuildModel(List<Placement> placements)
    {
        List<Placement> normalized = NormalizePlacements(placements);
        Bounds bounds = CalculateBounds(normalized);
        int width = Math.Max(1, bounds.Width);
        int height = Math.Max(1, bounds.Height);

        CrosswordModel model = new CrosswordModel();
        model.width = width;
        model.height = height;
        model.grid = CreateGrid(width, height);
        model.words = BuildOrderedWordList(normalized);

        for (int i = 0; i < normalized.Count; i++)
        {
            Placement placement = normalized[i];
            placement.Word.Data.startX = placement.StartX;
            placement.Word.Data.startY = placement.StartY;
            placement.Word.Data.isHorizontal = placement.Horizontal;
            PlaceWord(model, placement.Word.Data);
        }

        return model;
    }

    private List<Placement> NormalizePlacements(List<Placement> placements)
    {
        BoardState temporary = new BoardState(BoardWidth, BoardHeight);

        for (int i = 0; i < placements.Count; i++)
        {
            ApplyPlacement(temporary, placements[i]);
        }

        Bounds bounds = CalculateBounds(temporary);
        List<Placement> normalized = new List<Placement>();

        for (int i = 0; i < placements.Count; i++)
        {
            Placement placement = placements[i];
            normalized.Add(
                new Placement(
                    placement.Word,
                    placement.StartX - bounds.MinX,
                    placement.StartY - bounds.MinY,
                    placement.Horizontal
                )
            );
        }

        normalized.Sort((a, b) => a.Word.OriginalIndex.CompareTo(b.Word.OriginalIndex));
        return normalized;
    }

    private Bounds CalculateBounds(List<Placement> placements)
    {
        if (placements.Count == 0)
        {
            return new Bounds(0, 0, 0, 0);
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        for (int i = 0; i < placements.Count; i++)
        {
            Placement placement = placements[i];
            int endX = placement.StartX + (placement.Horizontal ? placement.Word.Answer.Length - 1 : 0);
            int endY = placement.StartY + (placement.Horizontal ? 0 : placement.Word.Answer.Length - 1);

            minX = Math.Min(minX, placement.StartX);
            minY = Math.Min(minY, placement.StartY);
            maxX = Math.Max(maxX, endX);
            maxY = Math.Max(maxY, endY);
        }

        return new Bounds(minX, minY, maxX, maxY);
    }

    private List<CrosswordWordData> BuildOrderedWordList(List<Placement> placements)
    {
        List<CrosswordWordData> result = new List<CrosswordWordData>();

        placements.Sort((a, b) => a.Word.OriginalIndex.CompareTo(b.Word.OriginalIndex));

        for (int i = 0; i < placements.Count; i++)
        {
            result.Add(placements[i].Word.Data);
        }

        return result;
    }

    private CellData[,] CreateGrid(int width, int height)
    {
        CellData[,] grid = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new CellData();
                grid[x, y].isBlocked = true;
                grid[x, y].correctLetter = '\0';
                grid[x, y].currentLetter = "";
            }
        }

        return grid;
    }

    private CrosswordModel CreateEmptyModel()
    {
        CrosswordModel model = new CrosswordModel();
        model.width = 1;
        model.height = 1;
        model.grid = CreateGrid(1, 1);
        model.words = new List<CrosswordWordData>();
        return model;
    }

    private void PlaceWord(CrosswordModel model, CrosswordWordData word)
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

    private sealed class WordEntry
    {
        public readonly CrosswordWordData Data;
        public readonly int OriginalIndex;
        public readonly string Answer;
        public int SharedLetterPotential;

        public WordEntry(CrosswordWordData data, int originalIndex, string answer)
        {
            Data = data;
            OriginalIndex = originalIndex;
            Answer = answer;
        }
    }

    private sealed class Placement
    {
        public readonly WordEntry Word;
        public readonly int StartX;
        public readonly int StartY;
        public readonly bool Horizontal;

        public Placement(WordEntry word, int startX, int startY, bool horizontal)
        {
            Word = word;
            StartX = startX;
            StartY = startY;
            Horizontal = horizontal;
        }

        public Placement Clone()
        {
            return new Placement(Word, StartX, StartY, Horizontal);
        }
    }

    private sealed class Candidate
    {
        public readonly Placement Placement;
        public readonly int Intersections;
        public readonly LayoutScore Score;

        public int StartX { get { return Placement.StartX; } }
        public int StartY { get { return Placement.StartY; } }
        public bool Horizontal { get { return Placement.Horizontal; } }

        public Candidate(Placement placement, int intersections, LayoutScore score)
        {
            Placement = placement;
            Intersections = intersections;
            Score = score;
        }

        public Placement ToPlacement()
        {
            return Placement.Clone();
        }
    }

    private sealed class WordOptions
    {
        public readonly WordEntry Word;
        public readonly List<Candidate> Candidates;
        public readonly bool HasConnectedCandidate;
        public readonly int BestIntersectionCount;

        public WordOptions(WordEntry word, List<Candidate> candidates)
        {
            Word = word;
            Candidates = candidates;
            BestIntersectionCount = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Intersections > 0)
                {
                    HasConnectedCandidate = true;
                    BestIntersectionCount = Math.Max(BestIntersectionCount, candidates[i].Intersections);
                }
            }
        }
    }

    private sealed class BoardState
    {
        public readonly BoardCell[,] Grid;
        public readonly List<Placement> Placements = new List<Placement>();

        public BoardState(int width, int height)
        {
            Grid = new BoardCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = new BoardCell();
                }
            }
        }
    }

    private sealed class BoardCell
    {
        public char Letter;
        public int HorizontalCount;
        public int VerticalCount;

        public bool HasHorizontal { get { return HorizontalCount > 0; } }
        public bool HasVertical { get { return VerticalCount > 0; } }
        public bool IsEmpty { get { return HorizontalCount == 0 && VerticalCount == 0; } }
    }

    private sealed class LayoutScore : IComparable<LayoutScore>
    {
        private readonly int placedWords;
        private readonly int intersections;
        private readonly int isolatedWords;
        private readonly int connectedComponents;
        private readonly int area;
        private readonly int perimeter;
        private readonly int centerDistance;
        private readonly int balance;

        public LayoutScore(
            int placedWords,
            int intersections,
            int isolatedWords,
            int connectedComponents,
            int area,
            int perimeter,
            int centerDistance,
            int balance)
        {
            this.placedWords = placedWords;
            this.intersections = intersections;
            this.isolatedWords = isolatedWords;
            this.connectedComponents = connectedComponents;
            this.area = area;
            this.perimeter = perimeter;
            this.centerDistance = centerDistance;
            this.balance = balance;
        }

        public bool IsBetterThan(LayoutScore other)
        {
            return other == null || CompareTo(other) > 0;
        }

        public int CompareTo(LayoutScore other)
        {
            int byPlaced = placedWords.CompareTo(other.placedWords);
            if (byPlaced != 0) return byPlaced;

            int byIntersections = intersections.CompareTo(other.intersections);
            if (byIntersections != 0) return byIntersections;

            int byIsolated = other.isolatedWords.CompareTo(isolatedWords);
            if (byIsolated != 0) return byIsolated;

            int byComponents = other.connectedComponents.CompareTo(connectedComponents);
            if (byComponents != 0) return byComponents;

            int byArea = other.area.CompareTo(area);
            if (byArea != 0) return byArea;

            int byPerimeter = other.perimeter.CompareTo(perimeter);
            if (byPerimeter != 0) return byPerimeter;

            int byCenter = other.centerDistance.CompareTo(centerDistance);
            if (byCenter != 0) return byCenter;

            return other.balance.CompareTo(balance);
        }
    }

    private sealed class Bounds
    {
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;

        public int Width { get { return MaxX - MinX + 1; } }
        public int Height { get { return MaxY - MinY + 1; } }
        public int Area { get { return Width * Height; } }

        public Bounds(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }

    private sealed class DisjointSet
    {
        private readonly int[] parent;

        public DisjointSet(int count)
        {
            parent = new int[count];

            for (int i = 0; i < count; i++)
            {
                parent[i] = i;
            }
        }

        public void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);

            if (rootA != rootB)
            {
                parent[rootB] = rootA;
            }
        }

        public int CountRoots()
        {
            int roots = 0;

            for (int i = 0; i < parent.Length; i++)
            {
                if (Find(i) == i)
                {
                    roots++;
                }
            }

            return roots;
        }

        private int Find(int value)
        {
            if (parent[value] != value)
            {
                parent[value] = Find(parent[value]);
            }

            return parent[value];
        }
    }
}
