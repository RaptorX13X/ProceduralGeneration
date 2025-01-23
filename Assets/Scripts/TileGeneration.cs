using System;
using UnityEngine;

[Serializable]
public class TerrainType
{
    public string name;
    public float height;
    public Color color;
    public int index;
}

[Serializable]
public class Biome
{
    public string name;
    public Color color;
    public int index;
}

[Serializable]
public class BiomeRow
{
    public Biome[] biomes;
}

public class TileData
{
    public float[,] heightMap;
    public float[,] heatMap;
    public float[,] moistureMap;
    public TerrainType[,] chosenHeightTerrainTypes;
    public TerrainType[,] chosenHeatTerrainTypes;
    public TerrainType[,] chosenMoistureTerrainTypes;
    public Biome[,] chosenBiomes;
    public Mesh mesh;
    public Texture2D texture;

    public TileData(float[,] heightMap, float[,] heatMap, float[,] moistureMap,
        TerrainType[,] chosenHeightTerrainTypes, TerrainType[,] chosenHeatTerrainTypes,
        TerrainType[,] chosenMoistureTerrainTypes,
        Biome[,] chosenBiomes, Mesh mesh, Texture2D texture)
    {
        this.heightMap = heightMap;
        this.heatMap = heatMap;
        this.moistureMap = moistureMap;
        this.chosenHeightTerrainTypes = chosenHeightTerrainTypes;
        this.chosenHeatTerrainTypes = chosenHeatTerrainTypes;
        this.chosenMoistureTerrainTypes = chosenMoistureTerrainTypes;
        this.chosenBiomes = chosenBiomes;
        this.mesh = mesh;
        this.texture = texture;
    }
}
public class TileGeneration : MonoBehaviour
{
    [SerializeField] private TerrainType[] heightTerrainTypes;
    [SerializeField] private TerrainType[] heatTerrainTypes;
    [SerializeField] private TerrainType[] moistureTerrainTypes;
    [SerializeField] private NoiseMapGeneration noiseMapGeneration;
    [SerializeField] private MeshRenderer tileRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private float mapScale;
    [SerializeField] private float heightMultiplier;
    [SerializeField] private AnimationCurve heightCurve;
    [SerializeField] private AnimationCurve heatCurve;
    [SerializeField] private AnimationCurve moistureCurve;
    [SerializeField] private Wave[] heightWaves;
    [SerializeField] private Wave[] heatWaves;
    [SerializeField] private Wave[] moistureWaves;
    [SerializeField] private VisualizationMode visualizationMode;
    [SerializeField] private BiomeRow[] biomes;

    // private void Start()
    // {
    //     GenerateTile(55, 55); // magic numbers go
    // }

    public TileData GenerateTile(float centerVertexZ, float maxDistanceZ)  // nagle nie private void?
    {
        Vector3[] meshVertices = meshFilter.mesh.vertices;
        int tileDepth = (int)Mathf.Sqrt(meshVertices.Length);
        int tileWidth = tileDepth;

        float offsetX = -gameObject.transform.position.x;
        float offsetZ = -gameObject.transform.position.z;

        float[,] heightMap = noiseMapGeneration.GeneratePerlinNoiseMap(tileDepth, tileWidth, mapScale, offsetX, offsetZ, heightWaves);

        Vector3 tileDimensions = meshFilter.mesh.bounds.size;
        float distanceBetweenVertices = tileDimensions.z / (float)tileDepth;
        float vertexOffsetZ = gameObject.transform.position.z / distanceBetweenVertices;

        float[,] uniformHeatMap =
            noiseMapGeneration.GenerateUniformNoiseMap(tileDepth, tileWidth, centerVertexZ, maxDistanceZ,
                vertexOffsetZ);
        float[,] randomHeatMap =
            noiseMapGeneration.GeneratePerlinNoiseMap(tileDepth, tileWidth, mapScale, offsetX, offsetZ, heatWaves);
        float[,] heatMap = new float[tileDepth, tileWidth];
        for (int zIndex = 0; zIndex < tileDepth; zIndex++)
        {
            for (int xIndex = 0; xIndex < tileWidth; xIndex++)
            {
                heatMap[zIndex, xIndex] = uniformHeatMap[zIndex, xIndex] * randomHeatMap[zIndex, xIndex];
                heatMap[zIndex, xIndex] += heatCurve.Evaluate(heightMap[zIndex, xIndex]) * heightMap[zIndex, xIndex];
            }
        }
        
        float[,] moistureMap =
            noiseMapGeneration.GeneratePerlinNoiseMap(tileDepth, tileWidth, mapScale, offsetX, offsetZ, moistureWaves);
        for (int zIndex = 0; zIndex < tileDepth; zIndex++)
        {
            for (int xIndex = 0; xIndex < tileWidth; xIndex++)
            {
                moistureMap[zIndex, xIndex] -=
                    moistureCurve.Evaluate(heightMap[zIndex, xIndex]) * heightMap[zIndex, xIndex];
            }
        }

        TerrainType[,] chosenHeightTerrainTypes = new TerrainType[tileDepth, tileWidth];
        Texture2D heightTexture = BuildTexture(heightMap, heightTerrainTypes, chosenHeightTerrainTypes);
        TerrainType[,] chosenHeatTerrainTypes = new TerrainType[tileDepth, tileWidth];
        Texture2D heatTexture = BuildTexture(heatMap, heatTerrainTypes, chosenHeatTerrainTypes);
        TerrainType[,] chosenMoistureTerrainTypes = new TerrainType[tileDepth, tileWidth];
        Texture2D moistureTexture = BuildTexture(moistureMap, moistureTerrainTypes, chosenMoistureTerrainTypes);

        Biome[,] chosenBiomes = new Biome[tileDepth, tileWidth];
        Texture2D biomeTexture =
            BuildBiomeTexture(chosenHeightTerrainTypes, chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes);

        switch (visualizationMode)
        {
            case VisualizationMode.Height:
                tileRenderer.material.mainTexture = heightTexture;
                break;
            case VisualizationMode.Heat:
                tileRenderer.material.mainTexture = heatTexture;
                break;
            case VisualizationMode.Moisture:
                tileRenderer.material.mainTexture = moistureTexture;
                break;
            case VisualizationMode.Biome:
                tileRenderer.material.mainTexture = biomeTexture;
                break;
        }
        
        UpdateMeshVertices(heightMap);

        TileData tileData = new TileData(heightMap, heatMap, moistureMap, chosenHeightTerrainTypes,
            chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes, meshFilter.mesh, biomeTexture);

        return tileData;
    }
    
