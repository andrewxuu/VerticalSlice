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
    public float mountainVariantScale = 3f;

    [Header("Nature Assets")]
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;
    public GameObject[] grassPrefabs;

    [Header("Nature Density")]
    [Range(0f, 1f)] public float treeDensity = 0.3f;
    [Range(0f, 1f)] public float bushDensity = 0.4f;
    [Range(0f, 1f)] public float grassDensity = 0.5f;

    public float natureSpawnRadius = 0.3f;

    [Header("Boundary Walls")]
    public float wallHeight = 5f;
    public float wallThickness = 1f;
    public Material wallMaterial;

    [Header("References")]
    public Transform mapParent;

    private GameObject[,] tiles;
    private GameObject wallParent;

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        ClearMap();

        tiles = new GameObject[mapWidth, mapHeight];

        Vector2 terrainOffset = GetNoiseOffset(seed);
        Vector2 variantOffset = GetNoiseOffset(seed + 1);
        Vector2 treeOffset    = GetNoiseOffset(seed + 2);
        Vector2 bushOffset    = GetNoiseOffset(seed + 3);
        Vector2 grassOffset   = GetNoiseOffset(seed + 4);

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

                bool isLand = terrainNoise >= waterThreshold && terrainNoise < mountainThreshold;
                if (isLand)
                {
                    TrySpawnNature(treePrefabs,  treeOffset,  treeDensity,  position, tile.transform, x, z);
                    TrySpawnNature(bushPrefabs,  bushOffset,  bushDensity,  position, tile.transform, x, z);
                    TrySpawnNature(grassPrefabs, grassOffset, grassDensity, position, tile.transform, x, z);
                }
            }
        }

        BuildWalls();
    }

    void BuildWalls()
    {
        if (wallParent != null)
            Destroy(wallParent);

        wallParent = new GameObject("BoundaryWalls");
        wallParent.transform.SetParent(transform);

        float hexWidth  = 2f * gridSize;
        float hexHeight = Mathf.Sqrt(3f) * gridSize;

        float horizontalDistance = hexWidth * (3f / 4f);
        float verticalDistance   = hexHeight;

        float halfX = (mapWidth  - 1) * horizontalDistance / 2f;
        float halfZ = (mapHeight - 1) * verticalDistance   / 2f;

        float padX = hexWidth  / 2f + wallThickness / 2f;
        float padZ = hexHeight / 2f + wallThickness / 2f;

        float totalX = halfX * 2f + padX * 2f;
        float totalZ = halfZ * 2f + padZ * 2f;
        float wallY  = wallHeight / 2f;

        SpawnWall("Wall_North", new Vector3(0f,             wallY, -(halfZ + padZ)), new Vector3(totalX,        wallHeight, wallThickness));
        SpawnWall("Wall_South", new Vector3(0f,             wallY,  (halfZ + padZ)), new Vector3(totalX,        wallHeight, wallThickness));
        SpawnWall("Wall_East",  new Vector3( (halfX + padX), wallY, 0f),             new Vector3(wallThickness, wallHeight, totalZ));
        SpawnWall("Wall_West",  new Vector3(-(halfX + padX), wallY, 0f),             new Vector3(wallThickness, wallHeight, totalZ));
    }

    void SpawnWall(string wallName, Vector3 position, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(wallParent.transform);
        wall.transform.position = position;
        wall.transform.localScale = size;

        if (wallMaterial != null)
        {
            wall.GetComponent<Renderer>().material = wallMaterial;
        }
        else
        {
            Material transparentMat = new Material(Shader.Find("Standard"));
            transparentMat.SetFloat("_Mode", 3);
            transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMat.SetInt("_ZWrite", 0);
            transparentMat.DisableKeyword("_ALPHATEST_ON");
            transparentMat.EnableKeyword("_ALPHABLEND_ON");
            transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMat.renderQueue = 3000;
            transparentMat.color = new Color(0.8f, 0.9f, 1f, 0.15f);
            wall.GetComponent<Renderer>().material = transparentMat;
        }
    }

    void TrySpawnNature(GameObject[] prefabs, Vector2 noiseOffset, float density, Vector3 tilePosition, Transform parent, int x, int z)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        float noiseValue = SampleNoise(x, z, noiseOffset, noiseScale);
        if (noiseValue > density) return;

        int index = Random.Range(0, prefabs.Length);
        GameObject prefab = prefabs[index];
        if (prefab == null) return;

        Vector2 randomOffset = Random.insideUnitCircle * natureSpawnRadius;
        Vector3 spawnPosition = tilePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);

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

        float width  = 2f * gridSize;
        float height = Mathf.Sqrt(3f) * gridSize;

        float horizontalDistance = width * (3f / 4f);
        float verticalDistance   = height;

        float offset    = shouldOffset ? height / 2f : 0f;
        float xPosition = x * horizontalDistance;
        float zPosition = (z * verticalDistance) - offset;

        float centerX = (mapWidth  - 1) * horizontalDistance / 2f;
        float centerZ = (mapHeight - 1) * verticalDistance   / 2f;

        return new Vector3(xPosition - centerX, 0, -zPosition + centerZ);
    }
}