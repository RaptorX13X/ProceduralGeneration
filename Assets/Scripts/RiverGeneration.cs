using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RiverGeneration : MonoBehaviour
{
    [SerializeField] private int numberOfRivers;
    [SerializeField] private float heightThreshold;
    [SerializeField] private Color riverColor;

    public void GenerateRivers(int levelDepth, int levelWidth, LevelData levelData)
    {
        for (int riverIndex = 0; riverIndex < numberOfRivers; riverIndex++)
        {
            Vector3 riverOrigin = ChooseRiverOrigin(levelDepth, levelWidth, levelData);
            
            BuildRiver (levelDepth, levelWidth, riverOrigin, levelData);
        }
    }

    private Vector3 ChooseRiverOrigin(int levelDepth, int levelWidth, LevelData levelData)
    {
        bool found = false;
        int randomZIndex = 0;
        int randomXIndex = 0;

        while (!found)
        {
            randomZIndex = Random.Range (0, levelDepth);
            randomXIndex = Random.Range (0, levelWidth);
            
            TileCoordinate tileCoordinate = levelData.ConvertToTileCoordinate (randomZIndex, randomXIndex);
            TileData tileData = levelData.tilesData [tileCoordinate.tileZIndex, tileCoordinate.tileXIndex];
            
            float heightValue = tileData.heightMap [tileCoordinate.coordinateZIndex, tileCoordinate.coordinateXIndex];

            if (heightValue >= heightThreshold)
            {
                found = true;
            }
        }

        return new Vector3(randomXIndex, 0, randomZIndex);
    }

    private void BuildRiver(int levelDepth, int levelWidth, Vector3 riverOrigin, LevelData levelData)
    {
        HashSet<Vector3> visitedCoordinates = new HashSet<Vector3>();
        
        Vector3 currentCoordinate = riverOrigin;
        bool foundWater = false;

        while (!foundWater)
        {
            TileCoordinate tileCoordinate = levelData.ConvertToTileCoordinate ((int)currentCoordinate.z, (int)currentCoordinate.x);
            TileData tileData = levelData.tilesData [tileCoordinate.tileZIndex, tileCoordinate.tileXIndex];
            
            visitedCoordinates.Add (currentCoordinate);

            if (tileData.chosenHeightTerrainTypes[tileCoordinate.coordinateZIndex, tileCoordinate.coordinateXIndex]
                    .name == "water")
            {
                foundWater = true;
            }
            else
            {
                tileData.texture.SetPixel (tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex, this.riverColor);
                tileData.texture.Apply ();
                List<Vector3> neighbors = new List<Vector3> ();

                if (currentCoordinate.z > 0)
                {
                    neighbors.Add(new Vector3 (currentCoordinate.x, 0, currentCoordinate.z - 1));
                }
                if (currentCoordinate.z < levelDepth - 1)
                {
                    neighbors.Add(new Vector3 (currentCoordinate.x, 0, currentCoordinate.z + 1));
                }
                if (currentCoordinate.x > 0)
                {
                    neighbors.Add(new Vector3 (currentCoordinate.x - 1, 0, currentCoordinate.z));
                }
                if (currentCoordinate.x < levelWidth - 1)
                {
                    neighbors.Add(new Vector3 (currentCoordinate.x + 1, 0, currentCoordinate.z));
                }
                
                float minHeight = float.MaxValue;
                Vector3 minNeighbor = new Vector3(0, 0, 0);

                foreach (Vector3 neighbor in neighbors)
                {
                    TileCoordinate neighborTileCoordinate = levelData.ConvertToTileCoordinate ((int)neighbor.z, (int)neighbor.x);
                    TileData neighborTileData = levelData.tilesData [neighborTileCoordinate.tileZIndex, neighborTileCoordinate.tileXIndex];
                    
                    float neighborHeight = tileData.heightMap [neighborTileCoordinate.coordinateZIndex, neighborTileCoordinate.coordinateXIndex];

                    if (neighborHeight < minHeight && !visitedCoordinates.Contains(neighbor))
                    {
                        minHeight = neighborHeight;
                        minNeighbor = neighbor;
                    }
                }
                currentCoordinate = minNeighbor;
            }
        }
    }
}
