using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.ParticleSystem;
public abstract class Unit : MonoBehaviour
{
    public int id;
    public Cell currentCell;
    public bool isDead;
    protected Player player;
    protected Vector3 unitPrefabSize;
    protected float scaleScalar;
    private float minimapIconYRotation;

    private const float UNIT_TO_CELL_SIZE_RATIO = 0.225f;
    private const float GHOST_FADE_OUT_DURATION = 8f;
    private const float DEATH_ANIMATION_DURATION = 2f;
    private const float LIE_DOWN_ROTATION_DEGREES = 80f;
    private const float PATH_WIDTH = 0.25f;

    private const float PARABOLA_STANDARD_HEIGHT = 6;
    private const float PARABOLA_STANDARD_DURATION = 1.3f;
    private const float PARABOLA_HEIGHT_POWER = 1.5f;
    private const float PARABOLA_HEIGHT_SCALAR = 0.1f;
    private const float PARABOLA_DURATION_SCALAR = 0.23f;
    private const float ROTATION_DURATION_SCALAR = 0.03f;

    public void Initialize(int id, Player player, Cell cell, bool isAudioEnabled)
    {
        this.id = id;
        this.player = player;
        scaleScalar = cell.transform.localScale.x;
        unitPrefabSize = GetComponent<Collider>().bounds.size;
        minimapIconYRotation = cell.transform.parent.rotation.eulerAngles.y - 90;
        SetupLineRenderer(cell);
        cell.AddUnit(this);
        GetComponent<AudioSource>().enabled = isAudioEnabled;
        SetLocalTransform();
        SetStandColor();
        SetMinimapIconColor();
        UpdateMinimapIconYRotation();

        void SetupLineRenderer(Cell cell)
        {
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = cell.transform.lossyScale.x * Cell.cellPrefabSize * PATH_WIDTH;
            EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
            EventTrigger.Entry onPointerEnter = new() { eventID = EventTriggerType.PointerEnter };
            EventTrigger.Entry onPointerExit = new() { eventID = EventTriggerType.PointerExit };
            onPointerEnter.callback.AddListener((eventData) => { lineRenderer.enabled = true; });
            onPointerExit.callback.AddListener((eventData) => { lineRenderer.enabled = false; });
            eventTrigger.triggers.Add(onPointerEnter);
            eventTrigger.triggers.Add(onPointerExit);
        }

        void SetStandColor()
        {
            MeshRenderer standMeshRenderer = transform.Find("Stand").GetComponent<MeshRenderer>();
            standMeshRenderer.material.color = new Color(player.color.r, player.color.g, player.color.b, standMeshRenderer.material.color.a);
        }

        void SetMinimapIconColor()
        {
            transform.Find("Minimap Icon").GetComponent<MeshRenderer>().material.color = player.color;
        }
    }

    protected abstract IEnumerator PerformAttack(Cell cell);
    protected abstract IEnumerator CellToCellMovement(Cell cell);

    public IEnumerator Attack(Cell cell)
    {
        yield return LookAt(cell.transform.position);
        PlayAudioClip("Attack/" + GetType().Name);
        yield return PerformAttack(cell);
    }

    public virtual IEnumerator Hold()
    {
        GameObject holdAnimation = Instantiate(Resources.Load<GameObject>("Prefabs/Hold Animation"), transform);
        holdAnimation.transform.localScale = currentCell.transform.lossyScale * UNIT_TO_CELL_SIZE_RATIO;
        ParticleSystem particleSystem = holdAnimation.GetComponent<ParticleSystem>();
        ColorOverLifetimeModule colorOverLifetimeModule = particleSystem.colorOverLifetime;
        colorOverLifetimeModule.color = player.colorGradient;
        yield return new WaitForSeconds(particleSystem.main.duration);
    }

