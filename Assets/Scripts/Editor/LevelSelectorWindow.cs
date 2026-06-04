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
            PlayerPrefs.SetInt(ProgressService.SelectedLevelForGameKey, level);
            PlayerPrefs.SetInt(ProgressService.LastPlayedLevelKey, level);
            PlayerPrefs.Save();

            Debug.Log($"[LevelSelector] Ready to play Level {level}");

            
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[LevelSelector] Stopped playback. Press Play to start the selected level.");
            }
            else
            {
                if (EditorSceneManager.GetActiveScene().name != SceneNames.Level)
                {
                     string scenePath = $"Assets/Scenes/{SceneNames.Level}.unity";
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
