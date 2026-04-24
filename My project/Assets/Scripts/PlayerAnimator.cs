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
        float dot = Vector3.Dot(delta.normalized, playerRoot.forward);

        float directionZ = (dot < -0.3f) ? -speed : speed;

        animator.SetFloat("speed", speed);
        animator.SetFloat("DirectionZ", directionZ);
    }
}