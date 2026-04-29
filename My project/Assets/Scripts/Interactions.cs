using UnityEngine;
using UnityEngine.UI;

public class Interactions : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public float chopTime = 2f;

    [Header("Item Drops")]
    public ItemData woodItem;
    public ItemData stoneItem;

    [Header("UI")]
    public Slider progressBar;
    public GameObject progressBarContainer;

    [Header("Drop Visuals")]
    public GameObject woodDropPrefab;
    public GameObject stoneDropPrefab;
    public float dropUpForce = 3f;
    public float dropLifetime = 3f;

    private Animator animator;
    private float chopProgress = 0f;
    private GameObject currentTarget;
    private ItemData currentDrop;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        SetProgressBarVisible(false);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                bool isTree = hit.collider.CompareTag("Tree");
                bool isRock = hit.collider.CompareTag("Rock");

                if (isTree || isRock)
                {
                    if (currentTarget != hit.collider.gameObject)
                    {
                        ResetChop();
                        currentTarget = hit.collider.gameObject;
                        currentDrop   = isTree ? woodItem : stoneItem;
                    }

                    animator.SetBool("isChopping", true);
                    SetProgressBarVisible(true);

                    chopProgress += Time.deltaTime;
                    if (progressBar != null)
                        progressBar.value = chopProgress / chopTime;

                    if (chopProgress >= chopTime)
                    {
                        if (currentDrop != null)
                            InventoryManager.Instance.AddItem(currentDrop);
                        else
                            Debug.LogWarning("[Interactions] currentDrop is null — assign ItemData in Inspector.");

                        SpawnDrop(currentDrop, currentTarget.transform.position);
                        Destroy(currentTarget);
                        ResetChop();
                    }

                    return;
                }
            }
        }

        ResetChop();
    }

    void SpawnDrop(ItemData item, Vector3 position)
    {
        GameObject prefab = item == woodItem ? woodDropPrefab : stoneDropPrefab;
        if (prefab == null) return;

        Vector3 spawnPos = position + Vector3.up * 0.5f;
        GameObject drop = Instantiate(prefab, spawnPos, Random.rotation);

        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 randomSideways = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
            rb.AddForce((Vector3.up + randomSideways) * dropUpForce, ForceMode.Impulse);
        }

        Destroy(drop, dropLifetime);
    }

    void ResetChop()
    {
        chopProgress = 0f;
        currentTarget = null;
        currentDrop   = null;
        animator.SetBool("isChopping", false);
        SetProgressBarVisible(false);
        if (progressBar != null)
            progressBar.value = 0f;
    }

    void SetProgressBarVisible(bool visible)
    {
        if (progressBarContainer != null)
            progressBarContainer.SetActive(visible);
    }
}