#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DreamGames.Board.Systems;
using DreamGames.Core;
using DreamGames.Gameplay;

namespace DreamGames.Editor
{
public class GameDebugWindow : EditorWindow
{
    [MenuItem("DreamGames/Debug Panel")]
    public static void Open()
    {
        var w = GetWindow<GameDebugWindow>("DG Debug");
        w.minSize = new Vector2(320f, 280f);
    }

    // ── State ─────────────────────────────────────────────────────────────────
    private GameManager _gm;
    private Vector2     _scroll;
    private int         _boardW = 9, _boardH = 9;
    private float       _cellSize = 1f;
    private int         _lastW = -1, _lastH = -1;
    private float       _lastCS = -1f;

    // ── Styles ────────────────────────────────────────────────────────────────
    private GUIStyle _headerLabel;
    private GUIStyle _valueLabel;
    private GUIStyle _dimLabel;
    private GUIStyle _btnNeutral;
    private GUIStyle _btnActive;
    private GUIStyle _btnApply;
    private GUIStyle _btnDanger;
    private GUIStyle _btnSmall;
    private bool     _stylesReady;

    private static readonly GUILayoutOption H22   = GUILayout.Height(22f);
    private static readonly GUILayoutOption H18   = GUILayout.Height(18f);
    private static readonly GUILayoutOption W80   = GUILayout.Width(80f);
    private static readonly GUILayoutOption W64   = GUILayout.Width(64f);
    private static readonly Color           BgHeader = new Color(0.12f, 0.12f, 0.16f, 1f);
    private static readonly Color           BgStatus = new Color(0.09f, 0.09f, 0.12f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    private void Update() { if (Application.isPlaying) Repaint(); }

    private void OnGUI()
    {
        EnsureStyles();

        if (!Application.isPlaying)
        {
            GUILayout.Space(10f);
            EditorGUILayout.HelpBox("Play mode'da çalışır.", MessageType.Info);
            return;
        }

        if (_gm == null) _gm = FindObjectOfType<GameManager>();
        if (_gm == null) { EditorGUILayout.HelpBox("GameManager bulunamadı.", MessageType.Warning); return; }

        SyncEdits();
        DrawStatusBar();

        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.Space(2f);

        Section("HAMLE", DrawMoves);
        Section("LEVEL", DrawLevel);
        Section($"BOARD BOYUTU  <color=#666>{_gm.DebugGrid?.Width ?? 0} × {_gm.DebugGrid?.Height ?? 0}</color>", DrawBoardSize);
        Section($"CELL SIZE  <color=#666>{_gm.DebugCellSize:F2}</color>", DrawCellSize);
        Section($"ITEM ARALIK  <color=#666>{_gm.DebugItemScale:F2}</color>", DrawItemScale);
        Section($"ANİMASYON HIZI  <color=#666>×{GameDebug.SpeedMultiplier:F2}</color>", DrawAnimSpeed);
        Section($"ROKET HIZI  <color=#666>{GameDebug.RocketSpeed:F1}</color>", DrawRocketSpeed);
        Section("GOALS  /  SHUFFLE", DrawActions);
        Section("LOG  /  GÖRÜNÜM", DrawLog);

        GUILayout.Space(10f);
        GUILayout.EndScrollView();
    }

    // ── Status bar ────────────────────────────────────────────────────────────
    private void DrawStatusBar()
    {
        Rect bar = EditorGUILayout.GetControlRect(false, 30f);
        EditorGUI.DrawRect(bar, BgStatus);

        bool over = _gm.DebugIsGameOver;
        bool proc = _gm.IsProcessingTurn;
        Color stCol = over  ? new Color(0.9f, 0.35f, 0.35f)
                    : proc  ? new Color(0.95f, 0.82f, 0.2f)
                    :         new Color(0.35f, 0.85f, 0.5f);
        string stTxt = over ? "GAME OVER" : proc ? "İşleniyor" : "Oynuyor";

        // Left: level + moves
        var leftStyle = new GUIStyle(_valueLabel) { fontSize = 12, alignment = TextAnchor.MiddleLeft, richText = true };
        GUI.Label(new Rect(bar.x + 10f, bar.y, bar.width * 0.65f, bar.height),
            $"Level  <b>{_gm.DebugCurrentLevel}</b>   —   Hamle  <b>{_gm.RemainingMoves}</b>",
            leftStyle);

        // Right: status
        var rightStyle = new GUIStyle(_valueLabel) { fontSize = 11, alignment = TextAnchor.MiddleRight };
        rightStyle.normal.textColor = stCol;
        GUI.Label(new Rect(bar.x, bar.y, bar.width - 10f, bar.height), stTxt, rightStyle);
    }

    // ── Section draw methods ──────────────────────────────────────────────────
    private void DrawMoves()
    {
        Row(() =>
        {
            if (Btn("+5",  _btnNeutral)) _gm.DebugAddMoves(5);
            if (Btn("+10", _btnNeutral)) _gm.DebugAddMoves(10);
            if (Btn("−5",  _btnNeutral)) _gm.DebugAddMoves(-5);
        });
    }

    private void DrawLevel()
    {
        Row(() =>
        {
            if (Btn("◄ Önceki",  _btnNeutral)) _gm.DebugLoadPrevLevel();
            if (Btn("↺ Yenile",  _btnNeutral)) _gm.DebugReloadLevel();
            if (Btn("Sonraki ►", _btnNeutral)) _gm.DebugLoadNextLevel();
        });
    }

    private void DrawBoardSize()
    {
        Row(() =>
        {
            GUILayout.Label("W", _dimLabel, GUILayout.Width(14f));
            if (SmBtn("−")) _boardW = Mathf.Max(2,  _boardW - 1);
            Val(_boardW.ToString(), 26f);
            if (SmBtn("+")) _boardW = Mathf.Min(20, _boardW + 1);
            GUILayout.Space(10f);
            GUILayout.Label("H", _dimLabel, GUILayout.Width(14f));
            if (SmBtn("−")) _boardH = Mathf.Max(2,  _boardH - 1);
            Val(_boardH.ToString(), 26f);
            if (SmBtn("+")) _boardH = Mathf.Min(20, _boardH + 1);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reload →", _btnApply, H22, W80))
                _gm.DebugReloadWithSize(_boardW, _boardH);
        });
    }

    private void DrawCellSize()
    {
        Row(() =>
        {
            if (SmBtn("−0.1"))  _cellSize = Mathf.Max(0.2f, _cellSize - 0.10f);
            if (SmBtn("−0.05")) _cellSize = Mathf.Max(0.2f, _cellSize - 0.05f);
            if (SmBtn("−0.01")) _cellSize = Mathf.Max(0.2f, _cellSize - 0.01f);
            Val(_cellSize.ToString("F2"), 36f);
            if (SmBtn("+0.01")) _cellSize = Mathf.Min(4f, _cellSize + 0.01f);
            if (SmBtn("+0.05")) _cellSize = Mathf.Min(4f, _cellSize + 0.05f);
            if (SmBtn("+0.1"))  _cellSize = Mathf.Min(4f, _cellSize + 0.10f);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply →", _btnApply, H22, W64))
                _gm.DebugApplyCellSize(_cellSize);
        });
    }

    private void DrawItemScale()
    {
        float itemScale = _gm.DebugItemScale;
        Row(() =>
        {
            if (SmBtn("−0.05")) ApplyItemScale(itemScale - 0.05f);
            if (SmBtn("−0.01")) ApplyItemScale(itemScale - 0.01f);
            Val(itemScale.ToString("F2"), 36f);
            if (SmBtn("+0.01")) ApplyItemScale(itemScale + 0.01f);
            if (SmBtn("+0.05")) ApplyItemScale(itemScale + 0.05f);
        });
    }

    private void DrawAnimSpeed()
    {
        Row(() =>
        {
            SpeedBtn(0.25f, "0.25×");
            SpeedBtn(0.5f,  "0.5×");
            SpeedBtn(1f,    "1×");
            SpeedBtn(2f,    "2×");
            SpeedBtn(4f,    "4×");
        });
    }

    private void DrawRocketSpeed()
    {
        Row(() =>
        {
            if (SmBtn("−2")) GameDebug.RocketSpeed = Mathf.Max(1f,  GameDebug.RocketSpeed - 2f);
            if (SmBtn("−1")) GameDebug.RocketSpeed = Mathf.Max(1f,  GameDebug.RocketSpeed - 1f);
            Val(GameDebug.RocketSpeed.ToString("F1"), 36f);
            if (SmBtn("+1")) GameDebug.RocketSpeed = Mathf.Min(80f, GameDebug.RocketSpeed + 1f);
            if (SmBtn("+5")) GameDebug.RocketSpeed = Mathf.Min(80f, GameDebug.RocketSpeed + 5f);
            GUILayout.FlexibleSpace();
            if (SmBtn("Reset")) GameDebug.RocketSpeed = 12f;
        });
    }

    private void DrawActions()
    {
        Row(() =>
        {
            if (GUILayout.Button("Force Win",     _btnDanger,  H22)) _gm.DebugForceCompleteGoals();
            if (GUILayout.Button("Force Shuffle", _btnNeutral, H22)) _gm.DebugForceShuffle();
        });
    }

    private void DrawLog()
    {
        Row(() =>
        {
            if (Btn("Export Log", _btnNeutral))
            {
                string log = _gm.ExportSessionLog();
                Debug.Log("[Debug] Log:\n" + log);
                EditorGUIUtility.systemCopyBuffer = log;
            }
            if (Btn("Snapshot", _btnNeutral))
            {
                var snap = _gm.CaptureBoardSnapshot();
                string s = snap != null ? snap.ToDebugString() : "null";
                Debug.Log("[Debug] Snapshot:\n" + s);
                EditorGUIUtility.systemCopyBuffer = s;
            }
        });
        GUILayout.Space(2f);
        GUILayout.BeginHorizontal();
        GUILayout.Space(10f);
        GameDebug.ShowBoardOverlay = GUILayout.Toggle(GameDebug.ShowBoardOverlay, "  Board Overlay", _dimLabel);
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
    }

    // ── UI primitives ─────────────────────────────────────────────────────────
    private void Section(string title, System.Action draw)
    {
        GUILayout.Space(4f);
        Rect hr = EditorGUILayout.GetControlRect(false, 21f);
        EditorGUI.DrawRect(hr, BgHeader);
        var hs = new GUIStyle(_headerLabel) { richText = true };
        GUI.Label(new Rect(hr.x + 8f, hr.y + 1f, hr.width - 10f, hr.height), title, hs);
        draw();
    }

    private void Row(System.Action content)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10f);
        GUILayout.BeginVertical();
        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        content();
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        GUILayout.EndVertical();
        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
    }

