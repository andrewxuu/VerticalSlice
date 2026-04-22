using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private Vector3 lastPosition;
    private Transform playerRoot;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerRoot = GetComponentInParent<CharacterController>().transform;
        lastPosition = playerRoot.position;

        Debug.Log("Animator found: " + (animator != null));
        Debug.Log("Player root found: " + (playerRoot != null));
    }

    void Update()
    {
        Vector3 delta = playerRoot.position - lastPosition;
        lastPosition = playerRoot.position;

        float speed = new Vector3(delta.x, 0f, delta.z).magnitude / Time.deltaTime;
        float directionZ = delta.z / Time.deltaTime;

        Debug.Log("Speed: " + speed + " | DirectionZ: " + directionZ);
        Debug.Log("Current Animator State: " + animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") + " Idle | " + animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") + " Walk | " + animator.GetCurrentAnimatorStateInfo(0).IsName("Backwards") + " Backwards");

        animator.SetFloat("speed", speed);
        animator.SetFloat("DirectionZ", directionZ);
    }
}