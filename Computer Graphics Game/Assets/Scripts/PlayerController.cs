using System.Text;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 4.0f;
    [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 5.1f;
    [SerializeField] private float jumpBuffer = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;

    private bool isJumping;
    private float jumpBufferTimer = 0f;
    private float turnSmoothVelocity = 0f;
    private Vector3 playerVelocity = Vector3.zero;
    
    void Start()
    {
        
    }

    void Update()
    {
        HandleJumpBuffer();
        HandleJumpCut();

        ApplyGravity();
        HandleMovement();
        HandleJump();
        controller.Move(playerVelocity * Time.deltaTime);
    }
    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        if(inputDir.magnitude >= 0.1f) 
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

    private void ApplyGravity() 
    {
        if (controller.isGrounded) 
        {
            playerVelocity.y = -1f;
            isJumping = false;
        }
        else 
        {
            playerVelocity.y = playerVelocity.y - 9.8f * Time.deltaTime;
        }
    }

    private void HandleJump() 
    {
        if(controller.isGrounded && jumpBufferTimer > 0f) 
        {
            playerVelocity.y = jumpSpeed;
            jumpBufferTimer = 0f;
            isJumping = true;
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
        if(Input.GetKeyUp(KeyCode.Space) && isJumping && playerVelocity.y > 0f) 
        {
            playerVelocity.y = playerVelocity.y * jumpCutMultiplier;
        }
    }

}
