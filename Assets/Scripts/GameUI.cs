using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Text gameNameText;
    [SerializeField] private Text VSText;
    [SerializeField] private Text player1Text;
    [SerializeField] private Text player2Text;
    [SerializeField] private Text turnText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button previousTurnButton;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button skipTurnButton;
    [SerializeField] private Button topViewButton;
    [SerializeField] private Button duelViewButton;
    [SerializeField] private Button runButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Image crosshair;
    [SerializeField] private GameObject minimap;
    [SerializeField] private Text errorText;

    private const float ERROR_MESSAGE_DURATION = 2.5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleKeyboardButtonClick(playPauseButton);
            HandleKeyboardButtonClick(jumpButton);
        }
        if (Input.GetKeyDown(KeyCode.R))
            HandleKeyboardButtonClick(restartButton);
        if (Input.GetKeyDown(KeyCode.Q))
            HandleKeyboardButtonClick(previousTurnButton);
        if (Input.GetKeyDown(KeyCode.E))
            HandleKeyboardButtonClick(skipTurnButton);
        if (Input.GetMouseButtonDown(1))
            HandleKeyboardButtonClick(topViewButton);
        if (Input.GetKeyDown(KeyCode.F))
            HandleKeyboardButtonClick(duelViewButton);
        if (Input.GetKey(KeyCode.LeftShift))
            HandleKeyboardButtonClick(runButton);
    }

    public void UpdateBoardUI(GameSession gameSession)
    {
        if (gameSession == null)
        {
            HideBoardUI();
            return;
        }
        ShowBoardUI();
        Action currentAction = gameSession.CurrentAction();
        gameNameText.text = gameSession.gameName;
        VSText.text = "VS";
        player1Text.text = (currentAction.player.Equals(gameSession.player1) ? "→ " : string.Empty) + gameSession.player1.name;
        player1Text.color = gameSession.player1.color;
        player2Text.text = gameSession.player2.name + (currentAction.player.Equals(gameSession.player2) ? " ←" : string.Empty);
        player2Text.color = gameSession.player2.color;
        int currentTurnNumberUI = (GameController.isPlaying && gameSession.IsAtTurnStart) || (!GameController.isPlaying && gameSession.GameEnded)
            ? gameSession.CurrentTurnNumber - 1
            : gameSession.CurrentTurnNumber;
        turnText.text = $"Turn {currentTurnNumberUI}" + $"{(gameSession.GameEnded ? " (Game Ended)" : string.Empty)}";
    }

    public void UpdatePlayPauseButton()
    {
        playPauseButton.GetComponent<Image>().sprite = Resources.Load<Sprite>(GameController.isPlaying ? "UI/Pause" : "UI/Play");
    }

    public void UpdateViewMode()
    {
        bool showTopViewIcon = GameController.viewMode == ViewMode.TOP || GameController.viewModeBeforeDuel == ViewMode.TOP;
        topViewButton.GetComponent<Image>().sprite = Resources.Load<Sprite>(showTopViewIcon ? "UI/Lateral View" : "UI/Top View");
        duelViewButton.GetComponent<Image>().sprite = Resources.Load<Sprite>(GameController.viewMode == ViewMode.DUEL ? "UI/Tavern Mode" : "UI/Duel Mode");
        minimap.SetActive(GameController.viewMode == ViewMode.TOP);
        crosshair.enabled = GameController.viewMode == ViewMode.NORMAL;
        transform.Find("Game Flow Controls").gameObject.SetActive(GameController.viewMode != ViewMode.DUEL);
        transform.Find("Player Soldier Controls").gameObject.SetActive(GameController.viewMode == ViewMode.DUEL);
        if (GameController.viewMode == ViewMode.DUEL)
            HideBoardUI();
    }

    public void DisplayErrorMessage(string message)
    {
        errorText.text = message;
        StopAllCoroutines();
        StartCoroutine(FadeOutMessage());

        IEnumerator FadeOutMessage()
        {
            float startTime = Time.time;
            float currentTime = 0f;

            while (currentTime < ERROR_MESSAGE_DURATION)
            {
                currentTime = Time.time - startTime;
                float t = Mathf.Clamp01(currentTime / ERROR_MESSAGE_DURATION);
                errorText.color = new Color(errorText.color.r, errorText.color.g, errorText.color.b, Mathf.Lerp(1, 0, t));
                yield return null;
            }
        }
    }

    private void HideBoardUI() => transform.Find("Game Session").gameObject.SetActive(false);
    private void ShowBoardUI() => transform.Find("Game Session").gameObject.SetActive(true);

    private void HandleKeyboardButtonClick(Button button)
    {
        if (!button.IsActive())
            return;
        button.onClick.Invoke();
        StartCoroutine(ButtonColorFade(button.colors.normalColor, button.colors.pressedColor, () =>
        {
            StartCoroutine(ButtonColorFade(button.colors.pressedColor, button.colors.normalColor, null));
        }));

        IEnumerator ButtonColorFade(Color startColor, Color targetColor, System.Action onComplete)
        {
            float startTime = Time.time;
            float currentTime = 0f;
            while (currentTime < button.colors.fadeDuration)
            {
                currentTime = Time.time - startTime;
                float t = Mathf.Clamp01(currentTime / button.colors.fadeDuration);
                button.image.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
            onComplete?.Invoke();
        }
    }
}