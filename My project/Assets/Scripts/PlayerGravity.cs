using UnityEngine;

public class PlayerGravity : MonoBehaviour
{
    private CharacterController controller;
    private float velocityY = 0f;
    public float gravity = -9.81f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller.isGrounded)
            velocityY = -2f;
        else
            velocityY += gravity * Time.deltaTime;

        controller.Move(new Vector3(0, velocityY * Time.deltaTime, 0));
    }
}