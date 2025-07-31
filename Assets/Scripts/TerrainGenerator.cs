using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Environment XML File")]
    [SerializeField] private TextAsset environmentXMLFile;

    [Header("Terrain Properties")]

    [SerializeField] private float scale;
    [SerializeField] private float flattenRadius;
    [SerializeField] private float flattenHeight;
    [SerializeField] private float smoothRadius;
    private float depth;
    private float offsetX;
    private float offsetY;
    private readonly float DEPTH_SCALAR = 0.2f;

    [Header("Terrain Objects Properties")]
    [SerializeField] private TerrainLayer rockLayer;
    [SerializeField] private TerrainLayer grassLayer;
    [SerializeField] private TerrainLayer forestLayer;
    [SerializeField] private TerrainLayer desertLayer;
    [SerializeField] private TerrainLayer villageLayer;
    [SerializeField] private TerrainLayer waterLayer;

    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject housePrefab;

    private const int WIDTH = 128;
    private const int HEIGHT = 128;
    private const float LOW_ALTITUDE_THRESHOLD = 0.2f;
    private const float HIGH_ALTITUDE_THRESHOLD = 0.8f;
    private readonly Environment environment = new();
    private readonly List<Vector3> occupiedPositions = new();

    private Terrain terrain;

    private void Start()
    {
        terrain = GetComponent<Terrain>();
        environment.SetupEnvironment(environmentXMLFile);

    }
    public void BuildDuelArena(CellType cellType)
    {
        CleanPreviousTerrainChildren();

        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);
        depth = (environment.environmentDictionary[cellType].maximumAltitude) * DEPTH_SCALAR;

        terrain.terrainData = GenerateTerrain(terrain.terrainData);
        ApplyTerrainLayers(cellType);
        ApplyTexture(terrain.terrainData);
        PlaceObjects(cellType);

    }

    private void CleanPreviousTerrainChildren()
    {
        foreach (var item in terrain.GetComponentsInChildren<Transform>())
        {
            if (item.gameObject != terrain.gameObject)
                Destroy(item.gameObject);
        }
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = WIDTH + 1;
        terrainData.size = new Vector3(WIDTH, depth, HEIGHT);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[WIDTH, HEIGHT];
        int centerX = WIDTH / 2;
        int centerY = HEIGHT / 2;

        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                float distanceToCenter = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));

                if (distanceToCenter < flattenRadius)
                    heights[x, y] = 0;
                else if (distanceToCenter < flattenRadius + smoothRadius)
                {
                    float t = (distanceToCenter - flattenRadius) / smoothRadius;
                    t = Mathf.SmoothStep(0, 1, t);
                    float perlinHeight = CalculateHeight(x, y);
                    heights[x, y] = Mathf.Lerp(flattenHeight / depth, perlinHeight, t);
                }
                else
                    heights[x, y] = CalculateHeight(x, y);

            }
        }
        return heights;
    }

    float CalculateHeight(int x, int y)
    {
        float xCoord = (float)x / WIDTH * scale + offsetX;
        float yCoord = (float)y / HEIGHT * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }

    void ApplyTerrainLayers(CellType cellType)
    {
        TerrainData terrainData = terrain.terrainData;

        switch (cellType)
        {
            case CellType.DESERT:
                terrainData.terrainLayers = new TerrainLayer[] { desertLayer, waterLayer };
                break;

            case CellType.FOREST:
                terrainData.terrainLayers = new TerrainLayer[] { forestLayer, rockLayer };
                break;

            case CellType.MOUNTAIN:
                terrainData.terrainLayers = new TerrainLayer[] { rockLayer, forestLayer };
                break;

            case CellType.PLAIN:
                terrainData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer };
                break;

            case CellType.VILLAGE:
                terrainData.terrainLayers = new TerrainLayer[] { villageLayer, rockLayer };
                break;

            default:
                Debug.LogWarning($"Cell type {cellType} does not exist.");
                break;
        }
    }

    void ApplyTexture(TerrainData terrainData)
    {
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;

        float[,,] splatmapData = new float[alphaMapWidth, alphaMapHeight, 2];
        int centerX = WIDTH / 2;
        int centerY = HEIGHT / 2;

        for (int x = 0; x < alphaMapWidth; x++)
        {
            for (int y = 0; y < alphaMapHeight; y++)
            {
                float normX = (float)x / (alphaMapWidth - 1);
                float normY = (float)y / (alphaMapHeight - 1);

                float terrainX = normX * WIDTH;
                float terrainY = normY * HEIGHT;

                float distanceToCenter = Mathf.Sqrt((terrainX - centerX) * (terrainX - centerX) + (terrainY - centerY) * (terrainY - centerY));

                if (distanceToCenter < flattenRadius)
                {
                    splatmapData[x, y, 0] = 0;
                    splatmapData[x, y, 1] = 1;
                }
                else if (distanceToCenter < flattenRadius + smoothRadius)
                {
                    float t = (distanceToCenter - flattenRadius) / smoothRadius;
                    t = Mathf.SmoothStep(0, 1, t);

                    splatmapData[x, y, 0] = t;
                    splatmapData[x, y, 1] = 1 - t;
                }
                else
                {
                    splatmapData[x, y, 0] = 1;
                    splatmapData[x, y, 1] = 0;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
    private void PlaceObjects(CellType cellType)
    {
        environment.environmentDictionary.TryGetValue(cellType, out TerrainTypeProperties terrainProperties);

        foreach (var terrainObject in terrainProperties.terrainObjects)
        {
            if (terrainObject.gameObject == rockPrefab)
                PlaceRocks(terrainObject.densityLowAltitude, terrainObject.densityHighAltitude);

            else if (terrainObject.gameObject == treePrefab)
                PlaceTrees(terrainObject.densityLowAltitude, terrainObject.densityHighAltitude);

            else if (terrainObject.gameObject == housePrefab)
                PlaceHouses(terrainObject.densityLowAltitude, terrainObject.densityHighAltitude);
        }
    }

    void PlaceRocks(float densityLow, float densityHigh)
    {
        TerrainData terrainData = terrain.terrainData;
        float maxHeight = terrainData.size.y;

        for (int x = 0; x < WIDTH; x++)
        {
            for (int z = 0; z < HEIGHT; z++)
            {
                float y = terrainData.GetHeight(x, z);

                if (y >= 0 && y <= LOW_ALTITUDE_THRESHOLD * maxHeight && Random.value < densityLow
                    || y >= HIGH_ALTITUDE_THRESHOLD * maxHeight && y <= maxHeight && Random.value < densityHigh)
                {
                    SpawnRock(terrain, new(x, y, z));
                }
            }
        }
    }

    private void SpawnRock(Terrain terrain, Vector3 position)
    {
        position = terrain.transform.TransformPoint(position);

        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        GameObject rockInstance = Instantiate(rockPrefab, position, randomRotation);

        rockInstance.transform.parent = terrain.transform;
        float randomScale = Random.Range(0.07f, 0.15f);
        rockInstance.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

        occupiedPositions.Add(position);
    }

    void PlaceTrees(float densityLow, float densityHigh)
    {
        TerrainData terrainData = terrain.terrainData;
        float maxHeight = terrainData.size.y;
        int centerX = WIDTH / 2;
        int centerY = HEIGHT / 2;
        for (int x = 0; x < WIDTH; x++)
        {
            for (int z = 0; z < HEIGHT; z++)
            {
                float y = terrainData.GetHeight(x, z);
                if (Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerY) * (z - centerY)) < flattenRadius + smoothRadius) continue;

                if ((y >= 0 && y <= LOW_ALTITUDE_THRESHOLD * maxHeight && Random.value < densityLow) ||
                    (y >= HIGH_ALTITUDE_THRESHOLD * maxHeight && y <= maxHeight && Random.value < densityHigh))
                    SpawnObjectInTerrain(treePrefab, new(x, y, z), Quaternion.Euler(0, Random.Range(0, 360), 0), Random.Range(0.1f, 0.3f));

            }
        }
    }

    private void SpawnObjectInTerrain(GameObject objectPrefab, Vector3 position, Quaternion rotation, float scale)
    {
        position = terrain.transform.TransformPoint(position);

        if (IsPositionOccupied(position))
        {
            return;
        }

        GameObject objectInstance = Instantiate(objectPrefab, position, rotation);

        objectInstance.transform.parent = terrain.transform;
        objectInstance.transform.localScale = new Vector3(scale, scale, scale);

        occupiedPositions.Add(position);
    }

    void PlaceHouses(float densityLow, float densityHigh)
    {
        TerrainData terrainData = terrain.terrainData;
        float maxHeight = terrainData.size.y;
        int centerX = WIDTH / 2;
        int centerY = HEIGHT / 2;
        for (int x = 0; x < WIDTH; x++)
        {
            for (int z = 0; z < HEIGHT; z++)
            {
                float y = terrainData.GetHeight(x, z);

                if (Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerY) * (z - centerY)) < flattenRadius + smoothRadius) continue;

                if ((y >= 0 && y <= LOW_ALTITUDE_THRESHOLD * maxHeight && Random.value < densityLow) ||
                    (y >= HIGH_ALTITUDE_THRESHOLD * maxHeight && y <= maxHeight && Random.value < densityHigh))
                    SpawnObjectInTerrain(housePrefab, new(x, y, z), Quaternion.Euler(-90, 0, Random.Range(0, 360)), Random.Range(0.35f, 0.45f));
            }
        }
    }

    bool IsPositionOccupied(Vector3 position)
    {
        float minDistance = 4f; // Minimum distance between objects
        foreach (Vector3 occupiedPosition in occupiedPositions)
        {
            if (Vector3.Distance(position, occupiedPosition) < minDistance)
            {
                return true;
            }
        }
        return false;
    }
}
