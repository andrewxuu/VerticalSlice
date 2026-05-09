using UnityEngine;
using UnityEngine.UI;

public class Interactions : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public float chopTime      = 2f;

    [Header("Item Drops")]
    public ItemData woodItem;
    public ItemData stoneItem;

    [Header("UI")]
    public Slider     progressBar;
    public GameObject progressBarContainer;

    [Header("Drop Visuals")]
    public GameObject woodDropPrefab;
    public GameObject stoneDropPrefab;
    public float dropUpForce  = 3f;
    public float dropLifetime = 3f;

    [Header("Highlight")]
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);

    private Animator   animator;
    private float      chopProgress;
    private GameObject currentTarget;
    private ItemData   currentDrop;

    private GameObject highlightedObject;
    private Renderer[] highlightedRenderers;
    private Color[]    originalColors;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        SetProgressBarVisible(false);
    }

    void Update()
    {
        GameObject nearest = GetBestInteractable();

        if (nearest != highlightedObject)
        {
            ClearHighlight();
            if (nearest != null) ApplyHighlight(nearest);
        }

        if (nearest != null && Input.GetKey(KeyCode.E))
        {
            if (currentTarget != nearest)
            {
                ResetChop();
                currentTarget = nearest;
                currentDrop   = nearest.CompareTag("Tree") ? woodItem : stoneItem;
            }

            animator.SetBool("isChopping", true);
            SetProgressBarVisible(true);

            chopProgress += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = chopProgress / chopTime;

            if (chopProgress >= chopTime)
            {
                SpawnDrop(currentDrop, currentTarget.transform.position);
                ClearHighlight();
                Destroy(currentTarget);
                ResetChop();
            }

            return;
        }

        ResetChop();
    }

    GameObject GetBestInteractable()
    {
        Collider[] hits      = Physics.OverlapSphere(transform.position, interactRange);
        GameObject best      = null;
        float      bestScore = float.NegativeInfinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Tree") && !hit.CompareTag("Rock")) continue;

            Vector3 toTarget = hit.bounds.center - transform.position;
            float   dist     = toTarget.magnitude;
            if (dist < 0.01f) continue;

            float score = Vector3.Dot(transform.forward, toTarget / dist) / dist;
            if (score > bestScore)
            {
                bestScore = score;
                best      = hit.gameObject;
            }
        }

        return best;
    }

    void SpawnDrop(ItemData item, Vector3 position)
    {
        GameObject prefab = item == woodItem ? woodDropPrefab : stoneDropPrefab;
        if (prefab == null) return;

        GameObject drop = Instantiate(prefab, position + Vector3.up * 0.5f, Random.rotation);

        PickupDrop pickup = drop.GetComponent<PickupDrop>();
        if (pickup != null)
            pickup.item = item;
        else
            Debug.LogWarning("[Interactions] PickupDrop component missing on drop prefab.");

        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 sideways = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
            rb.AddForce((Vector3.up + sideways) * dropUpForce, ForceMode.Impulse);
        }

        Destroy(drop, dropLifetime);
    }

    void ApplyHighlight(GameObject target)
    {
        highlightedObject    = target;
        highlightedRenderers = target.GetComponentsInChildren<Renderer>();
        originalColors       = new Color[highlightedRenderers.Length];

        for (int i = 0; i < highlightedRenderers.Length; i++)
        {
            originalColors[i]                      = highlightedRenderers[i].material.color;
            highlightedRenderers[i].material.color = highlightColor;
        }
    }

    void ClearHighlight()
    {
        if (highlightedRenderers != null)
            for (int i = 0; i < highlightedRenderers.Length; i++)
                if (highlightedRenderers[i] != null)
                    highlightedRenderers[i].material.color = originalColors[i];

        highlightedObject    = null;
        highlightedRenderers = null;
        originalColors       = null;
    }

    void ResetChop()
    {
        chopProgress  = 0f;
        currentTarget = null;
        currentDrop   = null;
        animator.SetBool("isChopping", false);
        SetProgressBarVisible(false);
        if (progressBar != null) progressBar.value = 0f;
    }

    void SetProgressBarVisible(bool visible)
    {
        if (progressBarContainer != null)
            progressBarContainer.SetActive(visible);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.3f);
        Gizmos.DrawSphere(transform.position, interactRange);
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}