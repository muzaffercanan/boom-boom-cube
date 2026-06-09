using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Core;
using DreamGames.Gameplay;

namespace DreamGames.Gameplay
{
// Sadece corner stats + board overlay. Kontrol paneli -> DreamGames/Debug Panel (EditorWindow).
public class GameDebugPanel : MonoBehaviour
{
    private GameManager _gameManager;

    private float _fpsAccum;
    private int   _fpsCnt;
    private float _fpsCurr = 60f;
    private float _fpsTimer;
    private float _memMb;
    private float _memTimer;
    private const float FpsInterval = 0.5f;

    private GUIStyle _cornerStyle;
    private GUIStyle _overlayStyle;
    private bool _stylesReady;

    public void SetGameManager(GameManager gm) => _gameManager = gm;

    private void Start()
    {
        if (_gameManager == null)
            _gameManager = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        // FPS
        if (Time.unscaledDeltaTime > 0f) _fpsAccum += 1f / Time.unscaledDeltaTime;
        _fpsCnt++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= FpsInterval)
        {
            _fpsCurr = _fpsAccum / _fpsCnt;
            _fpsAccum = 0f; _fpsCnt = 0; _fpsTimer = 0f;
        }

        // Memory
        _memTimer += Time.unscaledDeltaTime;
        if (_memTimer >= 1f)
        {
            _memMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            _memTimer = 0f;
        }

#if UNITY_EDITOR
        // F9 → Debug Panel penceresini aç
        if (Input.GetKeyDown(KeyCode.F9))
            UnityEditor.EditorApplication.ExecuteMenuItem("DreamGames/Debug Panel");
#endif
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawCornerStats();
        if (GameDebug.ShowBoardOverlay) DrawBoardOverlay();
    }

    // ─── Corner stats ─────────────────────────────────────────────────────────
    private void DrawCornerStats()
    {
        const float W = 210f;
        const float H = 22f;
        float x = Screen.width - W - 4f;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, 4f, W, H), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string fHex = _fpsCurr >= 55f ? "#5dde8a" : _fpsCurr >= 30f ? "#f5c842" : "#f55a5a";
        GUI.Label(new Rect(x + 4f, 6f, W - 8f, H),
            $"<color={fHex}>{_fpsCurr:F0} FPS</color>  " +
            $"<color=#aaaaaa>{_memMb:F1} MB</color>  " +
            $"<color=#666666>[F9] Debug</color>",
            _cornerStyle);
    }

    // ─── Board overlay ────────────────────────────────────────────────────────
    private void DrawBoardOverlay()
    {
        if (_gameManager == null) return;
        GridSystem grid  = _gameManager.DebugGrid;
        Transform  board = _gameManager.DebugBoardParent;
        float      cs    = _gameManager.DebugCellSize;
        if (grid == null || board == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        const float LW = 46f;
        const float LH = 20f;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                IBoardItem item = grid.GetItem(x, y);
                if (item == null) continue;

                Vector3 world  = board.TransformPoint(new Vector3(x * cs, y * cs, 0f));
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f) continue;

                float sx = screen.x - LW * 0.5f;
                float sy = Screen.height - screen.y - LH * 0.5f;

                GUI.color = new Color(0f, 0f, 0f, 0.60f);
                GUI.DrawTexture(new Rect(sx, sy, LW, LH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GetOverlayInfo(item, out string label, out Color col);
                GUI.contentColor = col;
                GUI.Label(new Rect(sx, sy, LW, LH), label, _overlayStyle);
                GUI.contentColor = Color.white;
            }
        }
    }

    private static void GetOverlayInfo(IBoardItem item, out string label, out Color color)
    {
        switch (item.GetItemType())
        {
            case ItemType.Cube:
                if (item is IMatchable m)
                {
                    switch (m.GetColor())
                    {
                        case CubeColor.Red:    label = "R"; color = new Color(1f, 0.4f, 0.4f);  return;
                        case CubeColor.Green:  label = "G"; color = new Color(0.4f, 1f, 0.4f);  return;
                        case CubeColor.Blue:   label = "B"; color = new Color(0.4f, 0.8f, 1f);  return;
                        case CubeColor.Yellow: label = "Y"; color = new Color(1f, 0.9f, 0.3f);  return;
                    }
                }
                label = "C"; color = Color.white; return;

            case ItemType.Rocket:
                label = (item is RocketItem r && r.IsHorizontal) ? "H→" : "V↑";
                color = new Color(1f, 0.6f, 0.1f); return;

            case ItemType.Obstacle:
                int hp  = item is IDamageable d ? d.Health : 0;
                string p = item is BoxItem   ? "BOX"
                         : item is StoneItem ? "STN"
                         : item is VaseItem  ? "VSE" : "OB";
                label = hp > 1 ? $"{p}{hp}" : p;
                color = new Color(0.75f, 0.75f, 0.75f); return;

            default: label = "?"; color = Color.white; return;
        }
    }

    // ─── Styles ───────────────────────────────────────────────────────────────
    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _cornerStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, richText = true };
        _cornerStyle.normal.textColor = Color.white;

        _overlayStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _overlayStyle.normal.textColor = Color.white;
    }
}
}
