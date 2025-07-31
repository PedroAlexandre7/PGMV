using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;
using Math = System.Math;
public class Board : MonoBehaviour
{
    [SerializeField] private TextAsset XMLFile;
    [SerializeField] private float horizontalThickness;
    [SerializeField] private float verticalThickness;
    [SerializeField] private float distanceFromTable;
    [SerializeField] private Color player1Color;
    [SerializeField] private Color player2Color;

    private Cell[,] cells;
    private readonly Dictionary<int, Unit> unitDictionary = new();
    private int ongoingActionCount = 0;
    public GameSession gameSession;
    public event System.Action ActionChanged;
    private bool IsCurrentBoard => ActionChanged != null;

    private void Start()
    {
        if (Cell.cellPrefab == null)
        {
            Cell.cellPrefab = Resources.Load<GameObject>("Prefabs/Cell");
            Cell.cellPrefabSize = Cell.cellPrefab.GetComponent<MeshRenderer>().bounds.size.x;
        }
        SetupBoard();
    }

    public void Initialize(TextAsset XMLFile, float horizontalThickness, float verticalThickness, float distanceFromTable, Color player1Color, Color player2Color)
    {
        this.XMLFile = XMLFile;
        this.horizontalThickness = horizontalThickness;
        this.verticalThickness = verticalThickness;
        this.distanceFromTable = distanceFromTable;
        this.player1Color = player1Color;
        this.player2Color = player2Color;
    }

    private void SetupBoard()
    {
        XmlDocument xmlDocument = new();
        xmlDocument.LoadXml(XMLFile.text);
        XmlNode rootNode = xmlDocument.DocumentElement;
        (Player, Player) players = ReadPlayers();
        gameSession = new GameSession(ReadGameName(), players.Item1, players.Item2, ReadTurns());
        CreateBoard(ReadCells());

        string ReadGameName()
        {
            return rootNode.Attributes["name"] != null ? rootNode.Attributes["name"].Value : "Unnamed Game";
        }

        (Player, Player) ReadPlayers()
        {
            XmlNodeList roleNodes = rootNode.SelectNodes("roles/role");
            Player player1 = new(roleNodes.Item(0).Attributes["name"].Value, player1Color);
            Player player2 = new(roleNodes.Item(1).Attributes["name"].Value, player2Color);
            return (player1, player2);
        }

        List<Turn> ReadTurns()
        {
            List<Turn> turns = new();
            foreach (XmlNode turnNode in rootNode.SelectNodes("turns/turn"))
            {
                List<Action> actions = new();
                foreach (XmlNode actionNode in turnNode.SelectNodes("unit"))
                {
                    XmlAttributeCollection attributes = actionNode.Attributes;
                    actions.Add(
                        new Action(
                            int.Parse(attributes["id"].Value),
                            (ActionType)Enum.Parse(typeof(ActionType), attributes["action"].Value.ToUpper()),
                            attributes["type"].Value,
                            attributes["role"].Value == players.Item1.name ? players.Item1 : players.Item2,
                            int.Parse(attributes["x"].Value) - 1,
                            int.Parse(attributes["y"].Value) - 1
                        )
                    );
                }
                turns.Add(new Turn(actions));
            }
            return turns;
        }

        CellType[,] ReadCells()
        {
            XmlNode boardNode = rootNode.SelectSingleNode("board");
            int width = int.Parse(boardNode.Attributes["width"].Value);
            int height = int.Parse(boardNode.Attributes["height"].Value);
            CellType[,] cellTypes = new CellType[height, width];
            int cellIndex = 0;
            foreach (XmlNode cellNode in boardNode.ChildNodes)
            {
                CellType cellType = (CellType)Enum.Parse(typeof(CellType), cellNode.Name.ToUpper());
                cellTypes[cellIndex / width, cellIndex % width] = cellType;
                cellIndex++;
            }
            return cellTypes;
        }

        void CreateBoard(CellType[,] cellTypes)
        {
            int numCellsWidth = cellTypes.GetLength(1);
            int numCellsHeight = cellTypes.GetLength(0);
            float boardCellScale = 1.0f / Math.Max(numCellsWidth, numCellsHeight);
            float boardWidth = Cell.cellPrefabSize * (numCellsWidth < numCellsHeight ? (float)numCellsWidth / numCellsHeight : 1);
            float boardHeight = Cell.cellPrefabSize * (numCellsHeight < numCellsWidth ? (float)numCellsHeight / numCellsWidth : 1);
            Vector3 boardOffset = new((boardHeight - Cell.cellPrefabSize * boardCellScale) / 2.0f, 0, (boardWidth - Cell.cellPrefabSize * boardCellScale) / 2.0f);
            DestroyDummyBoard();
            CreateCells();
            CreateBorder();

            void DestroyDummyBoard()
            {
                Destroy(GetComponent<ProBuilderMesh>());
                Destroy(GetComponent<MeshFilter>());
                Destroy(GetComponent<MeshRenderer>());
            }

            void CreateCells()
            {
                cells = new Cell[numCellsHeight, numCellsWidth];
                for (int i = 0; i < numCellsHeight; i++)
                    for (int j = 0; j < numCellsWidth; j++)
                    {
                        GameObject cell = Instantiate(Cell.cellPrefab, transform);
                        cell.transform.localScale *= boardCellScale;
                        cell.transform.localPosition = new Vector3((float)i / numCellsHeight * boardHeight, 0, (float)j / numCellsWidth * boardWidth) - boardOffset;
                        cells[i, j] = cell.GetComponent<Cell>();
                        cells[i, j].Initialize(cellTypes[i, j]);
                    }
            }

            void CreateBorder()
            {
                GameObject leftBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject bottomBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject rightBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject topBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Material boardMaterial = Resources.Load<Material>("Materials/Board");

                foreach (GameObject border in new[] { topBorder, rightBorder, bottomBorder, leftBorder })
                {
                    border.transform.parent = transform;
                    border.transform.localRotation = Quaternion.identity;
                    border.GetComponent<Renderer>().material = boardMaterial;
                }

                topBorder.name = "TopBorder";
                rightBorder.name = "RightBorder";
                bottomBorder.name = "BottomBorder";
                leftBorder.name = "LeftBorder";

                topBorder.transform.localPosition = new Vector3(-((float)boardHeight / 2 + horizontalThickness / 2), distanceFromTable, 0);
                rightBorder.transform.localPosition = new Vector3(0, distanceFromTable, -((float)boardWidth / 2 + horizontalThickness / 2));
                bottomBorder.transform.localPosition = new Vector3((float)boardHeight / 2 + horizontalThickness / 2, distanceFromTable, 0);
                leftBorder.transform.localPosition = new Vector3(0, distanceFromTable, (float)boardWidth / 2 + horizontalThickness / 2);

                topBorder.transform.localScale = new Vector3(horizontalThickness, verticalThickness, boardWidth + horizontalThickness * 2);
                rightBorder.transform.localScale = new Vector3(boardHeight, verticalThickness, horizontalThickness);
                bottomBorder.transform.localScale = new Vector3(horizontalThickness, verticalThickness, boardWidth + horizontalThickness * 2);
                leftBorder.transform.localScale = new Vector3(boardHeight, verticalThickness, horizontalThickness);
            }
        }
    }

