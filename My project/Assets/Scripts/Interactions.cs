using UnityEngine;
using UnityEngine.UI;

public class Interactions : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public float chopTime = 2f;

    [Header("UI")]
    public Slider progressBar;
    public GameObject progressBarContainer;

    private Animator animator;
    private float chopProgress = 0f;
    private GameObject currentTarget;
    private string currentTag;

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
                        currentTag = isTree ? "Tree" : "Rock";
                    }

                    animator.SetBool("isChopping", true);
                    SetProgressBarVisible(true);

                    chopProgress += Time.deltaTime;
                    if (progressBar != null)
                        progressBar.value = chopProgress / chopTime;

                    if (chopProgress >= chopTime)
                    {
                        string itemName = currentTag == "Tree" ? "Wood" : "Stone";
                        InventoryManager.Instance.AddItem(itemName);
                        Destroy(currentTarget);
                        ResetChop();
                    }

                    return;
                }
            }
        }

        ResetChop();
    }

    void ResetChop()
    {
        chopProgress = 0f;
        currentTarget = null;
        currentTag = null;
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