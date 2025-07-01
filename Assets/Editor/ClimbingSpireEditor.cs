using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClimbingSpire))]
public class ClimbingSpireEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ClimbingSpire spire = (ClimbingSpire)target;

        if (GUILayout.Button("Generate Spire"))
        {
            spire.GenerateSpire();
        }

        if (GUILayout.Button("Generate Prefabs"))
        {
            spire.GeneratePrefabs();
        }

        if (GUILayout.Button("Destroy Prefabs"))
        {
            spire.DestroyAllChildren();
        }
    }
}
