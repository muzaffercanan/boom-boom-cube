#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DreamGames.Board.Systems;
using DreamGames.Board.Items;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;

namespace DreamGames.Editor
{
public class GameDebugWindow : EditorWindow
{
    [MenuItem("DreamGames/Debug Panel")]
    public static void Open()
    {
        var w = GetWindow<GameDebugWindow>("DG Debug");
        w.minSize = new Vector2(330f, 300f);
    }

    // ─── State ────────────────────────────────────────────────────────────────
    private GameManager _gm;
    private Vector2 _scroll;

    private int   _boardW   = 9;
    private int   _boardH   = 9;
    private float _cellSize = 1f;
    private int   _lastW    = -1;
    private int   _lastH    = -1;
    private float _lastCS   = -1f;

    // ─── Cached styles ────────────────────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _btnActiveStyle;
    private GUIStyle _btnSmallStyle;
    private GUIStyle _infoStyle;
    private bool _stylesReady;

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (Application.isPlaying) Repaint();
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("Play mode'da çalışır.", MessageType.Info);
            return;
        }

        if (_gm == null) _gm = FindObjectOfType<GameManager>();
        if (_gm == null)
        {
            EditorGUILayout.HelpBox("GameManager bulunamadı.", MessageType.Warning);
            return;
        }

        SyncEdits();

