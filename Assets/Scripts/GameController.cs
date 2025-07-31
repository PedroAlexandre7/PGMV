using Cinemachine;
using System.Linq;
using UnityEngine;
public class GameController : MonoBehaviour
{
    [SerializeField] private float boardDetectionAngle;
    [SerializeField] private float boardDetectionDistance;
    private Board[] boards;
    private Board currentBoard;
    private Duel duel;
    private GameUI gameUI;
    private Transform previousCameraTransform;
    private CinemachineVirtualCamera cinemachineCamera;
    public static Camera mainCamera;
    public static bool isPlaying = false;
    public static ViewMode viewMode = ViewMode.NORMAL;
    public static ViewMode viewModeBeforeDuel = ViewMode.NORMAL;
    private const float TOP_VIEW_DISTANCE = 8.5f;

    private void Start()
    {
        TableSpawner[] tableSpawners = FindObjectsOfType<TableSpawner>();
        foreach (var tableSpawner in tableSpawners)
            tableSpawner.Initialize();

        boards = FindObjectsOfType<Board>();
        duel = FindObjectOfType<Duel>();
        gameUI = FindObjectOfType<GameUI>();
        mainCamera = Camera.main;
        cinemachineCamera = FindObjectOfType<CinemachineVirtualCamera>();
        previousCameraTransform = transform;
        gameUI.UpdatePlayPauseButton();
        gameUI.UpdateViewMode();
    }

    private void Update()
    {
        Board newCurrentBoard = CalculateCurrentBoard();
        //Debug.Log("current board: " + newCurrentBoard);
        if (newCurrentBoard == currentBoard)
            return;
        if (currentBoard != null)
        {
            currentBoard.ActionChanged -= UpdateBoardUI;
            currentBoard.EnableAudioSources(false);
        }
        currentBoard = newCurrentBoard;
        if (currentBoard != null)
        {
            currentBoard.ActionChanged += UpdateBoardUI;
            currentBoard.EnableAudioSources(true);
        }
        UpdateBoardUI();
    }

    private void OnDestroy()
    {
        if (currentBoard != null)
            currentBoard.ActionChanged -= UpdateBoardUI;
    }
    private void UpdateBoardUI() => gameUI.UpdateBoardUI(currentBoard == null ? null : currentBoard.gameSession);

    private Board CalculateCurrentBoard(bool useDetectionLimits = true)
    {
        Board currentBoard = null;
        float lowestDetectionAngle = useDetectionLimits ? boardDetectionAngle : float.MaxValue;
        foreach (Board board in boards)
        {
            Vector3 cameraToBoardVector = board.transform.position - mainCamera.transform.position;
            if (cameraToBoardVector.magnitude > boardDetectionDistance && useDetectionLimits)
                continue;
            float angleToBoard = Vector3.Angle(mainCamera.transform.forward, cameraToBoardVector);
            if (angleToBoard < lowestDetectionAngle)
            {
                lowestDetectionAngle = angleToBoard;
                currentBoard = board;
            }
        }
        return currentBoard;
    }

    public void TogglePlayPause()
    {
        isPlaying = !isPlaying;
        gameUI.UpdatePlayPauseButton();
        if (isPlaying)
            Play();
        else
            Pause();
    }

    private void Play()
    {
        foreach (Board board in boards)
            board.StartCoroutine(nameof(Board.Play));
    }

    private void Pause()
    {
        foreach (Board board in boards)
            board.StopCoroutine(nameof(Board.Play));
    }

    public void Restart()
    {
        foreach (Board board in boards)
            board.Restart();
        if (isPlaying)
            Play();
    }

    public void PreviousTurn()
    {
        foreach (Board board in boards)
            board.PreviousTurn();
        if (isPlaying)
            Play();
    }

    public void SkipTurn()
    {
        foreach (Board board in boards)
            board.SkipTurn();
        if (isPlaying)
            Play();
    }

    public void ToggleDuelView()
    {

        if (viewMode == ViewMode.DUEL)
        {
            viewMode = viewModeBeforeDuel;
            gameUI.UpdateViewMode();
            cinemachineCamera.enabled = false;
            RestoreCameraTransform();
            duel.DestroyPlayer();
            return;
        }

        Soldier soldierAttackingSoldierOnHold = FindSoldierAttackingSoldierOnHold();
        if (soldierAttackingSoldierOnHold == null)
        {
            gameUI.DisplayErrorMessage("A soldier must be attacking another soldier on hold.");
            return;
        }

        viewModeBeforeDuel = viewMode;
        viewMode = ViewMode.DUEL;
        gameUI.UpdateViewMode();
        SaveCameraTransform();
        duel.GenerateDuelTerrain(soldierAttackingSoldierOnHold.currentCell.cellType);
        duel.StartDuel();
        SetCameraToDuelView();
        if (isPlaying)
            TogglePlayPause();

        Soldier FindSoldierAttackingSoldierOnHold()
        {
            foreach (Board board in boards)
            {
                Soldier soldier = board.FindSoldierAttackingSoldierOnHold();
                if (soldier != null)
                    return soldier;
            }
            return null;
        }

        void SetCameraToDuelView()
        {

            cinemachineCamera.Follow = duel.playerSoldier.transform;
            cinemachineCamera.LookAt = duel.playerSoldier.transform;
            cinemachineCamera.enabled = true;
        }
    }

    public void ToggleTopView()
    {
        if (viewMode == ViewMode.DUEL)
            return;
        viewMode = viewMode == ViewMode.NORMAL ? ViewMode.TOP : ViewMode.NORMAL;
        gameUI.UpdateViewMode();
        if (viewMode == ViewMode.TOP)
        {
            SaveCameraTransform();
            SetCameraToTopView();
            UpdateBoardUI();
        }
        else
        {
            mainCamera.transform.SetParent(null, false);
            RestoreCameraTransform();
        }

        void SetCameraToTopView()
        {
            if (currentBoard == null)
                currentBoard = CalculateCurrentBoard(useDetectionLimits: false);
            mainCamera.transform.SetParent(currentBoard.transform, false);
            mainCamera.transform.localPosition = new Vector3(0, TOP_VIEW_DISTANCE, 0);
            mainCamera.transform.LookAt(currentBoard.transform.position, -currentBoard.transform.right);

        }
    }

    private void SaveCameraTransform()
    {
        previousCameraTransform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
    }

    private void RestoreCameraTransform()
    {
        mainCamera.transform.SetPositionAndRotation(previousCameraTransform.position, previousCameraTransform.rotation);
    }
}

public enum ViewMode
{
    NORMAL,
    TOP,
    DUEL
}