using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 20;
    public int mapHeight = 20;
    public float gridSize = 1f;

    [Header("Noise Settings")]
    public float noiseScale = 5f;
    public int seed = 42;

    [Header("Thresholds")]
    [Range(0f, 1f)] public float waterThreshold = 0.35f;
    [Range(0f, 1f)] public float mountainThreshold = 0.7f;

    [Header("Tile Prefabs")]
    public GameObject waterPrefab;
    public GameObject landPrefab;

    [Header("Mountain Variants")]
    public GameObject[] mountainPrefabs;
    [Tooltip("Lower = larger clusters of same variant, Higher = more variety per tile")]
    public float mountainVariantScale = 3f;

    [Header("Nature Assets")]
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;
    public GameObject[] grassPrefabs;

    [Header("Nature Density")]
    [Range(0f, 1f)] public float treeDensity = 0.3f;
    [Range(0f, 1f)] public float bushDensity = 0.4f;
    [Range(0f, 1f)] public float grassDensity = 0.5f;

    [Tooltip("Max random XZ offset of nature assets within a tile")]
    public float natureSpawnRadius = 0.3f;

    [Header("References")]
    public Transform mapParent;

    private GameObject[,] tiles;

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        ClearMap();

        tiles = new GameObject[mapWidth, mapHeight];

        Vector2 terrainOffset  = GetNoiseOffset(seed);
        Vector2 variantOffset  = GetNoiseOffset(seed + 1);
        Vector2 treeOffset     = GetNoiseOffset(seed + 2);
        Vector2 bushOffset     = GetNoiseOffset(seed + 3);
        Vector2 grassOffset    = GetNoiseOffset(seed + 4);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                float terrainNoise = SampleNoise(x, z, terrainOffset, noiseScale);
                GameObject prefab = GetTilePrefab(terrainNoise, x, z, variantOffset);

                if (prefab == null) continue;

                Vector3 position = GetHexPosition(x, z);
                GameObject tile = Instantiate(prefab, position, Quaternion.identity);
                tile.transform.Rotate(0, 90, 0);
                tile.transform.SetParent(mapParent);
                tile.name = $"Tile_{x}_{z}";

                tiles[x, z] = tile;

                // Spawn nature assets on land tiles only
                bool isLand = terrainNoise >= waterThreshold && terrainNoise < mountainThreshold;
                if (isLand)
                {
                    TrySpawnNature(treePrefabs,  treeOffset,  treeDensity,  position, tile.transform, x, z);
                    TrySpawnNature(bushPrefabs,  bushOffset,  bushDensity,  position, tile.transform, x, z);
                    TrySpawnNature(grassPrefabs, grassOffset, grassDensity, position, tile.transform, x, z);
                }
            }
        }
    }

    void TrySpawnNature(GameObject[] prefabs, Vector2 noiseOffset, float density, Vector3 tilePosition, Transform parent, int x, int z)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        float noiseValue = SampleNoise(x, z, noiseOffset, noiseScale);
        if (noiseValue > density) return;

        // Pick a random variant
        int index = Random.Range(0, prefabs.Length);
        GameObject prefab = prefabs[index];
        if (prefab == null) return;

        // Random offset 
        Vector2 randomOffset = Random.insideUnitCircle * natureSpawnRadius;
        Vector3 spawnPosition = tilePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);

        // Random Y rotation 
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject asset = Instantiate(prefab, spawnPosition, rotation);
        asset.transform.SetParent(parent);
        asset.name = $"{prefab.name}_{x}_{z}";
    }

    void ClearMap()
    {
        if (tiles == null) return;

        foreach (GameObject tile in tiles)
        {
            if (tile != null)
                Destroy(tile);
        }
    }

    Vector2 GetNoiseOffset(int offsetSeed)
    {
        Random.InitState(offsetSeed);
        return new Vector2(Random.Range(0f, 9999f), Random.Range(0f, 9999f));
    }

    float SampleNoise(int x, int z, Vector2 offset, float scale)
    {
        float sampleX = (x + offset.x) / scale;
        float sampleZ = (z + offset.y) / scale;
        return Mathf.PerlinNoise(sampleX, sampleZ);
    }

    GameObject GetTilePrefab(float terrainNoise, int x, int z, Vector2 variantOffset)
    {
        if (terrainNoise < waterThreshold)
            return waterPrefab;

        if (terrainNoise >= mountainThreshold)
            return GetMountainVariant(x, z, variantOffset);

        return landPrefab;
    }

    GameObject GetMountainVariant(int x, int z, Vector2 variantOffset)
    {
        if (mountainPrefabs == null || mountainPrefabs.Length == 0)
            return null;

        float variantNoise = SampleNoise(x, z, variantOffset, mountainVariantScale);
        int index = Mathf.FloorToInt(variantNoise * mountainPrefabs.Length);
        index = Mathf.Clamp(index, 0, mountainPrefabs.Length - 1);

        return mountainPrefabs[index];
    }

    Vector3 GetHexPosition(int x, int z)
    {
        bool shouldOffset = (x % 2) == 0;

        float width = 2f * gridSize;
        float height = Mathf.Sqrt(3f) * gridSize;

        float horizontalDistance = width * (3f / 4f);
        float verticalDistance = height;

        float offset = shouldOffset ? height / 2f : 0f;
        float xPosition = x * horizontalDistance;
        float zPosition = (z * verticalDistance) - offset;

        // Calculate center offset so map is centered on origin
        float centerX = (mapWidth - 1) * horizontalDistance / 2f;
        float centerZ = (mapHeight - 1) * verticalDistance / 2f;

        return new Vector3(xPosition - centerX, 0, -zPosition + centerZ);
    }
}