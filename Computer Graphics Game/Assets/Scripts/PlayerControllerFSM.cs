using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerControllerFSM : MonoBehaviour
{
    public enum State
    {
        Standing, // Run/Idle
        Slide,
        Jump,
        SlideJump,
        DoubleJump,
        WallJump,
        Fall,
        Bunnyhop
    }

    public class InputState
    {
        public Vector2 moveDirection = Vector2.zero;
        // If inputDir > 0.01
        public bool moveRequested = false;
        public bool jumpRequested = false;
        public bool jumpHeld = false;
        public bool slideRequested = false;

        public static InputState Get()
        {
            InputState state = new();
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");
            state.moveDirection = new Vector2(x, y);
            state.moveRequested = state.moveDirection.magnitude >= 0.01f;
            if (state.moveRequested)
            {
                state.moveDirection = state.moveDirection.normalized;
            }
            state.jumpRequested = Input.GetKeyDown(KeyCode.Space);
            state.jumpHeld = Input.GetKey(KeyCode.Space);
            state.slideRequested = Input.GetKey(KeyCode.LeftShift);
            return state;
        }
    }

    public class Timer
    {
        float timer = 0.0f;

        public Timer()
        {
        }

        public void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0.0f)
                timer = 0.0f;
        }
        public void Set(float setTime)
        {
            timer = setTime;
        }
        public void Reset()
        {
            timer = 0.0f;
        }
        public bool IsRunning()
        {
            return timer > 0.0f;
        }
        public bool Take()
        {
            bool buffered = IsRunning();
            timer = 0.0f;
            return buffered;
        }
    }

    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;
    [SerializeField] private TextMeshProUGUI velocityText;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 6.0f;
    [SerializeField] private float movementAcceleration = 6.0f;
    [SerializeField] private float movementDeceleration = 6.0f;
    [SerializeField] private float gravityVal = 10f;
    [SerializeField] private float slopeStickForce = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 6.0f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;
    private bool isFixedHeightJump = false;
    private bool isJumping = false;
    private Timer jumpBuffer = new Timer();

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideDeceleration = 2.0f;
    [SerializeField] private float slideMinimumSpeed = 8.0f;
    [SerializeField] private float slideStoppingSpeed = 2.0f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideJumpHeight = 4.0f;
    [SerializeField] private float slideJumpBoost = 1.0f;
    private Vector3 slideVelocity = Vector3.zero;
    private Vector3 slideJumpMomentum = Vector3.zero;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpSpeed = 8f;
    [SerializeField] private float wallJumpHeight = 5f;
    [SerializeField] private float wallDetectDistance = 0.6f;
    [SerializeField] private float wallJumpLockTime = 0.6f;
    [SerializeField] private float wallJumpCooldownTime = 0.3f;
    [SerializeField] private LayerMask wallMask;
    private bool isTouchingWall = false;
    private Timer wallJumpLockTimer = new Timer();
    private Timer wallJumpCooldownTimer = new Timer();
    private Vector3 wallNormal = Vector3.zero;


    [Header("vvv Debug Options")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showVelocityHUD = true;

    [Header("vvv Debug (Don't touch. Only for viz)")]
    [SerializeField] public State state = State.Standing;
    [SerializeField] public Vector3 velocity = Vector3.zero;
    // [SerializeField] public Vector3 lastPosition = Vector3.zero;
    [SerializeField] public Vector3 actualVelocity = Vector3.zero;
    [SerializeField] private float turnSmoothVelocity = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // Sets XZ velocity to target input direction.
    // Rotates character to align with target input direction.
    private void ControlPlanarVelocity(InputState input)
    {
        Vector3 targetPlanarVelocity = Vector3.zero;
        float delta = movementDeceleration;

        if (input.moveRequested)
        {
            // Move in target angle direction
            var cameraPlanarRotation = Quaternion.LookRotation(new Vector3(cam.forward.x, 0.0f, cam.forward.z), Vector3.up);
            var targetPlanarDirection = cameraPlanarRotation * new Vector3(input.moveDirection.x, 0.0f, input.moveDirection.y);
            targetPlanarVelocity = movementSpeed * targetPlanarDirection;
            delta = movementAcceleration;

            // Rotate player to velocity
            var velocityPlanarRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0.0f, velocity.z), Vector3.up);
            transform.rotation = velocityPlanarRotation;
        }

        Vector3 planarVelocity = new(velocity.x, 0.0f, velocity.z);
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, delta * Time.deltaTime);
        velocity.x = planarVelocity.x;
        velocity.z = planarVelocity.z;
    }

    private void ApplyFallGravity()
    {
        velocity.y -= gravityVal * Time.deltaTime;
    }
    private void ApplyStickToGround()
    {
        velocity.y = -20.0f;
    }

    // Returns the next state. By default it returns the current state.
    private State HandleStateBeforeMove(State state, InputState input)
    {
        Vector3 planarVelocity;

        switch (state)
        {
            case State.Standing:
                ControlPlanarVelocity(input);
                if (!controller.isGrounded)
                {
                    velocity.y = 0.0f;
                    return State.Fall;
                }
                else if (jumpBuffer.Take())
                {
                    velocity.y = jumpSpeed;
                    return State.Jump;
                }
                else
                {
                    ApplyStickToGround();
                }

                planarVelocity = new Vector3(velocity.x, 0.0f, velocity.z);
                if (input.slideRequested && input.moveRequested && planarVelocity.magnitude >= slideMinimumSpeed)
                {
                    return State.Slide;
                }
                break;
            case State.Jump:
                ControlPlanarVelocity(input);
                ApplyFallGravity();

                // Jump cutting
                if (!input.jumpHeld && velocity.y > 0f)
                {
                    velocity.y *= jumpCutMultiplier;
                    return State.Fall;
                }

                if (velocity.y <= 0.0f)
                {
                    return State.Fall;
                }
                break;
            case State.SlideJump:
                if (controller.isGrounded)
                {
                    // TODO: slide jump buffer
                    if (input.slideRequested)
                    {
                        return State.Slide;
                    }
                    else
                    {
                        return State.Standing;
                    }
                }
                else
                {
                    ApplyFallGravity();
                }
                break;
            case State.Fall:
                ControlPlanarVelocity(input);

                if (controller.isGrounded) {
                    return State.Standing;
                } else {
                    ApplyFallGravity();
                }

                if (jumpBuffer.Take())
                {
                    if (isTouchingWall)
                    {
                        wallJumpLockTimer.Set(wallJumpLockTime);
                        wallJumpCooldownTimer.Set(wallJumpCooldownTime);

                        Vector3 horizontalNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
                        Vector3 wallJumpDir = (horizontalNormal + Vector3.up).normalized;
                        velocity.x = wallJumpDir.x * wallJumpSpeed;
                        velocity.z = wallJumpDir.z * wallJumpSpeed;
                        velocity.y = wallJumpHeight;
                        transform.rotation = Quaternion.LookRotation(horizontalNormal);
                        return State.Fall;
                    }
                }
                break;
            case State.Slide:
                if (!controller.isGrounded)
                {
                    velocity.y = 0.0f;
                    return State.Fall;
                }
                else if (jumpBuffer.Take())
                {
                    velocity.y = jumpSpeed;
                    return State.SlideJump;
                }
                else
                {
                    ApplyStickToGround();
                }

                // Friction, decelerate
                planarVelocity = new Vector3(velocity.x, 0.0f, velocity.z);
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, slideDeceleration * Time.deltaTime);
                velocity.x = planarVelocity.x;
                velocity.z = planarVelocity.z;

                // If too slow, stand.
                if (planarVelocity.magnitude <= slideStoppingSpeed)
                {
                    return State.Standing;
                }
                break;
            case State.DoubleJump:
                break;
            case State.WallJump:
                ControlPlanarVelocity(input);
                ApplyFallGravity();
                if (velocity.y <= 0.0f) {
                    return State.Fall;
                }
                break;
            case State.Bunnyhop:
                break;
        }
        return state;
    }

    // Returns the next state. By default it returns the current state.
    private State HandleStateAfterMove(State state, InputState input)
    {
        switch (state)
        {
            case State.Standing:
            case State.Slide:
                if (!controller.isGrounded)
                {
                    velocity.y = 0.0f;
                    return State.Fall;
                }
                break;
            case State.Jump:
            case State.Fall:
                if (controller.isGrounded)
                {
                    return State.Standing;
                }
                break;
            case State.DoubleJump:
                break;
            case State.WallJump:
                break;
            case State.Bunnyhop:
                break;
        }
        return state;
    }

    private void CheckWall()
    {
        isTouchingWall = false;
        wallNormal = Vector3.zero;

        if (wallJumpCooldownTimer.IsRunning()) return;

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


    private void Update()
    {
        var input = InputState.Get();

        if (input.jumpRequested)
            jumpBuffer.Set(jumpBufferTime);

        jumpBuffer.Update();
        wallJumpLockTimer.Update();
        wallJumpCooldownTimer.Update();

        CheckWall();

        Vector3 lastPosition = transform.position;
        state = HandleStateBeforeMove(state, input);
        controller.Move(velocity * Time.deltaTime);
        // state = HandleStateAfterMove(state, input);
        actualVelocity = (transform.position - lastPosition) / Time.deltaTime;

        if (debugMode)
            HandleDebug();
    }

    private void HandleDebug()
    {
        if (showVelocityHUD)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
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