    public virtual IEnumerator MoveTo(Cell cell)
    {
        SpawnGhost();
        yield return LeaveCell();
        yield return CellToCellMovement(cell);
        yield return EnterCell(cell);

        void SpawnGhost()
        {
            GameObject ghostUnitGameObject = Instantiate(Resources.Load<GameObject>($"Prefabs/Units/{GetType().Name}"), transform);
            ghostUnitGameObject.transform.parent = currentCell.transform;
            Unit ghostUnit = ghostUnitGameObject.GetComponent<Unit>();
            ghostUnit.StartCoroutine(ghostUnit.FadeOut(GHOST_FADE_OUT_DURATION, isGhost: true));
        }

        IEnumerator LeaveCell()
        {
            yield return LookAt(currentCell.transform.position);
            yield return LinearMovement(gameObject, Vector3.zero, 0.75f);
            transform.parent = currentCell.transform.parent;
            currentCell.RemoveUnit(this);
        }

        IEnumerator EnterCell(Cell cell)
        {
            transform.parent = cell.transform;
            cell.AddUnit(this);
            yield return LookAt(cell.transform.TransformPoint(cell.UnitLocalPosition(this)));
            yield return LinearMovement(gameObject, cell.UnitLocalPosition(this), 0.75f);
        }
    }

    public IEnumerator Die()
    {
        if (isDead)
            yield break;
        isDead = true;
        PlayAudioClip("Death/" + GetType().Name);
        SpawnDeathParticles();
        yield return LieDown(DEATH_ANIMATION_DURATION * 0.25f);
        StartCoroutine(FadeOut(DEATH_ANIMATION_DURATION * 0.75f, isGhost: false));
        yield return Shrink(DEATH_ANIMATION_DURATION * 0.75f);
        yield return null;

        void SpawnDeathParticles()
        {
            GameObject deathAnimation = Instantiate(Resources.Load<GameObject>("Prefabs/Death Particles"), currentCell.transform);
            deathAnimation.transform.localPosition = currentCell.UnitLocalPosition(this);
            deathAnimation.transform.localScale *= UNIT_TO_CELL_SIZE_RATIO;
        }

        IEnumerator Shrink(float duration)
        {
            float startTime = Time.time;
            float currentTime = 0f;
            Vector3 startLocalScale = transform.localScale;

            while (currentTime < duration)
            {
                currentTime = Time.time - startTime;
                float t = Mathf.Clamp01(currentTime / duration);
                transform.localScale = startLocalScale * (1 - t);
                yield return null;
            }
        }
    }

