using System;
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
    [SerializeField] Transform cameraTransform;
    [SerializeField] CameraManager cameraManager;

    [Header("Walk and Sprint")]
    [SerializeField] float walkSpeed = 350f;
    [SerializeField] float sprintSpeed = 650f;
    [SerializeField] float walkSprintTransition = 30f;
    float speed;

    [Header("Rotation")]
    [SerializeField] float rotationSmoothTime = 0.1f;
    float rotationSmoothVelocity;

    [Header("Jump")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] Transform groundChecker;
    [SerializeField] float groundCheckerRadius = 0.2f;
    [SerializeField] LayerMask groundCheckerLayer;

    [Header("Stair")]
    [SerializeField] Vector3 upperStairOffset = Vector3.zero;
    [SerializeField] float stepCheckerDistance = 0.2f;
    [SerializeField] float stepForce = 15f;

    [Header("Climb")]
    [SerializeField] Transform climbChecker;
    [SerializeField] float climbCheckerDistance = 1f;
    [SerializeField] LayerMask climbCheckerLayer;
    [SerializeField] Vector3 climbOffset;
    [SerializeField] float climbSpeed = 20f;

    [Header("Clamp Edge Climb")]
    [Tooltip("Besar offset pada sisi horizontal dari Climb Checker")]
    [SerializeField][Min(0f)] float horizontalClimbOffset = 0.5f;
    [Tooltip("Besar offset pada sisi atas dari Climb Checker")]
    [SerializeField][Min(0f)] float topClimbOffset = 0.25f;

    PlayerStance playerStance = PlayerStance.Stand;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        HideAndLockCursor();
    }

    void HideAndLockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
                PlayerStandMovement(direction);
                break;

            case PlayerStance.Climb:
                PlayerClimbMovement(direction);
                break;
        }
    }

    void PlayerStandMovement(Vector2 direction)
    {
        switch (cameraManager.state)
        {
            case CameraState.ThirdPerson:
                if (direction.magnitude < 0.1f) return;

                float rotationAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref rotationSmoothVelocity, rotationSmoothTime);

                Quaternion rotation = Quaternion.Euler(0, smoothAngle, 0);
                transform.rotation = rotation;

                Vector3 movement = rotation * Vector3.forward;
                rb.AddForce(movement * speed * Time.deltaTime);

                break;

            case CameraState.FirstPerson: 
                transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
                Vector3 horizontal = direction.x * transform.right;
                Vector3 vertical = direction.y * transform.forward;

                Vector3 moveDirection = horizontal + vertical;

                rb.AddForce(moveDirection * speed * Time.deltaTime);
                break;
        }
    }

    void PlayerClimbMovement(Vector2 direction)
    {
        Vector3 horizontal = direction.x * transform.right;
        Vector3 vertical = direction.y * transform.up;

        Vector3 moveDirection = horizontal + vertical;
        // Membatasi pergerakan saat memanjat
        moveDirection = ClampClimbMovement(moveDirection);

        rb.AddForce(moveDirection * climbSpeed * Time.deltaTime);
    }

    // Mengecek apakah bagian sisi masih dapat dipanjat
    // Apabila tidak bisa maka akan menghentikan pergerakan pemain
    // Menghentikan pergerakan untuk memastikan tidak ada sisa velocity
    void CheckIfStillClimbable(bool isClimbable)
    {
        if (!isClimbable)
        {
            // Menghentikan pergerakan pemain dengan membuat velocity jadi 0
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Variabel untuk digunakan pada properti
    bool _isRightClimbable;
    //  Properti untuk memastikan apakah sisi kanan masih dapat dipanjat
    bool IsRightClimbable
    {
        // Mengembalikan nilai variabel
        get { return _isRightClimbable; }
        // Memasukkan nilai variabel
        set
        {
            // Jika nilai yang dimasukkan sama, maka nilai variabel tidak perlu diubah
            if (value ==  _isRightClimbable) return;
            // Mengubah nilai variabel jika nilai yang dimasukkan berbeda
            _isRightClimbable = value;
            // Mengecek apakah sisi kanan masih dapat dipanjat
            CheckIfStillClimbable(_isRightClimbable);
        }
    }

    // Penjelasan mirip variabel dan properti diatas
    bool _isLeftClimbable;
    bool IsLeftClimbable
    {
        get { return _isLeftClimbable; }
        set
        {
            if (value == _isLeftClimbable) return;
            _isLeftClimbable = value;
            CheckIfStillClimbable(_isLeftClimbable);
        }
    }

    // Penjelasan mirip variabel dan properti diatas
    bool _isTopClimbable;
    bool IsTopClimbable
    {
        get { return _isTopClimbable; }
        set
        {
            if (value == _isTopClimbable) return;
            _isTopClimbable = value;
            CheckIfStillClimbable(_isTopClimbable);
        }
    }

    // Membatasi pergerakan saat memanjat dengan menggunakan Raycast pada sisi pemain
    Vector3 ClampClimbMovement(Vector3 moveDirection)
    {
        // Jika sedang tidak memanjat, maka tidak ada yang perlu diubah
        if (playerStance != PlayerStance.Climb) return moveDirection;

        // Nilai arah sumbu maksimal saat bergerak (1,1)
        Vector3 maxAxis = Vector3.one;
        // Nilai arah sumbu minimal saat bergerak (-1,-1)
        Vector3 minAxis = -Vector3.one;

        // Mendapatkan posisi checker sisi horizontal (kanan dan kiri)
        Vector3 rightClimbChecker = climbChecker.TransformPoint(Vector3.right * horizontalClimbOffset);
        Vector3 leftClimbChecker = climbChecker.TransformPoint(Vector3.left * horizontalClimbOffset);

        // Mengecek apakah posisi karakter saat ini terbalik
        // dimana nilai posisi kanan lebih kecil dari kiri
        Vector3 diffSideChecker = rightClimbChecker - leftClimbChecker;
        bool isCharacterFlipped = diffSideChecker.x < 0 || diffSideChecker.z < 0;

        // Mengecek apakah sisi kanan pemain masih bagian dari Climbable
        IsRightClimbable = Physics.Raycast(rightClimbChecker, transform.forward, climbCheckerDistance, climbCheckerLayer);
        // Jika bukan bagian dari Climbable maka akan menyesuaikan nilai sumbu horizontal (x dan z)
        if (!IsRightClimbable)
        {
            // Jika arah posisi karakter saat ini terbalik
            // Maka pergerakan ke arah sumbu horizontal negatif dibuat menjadi 0
            if (isCharacterFlipped)
            {
                minAxis.x = 0;
                minAxis.z = 0;
            }
            // Jika tidak, pergerakan ke arah sumbu horizontal positif dibuat menjadi 0
            else
            {
                maxAxis.x = 0;
                maxAxis.z = 0;
            }
        }
        
        // Mirip checker sisi kanan, tetapi nilai axis yang diubah terbalik
        IsLeftClimbable = Physics.Raycast(leftClimbChecker, transform.forward, climbCheckerDistance, climbCheckerLayer);
        if (!IsLeftClimbable)
        {
            if (isCharacterFlipped)
            {
                maxAxis.x = 0;
                maxAxis.z = 0;
                
            }
            else
            {
                minAxis.x = 0;
                minAxis.z = 0;
            }
        }

        // Mendapatkan posisi checker sisi atas
        Vector3 topClimbableChecker = climbChecker.TransformPoint(Vector3.up * topClimbOffset);
        // Mengecek apakah sisi atas pemain masih bagian dari Climbable
        IsTopClimbable = Physics.Raycast(topClimbableChecker, transform.forward, climbCheckerDistance, climbCheckerLayer);
        // Jika bukan bagian dari Climbable maka akan membuat arah atas atau positif pada sumbu y menjadi 0
        if (!IsTopClimbable)
        {
            maxAxis.y = 0;
        }

        // Membatasi arah gerak sesuai nilai arah sumbu minimal dan maksimal
        moveDirection.x = Mathf.Clamp(moveDirection.x, minAxis.x, maxAxis.x);
        moveDirection.y = Mathf.Clamp(moveDirection.y, minAxis.y, maxAxis.y);
        moveDirection.z = Mathf.Clamp(moveDirection.z, minAxis.z, maxAxis.z);

        return moveDirection;
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
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(groundChecker.position, groundCheckerRadius, groundCheckerLayer);
    }

    void CheckStep()
    {
        bool isHitLower = Physics.Raycast(groundChecker.position, transform.forward, stepCheckerDistance);

        // Menggunakan TransformPoint karena jika penjumlahan vektor biasa
        // saat rotasi, posisi offset kurang sesuai (khususnya di titik z)
        bool isHitUpper = Physics.Raycast(groundChecker.TransformPoint(upperStairOffset), transform.forward, stepCheckerDistance);

        if (isHitLower && !isHitUpper)
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
            // Mendapatkan titik terdekat antara Climbable dengan Player
            Vector3 closestPointFromClimbable = hit.collider.bounds.ClosestPoint(transform.position);
            // Menentukan arah Player dengan selisih antara titik terdekat dengan pemain
            Vector3 hitForward = closestPointFromClimbable - transform.position;
            // Membuat arah sumbu y menjadi 0, karena hanya perlu sumbu x dan z
            hitForward.y = 0;
            // Me-rotasi pemain berdasarkan arah pemain terhadap titik terdekat dari Climbable
            transform.rotation = Quaternion.LookRotation(hitForward);

            Vector3 offset = (transform.forward * climbOffset.z) + (Vector3.up * climbOffset.y);
            transform.position = hit.point + offset;

            playerStance = PlayerStance.Climb;
            rb.useGravity = false;
            cameraManager.SetFPPClampedCamera(true, transform.rotation.eulerAngles);
            cameraManager.SetTPPFieldOfView(70);
        }
    }

    void CancelClimb()
    {
        if (playerStance == PlayerStance.Climb)
        {
            playerStance = PlayerStance.Stand;
            rb.useGravity = true;
            transform.position -= transform.forward;
            cameraManager.SetFPPClampedCamera(false, transform.rotation.eulerAngles);
            cameraManager.SetTPPFieldOfView(40);
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

        // Edge Climb Checker
        Gizmos.color = Color.red;
        Gizmos.DrawRay(climbChecker.TransformPoint(Vector3.right * horizontalClimbOffset), transform.forward * climbCheckerDistance);
        Gizmos.DrawRay(climbChecker.TransformPoint(Vector3.left * horizontalClimbOffset), transform.forward * climbCheckerDistance);
        Gizmos.DrawRay(climbChecker.TransformPoint(Vector3.up * topClimbOffset), transform.forward * climbCheckerDistance);
    }
}
