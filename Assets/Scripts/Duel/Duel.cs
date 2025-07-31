using UnityEngine;

public class Duel : MonoBehaviour
{

    [SerializeField] private TerrainGenerator terrainGenerator;
    [SerializeField] private Transform playerSoldierSpawnPoint;
    public GameObject playerSoldier;


    public void GenerateDuelTerrain(CellType cellType)
    {
        
        terrainGenerator.BuildDuelArena(cellType);
    }

    public void StartDuel()
    {
        playerSoldier = Instantiate(Resources.Load<GameObject>($"Prefabs/Player Soldier"), playerSoldierSpawnPoint);
        DuelController.instance.RestartDuel(); //restart because it starts on start automatically.
    }

    public void DestroyPlayer()
    {
        Destroy(playerSoldier);
    }

}