    private Cell CellAt(int x, int y)
    {
        return cells[y, x];
    }

    public void EnableAudioSources(bool isEnabled)
    {
        foreach (Unit unit in unitDictionary.Values)
            unit.GetComponent<AudioSource>().enabled = isEnabled;
    }

    public Soldier FindSoldierAttackingSoldierOnHold()
    {
        foreach (Unit unit in unitDictionary.Values)
            if (unit is Soldier soldier && soldier.isAttackingSoldierOnHold)
                return soldier;
        return null;
    }

    public IEnumerator Play()
    {
        Action action;
        yield return new WaitUntil(() => ongoingActionCount == 0);
        while (!gameSession.GameEnded)
        {
            if (gameSession.IsAtTurnStart)
                ResetSoldierHoldFlags();
            action = gameSession.NextAction();
            if (action.actionType == ActionType.ATTACK)
                yield return StartCoroutine(PerformAllTurnAttacks());
            else
            {
                ActionChanged?.Invoke();
                yield return StartCoroutine(PerformAction());
            }
        }

        IEnumerator PerformAction()
        {
            ongoingActionCount++;
            switch (action.actionType)
            {
                case ActionType.ATTACK:
                    yield return StartCoroutine(unitDictionary[action.unitID].Attack(CellAt(action.x, action.y)));
                    break;
                case ActionType.HOLD:
                    yield return StartCoroutine(unitDictionary[action.unitID].Hold());
                    break;
                case ActionType.MOVE_TO:
                    yield return StartCoroutine(unitDictionary[action.unitID].MoveTo(CellAt(action.x, action.y)));
                    break;
                case ActionType.SPAWN:
                    InstantiateUnitByAction(action, playSpawnAudioClip: true);
                    yield return new WaitForSeconds(1.25f);
                    break;
            }
            ongoingActionCount--;
        }

        IEnumerator PerformAllTurnAttacks()
        {
            ActionChanged?.Invoke();
            List<Coroutine> coroutines = new();
            HashSet<Unit> unitsToKill = new();
            while (true)
            {
                coroutines.Add(StartCoroutine(PerformAction()));
                if (gameSession.IsAtTurnStart)
                    break;
                action = gameSession.NextAction();
            }
            foreach (Coroutine coroutine in coroutines)
                yield return coroutine;
            foreach (Unit unit in unitDictionary.Values)
                if (unit.isDead)
                    unitsToKill.Add(unit);
            foreach (Unit unitToKill in unitsToKill)
            {
                unitToKill.Destroy();
                unitDictionary.Remove(unitToKill.id);
            }
        }

        void ResetSoldierHoldFlags()
        {
            foreach (Unit unit in unitDictionary.Values)
                if (unit is Soldier soldier)
                {
                    soldier.isOnHold = false;
                    soldier.isAttackingSoldierOnHold = false;
                }
        }
    }

