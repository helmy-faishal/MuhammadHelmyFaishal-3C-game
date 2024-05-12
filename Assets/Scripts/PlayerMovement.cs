using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerStance
{
    Stand,
    Climb,
    Crouch,
    Glide
}

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputManager input;
    [SerializeField] Transform cameraTransform;
    [SerializeField] CameraManager cameraManager;

    [Header("Walk, Sprint, Crouch")]
    [SerializeField] float walkSpeed = 350f;
    [SerializeField] float sprintSpeed = 650f;
    [SerializeField] float walkSprintTransition = 30f;
    [SerializeField] float crouchSpeed = 325f;
    [SerializeField] Transform crouchChecker;
    float speed;

    [Header("Gliding")]
    [SerializeField] float airDrag;
    [SerializeField] float glideSpeed;
    [SerializeField] Vector3 glideRotationSpeed;
    [SerializeField] float minGlideRotationX;
    [SerializeField] float maxGlideRotationX;

    [Header("Rotation")]
    [SerializeField] float rotationSmoothTime = 0.1f;
    float rotationSmoothVelocity;

    [Header("Jump")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] Transform groundChecker;
    [SerializeField] float groundCheckerRadius = 0.2f;
    [SerializeField] LayerMask groundCheckerLayer;
    bool isGrounded;

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

    [Header("Punch")]
    [SerializeField] float resetComboInterval = 5f;
    bool isPunching;
    int punchCombo;
    Coroutine resetCombo;

    [Header("Hit")]
    [SerializeField] Transform hitDetector;
    [SerializeField] float hitDetectorRadius = 1f;
    [SerializeField] LayerMask hitLayer;

    PlayerStance playerStance = PlayerStance.Stand;
    Rigidbody rb;
    CapsuleCollider capsuleCollider;

    // Animation
    Animator animator;
    private const string VELOCITY_ANIM_PARAM = "Velocity";
    private const string VELOCITY_X_ANIM_PARAM = "VelocityX";
    private const string VELOCITY_Z_ANIM_PARAM = "VelocityZ";
    private const string CHANGE_PERSPECTIIVE_ANIM_PARAM = "ChangePerspective";
    private const string JUMP_ANIM_PARAM = "Jump";
    private const string IS_GROUNDED_ANIM_PARAM = "IsGrounded";
    private const string IS_CROUCH_ANIM_PARAM = "IsCrouch";
    private const string CLIMB_VELOCITY_X_ANIM_PARAM = "ClimbVelocityX";
    private const string CLIMB_VELOCITY_Y_ANIM_PARAM = "ClimbVelocityY";
    private const string IS_CLIMBING_ANIM_PARAM = "IsClimbing";
    private const string IS_GLIDING_ANIM_PARAM = "IsGliding";
    private const string PUNCH_ANIM_PARAM = "Punch";
    private const string COMBO_ANIM_PARAM = "Combo";

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();
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
            input.OnInputCrouch += CrouchPlayer;
            input.OnInputGlide += StartGlide;
            input.OnInputCancelGlide += CancelGlide;
            input.OnInputPunch += PunchPlayer;
        }

        if (cameraManager != null)
        {
            cameraManager.OnChangePerspective += ChangePerspective;
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
            input.OnInputCrouch -= CrouchPlayer;
            input.OnInputGlide -= StartGlide;
            input.OnInputCancelGlide -= CancelGlide;
            input.OnInputPunch -= PunchPlayer;
        }

        if (cameraManager != null)
        {
            cameraManager.OnChangePerspective -= ChangePerspective;
        }
    }

    private void Update()
    {
        CheckStep();
        CheckIsInGround();
        GlidePlayer();
    }

    void MovePlayer(Vector2 direction)
    {
        switch (playerStance)
        {
            case PlayerStance.Stand:
            case PlayerStance.Crouch:
                PlayerStandMovement(direction);
                break;

            case PlayerStance.Glide:
                PlayerGlideMovement(direction);
                break;

            case PlayerStance.Climb:
                PlayerClimbMovement(direction);
                break;
        }
    }

    void PlayerStandMovement(Vector2 direction)
    {
        if (isPunching) return;

        switch (cameraManager.state)
        {
            case CameraState.ThirdPerson:
                if (direction.magnitude < 0.1f) break;

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

        Vector3 velocity = rb.velocity;
        velocity.y = 0;

        animator.SetFloat(VELOCITY_ANIM_PARAM, velocity.magnitude * direction.magnitude);
        animator.SetFloat(VELOCITY_X_ANIM_PARAM, velocity.magnitude * direction.x);
        animator.SetFloat(VELOCITY_Z_ANIM_PARAM, velocity.magnitude * direction.y);
    }

    void PlayerGlideMovement(Vector2 direction)
    {
        Vector3 rotationDegree = transform.eulerAngles;
        // Input Vertikal
        rotationDegree.x += glideRotationSpeed.x * direction.y * Time.deltaTime;
        rotationDegree.x = Mathf.Clamp(rotationDegree.x, minGlideRotationX, maxGlideRotationX);
        // Input Horizontal
        rotationDegree.y += glideRotationSpeed.y * direction.x * Time.deltaTime;
        rotationDegree.z += glideRotationSpeed.z * direction.x * Time.deltaTime;

        transform.rotation = Quaternion.Euler(rotationDegree);
    }

    void PlayerClimbMovement(Vector2 direction)
    {
        Vector3 horizontal = direction.x * transform.right;
        Vector3 vertical = direction.y * transform.up;

        Vector3 moveDirection = horizontal + vertical;
        // Membatasi pergerakan saat memanjat
        moveDirection = ClampClimbMovement(moveDirection);

        if (moveDirection.magnitude > 0)
        {
            rb.AddForce(moveDirection * climbSpeed * Time.deltaTime);
        }
        else
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        animator.SetFloat(CLIMB_VELOCITY_X_ANIM_PARAM, rb.velocity.magnitude * moveDirection.x);
        animator.SetFloat(CLIMB_VELOCITY_Y_ANIM_PARAM, rb.velocity.magnitude * moveDirection.y);
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
        if (playerStance != PlayerStance.Stand) return;

        float sprint = isSprint ? 1 : -1;
        speed += sprint * walkSprintTransition * Time.deltaTime;
        speed = Mathf.Clamp(speed, walkSpeed, sprintSpeed);
    }

    void JumpPlayer()
    {
        if (isGrounded && animator.GetCurrentAnimatorStateInfo(0).IsTag("CanJump"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger(JUMP_ANIM_PARAM);
        }
    }

    void CheckIsInGround()
    {
        isGrounded = Physics.CheckSphere(groundChecker.position, groundCheckerRadius, groundCheckerLayer);
        animator.SetBool(IS_GROUNDED_ANIM_PARAM, isGrounded);

        if (isGrounded)
        {
            CancelGlide();
        }
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

        if (isGrounded && isInFrontClimb && isNotClimb)
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

            animator.SetBool(IS_CLIMBING_ANIM_PARAM, true);
            capsuleCollider.center = Vector3.up * 0.9f;
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

            animator.SetBool(IS_CLIMBING_ANIM_PARAM, false);
            capsuleCollider.center = Vector3.up * 0.9f;
        }
    }

    void ChangePerspective()
    {
        animator.SetTrigger(CHANGE_PERSPECTIIVE_ANIM_PARAM);
    }

    void CrouchPlayer()
    {
        if (playerStance == PlayerStance.Stand)
        {
            playerStance = PlayerStance.Crouch;
            animator.SetBool(IS_CROUCH_ANIM_PARAM, true);
            speed = crouchSpeed;
            capsuleCollider.height = 1.3f;
            capsuleCollider.center = Vector3.up * 0.66f;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            if (!Physics.Raycast(crouchChecker.position, Vector3.up))
            {
                playerStance = PlayerStance.Stand;
                animator.SetBool(IS_CROUCH_ANIM_PARAM, false);
                speed = walkSpeed;
                capsuleCollider.height = 1.8f;
                capsuleCollider.center = Vector3.up * 0.9f;
            }
        }
    }

    void GlidePlayer()
    {
        if (playerStance != PlayerStance.Glide) return;

        float lift = transform.eulerAngles.x;
        Vector3 upForce = transform.up * (lift + airDrag);
        Vector3 forwardForce = transform.forward * glideSpeed;
        Vector3 totalForce = upForce + forwardForce;
        rb.AddForce(totalForce * Time.deltaTime);
    }

    void StartGlide()
    {
        if (playerStance != PlayerStance.Glide && playerStance != PlayerStance.Climb && !isGrounded)
        {
            playerStance = PlayerStance.Glide;
            animator.SetBool(IS_GLIDING_ANIM_PARAM, true);
            cameraManager.SetFPPClampedCamera(true, transform.rotation.eulerAngles);
        }
    }

    void CancelGlide()
    {
        if (playerStance == PlayerStance.Glide)
        {
            playerStance = PlayerStance.Stand;
            animator.SetBool(IS_GLIDING_ANIM_PARAM, false);
            cameraManager.SetFPPClampedCamera(false, transform.rotation.eulerAngles);
        }
    }

    void PunchPlayer()
    {
        if (!isPunching && playerStance == PlayerStance.Stand)
        {
            isPunching = true;

            if (punchCombo < 3)
            {
                punchCombo++;
            }
            else
            {
                punchCombo = 1;
            }

            animator.SetTrigger(PUNCH_ANIM_PARAM);
            animator.SetInteger(COMBO_ANIM_PARAM, punchCombo);
        }
    }

    void EndPunch()
    {
        isPunching = false;

        if (resetCombo != null)
        {
            StopCoroutine(resetCombo);
        }
        resetCombo = StartCoroutine(ResetComboCoroutine());
    }

    void HitObject()
    {
        Collider[] hitObjects = Physics.OverlapSphere(hitDetector.position, hitDetectorRadius, hitLayer);
        foreach (Collider obj in hitObjects)
        {
            if (obj.gameObject != null)
            {
                Destroy(obj.gameObject);
            }
        }
    }

    IEnumerator ResetComboCoroutine()
    {
        yield return new WaitForSeconds(resetComboInterval);
        punchCombo = 0;
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

        // Crouch Checker
        Gizmos.color = Color.red;
        Gizmos.DrawRay(crouchChecker.position, Vector3.up);
    }
}
