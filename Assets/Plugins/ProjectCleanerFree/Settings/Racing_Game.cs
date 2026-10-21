#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class Startup
{
    static Startup()    
    {
        EditorPrefs.SetInt("showCounts_pcf", EditorPrefs.GetInt("showCounts_pcf") + 1);

        if (EditorPrefs.GetInt("showCounts_pcf") == 1)       
        {        
                Application.OpenURL("https://assetstore.unity.com/publishers/23606");
        }
    }     
}
#endif
