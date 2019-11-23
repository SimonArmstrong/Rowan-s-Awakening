using UnityEngine;

public class Player : MonoBehaviour
{
    private enum LocomotionState { normal, lockon }
    [SerializeField] private Animator anim;
    [Header("Settings")]
    [SerializeField, Tooltip("How far the player needs to move the joystick before any movement input is recived. Excess input is used for rotation")]
    private float moveInputThreshold = 0.12f;

    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float rotateSpeed = 30;
    [SerializeField] private float stepHeight = 0.1f;
    [SerializeField] private float stepCorrectHeight = 0.3f;
    [SerializeField] private float stepSmoothing = 30;
    [SerializeField] private float heightCheck = 0.4f;
    [SerializeField] private float widthCheck = 0.275f;
    [SerializeField] private float jumpForce = 30;
    [SerializeField] private float animationBlendSmoothing = 5f;
    [SerializeField] private LocomotionState locomotionState;
    [Header("Grounding")]
    [SerializeField] private Transform groundNormalTransform;
    [SerializeField] private float groundNormalSmoothTime = 200;
    [SerializeField, Range(0.1f, 0.9f)] private float minSlopeFlatness;
    [SerializeField] private bool grounded = false;
    [Header("Attachments")]
    [SerializeField] private GameObject freeLookCamera;
    public Transform headTransform;
    public Transform headHeightTransform;
    public TMPro.TextMeshProUGUI groundNormalAngleDebugTextbox;
    public TMPro.TextMeshProUGUI forwardGroundNormalAngleDebugTextbox;

    [Header("Action Triggers")]
    [SerializeField] private bool triggerJump = false;

    [Header("Debug")]
    [SerializeField] private bool showGroundNormal;
    [SerializeField] private bool showGroundCheckRay;
    [SerializeField] private bool showInput;
    [SerializeField] public Vector3 movement;

    private new Rigidbody rigidbody;

    // Called on Start in PlayerManager
    public void Init() {
        rigidbody = GetComponent<Rigidbody>();
    }

    // Called on Update in PlayerManager
    public void Tick() {
        CheckGrounding();
    }

    // Called on FixedUpdate in PlayerManager
    public void FixedTick() {
        UpdateGroundNormalTransform();
        HandleLocomotionState();
        HandleCamera();
        HandleJumping();
        HandleMovement();
    }

    private void HandleLocomotionState()
    {
        bool lButton = Input.GetButton(StaticStrings.leftBumper);
        locomotionState = lButton == true ? LocomotionState.lockon : LocomotionState.normal;
    }

    private void HandleJumping() {
        if ((Input.GetButtonDown(StaticStrings.jump) && grounded) || triggerJump) {
            rigidbody.AddForce(jumpForce * ((Vector3.up * 1.5f)), ForceMode.VelocityChange);
            triggerJump = false;
        }
    }

    public void Jump() {
        triggerJump = true;
    }

    private void HandleMovement() {
        Vector3 moveDirection = LeftJoystick();

        if (anim != null) {
            Vector3 physicsInput = rigidbody.velocity;
            physicsInput.y = 0;
            float targetX = Vector3.ClampMagnitude(physicsInput, 1).x;
            float targetY = Vector3.ClampMagnitude(physicsInput, 1).z;
            // Set moveSpeed in animator
            anim.SetFloat(StaticStrings.anim_moveSpeed, Vector3.ClampMagnitude((physicsInput / moveSpeed) * 2, 1).magnitude);
            // Set horizontal in animator
            anim.SetFloat(StaticStrings.anim_horizontal, Mathf.Lerp(anim.GetFloat(StaticStrings.anim_horizontal), targetX, Time.deltaTime * animationBlendSmoothing));
            // Set vertical in animator
            anim.SetFloat(StaticStrings.anim_vertical, Mathf.Lerp(anim.GetFloat(StaticStrings.anim_vertical), targetY, Time.deltaTime * animationBlendSmoothing));
            // Set lockon in animator
            anim.SetBool(StaticStrings.anim_lockon, locomotionState == LocomotionState.lockon);
        }

        Vector3 lookPos = locomotionState == LocomotionState.normal ? transform.position + moveDirection - transform.position : transform.forward;
        if (lookPos.magnitude <= 0.01f)
            return;

        headTransform.LookAt(new Vector3(lookPos.x, lookPos.y + headHeightTransform.localPosition.y, lookPos.z));

        lookPos.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        Quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveDirection.magnitude * rotateSpeed);

        if(showInput) Debug.DrawRay(groundNormalTransform.position, Vector3.ClampMagnitude(moveDirection, 1), Color.white);

        float maxMagnitude = ((transform.position + (Vector3.up * stepHeight)) - (GetForwardHit().point)).magnitude - 0.1f; 

