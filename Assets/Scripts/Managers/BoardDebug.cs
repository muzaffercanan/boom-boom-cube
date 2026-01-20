using UnityEngine;
using System.Collections;

public static class BoardDebug
{
    public static void LogTransform(string tag, Transform t)
    {
        if (t == null)
        {
            Debug.Log($"[BoardDebug] {tag} Transform=NULL");
            return;
        }

        var p = t.parent;
        Debug.Log(
            $"[BoardDebug] {tag} | name={t.name} " +
            $"local={t.localPosition} world={t.position} " +
            $"parent={(p ? p.name : "NULL")} " +
            $"parentScale={(p ? p.localScale.ToString() : "NULL")} " +
            $"parentRot={(p ? p.rotation.eulerAngles.ToString() : "NULL")} " +
            $"selfScale={t.localScale} selfRot={t.rotation.eulerAngles}"
        );
    }

    public static IEnumerator LogNextFrame(string tag, Transform t)
    {
        yield return null;
        LogTransform(tag + " (NEXT FRAME)", t);
    }

    public static void LogBoardParent(Transform boardParent)
    {
        if (boardParent == null)
        {
            Debug.LogError("[BoardDebug] BoardParent is NULL!");
            return;
        }

        Debug.Log(
            $"[BoardDebug] BoardParent | name={boardParent.name} " +
            $"local={boardParent.localPosition} world={boardParent.position} " +
            $"scale={boardParent.localScale} rot={boardParent.rotation.eulerAngles} " +
            $"isRectTransform={(boardParent is RectTransform)}"
        );

        Transform cur = boardParent;
        int depth = 0;
        while (cur != null && depth < 10)
        {
            Debug.Log($"[BoardDebug] ParentChain[{depth}] {cur.name} scale={cur.localScale} rot={cur.rotation.eulerAngles}");
            cur = cur.parent;
            depth++;
        }
    }
}
