using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomWallGenerator))]
public class RandomWallGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RandomWallGenerator wallGen = (RandomWallGenerator)target;

        if (GUILayout.Button("Regenerate Wall Mesh"))
        {
            wallGen.GenerateWallMesh();
        }

        if (GUILayout.Button("Generate Prefabs"))
        {
            wallGen.GeneratePrefabs();
        }

        if (GUILayout.Button("Destroy Prefabs"))
        {
            wallGen.DestroyAllChildren();
        }
    }
}