    public void Restart()
    {
        StopAllCoroutines();
        foreach (Transform transform in GetComponentsInChildren<Transform>().Where(child => child.CompareTag("AttackObject")))
            Destroy(transform.gameObject);
        foreach (Unit unit in unitDictionary.Values)
            unit.Destroy();
        unitDictionary.Clear();
        gameSession.Restart();
        ongoingActionCount = 0;
        ActionChanged?.Invoke();

    }

    public void PreviousTurn()
    {
        SetBoardToTurnNumber(gameSession.CurrentTurnNumber - 1);
    }

    public void SkipTurn()
    {
        SetBoardToTurnNumber(gameSession.CurrentTurnNumber + 1);
    }

    private void SetBoardToTurnNumber(int turnNumber)
    {
        Restart();
        Action action;
        while (gameSession.CurrentTurnNumber < turnNumber && !gameSession.GameEnded)
        {
            action = gameSession.NextAction();
            switch (action.actionType)
            {
                case ActionType.ATTACK:
                    ProcessAllTurnAttacks();
                    break;
                case ActionType.HOLD:
                    break;
                case ActionType.MOVE_TO:
                    ProcessMovement();
                    break;
                case ActionType.SPAWN:
                    InstantiateUnitByAction(action, playSpawnAudioClip: false);
                    break;
            }
        }
        foreach (Unit unit in unitDictionary.Values)
            unit.SetLocalTransform();
        ActionChanged?.Invoke();

        void ProcessAllTurnAttacks()
        {
            HashSet<Unit> unitsToKill = new();
            while (true)
            {
                Unit[] unitsInAttackedCell = Array.FindAll(CellAt(action.x, action.y).units, unit => unit != null);
                Unit attackingUnit = unitDictionary[action.unitID];
                unitsToKill.AddRange(unitsInAttackedCell.Where(unit => unit != attackingUnit));
                if (gameSession.IsAtTurnStart)
                    break;
                action = gameSession.NextAction();
            }
            foreach (Unit unitToKill in unitsToKill)
            {
                unitToKill.Destroy();
                unitDictionary.Remove(unitToKill.id);
            }
        }

        void ProcessMovement()
        {
            Unit movingUnit = unitDictionary[action.unitID];
            Cell destinationCell = CellAt(action.x, action.y);
            movingUnit.RemoveFromCurrentCell();
            destinationCell.AddUnit(movingUnit);
        }
    }

    private void InstantiateUnitByAction(Action action, bool playSpawnAudioClip)
    {
        GameObject unitPrefab = Instantiate(Resources.Load<GameObject>($"Prefabs/Units/{action.unitType}"));
        Unit unit = unitPrefab.GetComponent<Unit>();
        unit.Initialize(action.unitID, action.player, CellAt(action.x, action.y), isAudioEnabled: IsCurrentBoard);
        unitDictionary[action.unitID] = unit;
        if (playSpawnAudioClip)
            unit.PlayAudioClip("Spawn/Spawn");
    }
}
