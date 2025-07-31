using System.Collections;
using UnityEngine;

public class Mage : Unit
{
    private readonly float HEIGHT_MULTIPLIER = 1f;
    private readonly float DURATION_MULTIPLIER = 3f;

    protected override IEnumerator PerformAttack(Cell cell)
    {
        GameObject fireball = MakeFireball();
        yield return ParabolicMovement(fireball, PositionInBoard(new(-scaleScalar * 7, scaleScalar * 33, scaleScalar * 5)), cell.transform.localPosition, DURATION_MULTIPLIER, HEIGHT_MULTIPLIER);
        Destroy(fireball);

        foreach (Unit unit in cell.units)
            if (unit != null && unit != this)
                yield return unit.Die();

        GameObject MakeFireball()
        {
            GameObject fireball = Instantiate(Resources.Load<GameObject>("Prefabs/Attacks/Eletric"), currentCell.transform.parent);
            fireball.transform.localScale *= currentCell.transform.localScale.x;
            TrailRenderer trail = fireball.GetComponent<TrailRenderer>();
            fireball.GetComponent<MeshRenderer>().material.color = player.color;

            trail.minVertexDistance = fireball.transform.lossyScale.y;
            trail.startWidth *= fireball.transform.lossyScale.y;
            trail.endWidth *= fireball.transform.lossyScale.y;
            trail.time *= fireball.transform.lossyScale.y;

            return fireball;
        }
    }

    protected override IEnumerator CellToCellMovement(Cell cell)
    {
        Vector3 localYMovement = 5 * cell.transform.localScale.x * Vector3.up;
        Coroutine rotateCoroutine = StartCoroutine(LookAt(cell.transform.position));
        yield return LinearMovement(gameObject, transform.localPosition + localYMovement, 0.5f, lockY: false);
        yield return rotateCoroutine;
        yield return LinearMovement(gameObject, cell.transform.localPosition, 1f);
        yield return LinearMovement(gameObject, transform.localPosition - localYMovement, 0.5f, lockY: false);
    }
}
