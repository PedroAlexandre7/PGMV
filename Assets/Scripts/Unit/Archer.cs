using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : Unit
{
    private readonly float HEIGHT_MULTIPLIER = 1.5f;
    private readonly float DURATION_MULTIPLIER = 1f;

    protected override IEnumerator PerformAttack(Cell cell)
    {
        List<Coroutine> coroutines = new();
        int count = 0;

        foreach (Unit unit in cell.units)
            if (unit != null && unit != this)
            {
                count++;
                coroutines.Add(StartCoroutine(ShootArrow(unit)));
                yield return new WaitForSeconds(0.5f);
            }
        foreach (Coroutine coroutine in coroutines)
            yield return coroutine;
        if (count == 0)
            yield return ShootArrow(cell.transform.localPosition);
    }

    private IEnumerator ShootArrow(Unit unit)
    {
        yield return ShootArrow(unit.PositionInBoard(new(0, scaleScalar * 16.5f, 0)));
        yield return unit.Die();
    }

    private IEnumerator ShootArrow(Vector3 position)
    {
        GameObject arrow = MakeArrow();
        yield return ParabolicMovement(arrow, PositionInBoard(new(-scaleScalar * 4.5f, scaleScalar * 23, scaleScalar * 8.5f)), position, DURATION_MULTIPLIER, HEIGHT_MULTIPLIER);
        Destroy(arrow);
        yield return null;

        GameObject MakeArrow()
        {
            GameObject arrow = Instantiate(Resources.Load<GameObject>("Prefabs/Attacks/Arrow"), currentCell.transform.parent);
            arrow.transform.localScale *= currentCell.transform.localScale.x;
            TrailRenderer trail = arrow.GetComponent<TrailRenderer>();
            trail.material.color = player.color;
            trail.minVertexDistance *= arrow.transform.lossyScale.y;
            trail.startWidth *= arrow.transform.lossyScale.y;
            trail.endWidth *= arrow.transform.lossyScale.y;
            trail.time *= arrow.transform.lossyScale.y;
            return arrow;
        }
    }

    protected override IEnumerator CellToCellMovement(Cell cell)
    {
        yield return LookAt(cell.transform.position);
        yield return LinearMovement(gameObject, cell.transform.localPosition, 2f);
    }
}