    private bool Btn(string label, GUIStyle style) =>
        GUILayout.Button(label, style, H22);

    private bool SmBtn(string label) =>
        GUILayout.Button(label, _btnSmall, H18);

    private void Val(string text, float w) =>
        GUILayout.Label(text, _valueLabel, GUILayout.Width(w), H22);

    private void SpeedBtn(float speed, string label)
    {
        bool active = Mathf.Approximately(GameDebug.SpeedMultiplier, speed);
        if (GUILayout.Button(label, active ? _btnActive : _btnNeutral, H22))
            GameDebug.SpeedMultiplier = speed;
    }

    private void ApplyItemScale(float val)
    {
        val = Mathf.Clamp(val, 0.2f, 1.5f);
        GameDebug.ItemScale = val;
        _gm.DebugSetItemScale(val);
    }

    private void SyncEdits()
    {
        var grid = _gm.DebugGrid;
        float cs = _gm.DebugCellSize;
        if (grid == null) return;
        if (grid.Width != _lastW || grid.Height != _lastH)
        {
            _boardW = grid.Width; _boardH = grid.Height;
            _lastW = grid.Width; _lastH = grid.Height;
        }
        if (!Mathf.Approximately(cs, _lastCS)) { _cellSize = cs; _lastCS = cs; }
    }

