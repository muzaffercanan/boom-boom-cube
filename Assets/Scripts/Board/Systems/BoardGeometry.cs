using UnityEngine;

namespace DreamGames.Board.Systems
{
public sealed class BoardGeometry
{
    private const float MinCellSize = 0.01f;

    public BoardGeometry(Transform boardParent, float cellSize)
    {
        BoardParent = boardParent;
        CellSize = Mathf.Max(MinCellSize, cellSize);
    }

    public Transform BoardParent { get; }
    public float CellSize { get; }

    public Vector3 CellToLocalPosition(int x, int y, float z = 0f)
    {
        return new Vector3(x * CellSize, y * CellSize, z);
    }

    public Vector3 CellToWorldPosition(int x, int y, float z = 0f)
    {
        Vector3 local = CellToLocalPosition(x, y, z);
        return BoardParent != null ? BoardParent.TransformPoint(local) : local;
    }

    public Rect GetCellLocalBounds(int x, int y)
    {
        Vector3 center = CellToLocalPosition(x, y);
        float half = CellSize * 0.5f;
        return new Rect(center.x - half, center.y - half, CellSize, CellSize);
    }

    public bool TryLocalPositionToCell(Vector2 localPosition, int width, int height, out Vector2Int cell)
    {
        int x = Mathf.FloorToInt(localPosition.x / CellSize + 0.5f);
        int y = Mathf.FloorToInt(localPosition.y / CellSize + 0.5f);

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            cell = default;
            return false;
        }

        cell = new Vector2Int(x, y);
        return true;
    }

    public bool TryWorldPositionToCell(Vector3 worldPosition, int width, int height, out Vector2Int cell)
    {
        Vector3 local = BoardParent != null ? BoardParent.InverseTransformPoint(worldPosition) : worldPosition;
        return TryLocalPositionToCell(local, width, height, out cell);
    }

    public bool TryScreenPositionToCell(Camera camera, Vector2 screenPosition, int width, int height, out Vector2Int cell)
    {
        cell = default;
        if (camera == null)
        {
            return false;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        Plane boardPlane = BoardParent != null
            ? new Plane(BoardParent.forward, BoardParent.position)
            : new Plane(Vector3.forward, Vector3.zero);

        if (!boardPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 worldPosition = ray.GetPoint(enter);
        return TryWorldPositionToCell(worldPosition, width, height, out cell);
    }

    public Rect GetLogicalBoardBounds(int width, int height)
    {
        return GetVisualBoardBounds(width, height, Vector2.one * (CellSize * 0.5f), Vector2.zero);
    }

    public Rect GetVisualBoardBounds(int width, int height, Vector2 maxVisualHalfExtents, Vector2 padding)
    {
        if (width <= 0 || height <= 0)
        {
            return new Rect(0f, 0f, 0f, 0f);
        }

        maxVisualHalfExtents = Vector2.Max(maxVisualHalfExtents, Vector2.one * (CellSize * 0.5f));
        padding = new Vector2(Mathf.Max(0f, padding.x), Mathf.Max(0f, padding.y));

        float minX = -maxVisualHalfExtents.x - padding.x;
        float minY = -maxVisualHalfExtents.y - padding.y;
        float maxX = (width - 1) * CellSize + maxVisualHalfExtents.x + padding.x;
        float maxY = (height - 1) * CellSize + maxVisualHalfExtents.y + padding.y;

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
}
}
