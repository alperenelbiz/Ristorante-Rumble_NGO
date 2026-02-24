using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private Animator animator;

    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isSprinting;
    private bool isGrounded;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        if (CameraManager.Instance != null)
            CameraManager.Instance.SetFollowTarget(transform);
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        isSprinting = sprintAction.IsPressed();

        if (jumpAction.WasPressedThisFrame())
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        GroundCheck();

        // Movement — preserve Y (gravity/jump)
        float speed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y).normalized * speed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        // Rotation — smooth via MoveRotation
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
        }

        // Jump
        if (jumpRequested && isGrounded)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        jumpRequested = false;

        // Animator
        if (animator != null)
        {
            float hSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            animator.SetFloat("Speed", hSpeed);
        }
    }

    private void GroundCheck()
    {
        isGrounded = Physics.Raycast(
            rb.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );
    }
}
