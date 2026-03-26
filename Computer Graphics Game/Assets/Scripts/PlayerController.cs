using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;
    [SerializeField] private TextMeshProUGUI velocityText;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 6.0f;
    [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 6.0f;
    [SerializeField] private float jumpBuffer = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    private bool isJumping = false;
    private float jumpBufferTimer = 0f;

    private Animator animator;

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideJumpHeight = 4.0f;
    [SerializeField] private float slideJumpBoost = 1.0f;
    [SerializeField] private float slideJumpMomentumDecay = 5f;
    private bool isSliding = false;
    private bool isSlideJumping = false;
    private Vector3 slideVelocity = Vector3.zero;
    private Vector3 slideJumpMomentum = Vector3.zero;

    [Header("Bhop")]
    [SerializeField] private float bhopWindow = 0.15f;
    [SerializeField] private float airSteerStrength = 50f;
    private float landingTimer = 0f;
    private bool justLanded = false;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpSpeed = 8f;
    [SerializeField] private float wallJumpHeight = 5f;
    [SerializeField] private float wallDetectDistance = 0.6f;
    [SerializeField] private float wallJumpSteerLockTime = 0.6f;
    [SerializeField] private float wallJumpCooldown = 0.3f;
    [SerializeField] private LayerMask wallMask;
    private bool isTouchingWall = false;
    private float wallJumpLockTimer = 0f;
    private float wallJumpCooldownTimer = 0f;
    private Vector3 wallNormal = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showVelocityHUD = true;

    private float turnSmoothVelocity = 0f;
    private Vector3 playerVelocity = Vector3.zero;

    private Coroutine slideCoroutine;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
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
        HandleSlide();
        HandleSlideJumpMomentum();
        CheckWall();
        HandleWallJump();

        if (debugMode)
            HandleDebug();

        controller.Move(playerVelocity * Time.deltaTime);
    }

    // --- Movement ---

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            animator.SetBool("isRunning", true);
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
            animator.SetBool("isRunning", false);
            playerVelocity.x = 0f;
            playerVelocity.z = 0f;
        }
    }

    // --- Jump ---

    private void HandleJump()
    {
        if (controller.isGrounded && jumpBufferTimer > 0f)
        {
            if (isSliding)
                PerformSlideJump();
            else
                PerformJump();

            jumpBufferTimer = 0f;
            isJumping = true;
            animator.SetBool("isJumping", true);
            landingTimer = bhopWindow + 1f;
        }
    }

    private void PerformJump()
    {
        playerVelocity.y = jumpSpeed;

        if (!Input.GetKey(KeyCode.Space))
            playerVelocity.y *= jumpCutMultiplier;
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
        if (Input.GetKeyUp(KeyCode.Space) && isJumping && !isSlideJumping && playerVelocity.y > 0f)
            playerVelocity.y *= jumpCutMultiplier;
    }

    // --- Gravity ---

    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (!justLanded)
                OnLand();

            landingTimer += Time.deltaTime;

            if (landingTimer > bhopWindow)
                slideJumpMomentum = Vector3.zero;

            playerVelocity.y = -1f;
            isJumping = false;
            isSlideJumping = false;

            var fwdVelocity = Vector3.Dot(controller.velocity, transform.forward);
            var upVelocity = Vector3.Dot(controller.velocity, transform.up);
            animator.SetFloat("fwdVelocity", fwdVelocity);
            animator.SetFloat("upVelocity", upVelocity);
            animator.SetBool("isJumping", false);
            animator.SetBool("isGrounded", true);
            animator.SetBool("isFalling", false);
        }
        else
        {
            justLanded = false;
            playerVelocity.y -= 9.8f * Time.deltaTime;
            animator.SetBool("isGrounded", false);
            animator.SetBool("isFalling", playerVelocity.y < 0f);
        }
    }

    private void OnLand()
    {
        justLanded = true;
        landingTimer = 0f;
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
            animator.SetBool("isSliding", true);
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
            elapsed += Time.deltaTime;
            yield return null;
        }

        PerformSlideEnd();
    }

    private void PerformSlideStart()
    {
        isSliding = true;
        slideVelocity = transform.forward * slideSpeed;
    }

    private void PerformSlideEnd()
    {
        isSliding = false;
        animator.SetBool("isSliding", false);
    }

    private void PerformSlideJump()
    {
        if (slideJumpMomentum.magnitude <= 0.01f)
            slideJumpMomentum = new Vector3(slideVelocity.x, 0f, slideVelocity.z) * slideJumpBoost;

        CancelSlide();
        isSlideJumping = true;
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

            playerVelocity.x += slideJumpMomentum.x;
            playerVelocity.z += slideJumpMomentum.z;
        }
    }

    // --- Wall Jump ---

    private void CheckWall()
    {
        isTouchingWall = false;
        wallNormal = Vector3.zero;

        if (controller.isGrounded) return;
        if (wallJumpCooldownTimer > 0f) return;

        Vector3 origin = transform.position + Vector3.up * (controller.height / 2f);

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
        if (!isTouchingWall) return;

        if (Input.GetKeyDown(KeyCode.Space))
            PerformWallJump();
    }

    private void PerformWallJump()
    {
        wallJumpLockTimer = wallJumpSteerLockTime;
        wallJumpCooldownTimer = wallJumpCooldown;

        Vector3 horizontalNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 wallJumpDir = (horizontalNormal + Vector3.up).normalized;

        playerVelocity.x = wallJumpDir.x * wallJumpSpeed;
        playerVelocity.z = wallJumpDir.z * wallJumpSpeed;
        playerVelocity.y = wallJumpHeight;

        transform.rotation = Quaternion.LookRotation(horizontalNormal);

        isTouchingWall = false;
        isJumping = true;
        jumpBufferTimer = 0f;
        animator.SetBool("isJumping", true);
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
            transform.position + Vector3.up * (controller.height / 2f),
            transform.forward * wallDetectDistance,
            isTouchingWall ? Color.green : Color.red
        );
    }
}
