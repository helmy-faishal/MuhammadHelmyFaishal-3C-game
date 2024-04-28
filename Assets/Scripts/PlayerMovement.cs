using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerStance
{
    Stand,
    Climb
}

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputManager input;

    [Header("Walk and Sprint")]
    [SerializeField] float walkSpeed = 350f;
    [SerializeField] float sprintSpeed = 450f;
    [SerializeField] float walkSprintTransition = 30f;
    float speed;

    [Header("Rotation")]
    [SerializeField] float rotationSmoothTime = 0.1f;
    float rotationSmoothVelocity;

    [Header("Jump")]
    [SerializeField] float jumpForce = 500f;
    [SerializeField] Transform groundChecker;
    [SerializeField] float groundCheckerRadius = 0.2f;
    [SerializeField] LayerMask groundCheckerLayer;

    [Header("Stair")]
    [SerializeField] Vector3 upperStairOffset = Vector3.zero;
    [SerializeField] float stepCheckerDistance = 0.4f;
    [SerializeField] float stepForce = 15f;
    [SerializeField] LayerMask stepLayer;

    [Header("Climb")]
    [SerializeField] Transform climbChecker;
    [SerializeField] float climbCheckerDistance = 1f;
    [SerializeField] LayerMask climbCheckerLayer;
    [SerializeField] Vector3 climbOffset;
    [SerializeField] float climbSpeed = 20f;


    PlayerStance playerStance = PlayerStance.Stand;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        speed = walkSpeed;

        if (input != null)
        {
            input.OnInputMove += MovePlayer;
            input.OnInputSprint += SprintPlayer;
            input.OnInputJump += JumpPlayer;
            input.OnInputClimb += StartClimb;
            input.OnInputCancelClimb += CancelClimb;
        }
    }

    private void OnDestroy()
    {
        if (input != null)
        {
            input.OnInputMove -= MovePlayer;
            input.OnInputSprint -= SprintPlayer;
            input.OnInputJump -= JumpPlayer;
            input.OnInputClimb -= StartClimb;
            input.OnInputCancelClimb -= CancelClimb;
        }
    }

    private void Update()
    {
        CheckStep();
    }

    void MovePlayer(Vector2 direction)
    {
        switch (playerStance)
        {
            case PlayerStance.Stand:
                if (direction.magnitude < 0.1f) return;

                float rotationAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref rotationSmoothVelocity, rotationSmoothTime);

                Quaternion rotation = Quaternion.Euler(0, smoothAngle, 0);
                transform.rotation = rotation;

                Vector3 movement = rotation * Vector3.forward;
                rb.AddForce(movement * speed * Time.deltaTime);

                break;

            case PlayerStance.Climb:
                Vector3 horizontal = direction.x * transform.right;
                Vector3 vertical = direction.y * transform.up;

                Vector3 moveDirection = horizontal + vertical;
                rb.AddForce(moveDirection * climbSpeed * Time.deltaTime);

                break;
        }
    }

    void SprintPlayer(bool isSprint)
    {
        float sprint = isSprint ? 1 : -1;
        speed += sprint * walkSprintTransition * Time.deltaTime;
        speed = Mathf.Clamp(speed, walkSpeed, sprintSpeed);
    }

    void JumpPlayer()
    {
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce);
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(groundChecker.position, groundCheckerRadius, groundCheckerLayer);
    }

    void CheckStep()
    {
        bool isHitLower = Physics.Raycast(groundChecker.position, transform.forward, stepCheckerDistance, stepLayer);

        // Menggunakan TransformPoint karena jika penjumlahan vektor biasa
        // saat rotasi, posisi offset kurang sesuai (khususnya di titik z)
        bool isHitUpper = Physics.Raycast(groundChecker.TransformPoint(upperStairOffset), transform.forward, stepCheckerDistance, stepLayer);

        if (isHitLower && isHitUpper)
        {
            rb.AddForce(0, stepForce, 0);
        }
    }

    void StartClimb()
    {
        bool isInFrontClimb = Physics.Raycast(climbChecker.position, transform.forward, out RaycastHit hit, climbCheckerDistance, climbCheckerLayer);
        bool isNotClimb = playerStance != PlayerStance.Climb;

        if (IsGrounded() && isInFrontClimb && isNotClimb)
        {
            Vector3 offset = (transform.forward * climbOffset.z) + (Vector3.up * climbOffset.y);
            transform.position = hit.point + offset;
            playerStance = PlayerStance.Climb;
            rb.useGravity = false;
        }
    }

    void CancelClimb()
    {
        if (playerStance == PlayerStance.Climb)
        {
            playerStance = PlayerStance.Stand;
            rb.useGravity = true;
            transform.position -= transform.forward;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Ground Checker
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundChecker.position, groundCheckerRadius);

        // Step Checker
        Gizmos.color = Color.red;
        Gizmos.DrawRay(groundChecker.position, transform.forward * stepCheckerDistance);
        Gizmos.DrawRay(groundChecker.TransformPoint(upperStairOffset), transform.forward * stepCheckerDistance);

        // Climb Checker
        Gizmos.color = Color.red;
        Gizmos.DrawRay(climbChecker.position, transform.forward * climbCheckerDistance);

    }
}
