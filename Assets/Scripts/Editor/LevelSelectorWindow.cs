using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DreamGames.Editor
{
    public class LevelSelectorWindow : EditorWindow
    {
        private int _levelToPlay = 1;

        [MenuItem("DreamGames/Open Level Selector")]
        public static void ShowWindow()
        {
            GetWindow<LevelSelectorWindow>("Level Selector");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Level to Test", EditorStyles.boldLabel);
            
            GUILayout.Space(10);

            _levelToPlay = EditorGUILayout.IntField("Level Number:", _levelToPlay);

            GUILayout.Space(10);

            if (GUILayout.Button("Play Level", GUILayout.Height(40)))
            {
                PlayLevel(_levelToPlay);
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Reset Progress (Unlock First Level Only)"))
            {
                ResetProgress();
            }
        }

        private void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[LevelSelector] All progress cleared! Game execution will start from Level 1.");
        }

        private void PlayLevel(int level)
        {
            // Set the preferences that GameManager reads
            PlayerPrefs.SetInt("SelectedLevelForGame", level);
            PlayerPrefs.SetInt("LastPlayedLevel", level);
            PlayerPrefs.Save();

            Debug.Log($"[LevelSelector] Ready to play Level {level}");

            // If we are already playing, we might want to stop and restart, or just let the user restart manually.
            // But for convenience, let's force a restart if playing, or start if not.
            
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                // We can't immediately start playing again in the same frame.
                // The user will have to click Play again or we could hook into update. 
                // For simplicity, let's just stop playing and let the user know they can start.
                // Actually, often it's better to just ensure the Prefs are set (done above).
                Debug.Log("[LevelSelector] Stopped playback. Press Play to start the selected level.");
            }
            else
            {
                // Ensure we are in the correct scene if possible, or just start playing.
                // If GameManager is in the current scene or DontDestroyOnLoad, it should pick it up.
                // Assuming LevelScene is the main scene.
                if (EditorSceneManager.GetActiveScene().name != "LevelScene")
                {
                    // Optional: Open LevelScene if known
                     string scenePath = "Assets/Scenes/LevelScene.unity";
                     if (System.IO.File.Exists(scenePath)) 
                     {
                         EditorSceneManager.OpenScene(scenePath);
                     }
                }
                
                EditorApplication.isPlaying = true;
            }
        }
    }
}
