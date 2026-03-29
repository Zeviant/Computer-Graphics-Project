using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;
    [SerializeField] private TextMeshProUGUI velocityText;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 6.0f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float gravityVal = 10f;
    [SerializeField] private float slopeStickForce = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 6.0f;
    [SerializeField] private float jumpBuffer = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;
    private bool isFixedHeightJump = false;
    private bool isJumping = false;
    private float jumpBufferTimer = 0f;

    [Header("Double Jump")]
    [SerializeField] private float doubleJumpSpeed = 6f;
    private bool hasDoubleJump = false;

    private Animator animator;

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideJumpHeight = 4.0f;
    [SerializeField] private float slideJumpBoost = 1.0f;
    public bool isSliding = false;
    private Vector3 slideVelocity = Vector3.zero;
    private Vector3 slideJumpMomentum = Vector3.zero;

    [Header("Bhop")]
    [SerializeField] private float bhopWindow = 0.15f;
    [SerializeField] private float airSteerStrength = 50f;
    private float landingTimer = 0f;
    private bool justLanded = false;
    private bool wasGrounded = true;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpSpeed = 8f;
    [SerializeField] private float wallJumpHeight = 5f;
    [SerializeField] private float wallDetectDistance = 0.6f;
    [SerializeField] private float wallJumpSteerLockTime = 0.6f;
    [SerializeField] private float wallJumpCooldown = 0.3f;
    [SerializeField] private float wallJumpBuffer = 0.15f;
    private float wallJumpBufferTimer = 0f;
    [SerializeField] private LayerMask wallMask;
    private bool isTouchingWall = false;
    private float wallJumpLockTimer = 0f;
    private float wallJumpCooldownTimer = 0f;
    private Vector3 wallNormal = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showVelocityHUD = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip slideJumpSound;
    [SerializeField] private AudioClip wallJumpSound;

    private float turnSmoothVelocity = 0f;
    private Vector3 playerVelocity = Vector3.zero;
    private Vector3 inputDir;

    private Coroutine slideCoroutine;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        Application.targetFrameRate = 60; // REMOVE LATER 
    }

    void Update()
    {
        HandleJumpBuffer();
        HandleJumpCut();
        ApplyGravity();

        if (wallJumpLockTimer > 0f)
            wallJumpLockTimer -= Time.deltaTime;

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;

        if (!isSliding && wallJumpLockTimer <= 0f)
            HandleMovement();

        HandleJump();
        HandleDoubleJump();
        HandleSlide();
        HandleSlideJumpMomentum();
        CheckWall();
        HandleWallJump();

        if (debugMode)
            HandleDebug();

        controller.Move(playerVelocity * Time.deltaTime);
        UpdateAnimator();
    }

    // --- Movement ---

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(x, 0f, z).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            Vector3 horizontalVelocity = moveDir.normalized * movementSpeed;
            playerVelocity.x = horizontalVelocity.x;
            playerVelocity.z = horizontalVelocity.z;
        }
        else
        {
            playerVelocity.x = 0f;
            playerVelocity.z = 0f;
        }
    }

    // --- Gravity ---

    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (!justLanded)
            {
                justLanded = true;
                landingTimer = 0f;
            }

            coyoteTimer = coyoteTime;
            landingTimer += Time.deltaTime;

            if (landingTimer > bhopWindow) 
            {
                slideJumpMomentum = Vector3.zero;
            }

            playerVelocity.y = -20f;
            isJumping = false;
            isFixedHeightJump = false;
            hasDoubleJump = true;


        }
        else
        {
            // If was grounded and now no longer is, and not jumping e.g. fall off of edge, then set vel to zero.
            if (wasGrounded && !isJumping)
            {
                playerVelocity.y = 0.0f;
            }

            justLanded = false;
            coyoteTimer = coyoteTimer - Time.deltaTime;

            float gravity = 0f;
            if(playerVelocity.y < 0f) 
            {
                gravity = gravityVal * fallMultiplier;
            }
            else 
            {
                gravity = gravityVal;
            }

            playerVelocity.y -= gravity * Time.deltaTime;
        }

        wasGrounded = controller.isGrounded;
    }

    // --- Jump ---

    private void HandleJump()
    {
        if (coyoteTimer > 0f && jumpBufferTimer > 0f)
        {
            if (isSliding) 
            {
                PerformSlideJump();
            }
            else 
            {
                PerformJump();
            }

            jumpBufferTimer = 0f;
            isJumping = true;
            landingTimer = bhopWindow + 1f;
        }
    }

    private void PerformJump()
    {
        audioSource.PlayOneShot(jumpSound, 0.85f);
        isFixedHeightJump = false;
        playerVelocity.y = jumpSpeed;

        if (!Input.GetKey(KeyCode.Space))
        {
            playerVelocity.y = playerVelocity.y * jumpCutMultiplier;
        }
    }

    private void HandleJumpBuffer()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBuffer;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleJumpCut()
    {
        if (Input.GetKeyUp(KeyCode.Space) && isJumping && !isFixedHeightJump && playerVelocity.y > 0f)
        {
            playerVelocity.y *= jumpCutMultiplier;
        }
    }

    // --- Double Jump ---

    private void HandleDoubleJump()
    {
        if (controller.isGrounded) return;
        if (wallJumpLockTimer > 0f) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (!hasDoubleJump) return;
        if (IsNearGround()) return;

        PerformDoubleJump();
    }
    private void PerformDoubleJump()
    {
        audioSource.PlayOneShot(doubleJumpSound);
        hasDoubleJump = false;
        playerVelocity.y = doubleJumpSpeed;
        isJumping = true;
        isFixedHeightJump = true;
    }
    private bool IsNearGround()
    {
        // Claude generated formula for checking whether to double jump or to just let jump buffer handle the jump
        float checkDistance = Mathf.Abs(playerVelocity.y) * jumpBuffer + controller.skinWidth;
        Vector3 origin = transform.position + controller.center;
        return Physics.SphereCast(origin, controller.radius, Vector3.down, out _, checkDistance);
    }

    // --- Slide ---

    private void HandleSlide()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && controller.isGrounded && !isSliding)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            if (new Vector3(x, 0f, z).magnitude < 0.1f)
                return;

            if (slideCoroutine != null)
                StopCoroutine(slideCoroutine);

            slideCoroutine = StartCoroutine(SlideDash());
        }

        if (isSliding)
        {
            playerVelocity.x = slideVelocity.x;
            playerVelocity.z = slideVelocity.z;
        }
    }

    private IEnumerator SlideDash()
    {
        PerformSlideStart();

        float elapsed = 0f;
        while (elapsed < slideDuration && isSliding)
        {
            elapsed = elapsed + Time.deltaTime;
            yield return null;
        }

        PerformSlideEnd();
    }

    private void PerformSlideStart()
    {
        audioSource.PlayOneShot(slideSound, 0.5f);
        isSliding = true;
        slideVelocity = transform.forward * slideSpeed;
    }

    private void PerformSlideEnd()
    {
        isSliding = false;
    }

    private void PerformSlideJump()
    {
        audioSource.PlayOneShot(slideJumpSound);
        if (slideJumpMomentum.magnitude <= 0.01f) 
        {
            slideJumpMomentum = new Vector3(slideVelocity.x, 0f, slideVelocity.z) * slideJumpBoost;
        }

        CancelSlide();
        isFixedHeightJump = true;
        playerVelocity.y = slideJumpHeight;
    }

    private void CancelSlide()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        isSliding = false;
    }

    // --- Slide Jump Momentum ---

    private void HandleSlideJumpMomentum()
    {
        if (slideJumpMomentum.magnitude > 0.01f)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(x, 0f, z).normalized;

            if (inputDir.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                Vector3 worldInputDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                float currentSpeed = slideJumpMomentum.magnitude;
                Vector3 steered = Vector3.RotateTowards(
                    slideJumpMomentum.normalized,
                    worldInputDir,
                    airSteerStrength * Mathf.Deg2Rad * Time.deltaTime,
                    0f
                );
                slideJumpMomentum = steered * currentSpeed;
            }

            playerVelocity.x = slideJumpMomentum.x;
            playerVelocity.z = slideJumpMomentum.z;
        }
    }

    // --- Wall Jump ---

    private void CheckWall()
    {
        isTouchingWall = false;
        wallNormal = Vector3.zero;

        if (controller.isGrounded) return;
        if (wallJumpCooldownTimer > 0f) return;

        Vector3 origin = transform.position + controller.center;

        if (Physics.SphereCast(origin, controller.radius, transform.forward, out RaycastHit hit, wallDetectDistance, wallMask))
        {
            float facingDot = Vector3.Dot(transform.forward, hit.normal);
            if (facingDot < -0.5f)
            {
                isTouchingWall = true;
                wallNormal = hit.normal;
            }
        }
    }
    private void HandleWallJump()
    {
        if (controller.isGrounded)
        {
            wallJumpBufferTimer = 0f;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            wallJumpBufferTimer = wallJumpBuffer;

        if (wallJumpBufferTimer > 0f)
            wallJumpBufferTimer -= Time.deltaTime;

        if (!isTouchingWall) return;

        if (wallJumpBufferTimer > 0f)
        {
            wallJumpBufferTimer = 0f;
            PerformWallJump();
        }
    }

    private void PerformWallJump()
    {
        audioSource.PlayOneShot(wallJumpSound, 0.7f);
        wallJumpLockTimer = wallJumpSteerLockTime;
        wallJumpCooldownTimer = wallJumpCooldown;

        slideJumpMomentum = Vector3.zero; // clear slide jump momentum

        Vector3 horizontalNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 wallJumpDir = (horizontalNormal + Vector3.up).normalized;

        playerVelocity.x = wallJumpDir.x * wallJumpSpeed;
        playerVelocity.z = wallJumpDir.z * wallJumpSpeed;
        playerVelocity.y = wallJumpHeight;

        transform.rotation = Quaternion.LookRotation(horizontalNormal);

        isTouchingWall = false;
        isJumping = true;
        isFixedHeightJump = true;
        hasDoubleJump = true;
        jumpBufferTimer = 0f;
    }

    private void UpdateAnimator() {
        var fwdVelocity = Vector3.Dot(controller.velocity, transform.forward);
        var upVelocity = Vector3.Dot(controller.velocity, transform.up);
        animator.SetFloat("fwdVelocity", fwdVelocity);
        animator.SetFloat("upVelocity", upVelocity);
        animator.SetBool("isRunning", inputDir.magnitude >= 0.1f);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isGrounded", controller.isGrounded);
        animator.SetBool("isFalling", playerVelocity.y < 0f);
        animator.SetBool("isSliding", isSliding);
    }

    // --- Debug ---

    private void HandleDebug()
    {
        if (showVelocityHUD)
        {
            Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
            velocityText.text = "Velocity:\n" + horizontalVelocity.magnitude;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            controller.enabled = false;
            transform.position = new Vector3(0f, 10f, 0f);
            controller.enabled = true;
        }

        Debug.DrawRay(
            transform.position + controller.center,
            transform.forward * wallDetectDistance,
            isTouchingWall ? Color.green : Color.red
        );
    }
}
