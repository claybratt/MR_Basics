using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ClimbingSpire : MonoBehaviour
{
    [Header("Geometry Settings")]
    public float radius = 1f;
    public float height = 2f;
    [Range(3, 128)] public int radialSegments = 24;
    [Range(1, 128)] public int heightSegments = 8;

    [Header("Noise Settings")]
    [Range(0, 10)] public float noiseAmplitude = 0.2f;
    [Range(0, 100)] public float noiseScale = 1f;
    [Range(1, 5)] public int octaves = 4;
    [Range(0, 1)] public float persistence = 0.5f;
    [Range(0, 5)] public float lacunarity = 2f;

    [System.Serializable]
    public struct PrefabSpawnData
    {
        public GameObject prefab;
        [Range(0, 1)] public float spawnRatio;
        public int maxToSpawn;
        public float normalOffset; // Offset from the surface normal for spawning
    }

    [Header("Prefab Placement")]
    public List<PrefabSpawnData> prefabsToSpawn = new List<PrefabSpawnData>();

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public void GenerateSpire()
    {
        Mesh mesh = new Mesh();
        //mesh.name = "Climbing Spire Mesh";

        int sideVertCount = radialSegments * (heightSegments + 1);
        int capCenterCount = 2;
        int capRimCount = radialSegments * 2;
        int totalVertCount = sideVertCount + capCenterCount + capRimCount;

        Vector3[] vertices = new Vector3[totalVertCount];
        Vector2[] uvs = new Vector2[totalVertCount];
        int[] triangles = new int[(radialSegments * heightSegments * 6) + (radialSegments * 3 * 2)];

        int vertIndex = 0;
        int triIndex = 0;

        // Side vertices
        for (int y = 0; y <= heightSegments; y++)
        {
            float v = (float)y / heightSegments;
            float yPos = v * height;

            for (int i = 0; i < radialSegments; i++)
            {
                float u = (float)i / radialSegments;
                float wrappedU = (i == radialSegments - 1) ? 0f : u;
                float mirroredU = wrappedU <= 0.5f ? wrappedU : 1f - wrappedU;

                float angle = u * Mathf.PI * 2;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);

                // Layered Perlin noise
                float frequency = noiseScale;
                float amplitude = 1f;
                float totalNoise = 0f;
                float maxAmplitude = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float perlinValue = Mathf.PerlinNoise(mirroredU * frequency, v * frequency);
                    totalNoise += perlinValue * amplitude;
                    maxAmplitude += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                float normalizedNoise = totalNoise / maxAmplitude;
                float displacedRadius = radius + normalizedNoise * noiseAmplitude;

                Vector3 vertex = new Vector3(x * displacedRadius, yPos, z * displacedRadius);
                vertices[vertIndex] = vertex;
                uvs[vertIndex] = new Vector2(u, v);
                vertIndex++;
            }
        }

        // Side triangles
        for (int y = 0; y < heightSegments; y++)
        {
            for (int i = 0; i < radialSegments; i++)
            {
                int current = y * radialSegments + i;
                int next = y * radialSegments + (i + 1) % radialSegments;
                int currentAbove = current + radialSegments;
                int nextAbove = next + radialSegments;

                triangles[triIndex++] = current;
                triangles[triIndex++] = currentAbove;
                triangles[triIndex++] = next;

                triangles[triIndex++] = next;
                triangles[triIndex++] = currentAbove;
                triangles[triIndex++] = nextAbove;
            }
        }

        // Cap centers
        int bottomCenterIndex = vertIndex++;
        vertices[bottomCenterIndex] = new Vector3(0, 0, 0);

        int topCenterIndex = vertIndex++;
        vertices[topCenterIndex] = new Vector3(0, height, 0);

        // Bottom rim
        for (int i = 0; i < radialSegments; i++)
        {
            Vector3 v = vertices[i];
            vertices[vertIndex++] = new Vector3(v.x, 0, v.z);
        }

        // Top rim
        int topStart = radialSegments * heightSegments;
        for (int i = 0; i < radialSegments; i++)
        {
            Vector3 v = vertices[topStart + i];
            vertices[vertIndex++] = new Vector3(v.x, height, v.z);
        }

        // Bottom cap triangles
        int bottomRimStart = sideVertCount + 2;
        for (int i = 0; i < radialSegments; i++)
        {
            int next = (i + 1) % radialSegments;
            triangles[triIndex++] = bottomRimStart + i;
            triangles[triIndex++] = bottomRimStart + next;
            triangles[triIndex++] = bottomCenterIndex;
        }

        // Top cap triangles
        int topRimStart = bottomRimStart + radialSegments;
        for (int i = 0; i < radialSegments; i++)
        {
            int next = (i + 1) % radialSegments;
            triangles[triIndex++] = topRimStart + next;
            triangles[triIndex++] = topRimStart + i;
            triangles[triIndex++] = topCenterIndex;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    public void SetDepth(float value)
    {
        noiseAmplitude = value * 10;
    }

    public void SetNoiseScale(float value)
    {
        noiseScale = value * 100;
    }
    public void SetPersistence(float value)
    {
        persistence = value / 2;
    }
    public void SetLacunarity(float value)
    {
        lacunarity = value * 10; // Prevent division by zero
    }

    public void GeneratePrefabs()
    {
        Mesh mesh = meshFilter.sharedMesh;
        DestroyAllChildren();

        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0)
            return;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        foreach (var spawnData in prefabsToSpawn)
        {
            if (spawnData.prefab == null || spawnData.maxToSpawn <= 0)
                continue;

            int toSpawn = Mathf.RoundToInt(spawnData.spawnRatio * spawnData.maxToSpawn);
            for (int i = 0; i < toSpawn; i++)
            {
                int index = Random.Range(0, vertices.Length);
                Vector3 localPos = vertices[index];
                Vector3 worldPos = transform.TransformPoint(localPos);
                worldPos += normals[index] * spawnData.normalOffset; // Offset from the surface normal

                Vector3 normal = normals[index];
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

                Instantiate(spawnData.prefab, worldPos, rotation, transform);
            }
        }
    }
    public void SetPrefabNumber(int index, float input)
    {
        if (index < 0 || index >= prefabsToSpawn.Count)
        {
            Debug.LogWarning("Index out of range");
            return;
        }

        PrefabSpawnData data = prefabsToSpawn[index];       // Get a copy
        data.spawnRatio = Mathf.Clamp01(input);             // Modify the copy
        prefabsToSpawn[index] = data;                       // Assign it back
    }
    public void SetPrefabNumberZero(float input)
    {
        SetPrefabNumber(0, input);
    }
    public void SetPrefabNumberOne(float input)
    {
        SetPrefabNumber(1, input);
    }
    public void SetPrefabNumberTwo(float input)
    {
        SetPrefabNumber(2, input);
    }
    public void SetPrefabNumberThree(float input)
    {
        SetPrefabNumber(3, input);
    }
    public void SetPrefabNumberFour(float input)
    {
        SetPrefabNumber(4, input);
    }
    public void DestroyAllChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }
    }
}
