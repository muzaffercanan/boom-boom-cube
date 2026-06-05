using UnityEngine;

public class BoardSetupController : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private SpriteRenderer _gridBackgroundRenderer;
    [SerializeField] private float _backgroundPaddingX = 0.05f;
    [SerializeField] private float _backgroundPaddingY = 0.05f;

    [Header("Camera")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _cameraPadding = 0.15f;
    [SerializeField] private float _verticalCameraPadding = 1.1f;
    [SerializeField, Range(0.5f, 1.0f)] private float _targetBoardViewportWidth = 0.97f;
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

        Bounds boardBounds = CalculateBoardBounds(levelData, boardParent, cellSize);
        Vector3 worldCenter = boardBounds.center;
        worldCenter.y += _cameraOffsetY;
        worldCenter.z = -10f;
        _camera.transform.position = worldCenter;

        float screenRatio = Screen.height > 0
            ? (float)Screen.width / Screen.height
            : _camera.aspect;
        if (screenRatio <= 0f)
        {
            screenRatio = 1f;
        }

        float targetViewportWidth = Mathf.Clamp(_targetBoardViewportWidth, 0.5f, 1.0f);
        float paddedBoardWidth = boardBounds.size.x + _cameraPadding * 2f;
        float widthBasedSize = paddedBoardWidth / (2f * screenRatio * targetViewportWidth);
        float heightBasedSize = boardBounds.size.y / 2f + _verticalCameraPadding + Mathf.Abs(_cameraOffsetY);

        _camera.orthographicSize = Mathf.Max(widthBasedSize, heightBasedSize);
    }

    private Bounds CalculateBoardBounds(LevelData levelData, Transform boardParent, float cellSize)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds();

        if (boardParent != null)
        {
            SpriteRenderer[] renderers = boardParent.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (_gridBackgroundRenderer != null && _gridBackgroundRenderer.enabled)
        {
            if (!hasBounds)
            {
                bounds = _gridBackgroundRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(_gridBackgroundRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        float gridWidth = levelData.grid_width * cellSize;
        float gridHeight = levelData.grid_height * cellSize;
        Vector3 centerPos = new Vector3(
            (gridWidth - cellSize) / 2f,
            (gridHeight - cellSize) / 2f,
            0f
        );
        Vector3 worldCenter = boardParent != null ? boardParent.TransformPoint(centerPos) : centerPos;

        return new Bounds(worldCenter, new Vector3(gridWidth, gridHeight, 0f));
    }
}
