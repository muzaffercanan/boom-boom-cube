using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.UI
{
public class GoalItemView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _countNumber;

    public void SetGoal(Sprite icon, int count)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }
        UpdateCount(count);
    }

    public void UpdateCount(int count)
    {
        if (_countNumber == null) return;
        _countNumber.text = Mathf.Max(0, count).ToString();
    }

}
}
