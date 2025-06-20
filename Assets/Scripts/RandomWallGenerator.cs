using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RandomWallGenerator : MonoBehaviour
{
    [Header("Wall Size (World Units)")]
    public float width = 10f;
    public float height = 10f;
    public float depth = 1f;

    [Header("Wall Resolution (Vertices)")]
    public int widthSegments = 10;
    public int heightSegments = 10;

    [Header("Center Mesh Around Origin")]
    public bool centerX = true;
    public bool centerY = true;
    public bool centerZ = false;

    [Header("Noise Settings")]
    public float noiseScale = 5f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 noiseOffset;

    [Header("Seed Settings")]
    public int seed = 0;
    public bool useRandomSeed = true;

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

    public void GenerateWallMesh()
    {
        if (useRandomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);

        Random.InitState(seed);
        noiseOffset = new Vector2(Random.value * 1000f, Random.value * 1000f);

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float dx = width / widthSegments;
        float dy = height / heightSegments;

        float offsetX = centerX ? -width / 2f : 0f;
        float offsetY = centerY ? -height / 2f : 0f;

        for (int y = 0; y <= heightSegments; y++)
        {
            for (int x = 0; x <= widthSegments; x++)
            {
                float posX = x * dx + offsetX;
                float posY = y * dy + offsetY;
                float z = GenerateNoise(x, y);
                float posZ = centerZ ? z - depth / 2f : z;

                vertices.Add(new Vector3(posX, posY, posZ));
                uvs.Add(new Vector2((float)x / widthSegments, (float)y / heightSegments));
            }
        }

        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int i = y * (widthSegments + 1) + x;

                triangles.Add(i);
                triangles.Add(i + widthSegments + 1);
                triangles.Add(i + 1);

                triangles.Add(i + 1);
                triangles.Add(i + widthSegments + 1);
                triangles.Add(i + widthSegments + 2);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        GeneratePrefabs();
    }

    float GenerateNoise(int x, int y)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + noiseOffset.x) / noiseScale * frequency;
            float sampleY = (y + noiseOffset.y) / noiseScale * frequency;
            float perlin = Mathf.PerlinNoise(sampleX, sampleY);

            noiseHeight += perlin * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight * depth;
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
