using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

[InitializeOnLoad]
public class SetAppIcon
{
    static SetAppIcon()
    {
        EditorApplication.delayCall += ApplyIcon;
    }

    static void ApplyIcon()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/AppIcon.png");
        if (icon != null)
        {
            var iosTarget = NamedBuildTarget.iOS;
            var iOSIconKinds = PlayerSettings.GetSupportedIconKinds(iosTarget);
            foreach (var kind in iOSIconKinds)
            {
                var sizes = PlayerSettings.GetIconSizes(iosTarget, kind);
                Texture2D[] kindIcons = new Texture2D[sizes.Length];
                for (int i = 0; i < sizes.Length; i++)
                {
                    kindIcons[i] = icon;
                }
                PlayerSettings.SetIcons(iosTarget, kindIcons, kind);
            }
            
            AssetDatabase.SaveAssets();
        }
    }
}
