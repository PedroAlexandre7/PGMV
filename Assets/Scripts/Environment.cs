using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

public class Environment
{
    public Dictionary<CellType, TerrainTypeProperties> environmentDictionary = new();

    public void SetupEnvironment(TextAsset XMLFile)
    {
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(XMLFile.text);
        XmlNodeList squareNodes = xmlDoc.SelectNodes("//environment/square");

        foreach (XmlNode squareNode in squareNodes)
        {
            if (squareNode.Attributes["type"] == null || squareNode.Attributes["maximum_elevation"] == null)
            {
                Debug.LogError("Square node missing 'type' or 'maximum_elevation' attribute.");
                continue;
            }

            CellType cellType;
            try
            {
                cellType = (CellType)System.Enum.Parse(typeof(CellType), squareNode.Attributes["type"].Value.ToUpper());
            }
            catch
            {
                Debug.LogError($"Invalid cell type: {squareNode.Attributes["type"].Value}");
                continue;
            }

            if (!float.TryParse(squareNode.Attributes["maximum_elevation"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float maximumElevation))
            {
                Debug.LogError($"Invalid maximum elevation: {squareNode.Attributes["maximum_elevation"].Value}");
                continue;
            }

            List<TerrainObjectProperties> terrainObjects = new();
            XmlNodeList objectNodes = squareNode.SelectNodes("object");
            foreach (XmlNode objectNode in objectNodes)
            {
                if (objectNode.Attributes["type"] == null ||
                    objectNode.Attributes["density_low_altitude"] == null ||
                    objectNode.Attributes["density_high_altitude"] == null)
                {

                    continue;
                }

                string objectType = objectNode.Attributes["type"].Value;
                string rawDensityLowAltitude = objectNode.Attributes["density_low_altitude"].Value;
                string rawDensityHighAltitude = objectNode.Attributes["density_high_altitude"].Value;

                if (!float.TryParse(rawDensityLowAltitude, NumberStyles.Float, CultureInfo.InvariantCulture, out float densityLowAltitude) ||
                    !float.TryParse(rawDensityHighAltitude, NumberStyles.Float, CultureInfo.InvariantCulture, out float densityHighAltitude))
                {
                    continue;
                }

                GameObject gameObject = Resources.Load<GameObject>($"Prefabs/{objectType}");

                TerrainObjectProperties terrainObject = new(
                    gameObject,
                    densityLowAltitude,
                    densityHighAltitude
                );

                terrainObjects.Add(terrainObject);
            }

            TerrainTypeProperties terrainType = new(maximumElevation, terrainObjects);

            environmentDictionary[cellType] = terrainType;
        }
    }
}

public readonly struct TerrainTypeProperties
{
    public readonly float maximumAltitude;
    public readonly List<TerrainObjectProperties> terrainObjects;

    public TerrainTypeProperties(float maximumAltitude, List<TerrainObjectProperties> terrainObjects)
    {
        this.maximumAltitude = maximumAltitude;
        this.terrainObjects = terrainObjects;
    }
}

public readonly struct TerrainObjectProperties
{
    public readonly GameObject gameObject;
    public readonly float densityLowAltitude;
    public readonly float densityHighAltitude;

    public TerrainObjectProperties(GameObject gameObject, float densityLowAltitude, float densityHighAltitude)
    {
        this.gameObject = gameObject;
        this.densityLowAltitude = densityLowAltitude;
        this.densityHighAltitude = densityHighAltitude;
    }
}