    // ── Style init ────────────────────────────────────────────────────────────
    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _headerLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        _headerLabel.normal.textColor = new Color(1f, 0.84f, 0.32f);

        _valueLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
        _valueLabel.normal.textColor = Color.white;

        _dimLabel = new GUIStyle(EditorStyles.label) { fontSize = 11 };
        _dimLabel.normal.textColor = new Color(0.72f, 0.72f, 0.72f);

        _btnNeutral = Btn2(new Color(0.23f, 0.23f, 0.27f), new Color(0.30f, 0.30f, 0.36f));
        _btnSmall   = Btn2(new Color(0.20f, 0.20f, 0.24f), new Color(0.28f, 0.28f, 0.34f), 10);
        _btnActive  = Btn2(new Color(0.13f, 0.40f, 0.68f), new Color(0.17f, 0.50f, 0.82f));
        _btnApply   = Btn2(new Color(0.13f, 0.44f, 0.22f), new Color(0.17f, 0.55f, 0.28f));
        _btnDanger  = Btn2(new Color(0.50f, 0.13f, 0.13f), new Color(0.62f, 0.17f, 0.17f));
    }

    private GUIStyle Btn2(Color normal, Color hover, int size = 11)
    {
        var s = new GUIStyle(GUI.skin.button) { fontSize = size, fontStyle = FontStyle.Bold };
        s.normal.background = Tex(normal);
        s.hover.background  = Tex(hover);
        s.normal.textColor  = Color.white;
        s.hover.textColor   = Color.white;
        s.padding = new RectOffset(4, 4, 2, 2);
        return s;
    }

    private static Texture2D Tex(Color c)
    {
        var t = new Texture2D(2, 2);
        t.SetPixels(new[] { c, c, c, c });
        t.Apply();
        return t;
    }
}
}
#endif