        // ── Status bar ───────────────────────────────────────────────────────
        int  lvl  = _gm.DebugCurrentLevel;
        bool over = _gm.DebugIsGameOver;
        bool proc = _gm.IsProcessingTurn;
        int  mov  = _gm.RemainingMoves;
        string st = over ? "GAME OVER" : proc ? "İşleniyor…" : "Oynuyor";
        Color stCol = over ? Color.red : proc ? Color.yellow : Color.green;
        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(24f));
        GUI.color = Color.white;
        Rect statusRect = GUILayoutUtility.GetLastRect();
        GUI.Label(new Rect(statusRect.x + 6f, statusRect.y + 4f, statusRect.width - 8f, 18f),
            $"Level <b>{lvl}</b>   Hamle <b>{mov}</b>", _infoStyle);
        var oldColor = GUI.contentColor;
        GUI.contentColor = stCol;
        GUI.Label(new Rect(statusRect.x + statusRect.width - 90f, statusRect.y + 4f, 86f, 18f),
            $"<b>{st}</b>", _infoStyle);
        GUI.contentColor = oldColor;

        GUILayout.Space(2f);

        _scroll = GUILayout.BeginScrollView(_scroll);

        // ── HAMLE ─────────────────────────────────────────────────────────────
        DrawHeader("HAMLE");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+5",  _btnStyle)) _gm.DebugAddMoves(5);
        if (GUILayout.Button("+10", _btnStyle)) _gm.DebugAddMoves(10);
        if (GUILayout.Button("-5",  _btnStyle)) _gm.DebugAddMoves(-5);
        GUILayout.EndHorizontal();
        DrawSep();

        // ── LEVEL ─────────────────────────────────────────────────────────────
        DrawHeader("LEVEL");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◄ Önceki",  _btnStyle)) _gm.DebugLoadPrevLevel();
        if (GUILayout.Button("↺ Yenile",  _btnStyle)) _gm.DebugReloadLevel();
        if (GUILayout.Button("Sonraki ►", _btnStyle)) _gm.DebugLoadNextLevel();
        GUILayout.EndHorizontal();
        DrawSep();

        // ── BOARD BOYUTU ──────────────────────────────────────────────────────
        DrawHeader("BOARD BOYUTU");
        int curW = _gm.DebugGrid?.Width  ?? 0;
        int curH = _gm.DebugGrid?.Height ?? 0;
        GUILayout.Label($"Mevcut: {curW} × {curH}", _infoStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("W:", _infoStyle, GUILayout.Width(20f));
        if (GUILayout.Button("−", _btnSmallStyle, GUILayout.Width(26f))) _boardW = Mathf.Max(2, _boardW - 1);
        GUILayout.Label(_boardW.ToString(), _infoStyle, GUILayout.Width(22f));
        if (GUILayout.Button("+", _btnSmallStyle, GUILayout.Width(26f))) _boardW = Mathf.Min(20, _boardW + 1);
        GUILayout.Space(10f);
        GUILayout.Label("H:", _infoStyle, GUILayout.Width(20f));
        if (GUILayout.Button("−", _btnSmallStyle, GUILayout.Width(26f))) _boardH = Mathf.Max(2, _boardH - 1);
        GUILayout.Label(_boardH.ToString(), _infoStyle, GUILayout.Width(22f));
        if (GUILayout.Button("+", _btnSmallStyle, GUILayout.Width(26f))) _boardH = Mathf.Min(20, _boardH + 1);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reload →", _btnStyle)) _gm.DebugReloadWithSize(_boardW, _boardH);
        GUILayout.EndHorizontal();
        DrawSep();

        // ── CELL SIZE ─────────────────────────────────────────────────────────
        DrawHeader("CELL SIZE");
        GUILayout.Label($"Mevcut: {_gm.DebugCellSize:F2}", _infoStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("−0.1",  _btnSmallStyle)) _cellSize = Mathf.Max(0.2f, _cellSize - 0.1f);
        if (GUILayout.Button("−0.05", _btnSmallStyle)) _cellSize = Mathf.Max(0.2f, _cellSize - 0.05f);
        GUILayout.Label(_cellSize.ToString("F2"), _infoStyle, GUILayout.Width(36f));
        if (GUILayout.Button("+0.05", _btnSmallStyle)) _cellSize = Mathf.Min(4f, _cellSize + 0.05f);
        if (GUILayout.Button("+0.1",  _btnSmallStyle)) _cellSize = Mathf.Min(4f, _cellSize + 0.1f);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply →", _btnStyle)) _gm.DebugApplyCellSize(_cellSize);
        GUILayout.EndHorizontal();
        DrawSep();

        // ── ITEM ARALIK ───────────────────────────────────────────────────────
        DrawHeader("ITEM ARALIK  (scale)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("−0.05", _btnSmallStyle))
        {
            GameDebug.ItemScale = Mathf.Max(0.2f, GameDebug.ItemScale - 0.05f);
            _gm.DebugSetItemScale(GameDebug.ItemScale);
        }
        GUILayout.Label(GameDebug.ItemScale.ToString("F2"), _infoStyle, GUILayout.Width(36f));
        if (GUILayout.Button("+0.05", _btnSmallStyle))
        {
            GameDebug.ItemScale = Mathf.Min(1.5f, GameDebug.ItemScale + 0.05f);
            _gm.DebugSetItemScale(GameDebug.ItemScale);
        }
        GUILayout.Label(" anlık uygulanır", _infoStyle);
        GUILayout.EndHorizontal();
        DrawSep();

        // ── ANİMASYON HIZI ────────────────────────────────────────────────────
        DrawHeader($"ANİMASYON HIZI  ×{GameDebug.SpeedMultiplier:F2}");
        GUILayout.BeginHorizontal();
        DrawSpeedBtn(0.25f, "0.25×");
        DrawSpeedBtn(0.5f,  "0.5×");
        DrawSpeedBtn(1f,    "1×");
        DrawSpeedBtn(2f,    "2×");
        DrawSpeedBtn(4f,    "4×");
        GUILayout.EndHorizontal();
        DrawSep();

        // ── ROKET HIZI ────────────────────────────────────────────────────────
        DrawHeader($"ROKET HIZI  {GameDebug.RocketSpeed:F1}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("−2",   _btnSmallStyle)) GameDebug.RocketSpeed = Mathf.Max(1f,  GameDebug.RocketSpeed - 2f);
        if (GUILayout.Button("−1",   _btnSmallStyle)) GameDebug.RocketSpeed = Mathf.Max(1f,  GameDebug.RocketSpeed - 1f);
        GUILayout.Label(GameDebug.RocketSpeed.ToString("F1"), _infoStyle, GUILayout.Width(36f));
        if (GUILayout.Button("+1",   _btnSmallStyle)) GameDebug.RocketSpeed = Mathf.Min(80f, GameDebug.RocketSpeed + 1f);
        if (GUILayout.Button("+5",   _btnSmallStyle)) GameDebug.RocketSpeed = Mathf.Min(80f, GameDebug.RocketSpeed + 5f);
        if (GUILayout.Button("Reset",_btnSmallStyle)) GameDebug.RocketSpeed = 12f;
        GUILayout.EndHorizontal();
        DrawSep();

        // ── GOALS ─────────────────────────────────────────────────────────────
        DrawHeader("GOALS");
        if (GUILayout.Button("Force Win  (tüm goal'ları tamamla)", _btnStyle))
            _gm.DebugForceCompleteGoals();
        DrawSep();

        // ── SHUFFLE ───────────────────────────────────────────────────────────
        DrawHeader("SHUFFLE");
        if (GUILayout.Button("Force Shuffle  (küpleri karıştır)", _btnStyle))
            _gm.DebugForceShuffle();
        DrawSep();

        // ── LOG ───────────────────────────────────────────────────────────────
        DrawHeader("LOG / SNAPSHOT");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Export Log", _btnStyle))
        {
            string log = _gm.ExportSessionLog();
            Debug.Log("[DebugPanel] Session Log:\n" + log);
            EditorGUIUtility.systemCopyBuffer = log;
        }
        if (GUILayout.Button("Board Snapshot", _btnStyle))
        {
            var snap = _gm.CaptureBoardSnapshot();
            string s = snap != null ? snap.ToDebugString() : "null";
            Debug.Log("[DebugPanel] Snapshot:\n" + s);
            EditorGUIUtility.systemCopyBuffer = s;
        }
        GUILayout.EndHorizontal();
        DrawSep();

        // ── GÖRÜNÜM ───────────────────────────────────────────────────────────
        DrawHeader("GÖRÜNÜM");
        GameDebug.ShowBoardOverlay = GUILayout.Toggle(
            GameDebug.ShowBoardOverlay,
            "  Board Overlay  (item tipi + health)",
            _infoStyle);

        GUILayout.Space(6f);
        GUILayout.EndScrollView();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private void SyncEdits()
    {
        var grid = _gm.DebugGrid;
        float cs = _gm.DebugCellSize;
        if (grid == null) return;

        if (grid.Width != _lastW || grid.Height != _lastH)
        {
            _boardW = grid.Width;
            _boardH = grid.Height;
            _lastW  = grid.Width;
            _lastH  = grid.Height;
        }
        if (!Mathf.Approximately(cs, _lastCS))
        {
            _cellSize = cs;
            _lastCS   = cs;
        }
    }

    private void DrawHeader(string text)
    {
        GUILayout.Space(2f);
        GUILayout.Label(text, _headerStyle);
    }

    private void DrawSep()
    {
        GUILayout.Space(2f);
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.4f, 0.4f, 0.4f, 0.4f));
        GUILayout.Space(2f);
    }

    private void DrawSpeedBtn(float speed, string label)
    {
        bool active = Mathf.Approximately(GameDebug.SpeedMultiplier, speed);
        if (GUILayout.Button(label, active ? _btnActiveStyle : _btnStyle))
            GameDebug.SpeedMultiplier = speed;
    }

    // ─── Style init ───────────────────────────────────────────────────────────
    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
        };
        _headerStyle.normal.textColor = new Color(1f, 0.82f, 0.3f);

        _infoStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            richText = true,
        };
        _infoStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        Texture2D tBtn  = MakeTex(new Color(0.25f, 0.25f, 0.28f, 1f));
        Texture2D tBtnH = MakeTex(new Color(0.33f, 0.33f, 0.38f, 1f));
        Texture2D tAct  = MakeTex(new Color(0.15f, 0.42f, 0.70f, 1f));
        Texture2D tActH = MakeTex(new Color(0.20f, 0.52f, 0.84f, 1f));

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
        };
        _btnStyle.normal.background = tBtn;
        _btnStyle.hover.background  = tBtnH;
        _btnStyle.normal.textColor  = Color.white;
        _btnStyle.hover.textColor   = Color.white;
        _btnStyle.padding = new RectOffset(5, 5, 3, 3);

        _btnActiveStyle = new GUIStyle(_btnStyle);
        _btnActiveStyle.normal.background = tAct;
        _btnActiveStyle.hover.background  = tActH;

        _btnSmallStyle = new GUIStyle(_btnStyle) { fontSize = 10 };
        _btnSmallStyle.padding = new RectOffset(4, 4, 2, 2);
    }

    private static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(2, 2);
        t.SetPixels(new[] { c, c, c, c });
        t.Apply();
        return t;
    }
}
}
#endif
