using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;
    [SerializeField] private TextMeshProUGUI velocityText;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 10.0f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float gravityVal = 10f;
    [SerializeField] private float slopeStickForce = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 6.0f;
    [SerializeField] private float jumpBuffer = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Double Jump")]
    [SerializeField] private float doubleJumpSpeed = 6f;

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 15f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideJumpHeight = 4.0f;
    [SerializeField] private float slideJumpBoost = 1.0f;

    [Header("Moving Platform")]
    [SerializeField] private LayerMask movingPlatformMask;
    [SerializeField] private float movingPlatformCheckDistance = 0.35f;

    [Header("Bhop")]
    [SerializeField] private float bhopWindow = 0.15f;
    [SerializeField] private float airSteerStrength = 50f;
    [SerializeField] private float perfectBhopWindow = 0.04f;
    [SerializeField] private float greatBhopWindow = 0.10f;
    [SerializeField] private float normalBhopWindow = 0.15f;
    [SerializeField] private float perfectBhopSpeed = 20f;
    [SerializeField] private float greatBhopSpeed = 17f;
    [SerializeField] private float normalBhopSpeed = 15f;
    [SerializeField] private float bhopDecayRate = 10f;
    [SerializeField] private float bhopSharpTurnCancelAngle = 120f;

    [Header("Jump Effects")]
    [SerializeField] private ParticleSystem bhopLandingDust;
    [SerializeField] private ParticleSystem doubleJumpEffect;
    [SerializeField] private Vector3 doubleJumpEffectOffset = Vector3.zero;
    [SerializeField] private float dustGroundOffset = 0.03f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpSpeed = 8f;
    [SerializeField] private float wallJumpHeight = 5f;
    [SerializeField] private float wallDetectDistance = 0.6f;
    [SerializeField] private float wallJumpSteerLockTime = 0.6f;
    [SerializeField] private float wallJumpCooldown = 0.3f;
    [SerializeField] private float wallJumpBuffer = 0.15f;
    [SerializeField] private LayerMask wallMask;

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

    public bool isSliding = false;

    private Animator animator;

    private Vector3 playerVelocity = Vector3.zero;
    private Vector3 inputDir = Vector3.zero;
    private Vector3 slideVelocity = Vector3.zero;
    private Vector3 slideJumpMomentum = Vector3.zero;
    private Vector3 bhopDirection = Vector3.zero;
    private Vector3 wallNormal = Vector3.zero;

    private float turnSmoothVelocity = 0f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private float lastJumpPressedTime = -999f;
    private float landingTimer = 0f;
    private float wallJumpBufferTimer = 0f;
    private float wallJumpLockTimer = 0f;
    private float wallJumpCooldownTimer = 0f;
    private float bhopCurrentSpeed = 0f;
    private float externalLaunchTimer = 0f;
    private const float ExternalLaunchGraceTime = 0.12f;

    private bool isJumping = false;
    private bool isFixedHeightJump = false;
    private bool hasDoubleJump = false;
    private bool justLanded = false;
    private bool wasGrounded = true;
    private bool isTouchingWall = false;
    private bool isBhopActive = false;
    private bool canBhopFromSlideJump = false;
    private bool jumpWasBufferedBeforeLanding = false;
    private bool shouldSpawnJumpDustOnLanding = false;
    private bool clearMomentumOnGround = false;

    private Coroutine slideCoroutine;
    private MoveBetweenAB currentMovingPlatform;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        Application.targetFrameRate = 60; // REMOVE LATER
    }

    private void Update()
    {
        HandleJumpBuffer();
        HandleJumpCut();
        ApplyGravity();
        UpdateWallTimers();

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

        if (controller == null || !controller.enabled)
            return;

        UpdateMovingPlatform();
        ApplyMovingPlatformMovement();

        controller.Move(playerVelocity * Time.deltaTime);
        UpdateAnimator();
    }

    // --- Movement ---

    private void HandleMovement()
    {
        inputDir = GetRawInputDirection();

        if (inputDir.magnitude < 0.1f)
        {
            SetHorizontalVelocity(Vector3.zero);
            return;
        }

        float targetAngle = GetCameraRelativeInputAngle(inputDir);
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetAngle,
            ref turnSmoothVelocity,
            turnSmoothTime
        );

        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

        Vector3 moveDir = DirectionFromAngle(targetAngle);
        SetHorizontalVelocity(moveDir.normalized * movementSpeed);
    }

    private Vector3 GetRawInputDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        return new Vector3(x, 0f, z).normalized;
    }

    private float GetCameraRelativeInputAngle(Vector3 input)
    {
        return Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
    }

    private Vector3 DirectionFromAngle(float angle)
    {
        return Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
    }

    private void SetHorizontalVelocity(Vector3 horizontalVelocity)
    {
        playerVelocity.x = horizontalVelocity.x;
        playerVelocity.z = horizontalVelocity.z;
    }

    // --- Moving Platform ---

    private void UpdateMovingPlatform()
    {
        currentMovingPlatform = null;

        if (!controller.isGrounded)
            return;

        Vector3 origin = transform.position + controller.center;
        float radius = Mathf.Max(0.05f, controller.radius * 0.9f);

        bool hitPlatform = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            movingPlatformCheckDistance + controller.height * 0.5f,
            movingPlatformMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hitPlatform)
            return;

        currentMovingPlatform = hit.collider.GetComponentInParent<MoveBetweenAB>();
    }

    private void ApplyMovingPlatformMovement()
    {
        if (currentMovingPlatform == null)
            return;

        Vector3 platformDelta = currentMovingPlatform.DeltaMovement;

        if (platformDelta.sqrMagnitude <= 0.000001f)
            return;

        controller.Move(platformDelta);
    }

    // --- Gravity / Landing ---

    private void ApplyGravity()
    {
        if (externalLaunchTimer > 0f)
        {
            externalLaunchTimer -= Time.deltaTime;
            justLanded = false;
            coyoteTimer = 0f;

            float gravity = playerVelocity.y < 0f
                ? gravityVal * fallMultiplier
                : gravityVal;

            playerVelocity.y -= gravity * Time.deltaTime;

            wasGrounded = false;
            return;
        }

        if (controller.isGrounded)
        {
            HandleGroundedGravity();
        }
        else
        {
            HandleAirGravity();
        }

        wasGrounded = controller.isGrounded;
    }

    private void HandleGroundedGravity()
    {
        if (!justLanded)
            HandleLanding();

        if (clearMomentumOnGround)
        {
            slideJumpMomentum = Vector3.zero;
            clearMomentumOnGround = false;
            canBhopFromSlideJump = false;
            ClearBhopState();
        }

        coyoteTimer = coyoteTime;
        landingTimer += Time.deltaTime;

        if (landingTimer > bhopWindow)
            ClearBhopChain();

        playerVelocity.y = -20f;
        isJumping = false;
        isFixedHeightJump = false;
        hasDoubleJump = true;
    }

    private void HandleLanding()
    {
        justLanded = true;
        landingTimer = 0f;

        if (shouldSpawnJumpDustOnLanding)
        {
            PlayJumpLandingDust();
            shouldSpawnJumpDustOnLanding = false;
        }
    }

    private void HandleAirGravity()
    {
        if (wasGrounded && !isJumping)
            playerVelocity.y = 0f;

        justLanded = false;
        coyoteTimer -= Time.deltaTime;

        float gravity = playerVelocity.y < 0f
            ? gravityVal * fallMultiplier
            : gravityVal;

        playerVelocity.y -= gravity * Time.deltaTime;
    }

    // --- Jump ---

    private void HandleJump()
    {
        if (coyoteTimer <= 0f || jumpBufferTimer <= 0f)
            return;

        if (isSliding)
        {
            PerformSlideJump();
        }
        else if (CanPerformBhop())
        {
            PerformBhopJump();
        }
        else
        {
            PerformJump();
        }

        jumpBufferTimer = 0f;
        jumpWasBufferedBeforeLanding = false;
        isJumping = true;
        landingTimer = bhopWindow + 1f;
    }

    private void PerformJump()
    {
        PlaySound(jumpSound, 0.85f);

        isFixedHeightJump = false;
        playerVelocity.y = jumpSpeed;

        ApplyJumpCutIfNeeded();
        shouldSpawnJumpDustOnLanding = true;
    }

    private void HandleJumpBuffer()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBuffer;
            lastJumpPressedTime = Time.time;
            jumpWasBufferedBeforeLanding = !controller.isGrounded;
        }

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleJumpCut()
    {
        if (Input.GetKeyUp(KeyCode.Space) && isJumping && !isFixedHeightJump && playerVelocity.y > 0f)
            playerVelocity.y *= jumpCutMultiplier;
    }

    private void ApplyJumpCutIfNeeded()
    {
        if (!Input.GetKey(KeyCode.Space))
            playerVelocity.y *= jumpCutMultiplier;
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
        PlaySound(doubleJumpSound);
        PlayDoubleJumpEffect();

        hasDoubleJump = false;
        playerVelocity.y = doubleJumpSpeed;
        isJumping = true;
        isFixedHeightJump = true;
        shouldSpawnJumpDustOnLanding = true;
    }

    private bool IsNearGround()
    {
        float checkDistance = Mathf.Abs(playerVelocity.y) * jumpBuffer + controller.skinWidth;
        Vector3 origin = transform.position + controller.center;

        return Physics.SphereCast(
            origin,
            controller.radius,
            Vector3.down,
            out _,
            checkDistance
        );
    }

    // --- Slide ---

    private void HandleSlide()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && controller.isGrounded && !isSliding)
            TryStartSlide();

        if (isSliding)
            SetHorizontalVelocity(slideVelocity);
    }

    private void TryStartSlide()
    {
        Vector3 input = GetRawInputDirection();

        if (input.magnitude < 0.1f)
            return;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideDash());
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
        PlaySound(slideSound, 0.5f);

        isSliding = true;
        slideVelocity = transform.forward * slideSpeed;

        slideJumpMomentum = Vector3.zero;

        ClearBhopState();
        canBhopFromSlideJump = false;
        clearMomentumOnGround = false;
    }

    private void PerformSlideEnd()
    {
        isSliding = false;
        slideCoroutine = null;
    }

    private void PerformSlideJump()
    {
        PlaySound(slideJumpSound);

        ClearBhopState();

        Vector3 slideDirection = GetHorizontalMomentum(slideVelocity).normalized;

        if (slideDirection.magnitude <= 0.01f)
            slideDirection = transform.forward;

        slideJumpMomentum = slideDirection * normalBhopSpeed;

        canBhopFromSlideJump = true;
        clearMomentumOnGround = false;

        CancelSlide();

        isFixedHeightJump = true;
        playerVelocity.y = slideJumpHeight;
        shouldSpawnJumpDustOnLanding = true;
    }

    private void CancelSlide()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = null;
        isSliding = false;
    }

    // --- Bhop / Slide Jump Momentum ---

    private bool CanPerformBhop()
    {
        if (!controller.isGrounded)
            return false;

        if (landingTimer > bhopWindow)
            return false;

        if (!canBhopFromSlideJump)
            return false;

        Vector3 horizontalMomentum = GetHorizontalMomentum(slideJumpMomentum);

        if (horizontalMomentum.magnitude <= movementSpeed + 0.1f)
            return false;

        return true;
    }

    private void PerformBhopJump()
    {
        PlaySound(jumpSound, 0.9f);

        Vector3 horizontalMomentum = GetHorizontalMomentum(slideJumpMomentum);

        if (horizontalMomentum.magnitude <= 0.01f)
            horizontalMomentum = transform.forward * normalBhopSpeed;

        bhopDirection = horizontalMomentum.normalized;
        bhopCurrentSpeed = GetBhopSpeedFromTiming();
        isBhopActive = true;
        canBhopFromSlideJump = true;
        clearMomentumOnGround = false;

        slideJumpMomentum = bhopDirection * bhopCurrentSpeed;

        isFixedHeightJump = false;
        playerVelocity.y = jumpSpeed;

        ApplyJumpCutIfNeeded();
        shouldSpawnJumpDustOnLanding = true;
    }

    private float GetBhopSpeedFromTiming()
    {
        float timingError = jumpWasBufferedBeforeLanding
            ? Time.time - lastJumpPressedTime
            : landingTimer;

        if (timingError <= perfectBhopWindow)
            return perfectBhopSpeed;

        if (timingError <= greatBhopWindow)
            return greatBhopSpeed;

        return normalBhopSpeed;
    }

    private void HandleSlideJumpMomentum()
    {
        if (slideJumpMomentum.magnitude <= 0.01f)
            return;

        Vector3 input = GetRawInputDirection();

        if (isBhopActive)
            ApplyBhopDecay();

        if (input.magnitude > 0.1f)
            SteerSlideJumpMomentum(input);

        SetHorizontalVelocity(slideJumpMomentum);
    }

    private void ApplyBhopDecay()
    {
        bhopCurrentSpeed = Mathf.MoveTowards(
            bhopCurrentSpeed,
            normalBhopSpeed,
            bhopDecayRate * Time.deltaTime
        );

        if (bhopDirection.magnitude <= 0.01f)
            bhopDirection = slideJumpMomentum.normalized;

        slideJumpMomentum = bhopDirection.normalized * bhopCurrentSpeed;
    }

    private void SteerSlideJumpMomentum(Vector3 input)
    {
        float targetAngle = GetCameraRelativeInputAngle(input);
        Vector3 worldInputDir = DirectionFromAngle(targetAngle);
        Vector3 currentDirection = slideJumpMomentum.normalized;

        if (isBhopActive && bhopDirection.magnitude > 0.01f)
            currentDirection = bhopDirection.normalized;

        float turnAngle = Vector3.Angle(currentDirection, worldInputDir);

        if (turnAngle >= bhopSharpTurnCancelAngle)
        {
            CancelMomentumFromSharpTurn(worldInputDir);
            return;
        }

        float currentSpeed = isBhopActive
            ? bhopCurrentSpeed
            : slideJumpMomentum.magnitude;

        Vector3 steeredDirection = Vector3.RotateTowards(
            currentDirection,
            worldInputDir,
            airSteerStrength * Mathf.Deg2Rad * Time.deltaTime,
            0f
        ).normalized;

        if (isBhopActive)
        {
            bhopDirection = steeredDirection;
            slideJumpMomentum = bhopDirection * currentSpeed;
        }
        else
        {
            slideJumpMomentum = steeredDirection * currentSpeed;
        }
    }

    private void CancelMomentumFromSharpTurn(Vector3 desiredDirection)
    {
        ClearBhopState();
        canBhopFromSlideJump = false;
        clearMomentumOnGround = true;

        Vector3 flatDirection = GetHorizontalMomentum(desiredDirection).normalized;

        if (flatDirection.magnitude <= 0.01f)
            flatDirection = transform.forward;

        slideJumpMomentum = flatDirection * movementSpeed;
        SetHorizontalVelocity(slideJumpMomentum);

        transform.rotation = Quaternion.LookRotation(flatDirection);
    }

    private void ClearBhopChain()
    {
        slideJumpMomentum = Vector3.zero;
        ClearBhopState();
        canBhopFromSlideJump = false;
        clearMomentumOnGround = false;
    }

    private void ClearBhopState()
    {
        isBhopActive = false;
        bhopCurrentSpeed = 0f;
        bhopDirection = Vector3.zero;
    }

    private Vector3 GetHorizontalMomentum(Vector3 vector)
    {
        return new Vector3(vector.x, 0f, vector.z);
    }

    // --- Wall Jump ---

    private void UpdateWallTimers()
    {
        if (wallJumpLockTimer > 0f)
            wallJumpLockTimer -= Time.deltaTime;

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;
    }

    private void CheckWall()
    {
        isTouchingWall = false;
        wallNormal = Vector3.zero;

        if (controller.isGrounded) return;
        if (wallJumpCooldownTimer > 0f) return;

        Vector3 origin = transform.position + controller.center;

        bool hitWall = Physics.SphereCast(
            origin,
            controller.radius,
            transform.forward,
            out RaycastHit hit,
            wallDetectDistance,
            wallMask
        );

        if (!hitWall)
            return;

        float facingDot = Vector3.Dot(transform.forward, hit.normal);

        if (facingDot < -0.5f)
        {
            isTouchingWall = true;
            wallNormal = hit.normal;
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

        if (!isTouchingWall)
            return;

        if (wallJumpBufferTimer > 0f)
        {
            wallJumpBufferTimer = 0f;
            PerformWallJump();
        }
    }

    private void PerformWallJump()
    {
        PlaySound(wallJumpSound, 0.5f);

        wallJumpLockTimer = wallJumpSteerLockTime;
        wallJumpCooldownTimer = wallJumpCooldown;

        slideJumpMomentum = Vector3.zero;
        ClearBhopState();
        canBhopFromSlideJump = false;
        clearMomentumOnGround = false;

        Vector3 horizontalNormal = GetHorizontalMomentum(wallNormal).normalized;
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
        shouldSpawnJumpDustOnLanding = true;
    }

    // --- Effects ---

    private void PlayDoubleJumpEffect()
    {
        if (doubleJumpEffect == null)
            return;

        Vector3 spawnPosition =
            transform.position +
            transform.right * doubleJumpEffectOffset.x +
            Vector3.up * doubleJumpEffectOffset.y +
            transform.forward * doubleJumpEffectOffset.z;

        ParticleSystem effect = Instantiate(
            doubleJumpEffect,
            spawnPosition,
            doubleJumpEffect.transform.rotation
        );

        effect.Play();

        Destroy(effect.gameObject, 2f);
    }

    private void PlayJumpLandingDust()
    {
        if (bhopLandingDust == null)
            return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float rayDistance = 2f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
            return;

        float groundAngle = Vector3.Angle(hit.normal, Vector3.up);

        if (groundAngle > 50f)
            return;

        Vector3 spawnPosition = hit.point + Vector3.up * dustGroundOffset;

        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem dust = Instantiate(bhopLandingDust, spawnPosition, spawnRotation);
        dust.Play();

        Destroy(dust.gameObject, 2f);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    // --- Animator ---

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        float fwdVelocity = Vector3.Dot(controller.velocity, transform.forward);
        float upVelocity = Vector3.Dot(controller.velocity, transform.up);

        animator.SetFloat("fwdVelocity", fwdVelocity);
        animator.SetFloat("upVelocity", upVelocity);
        animator.SetBool("isRunning", inputDir.magnitude >= 0.1f);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isGrounded", controller.isGrounded);
        animator.SetBool("isFalling", playerVelocity.y < 0f);
        animator.SetBool("isSliding", isSliding);
    }

    // --- Public Helpers ---

    public void ResetVelocity()
    {
        playerVelocity = Vector3.zero;
        slideVelocity = Vector3.zero;
        slideJumpMomentum = Vector3.zero;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = null;

        isSliding = false;
        isJumping = false;
        isFixedHeightJump = false;

        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        landingTimer = 0f;
        wallJumpBufferTimer = 0f;
        wallJumpLockTimer = 0f;
        wallJumpCooldownTimer = 0f;

        currentMovingPlatform = null;

        ClearBhopState();

        jumpWasBufferedBeforeLanding = false;
        lastJumpPressedTime = -999f;
        canBhopFromSlideJump = false;
        shouldSpawnJumpDustOnLanding = false;
        clearMomentumOnGround = false;
    }

    public void Launch(Vector3 direction, float force)
    {
        direction = direction.normalized;

        ClearBhopChain();

        externalLaunchTimer = ExternalLaunchGraceTime;

        playerVelocity.y = 0f;
        playerVelocity += direction * force;

        isJumping = true;
        isFixedHeightJump = true;
        justLanded = false;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;

        shouldSpawnJumpDustOnLanding = true;
    }

    // --- Debug ---

    private void HandleDebug()
    {
        if (showVelocityHUD && velocityText != null)
        {
            Vector3 horizontalVelocity = GetHorizontalMomentum(playerVelocity);
            velocityText.text = "Velocity:\n" + horizontalVelocity.magnitude;
        }

        if (Input.GetKeyDown(KeyCode.R) && CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.Respawn(gameObject);
        }

        Debug.DrawRay(
            transform.position + controller.center,
            transform.forward * wallDetectDistance,
            isTouchingWall ? Color.green : Color.red
        );
    }
}