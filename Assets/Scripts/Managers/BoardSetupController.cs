using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Gameplay
{
public class BoardSetupController : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private SpriteRenderer _gridBackgroundRenderer;
    [SerializeField] private float _backgroundPaddingX = 0.05f;
    [SerializeField] private float _backgroundPaddingY = 0.05f;

    [Header("Camera")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _cameraOffsetY = 2.0f;

    public void SetupForLevel(LevelData levelData, Transform boardParent, float cellSize)
    {
        UpdateGridBackground(levelData, boardParent, cellSize);
        UpdateCamera(levelData, boardParent, cellSize);
    }

    public void HideBackground()
    {
        if (_gridBackgroundRenderer != null)
            _gridBackgroundRenderer.gameObject.SetActive(false);
    }

    private void UpdateGridBackground(LevelData levelData, Transform boardParent, float cellSize)
    {
        if (_gridBackgroundRenderer == null) return;

        if (_gridBackgroundRenderer.transform.parent != null)
            _gridBackgroundRenderer.transform.SetParent(null);

        _gridBackgroundRenderer.gameObject.SetActive(true);
        _gridBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;

        float gridWidth = levelData.grid_width * cellSize;
        float gridHeight = levelData.grid_height * cellSize;

        _gridBackgroundRenderer.size = new Vector2(
            gridWidth + _backgroundPaddingX * 2,
            gridHeight + _backgroundPaddingY * 2
        );

        float centerX = (gridWidth - cellSize) / 2f;
        float centerY = (gridHeight - cellSize) / 2f;
        Vector3 worldCenter = boardParent.TransformPoint(new Vector3(centerX, centerY, 0f));
        worldCenter.z = 0.5f;

        _gridBackgroundRenderer.transform.position = worldCenter;
        _gridBackgroundRenderer.transform.localScale = Vector3.one;
    }

    private void UpdateCamera(LevelData levelData, Transform boardParent, float cellSize)
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        float screenRatio = Screen.height > 0
            ? (float)Screen.width / Screen.height
            : _camera.aspect;
        if (screenRatio <= 0f) screenRatio = 1f;

        float boardWidth = levelData.grid_width * cellSize;
        float boardHeight = levelData.grid_height * cellSize;

        // Board item centers are at x*cellSize, y*cellSize; visual extents add ±cellSize/2
        float localCenterX = (boardWidth - cellSize) / 2f;
        float localCenterY = (boardHeight - cellSize) / 2f;
        Vector3 worldCenter = boardParent != null
            ? boardParent.TransformPoint(new Vector3(localCenterX, localCenterY, 0f))
            : new Vector3(localCenterX, localCenterY, 0f);

        _camera.transform.position = new Vector3(worldCenter.x, worldCenter.y + _cameraOffsetY, -10f);

        // Background frame border (_backgroundPaddingX each side) fills exactly to screen edge
        float visibleWidth = boardWidth + _backgroundPaddingX * 2f;
        _camera.orthographicSize = visibleWidth / (2f * screenRatio);
    }
}
}
