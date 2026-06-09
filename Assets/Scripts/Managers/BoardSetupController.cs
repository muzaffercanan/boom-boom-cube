using UnityEngine;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Data;

namespace DreamGames.Gameplay
{
public class BoardSetupController : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private SpriteRenderer _gridBackgroundRenderer;
    [SerializeField] private float _backgroundPaddingX = 0.05f;
    [SerializeField] private float _backgroundPaddingY = 0.05f;
    [SerializeField] private BoardVisualConfig _visualConfig;

    [Header("Camera")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _cameraOffsetY = 2.0f;

    public Camera Camera => _camera != null ? _camera : Camera.main;

    public void SetVisualConfig(BoardVisualConfig visualConfig)
    {
        if (visualConfig != null)
        {
            _visualConfig = visualConfig;
        }
    }

    public void SetupForLevel(LevelData levelData, Transform boardParent, float cellSize)
    {
        SetupForLevel(levelData, boardParent, cellSize, _visualConfig, null);
    }

    public void SetupForLevel(
        LevelData levelData,
        Transform boardParent,
        float cellSize,
        BoardVisualConfig visualConfig,
        ItemFactory itemFactory)
    {
        if (levelData == null)
        {
            return;
        }

        if (visualConfig != null)
        {
            _visualConfig = visualConfig;
        }

        BoardGeometry geometry = new BoardGeometry(boardParent, ResolveCellSize(cellSize));
        Vector2 backgroundPadding = ResolveBackgroundPadding();
        Vector2 visualHalfExtents = ResolveVisualHalfExtents(itemFactory, geometry.CellSize);
        Rect visualBounds = geometry.GetVisualBoardBounds(
            levelData.grid_width,
            levelData.grid_height,
            visualHalfExtents,
            backgroundPadding);

        UpdateGridBackground(boardParent, visualBounds);
        UpdateCamera(boardParent, visualBounds, ResolveCameraPadding());
    }

    public void HideBackground()
    {
        if (_gridBackgroundRenderer != null)
        {
            _gridBackgroundRenderer.gameObject.SetActive(false);
        }
    }

    private void UpdateGridBackground(Transform boardParent, Rect visualBounds)
    {
        if (_gridBackgroundRenderer == null) return;

        if (_gridBackgroundRenderer.transform.parent != null)
        {
            _gridBackgroundRenderer.transform.SetParent(null);
        }

        _gridBackgroundRenderer.gameObject.SetActive(true);
        _gridBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;
        _gridBackgroundRenderer.size = visualBounds.size;

        Vector2 localCenter = visualBounds.center;
        Vector3 worldCenter = boardParent != null
            ? boardParent.TransformPoint(new Vector3(localCenter.x, localCenter.y, 0f))
            : new Vector3(localCenter.x, localCenter.y, 0f);
        worldCenter.z = 0.5f;

        _gridBackgroundRenderer.transform.position = worldCenter;
        _gridBackgroundRenderer.transform.localScale = Vector3.one;
    }

    private void UpdateCamera(Transform boardParent, Rect visualBounds, Vector2 cameraPadding)
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        float screenRatio = Screen.height > 0
            ? (float)Screen.width / Screen.height
            : _camera.aspect;
        if (screenRatio <= 0f) screenRatio = 1f;

        Vector2 localCenter = visualBounds.center;
        Vector3 worldCenter = boardParent != null
            ? boardParent.TransformPoint(new Vector3(localCenter.x, localCenter.y, 0f))
            : new Vector3(localCenter.x, localCenter.y, 0f);

        _camera.transform.position = new Vector3(worldCenter.x, worldCenter.y + _cameraOffsetY, -10f);
        _camera.orthographicSize = CalculateOrthographicSize(
            visualBounds,
            screenRatio,
            _cameraOffsetY,
            cameraPadding);
    }

    public static float CalculateOrthographicSize(
        Rect visualBounds,
        float aspect,
        float cameraOffsetY,
        Vector2 cameraPadding)
    {
        aspect = Mathf.Max(0.01f, aspect);
        cameraPadding = new Vector2(Mathf.Max(0f, cameraPadding.x), Mathf.Max(0f, cameraPadding.y));

        float requiredByWidth = (visualBounds.width + cameraPadding.x * 2f) / (2f * aspect);
        float cameraLocalY = visualBounds.center.y + cameraOffsetY;
        float requiredByHeight = Mathf.Max(
            Mathf.Abs(visualBounds.yMax - cameraLocalY),
            Mathf.Abs(cameraLocalY - visualBounds.yMin)) + cameraPadding.y;

        return Mathf.Max(requiredByWidth, requiredByHeight);
    }

    private float ResolveCellSize(float fallbackCellSize)
    {
        return _visualConfig != null ? _visualConfig.CellSize : Mathf.Max(0.01f, fallbackCellSize);
    }

    private Vector2 ResolveBackgroundPadding()
    {
        if (_visualConfig != null)
        {
            return _visualConfig.BackgroundPadding;
        }

        return new Vector2(Mathf.Max(0f, _backgroundPaddingX), Mathf.Max(0f, _backgroundPaddingY));
    }

    private Vector2 ResolveCameraPadding()
    {
        return _visualConfig != null ? _visualConfig.CameraPadding : Vector2.zero;
    }

    private Vector2 ResolveVisualHalfExtents(ItemFactory itemFactory, float cellSize)
    {
        if (itemFactory != null)
        {
            return itemFactory.EstimateMaxVisualHalfExtents(cellSize);
        }

        return _visualConfig != null
            ? _visualConfig.EstimateMaxVisualHalfExtents()
            : Vector2.one * (cellSize * 0.5f);
    }
}
}
