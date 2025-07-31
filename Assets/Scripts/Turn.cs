using System.Collections.Generic;
public readonly struct Turn
{
    public readonly List<Action> actions;
    public Turn(List<Action> actions)
    {
        this.actions = actions;
        this.actions.Sort((a1, a2) =>
        {
            if (a1.actionType == ActionType.ATTACK && a2.actionType != ActionType.ATTACK) return 1;
            else if (a1.actionType != ActionType.ATTACK && a2.actionType == ActionType.ATTACK) return -1;
            else return 0;
        });
    }
    public int Count => actions.Count;
    public Action this[int index]
    {
        get
        {
            return actions[index];
        }
    }
}
public readonly struct Action
{
    public readonly int unitID;
    public readonly ActionType actionType;
    public readonly string unitType;
    public readonly Player player;
    public readonly int x;
    public readonly int y;
    public Action(int unitID, ActionType actionType, string unitType, Player player, int x, int y)
    {
        this.unitID = unitID;
        this.actionType = actionType;
        this.unitType = unitType;
        this.player = player;
        this.x = x;
        this.y = y;
    }
}

public enum ActionType
{
    ATTACK,
    HOLD,
    MOVE_TO,
    SPAWN
}

