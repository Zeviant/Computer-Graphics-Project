using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController player;
    [SerializeField] private Transform cam;
    [SerializeField] private float movementSpeed = 4.0f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity = 0f;
    
    void Start()
    {
        
    }

    void Update()
    {
        float xMovement = Input.GetAxisRaw("Horizontal");
        float yMovement = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(xMovement, 0f, yMovement).normalized;

        if(direction.magnitude >= 0.1f) 
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; 
            player.Move(moveDir.normalized * movementSpeed *  Time.deltaTime);
        }
    }
}