        movement = Vector3.ClampMagnitude(moveDirection, 1) * moveSpeed;

        if (moveDirection.magnitude > moveInputThreshold && grounded) {
            rigidbody.rotation = rotation;
            rigidbody.velocity = movement;
        }
    }

    private void UpdateGroundNormalTransform() {
        Vector3 groundNormal = GetGroundHit().point != Vector3.zero ? GetGroundHit().normal : Vector3.up;
        Vector3 forwardGroundNormal = GameEngine.GetGroundHit(transform.position + (Vector3.up * stepHeight) + movement).point;
        Vector3 parallel = Vector3.Cross(Camera.main.transform.right, groundNormal);
        float slopeFlatness = Vector3.Dot(Vector3.up, groundNormal);
        float forwardSlopeFlatness = Vector3.Dot(Vector3.up, forwardGroundNormal);
        if(groundNormalAngleDebugTextbox != null) groundNormalAngleDebugTextbox.text = (slopeFlatness * 100).ToString("0");
        if(forwardGroundNormalAngleDebugTextbox != null) forwardGroundNormalAngleDebugTextbox.text = (forwardSlopeFlatness * 100).ToString("0");
        
        Quaternion groundAngleTargetRotation = Quaternion.LookRotation(parallel, groundNormal);
        Quaternion groundAngleRotation = groundNormalTransform != null
            ? groundAngleRotation = Quaternion.Slerp(groundNormalTransform.rotation, groundAngleTargetRotation, Time.deltaTime * groundNormalSmoothTime)
            : Quaternion.identity;

        if (groundNormalTransform == null) {
            GameObject groundUpObject = new GameObject("groundUp");
            groundUpObject.transform.position = GetGroundHit().point;
            groundUpObject.transform.rotation = groundAngleRotation;
            groundNormalTransform = groundUpObject.transform;
        }
        else {
            groundNormalTransform.position = GetGroundHit().point;
            groundNormalTransform.rotation = groundAngleRotation;
        }

        groundNormalTransform.rotation = /* slopeFlatness < minSlopeFlatness ? Quaternion.LookRotation(Camera.main.transform.forward) : */ groundAngleRotation;
        groundNormalTransform.position = GetGroundHit().point;

        if(showGroundNormal) Debug.DrawRay(groundNormalTransform.position, groundNormal, Color.magenta, 5f);
    }

    private Vector3 LeftJoystick() {
        Vector3 _horizontal = Input.GetAxisRaw(StaticStrings.horizontal) * groundNormalTransform.right;
        Vector3 _vertical = Input.GetAxisRaw(StaticStrings.vertical) * groundNormalTransform.forward;
        
        Vector3 r = _horizontal + _vertical;
        return r;
    }

    private Vector3 RightJoystick() {
        float x = Input.GetAxisRaw(StaticStrings.r_horizontal);
        float y = Input.GetAxisRaw(StaticStrings.r_vertical);
        Vector3 r = new Vector3(x, y, 0);
        return r;
    }

    private void HandleCamera() {
        Cinemachine.CinemachineFreeLook cam = freeLookCamera.GetComponent<Cinemachine.CinemachineFreeLook>();
        bool lButton = locomotionState == LocomotionState.lockon;

        cam.m_RecenterToTargetHeading.m_enabled = lButton;
        //cam.m_BindingMode = lButton ? Cinemachine.CinemachineTransposer.BindingMode.LockToTarget : Cinemachine.CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
    }

    public void CheckGrounding() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);

        RaycastHit groundHit;
        Physics.Raycast(new Ray(transform.position, Vector3.down), out groundHit, stepCorrectHeight, gameObject.layer);

        float distToGround = (groundHit.point - transform.position).magnitude;

        grounded = Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, stepHeight + stepCorrectHeight, gameObject.layer);

        if (grounded)
        {
            if (Mathf.Abs((transform.position - hit.point).magnitude) > 0.05f)
            {
                transform.position = Vector3.Lerp(transform.position, hit.point, Time.deltaTime * stepSmoothing);
            }
            else {
                transform.position = hit.point;
            }
            rigidbody.drag = 20;
            rigidbody.useGravity = false;
        }
        else {
            rigidbody.drag = 0;
            rigidbody.useGravity = true;
        }

        if (showGroundCheckRay) Debug.DrawRay(castPoint, Vector3.down * stepHeight);

    }

    public RaycastHit GetForwardHit() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);
        Physics.Raycast(new Ray(castPoint, movement), out hit, 1f, gameObject.layer);

        return hit;
    }

    public RaycastHit GetGroundHit()
    {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);
        Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 1f, gameObject.layer);

        return hit;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if(groundNormalTransform != null && showInput) Gizmos.DrawSphere(groundNormalTransform.position, 1);
    }
}