    enum VisualizationMode {Height, Heat, Moisture, Biome}

    private void UpdateMeshVertices(float[,] heightMap)
    {
        int tileDepth = heightMap.GetLength(0);
        int tileWidth = heightMap.GetLength(1);

        Vector3[] meshVertices = meshFilter.mesh.vertices;

        int vertexIndex = 0;
        for (int zIndex = 0; zIndex < tileDepth; zIndex++)
        {
            for (int xIndex = 0; xIndex < tileWidth; xIndex++)
            {
                float height = heightMap[zIndex, xIndex];
                Vector3 vertex = meshVertices[vertexIndex];
                meshVertices[vertexIndex] = new Vector3(vertex.x, heightCurve.Evaluate(height) * heightMultiplier, vertex.z);
                vertexIndex++;
            }
        }

        meshFilter.mesh.vertices = meshVertices;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();

        meshCollider.sharedMesh = meshFilter.mesh;
    }

    private Texture2D BuildTexture(float[,] heightMap, TerrainType[] terrainTypes, TerrainType[,] chosenTerrainTypes)
    {
        int tileDepth = heightMap.GetLength(0);
        int tileWidth = heightMap.GetLength(1);

        Color[] colorMap = new Color[tileDepth * tileWidth];
        for (int zIndex = 0; zIndex < tileDepth; zIndex++)
        {
            for (int xIndex = 0; xIndex < tileWidth; xIndex++)
            {
                int colorIndex = zIndex * tileWidth + xIndex;
                float height = heightMap[zIndex, xIndex];
                TerrainType terrainType = ChooseTerrainType(height, terrainTypes);
                colorMap[colorIndex] = terrainType.color;
                chosenTerrainTypes[zIndex, xIndex] = terrainType;
            }
        }

        Texture2D tileTexture = new Texture2D(tileWidth, tileDepth);
        tileTexture.wrapMode = TextureWrapMode.Clamp;
        tileTexture.SetPixels(colorMap);
        tileTexture.Apply();

        return tileTexture;
    }

    private TerrainType ChooseTerrainType(float height, TerrainType[] terrainTypes)
    {
        foreach (TerrainType terrainType in terrainTypes)
        {
            if (height < terrainType.height)
            {
                return terrainType;
            }
        }

        return terrainTypes[terrainTypes.Length - 1];
    }

    private Texture2D BuildBiomeTexture(TerrainType[,] heightTerrainTypes, TerrainType[,] heatTerrainTypes,
        TerrainType[,] moistureTerrainTypes, Biome[,] chosenBiomes)
    {
        int tileDepth = heatTerrainTypes.GetLength(0);
        int tileWidth = heatTerrainTypes.GetLength(1);

        Color[] colorMap = new Color[tileDepth * tileWidth];
        for (int zIndex = 0; zIndex < tileDepth; zIndex++)
        {
            for (int xIndex = 0; xIndex < tileWidth; xIndex++)
            {
                int colorIndex = zIndex * tileWidth + xIndex;

                TerrainType heightTerrainType = heightTerrainTypes[zIndex, xIndex];
                if (heightTerrainType.name != "water")
                {
                    TerrainType heatTerrainType = heatTerrainTypes[zIndex, xIndex];
                    TerrainType moistureTerrainType = moistureTerrainTypes[zIndex, xIndex];

                    Biome biome = biomes[moistureTerrainType.index].biomes[heatTerrainType.index];
                    colorMap[colorIndex] = biome.color;

                    chosenBiomes[zIndex, xIndex] = biome;
                }
                else
                {
                    colorMap[colorIndex] = Color.blue;
                }
            }
        }

        Texture2D tileTexture = new Texture2D(tileWidth, tileDepth);
        tileTexture.filterMode = FilterMode.Point;
        tileTexture.wrapMode = TextureWrapMode.Clamp;
        tileTexture.SetPixels(colorMap);
        tileTexture.Apply();

        return tileTexture;
    }
}
