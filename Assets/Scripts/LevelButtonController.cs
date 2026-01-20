using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButtonController : MonoBehaviour
{
    public int levelIndex = 1;

    [SerializeField] private Button _button;

    private void Start()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(LoadLevel);
    }

    void LoadLevel()
    {
        Debug.Log("[LevelButtonController] Butona tıklandı! LevelScene yükleniyor...");
        PlayerPrefs.SetInt("CurrentLevel", levelIndex);
        SceneManager.LoadScene("LevelScene");
    }
}
