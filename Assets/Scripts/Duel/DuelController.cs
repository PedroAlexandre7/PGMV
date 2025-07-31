using System.Collections;
using UnityEngine;

public class DuelController : MonoBehaviour
{
    public static DuelController instance;

    private Animator attackerAnimator;
    private Animator defenderAnimator;
    private Vector3 attackerStartPosition;
    private Vector3 defenderStartPosition;

    private readonly float DELAY_BETWEEN_STRIKE_AND_DEATH = 0.5f;

    private DuelAction idle;
    private DuelAction strike;
    private DuelAction dodge;
    private DuelAction block;
    private DuelAction die;
    private DuelAction celebrate;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Animator attackerAnimator = GameObject.FindWithTag("AttackingSoldier").GetComponent<Animator>();
        Animator defenderAnimator = GameObject.FindWithTag("HoldingSoldier").GetComponent<Animator>();

        StartNewDuel(attackerAnimator, defenderAnimator);
    }

    public void RestartDuel()
    {
        StopAllCoroutines();
        attackerAnimator.transform.localPosition = attackerStartPosition;
        defenderAnimator.transform.localPosition = defenderStartPosition;
        StartCoroutine(Duel());
    }

    public void StartNewDuel(Animator attackerAnimator, Animator defenderAnimator)
    {
        StopAllCoroutines();
        this.attackerAnimator = attackerAnimator;
        this.defenderAnimator = defenderAnimator;
        attackerStartPosition = attackerAnimator.transform.localPosition;
        defenderStartPosition = defenderAnimator.transform.localPosition;
        StartCoroutine(Duel());
    }

    private void SetupDuel()
    {
        idle = new(Animator.StringToHash("Idle"), 1f);
        strike = new(Animator.StringToHash("Strike"), 1.5f);
        dodge = new(Animator.StringToHash("Dodge"), 2.63f);
        block = new(Animator.StringToHash("Block"), 1.33f);
        die = new(Animator.StringToHash("Die"), 2.6f);
        celebrate = new(Animator.StringToHash("Celebrate"), 3f);
        if (attackerAnimator == null || defenderAnimator == null)
            throw new MissingReferenceException("at least one of the soldiers is missing");
        attackerAnimator.SetTrigger("Withdraw");
        defenderAnimator.SetTrigger("Withdraw");
    }

    private IEnumerator Duel()
    {
        SetupDuel();

        Animator actor;
        Animator reactor;
        DuelAction actorAction;
        DuelAction reactorAction;
        float waitTime;
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            actor = GetActor();
            reactor = GetReactor(actor);

            actorAction = GetActorAction();
            reactorAction = GetReactorAction(reactor, actorAction);

            waitTime = GetWaitTime(actorAction, reactorAction);

            StartDuelActionAnimation(actor, actorAction);
            if (reactorAction == die)
                yield return new WaitForSeconds(DELAY_BETWEEN_STRIKE_AND_DEATH);
            StartDuelActionAnimation(reactor, reactorAction);

            yield return new WaitForSeconds(waitTime);

            if (reactorAction == die)
            {
                StartDuelActionAnimation(actor, celebrate);
                break;
            }
        }

        yield return null;
    }

    private float GetWaitTime(DuelAction actorAction, DuelAction reactorAction)
    {
        if (actorAction == idle)
            return 1f;

        if (reactorAction == die)
            return actorAction.time - DELAY_BETWEEN_STRIKE_AND_DEATH;
        return Mathf.Max(actorAction.time, reactorAction.time);
    }

    private void StartDuelActionAnimation(Animator actor, DuelAction actorAction)
    {
        actor.SetTrigger(actorAction.hash);
        if (actorAction == dodge)
        {
            StartCoroutine(OnDodge(actor));
        }
    }

    private IEnumerator OnDodge(Animator actor)
    {

        Vector3 startPosition = actor.transform.localPosition;
        yield return new WaitForSeconds(0.1f);
        yield return Unit.LinearMovement(actor.gameObject, startPosition + actor.transform.rotation * new Vector3(0, 0, -2.091191f), 0.75f);
        yield return new WaitForSeconds(0.6833f);
        yield return Unit.LinearMovement(actor.gameObject, startPosition, 0.6f);
    }

    private DuelAction GetReactorAction(Animator reactor, DuelAction actorAction)
    {
        if (actorAction == idle)
            return idle;

        switch (Random.Range(0, reactor == defenderAnimator ? 3 : 2))
        {
            case 0:
                return block;
            case 1:
                return dodge;
            case 2:
                return die;
        }
        Debug.Log("GetReactorAction switch failed, setting to idle");
        return idle;
    }

    private DuelAction GetActorAction()
    {
        if (Random.value <= 0.5)
            return idle;
        else
            return strike;
    }

    private Animator GetReactor(Animator actor)
    {
        if (actor == attackerAnimator)
            return defenderAnimator;
        else
            return attackerAnimator;
    }

    private Animator GetActor()
    {
        if (Random.value <= 0.5)
            return attackerAnimator;
        else
            return defenderAnimator;
    }
}
