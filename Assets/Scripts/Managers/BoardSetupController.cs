using UnityEngine;

public class BoardSetupController : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private SpriteRenderer _gridBackgroundRenderer;
    [SerializeField] private float _backgroundPaddingX = 0.05f;
    [SerializeField] private float _backgroundPaddingY = 0.05f;

    [Header("Camera")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _cameraPadding = 2.0f;
    [SerializeField] private float _cameraOffsetY = 100.0f;

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

        float gridWidth = levelData.grid_width * cellSize;
        float gridHeight = levelData.grid_height * cellSize;

        Vector3 centerPos = new Vector3(
            (gridWidth - cellSize) / 2f,
            (gridHeight - cellSize) / 2f
        );

        Vector3 worldCenter = boardParent.TransformPoint(centerPos);
        worldCenter.y += _cameraOffsetY;
        worldCenter.z = -10f;
        _camera.transform.position = worldCenter;

        float targetHeight = gridHeight + _cameraPadding * 2;
        float targetWidth = gridWidth + _cameraPadding * 2;
        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = targetWidth / targetHeight;

        _camera.orthographicSize = screenRatio >= targetRatio
            ? targetHeight / 2f
            : targetHeight / 2f * (targetRatio / screenRatio);
    }
}
