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
    [SerializeField] private float jumpSpeed = 5.1f;
    [SerializeField] private float jumpBuffer = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    private bool isJumping = false;
    private float jumpBufferTimer = 0f;

    private Animator animator;

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideDuration = 0.5f;
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

        if (!isSliding)
            HandleMovement();

        HandleJump();
        HandleSlide();
        HandleSlideJumpMomentum();

        velocityText.text = "Velocity:\n" + playerVelocity.magnitude;

        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleSlide()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && controller.isGrounded && !isSliding)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            
            if (new Vector3(x, 0f, z).magnitude < 0.1f) 
            {

                return;
            }

            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
            }
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
        isSliding = true;

        Vector3 SlideDir = transform.forward;
        slideVelocity = SlideDir * slideSpeed;

        float elapsed = 0f;
        
        while (elapsed < slideDuration && isSliding) 
        {

            elapsed += Time.deltaTime;
            yield return null;
        }

        isSliding = false;
        animator.SetBool("isSliding", false);
    }

    private void CancelSlide() 
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        isSliding = false;
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        if(inputDir.magnitude >= 0.1f) 
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

    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (!justLanded) 
            {
                justLanded = true;
                landingTimer = 0f;
            }

            landingTimer += Time.deltaTime;

            if(landingTimer > bhopWindow) 
            {
                slideJumpMomentum = Vector3.zero;
            }

            playerVelocity.y = -1f;
            isJumping = false;
            isSlideJumping = false;
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

    private void HandleJump()
    {
        if (controller.isGrounded && jumpBufferTimer > 0f)
        {
            if (isSliding)
            {
                if(slideJumpMomentum.magnitude <= 0.01f) 
                {
                    slideJumpMomentum = new Vector3(slideVelocity.x, 0f, slideVelocity.z) * slideJumpBoost;
                }
                CancelSlide();
                isSlideJumping = true;
                playerVelocity.y = jumpSpeed / 2f;
            }
            else 
            {
                playerVelocity.y = jumpSpeed;

                // Buffer short jump if player is no longer pressing jump
                if (!Input.GetKey(KeyCode.Space)) 
                {
                    playerVelocity.y *= jumpCutMultiplier;
                }
            }

            jumpBufferTimer = 0f;
            isJumping = true;
            animator.SetBool("isJumping", true);
            landingTimer = bhopWindow + 1f;
        }
    }

    private void HandleSlideJumpMomentum()
    {
        if (slideJumpMomentum.magnitude > 0.01f)
        {
            // Read player input direction relative to camera
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(x, 0f, z).normalized;

            if (inputDir.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                Vector3 worldInputDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                // Steer momentum direction toward input, preserve magnitude
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

    private void HandleJumpBuffer()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            jumpBufferTimer = jumpBuffer;
        }

        if (jumpBufferTimer > 0f) 
        {
            jumpBufferTimer = jumpBufferTimer - Time.deltaTime;
        }
    }

    private void HandleJumpCut() 
    {
        if(Input.GetKeyUp(KeyCode.Space) && isJumping && !isSlideJumping && playerVelocity.y > 0f) 
        {
            playerVelocity.y = playerVelocity.y * jumpCutMultiplier;
        }
    }

}
