using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutoFixSettings
{
    static AutoFixSettings()
    {
        FixGridBackground();
    }

    static void FixGridBackground()
    {
        string[] paths = { 
            "Assets/UI/Gameplay/grid_background.png", 
            "Assets/Resources/UI/Gameplay/grid_background.png" 
        };

        foreach (var path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                
                bool dirty = false;
                
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    dirty = true;
                }

                if (settings.spriteBorder == Vector4.zero)
                {
                    settings.spriteBorder = new Vector4(60, 60, 60, 60);
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    Debug.Log($"[AutoFix] Fixed TextureImporter settings for {path}");
                }
            }
        }
    }
}
