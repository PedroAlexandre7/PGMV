using System;
using UnityEngine;

public class TableSpawner : MonoBehaviour
{
    [Header("Board game")]
    [SerializeField] private TextAsset XMLFile;
    [Header("Board border")]
    [SerializeField] private float horizontalThickness;
    [SerializeField] private float verticalThickness;
    [SerializeField] private float distanceFromTable;
    [Header("Player properties")]
    [SerializeField] private Color player1Color;
    [SerializeField] private Color player2Color;

    public void Initialize()
    {//
        GameObject table = Instantiate(Resources.Load<GameObject>("Prefabs/Game Table"), transform);
        if (XMLFile == null)
            throw new ArgumentNullException("Empty Field", "No XMLFile was given to the spawner");
        gameObject.GetComponentInChildren<Camera>().orthographicSize *= Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        table.GetComponentInChildren<Board>().Initialize(XMLFile, horizontalThickness, verticalThickness, distanceFromTable, player1Color, player2Color);

    }
}
