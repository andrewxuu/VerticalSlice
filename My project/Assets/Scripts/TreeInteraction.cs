using UnityEngine;

public class TreeInteraction : MonoBehaviour
{
    public float interactRange = 2f;
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();    
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                if (hit.collider.CompareTag("Tree"))
                {
                    animator.SetBool("isChopping", true);
                    return;
                }
            }
        }

        animator.SetBool("isChopping", false);
    }
}