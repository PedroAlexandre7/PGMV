using System.Collections;
using UnityEngine;
public class Soldier : Unit
{

    private readonly float SWING_DURATION = 0.6f;
    private GameObject prefabSword;
    public bool isOnHold;
    public bool isAttackingSoldierOnHold;

    private void Awake()
    {
        prefabSword = FindPrefabSword();
    }

    protected override IEnumerator PerformAttack(Cell cell)
    {
        CheckForSoldiersOnHold();
        GameObject sword = MakeSword();
        prefabSword.SetActive(false);
        yield return SwingSword(sword);
        Destroy(sword);
        prefabSword.SetActive(true);
        foreach (Unit unit in cell.units)
            if (unit != null && unit != this)
                yield return unit.Die();

        GameObject MakeSword()
        {
            GameObject sword = Instantiate(Resources.Load<GameObject>("Prefabs/Attacks/Sword"), transform);
            sword.tag = "AttackObject";
            sword.transform.SetLocalPositionAndRotation(new(0, unitPrefabSize.y * 0.4f, 0), Quaternion.Euler(new(90, 10, 0)));
            return sword;
        }

        IEnumerator SwingSword(GameObject sword)
        {
            Quaternion startRotation = sword.transform.rotation;
            yield return LinearRotation(sword, startRotation, startRotation * Quaternion.Euler(0, 0, -80), SWING_DURATION);
        }

        void CheckForSoldiersOnHold()
        {
            foreach (Unit unit in cell.units)
                if (unit is Soldier soldier && soldier.isOnHold)
                    isAttackingSoldierOnHold = true;
        }
    }

    private GameObject FindPrefabSword()
    {
        static GameObject FindObjectWithSwordTag(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.CompareTag("Sword"))
                    return child.gameObject;
                GameObject foundChild = FindObjectWithSwordTag(child);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }
        return FindObjectWithSwordTag(transform);
    }

    public override IEnumerator Hold()
    {
        isOnHold = true;
        return base.Hold();
    }

    protected override IEnumerator CellToCellMovement(Cell cell)
    {
        yield return LookAt(cell.transform.position);
        yield return LinearMovement(gameObject, cell.transform.localPosition, 2f);
    }
}