    protected virtual IEnumerator LieDown(float duration)
    {

        float randomAngle = Random.Range(-Mathf.PI, Mathf.PI);
        Quaternion startRotation = transform.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(Mathf.Cos(randomAngle) * LIE_DOWN_ROTATION_DEGREES, 0, Mathf.Sin(randomAngle) * LIE_DOWN_ROTATION_DEGREES);
        Transform standTransform = transform.Find("Stand");

        float startTime = Time.time;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime = Time.time - startTime;
            float t = Mathf.Clamp01(currentTime / duration);
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            standTransform.localRotation = Quaternion.Inverse(transform.localRotation);
            yield return null;
        }
    }

    public void Destroy()
    {
        RemoveFromCurrentCell();
        Destroy(gameObject);
    }

    public void SetLocalTransform()
    {
        float size = Mathf.Max(unitPrefabSize.x, unitPrefabSize.z) / (Cell.cellPrefabSize * UNIT_TO_CELL_SIZE_RATIO);
        transform.SetParent(currentCell.transform, false);
        transform.localScale = Vector3.one / size;
        transform.localPosition = currentCell.UnitLocalPosition(this);
    }

    public void RemoveFromCurrentCell()
    {
        if (currentCell != null)
            currentCell.RemoveUnit(this);
    }

    public void AddPositionToPath(Vector3 position)
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, position + Vector3.up * lineRenderer.startWidth / 2);
    }

    public void PlayAudioClip(string audioClipFilePath)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource.enabled)
            audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/" + audioClipFilePath));
    }

    public Vector3 PositionInBoard(Vector3 displacement)
    {
        Vector3 localRotation = transform.localRotation.eulerAngles;
        Vector3 localPosition = transform.localPosition;

        Vector3 cellLocalScale = currentCell.transform.localScale;
        Vector3 cellLocalRotation = currentCell.transform.localRotation.eulerAngles;
        Vector3 cellLocalPosition = currentCell.transform.localPosition;

        Vector3 r_p = Quaternion.Euler(localRotation) * displacement;
        Vector3 t_r_p = localPosition + r_p;
        Vector3 cs_t_r_p = Vector3.Scale(cellLocalScale, t_r_p);
        Vector3 cr_cs_t_r_p = Quaternion.Euler(cellLocalRotation) * cs_t_r_p;
        Vector3 ct_cr_cs_t_r_p = cellLocalPosition + cr_cs_t_r_p;

        return ct_cr_cs_t_r_p;
    }

    public IEnumerator LookAt(Vector3 positionInWorld, float durationMultiplier = 0.7f)
    {
        Quaternion rotationNeeded = Quaternion.LookRotation(positionInWorld - transform.position);
        yield return LinearRotation(gameObject, transform.rotation, rotationNeeded, Mathf.Sqrt(Quaternion.Angle(transform.rotation, rotationNeeded)) * durationMultiplier * ROTATION_DURATION_SCALAR);
    }

    public static IEnumerator LinearMovement(GameObject gameObject, Vector3 endLocalPosition, float duration, bool lockY = true)
    {
        Vector3 startPosition = gameObject.transform.localPosition;
        float startTime = Time.time;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime = Time.time - startTime;
            float t = Mathf.Clamp01(currentTime / duration);
            Vector3 newLocalPosition = Vector3.Lerp(startPosition, endLocalPosition, t);
            if (lockY)
                newLocalPosition.y = gameObject.transform.localPosition.y;
            gameObject.transform.localPosition = newLocalPosition;
            yield return null;
        }
    }

    protected IEnumerator LinearRotation(GameObject gameObject, Quaternion startRotation, Quaternion endRotation, float duration)
    {

        float startTime = Time.time;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime = Time.time - startTime;
            float t = Mathf.Clamp01(currentTime / duration);
            gameObject.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            if (gameObject.CompareTag("Unit"))
                gameObject.GetComponent<Unit>().UpdateMinimapIconYRotation();
            yield return null;
        }
    }

    protected IEnumerator ParabolicMovement(GameObject gameObject, Vector3 startPosition, Vector3 endPosition, float durationMultiplier, float heightMultiplier)
    {
        float distance = Vector3.Distance(startPosition, endPosition);
        float height = scaleScalar * (PARABOLA_STANDARD_HEIGHT + heightMultiplier * Mathf.Pow(distance, PARABOLA_HEIGHT_POWER) * PARABOLA_HEIGHT_SCALAR);
        float duration = scaleScalar * (PARABOLA_STANDARD_DURATION + durationMultiplier * distance * PARABOLA_DURATION_SCALAR);

        float startTime = Time.time;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime = Time.time - startTime;
            float t = Mathf.Clamp01(currentTime / duration);
            gameObject.transform.localPosition = ParabolicInterpolation(t);
            yield return null;
        }

        Vector3 ParabolicInterpolation(float t)
        {
            float heightFormula(float x) => -4 * height * x * x + 4 * height * x;

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, t);

            return new Vector3(currentPosition.x, heightFormula(t) + Mathf.Lerp(startPosition.y, endPosition.y, t), currentPosition.z);
        }
    }

    private IEnumerator FadeOut(float duration, bool isGhost)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Material[] materials = renderers.SelectMany(meshRenderer => meshRenderer.materials).ToArray();
        foreach (Material material in materials)
        {
            material.shader = Shader.Find("Transparent/Specular");
            if (isGhost)
                material.color = Color.gray;
        }

        float startTime = Time.time;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime = Time.time - startTime;
            float t = Mathf.Clamp01(currentTime / duration);
            foreach (Material material in materials)
            {
                Color materialColor = material.color;
                materialColor.a = Mathf.Pow(1 - t, 2);
                material.color = materialColor;
            }
            yield return null;
        }

        if (isGhost)
            Destroy(gameObject);
    }

    private void UpdateMinimapIconYRotation()
    {
        transform.Find("Minimap Icon").rotation = Quaternion.Euler(90, minimapIconYRotation, 0);
    }
}