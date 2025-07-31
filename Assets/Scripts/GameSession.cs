using System;
using System.Collections.Generic;

public class GameSession
{
    public readonly string gameName;
    public readonly Player player1;
    public readonly Player player2;
    private readonly List<Turn> turns;
    private int currentTurnIndex = 0;
    private int nextActionIndex = 0;
    public int CurrentTurnNumber => currentTurnIndex + 1;
    public bool IsAtTurnStart => nextActionIndex == 0;
    public bool GameEnded => CurrentTurnNumber == turns.Count + 1;

    public GameSession(string gameName, Player player1, Player player2, List<Turn> turns)
    {
        this.gameName = gameName;
        this.player1 = player1;
        this.player2 = player2;
        this.turns = turns;
    }

    public Action CurrentAction()
    {
        if (IsAtTurnStart)
            return currentTurnIndex == 0 ? turns[0][0] : turns[currentTurnIndex - 1][^1];
        return turns[currentTurnIndex][nextActionIndex - 1];
    }

    public Action NextAction()
    {
        if (GameEnded)
            throw new InvalidOperationException("Game ended.");
        Action nextAction = turns[currentTurnIndex][nextActionIndex];
        nextActionIndex++;
        if (nextActionIndex == turns[currentTurnIndex].Count)
        {
            currentTurnIndex++;
            nextActionIndex = 0;
        }
        return nextAction;
    }

    public void Restart()
    {
        currentTurnIndex = 0;
        nextActionIndex = 0;
    }
}