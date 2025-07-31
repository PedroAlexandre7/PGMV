using System;
using UnityEngine;
public class Cell : MonoBehaviour
{
    public readonly Unit[] units = new Unit[4];
    public CellType cellType;
    public static GameObject cellPrefab;
    public static float cellPrefabSize;

    public void Initialize(CellType cellType)
    {
        this.cellType = cellType;
        name = $"{cellType.ToString()[0]}{cellType.ToString().ToLower()[1..]}";
        GetComponent<Renderer>().material = Resources.Load<Material>("Materials/" + name);
    }
    public void AddUnit(Unit unit)
    {
        units[Array.IndexOf(units, null)] = unit;
        unit.currentCell = this;
        unit.AddPositionToPath(transform.position);
    }

    public void RemoveUnit(Unit unit)
    {
        units[Array.IndexOf(units, unit)] = null;
        unit.currentCell = null;
    }

    public Vector3 UnitLocalPosition(Unit unit)
    {
        return Array.IndexOf(units, unit) switch
        {
            0 => new Vector3(-cellPrefabSize / 4, 0, cellPrefabSize / 4),
            1 => new Vector3(cellPrefabSize / 4, 0, cellPrefabSize / 4),
            2 => new Vector3(-cellPrefabSize / 4, 0, -cellPrefabSize / 4),
            3 => new Vector3(cellPrefabSize / 4, 0, -cellPrefabSize / 4),
            _ => throw new ArgumentException()
        };
    }
}

public enum CellType
{
    DESERT,
    FOREST,
    MOUNTAIN,
    PLAIN,
    SEA,
    VILLAGE
}
