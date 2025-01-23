using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LevelData
{
    private int tileDepthInVertices, tileWidthInVertices;

    public TileData[,] tilesData;

    public LevelData(int tileDepthInVertices, int tileWidthInVertices, int levelDepthInTiles, int levelWidthInTiles)
    {
        tilesData = new TileData[tileDepthInVertices * levelDepthInTiles, tileWidthInVertices * levelWidthInTiles];
        this.tileDepthInVertices = tileDepthInVertices;
        this.tileWidthInVertices = tileWidthInVertices;
    }

    public void AddTileData(TileData tileData, int tileZIndex, int tileXIndex)
    {
        tilesData[tileZIndex, tileXIndex] = tileData;
    }

    public TileCoordinate ConvertToTileCoordinate(int zIndex, int xIndex)
    {
        int tileZIndex = (int)Mathf.Floor ((float)zIndex / tileDepthInVertices);
        int tileXIndex = (int)Mathf.Floor ((float)xIndex / tileWidthInVertices);
        
        int coordinateZIndex = tileDepthInVertices - (zIndex % tileDepthInVertices) - 1;
        int coordinateXIndex = tileWidthInVertices - (xIndex % tileDepthInVertices) - 1;
        TileCoordinate tileCoordinate = new TileCoordinate (tileZIndex, tileXIndex, coordinateZIndex, coordinateXIndex);
        return tileCoordinate;
    }
}

public class TileCoordinate 
{
    public int tileZIndex;
    public int tileXIndex;
    public int coordinateZIndex;
    public int coordinateXIndex;
    public TileCoordinate(int tileZIndex, int tileXIndex, int coordinateZIndex, int coordinateXIndex) {
        this.tileZIndex = tileZIndex;
        this.tileXIndex = tileXIndex;
        this.coordinateZIndex = coordinateZIndex;
        this.coordinateXIndex = coordinateXIndex;
    }
}
public class LevelGeneration : MonoBehaviour
{
    [SerializeField] private int mapWidthInTiles, mapDepthInTiles;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int centerVertexZ = 55;
    [SerializeField] private int maxDistanceZ = 55;
    [SerializeField] private TreeGeneration treeGeneration;
    [SerializeField] private RiverGeneration riverGeneration;

    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        Vector3 tileSize = tilePrefab.GetComponent<MeshRenderer>().bounds.size;
        int tileWidth = (int)tileSize.x;
        int tileDepth = (int)tileSize.z;

        Vector3[] tileMeshVertices = tilePrefab.GetComponent<MeshFilter>().sharedMesh.vertices;
        int tileDepthInVertices = (int)Mathf.Sqrt(tileMeshVertices.Length);
        int tileWidthInVertices = tileDepthInVertices;
        
        float distanceBetweenVertices = (float)tileDepth / (float)tileDepthInVertices;

        LevelData levelData = new LevelData(tileDepthInVertices, tileWidthInVertices, mapDepthInTiles, mapWidthInTiles);
        

        for (int xTileIndex = 0; xTileIndex < mapWidthInTiles; xTileIndex++)
        {
            for (int zTileIndex = 0; zTileIndex < mapDepthInTiles; zTileIndex++)
            {
                Vector3 tilePosition = new Vector3(gameObject.transform.position.x + xTileIndex * tileWidth + 5,
                    gameObject.transform.position.y, gameObject.transform.position.z + zTileIndex * tileDepth + 5);

                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);

                TileData tileData = tile.GetComponent<TileGeneration>().GenerateTile(centerVertexZ, maxDistanceZ);
                levelData.AddTileData(tileData, zTileIndex, xTileIndex);
            }
        }
        treeGeneration.GenerateTrees(mapDepthInTiles * tileDepthInVertices, mapWidthInTiles * tileWidthInVertices, distanceBetweenVertices, levelData);
        riverGeneration.GenerateRivers(mapDepthInTiles * tileDepthInVertices, mapWidthInTiles * tileWidthInVertices, levelData);
    }
}
