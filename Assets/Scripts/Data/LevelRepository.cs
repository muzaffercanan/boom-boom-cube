using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Data
{
public sealed class LevelLoadResult
{
    public bool Success { get; }
    public LevelData LevelData { get; }
    public string Source { get; }
    public string Error { get; }

    private LevelLoadResult(bool success, LevelData levelData, string source, string error)
    {
        Success = success;
        LevelData = levelData;
        Source = source;
        Error = error;
    }

    public static LevelLoadResult Loaded(LevelData levelData, string source)
    {
        return new LevelLoadResult(true, levelData, source, null);
    }

    public static LevelLoadResult Failed(string error)
    {
        return new LevelLoadResult(false, null, null, error);
    }
}

public static class LevelRepository
{
    private const string LevelsDirectoryName = "Levels";
    private const string LevelFilePrefix = "level_";
    private const string JsonExtension = ".json";

    public static IEnumerator LoadLevelAsync(int levelNumber, Action<LevelLoadResult> onCompleted, TextAsset fallbackLevelJson = null)
    {
        if (onCompleted == null)
        {
            yield break;
        }

        if (levelNumber <= 0)
        {
            onCompleted(LevelLoadResult.Failed($"Level number must be positive. Requested: {levelNumber}"));
            yield break;
        }

        string levelFileName = $"{LevelFilePrefix}{levelNumber:D2}";

        LevelLoadResult resourcesResult = null;
        yield return TryLoadFromResourcesAsync(levelFileName, result => resourcesResult = result);
        if (resourcesResult.Success)
        {
            onCompleted(resourcesResult);
            yield break;
        }

        LevelLoadResult streamingResult = null;
        yield return TryLoadFromStreamingAssetsAsync(levelFileName, result => streamingResult = result);
        if (streamingResult.Success)
        {
            onCompleted(streamingResult);
            yield break;
        }

        if (fallbackLevelJson != null)
        {
            onCompleted(ParseAndValidate(fallbackLevelJson.text, $"Inspector fallback '{fallbackLevelJson.name}'"));
            yield break;
        }

        onCompleted(LevelLoadResult.Failed(
            $"Level {levelNumber} could not be loaded. Tried Resources/{LevelsDirectoryName}/{levelFileName}, " +
            $"and StreamingAssets/{LevelsDirectoryName}/{levelFileName}{JsonExtension}."));
    }

    public static LevelLoadResult LoadLevel(int levelNumber, TextAsset fallbackLevelJson = null)
    {
        if (levelNumber <= 0)
        {
            return LevelLoadResult.Failed($"Level number must be positive. Requested: {levelNumber}");
        }

        string levelFileName = $"{LevelFilePrefix}{levelNumber:D2}";

        LevelLoadResult result = TryLoadFromResources(levelFileName);
        if (result.Success) return result;

        if (fallbackLevelJson != null)
        {
            return ParseAndValidate(fallbackLevelJson.text, $"Inspector fallback '{fallbackLevelJson.name}'");
        }

        return LevelLoadResult.Failed(
            $"Level {levelNumber} could not be loaded from Resources/{LevelsDirectoryName}/{levelFileName}. " +
            "Use LoadLevelAsync for StreamingAssets fallback.");
    }

    public static LevelLoadResult ParseAndValidate(string json, string source)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return LevelLoadResult.Failed($"Level JSON is empty. Source: {source}");
        }

        LevelData data;
        try
        {
            data = JsonUtility.FromJson<LevelData>(json);
        }
        catch (Exception ex)
        {
            return LevelLoadResult.Failed($"Level JSON parse failed. Source: {source}. Error: {ex.Message}");
        }

        string validationError = Validate(data);
        if (!string.IsNullOrEmpty(validationError))
        {
            return LevelLoadResult.Failed($"Level validation failed. Source: {source}. Error: {validationError}");
        }

        return LevelLoadResult.Loaded(data, source);
    }

    public static string Validate(LevelData data)
    {
        if (data == null)
        {
            return "Parsed level data is null.";
        }

        if (data.level_number <= 0)
        {
            return $"level_number must be positive. Actual: {data.level_number}";
        }

        if (data.grid_width <= 0 || data.grid_height <= 0)
        {
            return $"grid_width and grid_height must be positive. Actual: {data.grid_width}x{data.grid_height}";
        }

        if (data.move_count < 0)
        {
            return $"move_count cannot be negative. Actual: {data.move_count}";
        }

        bool hasCells = data.cells != null && data.cells.Count > 0;
        bool hasGrid = data.grid != null && data.grid.Count > 0;
        if (!hasCells && !hasGrid)
        {
            return "level must define either legacy grid or cells.";
        }

        int expectedCellCount = data.grid_width * data.grid_height;
        if (hasGrid && data.grid.Count != expectedCellCount)
        {
            return $"grid cell count must equal grid_width * grid_height. Expected: {expectedCellCount}, actual: {data.grid.Count}";
        }

        if (hasCells && data.cells.Count != expectedCellCount)
        {
            return $"cells count must equal grid_width * grid_height. Expected: {expectedCellCount}, actual: {data.cells.Count}";
        }

        if (hasGrid)
        {
            for (int i = 0; i < data.grid.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(data.grid[i]))
                {
                    return $"grid contains an empty item id at index {i}.";
                }
            }
        }

        if (hasCells)
        {
            for (int i = 0; i < data.cells.Count; i++)
            {
                LevelCellData cell = data.cells[i];
                if (cell == null)
                {
                    return $"cells contains null cell data at index {i}.";
                }

                if (!BoardCellState.IsKnownCellType(cell.cell_type))
                {
                    return $"cells contains unknown cell_type '{cell.cell_type}' at index {i}.";
                }

                if (cell.item != null && string.IsNullOrWhiteSpace(cell.item))
                {
                    return $"cells contains an empty item id at index {i}. Omit item for empty playable cells.";
                }

                BoardCellState state = BoardCellState.FromLevelCell(cell);
                string resolvedItem = data.GetItemIdAt(i);
                if (!string.IsNullOrWhiteSpace(resolvedItem) && !state.CanHoldItem)
                {
                    return $"cell at index {i} has item '{resolvedItem}' but cell_type '{cell.cell_type}' cannot hold items.";
                }
            }
        }

        return null;
    }

    private static LevelLoadResult TryLoadFromResources(string levelFileName)
    {
        string resourcePath = $"{LevelsDirectoryName}/{levelFileName}";
        TextAsset resource = Resources.Load<TextAsset>(resourcePath);
        if (resource == null)
        {
            return LevelLoadResult.Failed($"No Resources level found at {resourcePath}.");
        }

        return ParseAndValidate(resource.text, $"Resources/{resourcePath}");
    }

    private static IEnumerator TryLoadFromResourcesAsync(string levelFileName, Action<LevelLoadResult> onCompleted)
    {
        string resourcePath = $"{LevelsDirectoryName}/{levelFileName}";
        ResourceRequest request = Resources.LoadAsync<TextAsset>(resourcePath);
        yield return request;

        TextAsset resource = request.asset as TextAsset;
        onCompleted(resource == null
            ? LevelLoadResult.Failed($"No Resources level found at {resourcePath}.")
            : ParseAndValidate(resource.text, $"Resources/{resourcePath}"));
    }

    private static IEnumerator TryLoadFromStreamingAssetsAsync(string levelFileName, Action<LevelLoadResult> onCompleted)
    {
        string path = Path.Combine(Application.streamingAssetsPath, LevelsDirectoryName, levelFileName + JsonExtension);
        string url = GetStreamingAssetsUrl(path);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onCompleted(LevelLoadResult.Failed($"No StreamingAssets level found at {path}. Error: {request.error}"));
                yield break;
            }

            onCompleted(ParseAndValidate(request.downloadHandler.text, $"StreamingAssets: {path}"));
        }
    }

    private static string GetStreamingAssetsUrl(string path)
    {
        if (path.Contains("://"))
        {
            return path;
        }

        return "file://" + path.Replace("\\", "/");
    }
}
}
