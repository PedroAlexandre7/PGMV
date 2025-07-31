using System;
using System.Collections;
using UnityEngine;

public class Catapult : Unit
{

    private readonly float HEIGHT_MULTIPLIER = 3f;
    private readonly float DURATION_MULTIPLIER = 3f;

    protected override IEnumerator PerformAttack(Cell cell)
    {
        GameObject boulder = MakeBoulder();
        yield return ParabolicMovement(boulder, PositionInBoard(new(scaleScalar * 6.5f, scaleScalar * 3, 0)), cell.transform.localPosition, DURATION_MULTIPLIER, HEIGHT_MULTIPLIER);
        Destroy(boulder);
        foreach (Unit unit in cell.units)
            if (unit != null && unit != this)
                yield return unit.Die();

        GameObject MakeBoulder()
        {
            GameObject boulder = Instantiate(Resources.Load<GameObject>("Prefabs/Attacks/Boulder"), currentCell.transform.parent);
            boulder.transform.localScale *= currentCell.transform.localScale.x;
            boulder.GetComponentInChildren<MeshRenderer>().material.color = (player.color * 1f);
            return boulder;
        }
    }

    public override IEnumerator MoveTo(Cell cell)
    {
        throw new NotSupportedException("Catapults can't move.");
    }

    protected override IEnumerator CellToCellMovement(Cell cell)
    {
        throw new NotSupportedException("Catapults can't move.");
    }

    protected override IEnumerator LieDown(float duration)
    {
        yield return null;
    }
}
