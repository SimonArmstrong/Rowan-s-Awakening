using UnityEngine;

public class Player : MonoBehaviour
{
    private enum LocomotionState { normal, lockon }

    [Header("Settings")]
    [SerializeField, Tooltip("How far the player needs to move the joystick before any movement input is recived. Excess input is used for rotation")]
    private float moveInputThreshold = 0.12f;
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float rotateSpeed = 30;
    [SerializeField] private float widthCheck = 0.32f;
    [SerializeField] private float animationBlendSmoothing = 5f;
    [SerializeField] private LocomotionState locomotionState;

    [Header("Vaulting Settings")]
    [SerializeField] private Vector3 vaultPos;
    [SerializeField] private float ledgeGrabHeight = 0.3f;
    [SerializeField] private float jumpToLedgeGrabHeight = 1.7f;
    [SerializeField] private float mountHeight = 0.55f;
    [SerializeField] private float hopHeight = 0.32f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 30;
    [SerializeField] private float forwardGroundDistanceToJump = 0.5f;


    [Header("Step Settings")]
    [SerializeField] private float stepHeight = 0.1f;
    [SerializeField] private float stepSmoothing = 30;
    [SerializeField] private float stepCorrectHeight = 0.3f;

    [Header("Grounding")]
    [SerializeField] private float heightCheck = 0.4f;
    [SerializeField] private Transform groundNormalTransform;
    [SerializeField] private float groundNormalSmoothTime = 200;
    [SerializeField, Range(0.1f, 0.9f)] private float minSlopeFlatness;
    [SerializeField] private bool grounded = false;

    [Header("Attachments")]
    [SerializeField] private Model model;
    [SerializeField] private GameObject freeLookCamera;
    public TMPro.TextMeshProUGUI groundNormalAngleDebugTextbox;
    public TMPro.TextMeshProUGUI forwardGroundNormalAngleDebugTextbox;

    [Header("Transforms")]
    public Transform headHeightTransform;

    [Header("Action Flags")]
    public bool shouldLedgeGrab;
    public bool shouldVault;
    public bool canJumpToLedgeGrab;
    public bool canMount;
    public bool canHop;

    [Header("States")]
    public bool jumping;
    public bool vaulting;

    [Header("Action Triggers")]
    [SerializeField] private bool jumpInput = false;
    [SerializeField] private bool hop = false;
    [SerializeField] private bool mount = false;
    [SerializeField] private bool jumpToLedge = false;
    [SerializeField] private bool grabLedge = false;

    [Header("Debug")]
    [SerializeField] private bool showGroundNormal;
    [SerializeField] private bool showGroundCheckRay;
    [SerializeField] private bool showInput;
    [SerializeField] private bool showLedgeGrabRay;
    [SerializeField] private bool showVaultRay;
    [SerializeField] public Vector3 movement;

    private new Rigidbody rigidbody;
    private bool stepAdjust = true;

    // Called on Start in PlayerManager
    public void Init() {
        UpdateGroundNormalTransform();
        rigidbody = GetComponent<Rigidbody>();
        if (model == null) model = GetComponentInChildren<Model>();
        model.stepHeight = stepHeight;
        model.Init();
    }

    // Called on Update in PlayerManager
    public void Tick() {
        HandleActionInputs();
        CheckGrounding();
        CheckVaulting();
        UpdateGroundNormalTransform();
    }

    // Called on FixedUpdate in PlayerManager
    public void FixedTick() {
        HandleLocomotionState();
        HandleCamera();
        HandleLedgeGrab();
        HandleMovement();
        HandleVaulting();
        HandleJumping();
    }

    // -------------------------------------------

    private void HandleActionInputs() {
        jumpInput = Input.GetButtonDown(StaticStrings.jump);
    }

    private void HandleLedgeGrab() {
        Vector3 ledgePos = GetLedgeGrabHit().point;
        if (!grounded && shouldLedgeGrab) {
            // Grab Ledge
            jumpToLedge = false;
            mount = false;
            jumpToLedge = false;
            jumpInput = false;
            jumping = false;
        }
    }

    private void HandleLocomotionState()
    {
        bool lButton = Input.GetButton(StaticStrings.leftBumper);
        locomotionState = lButton == true ? LocomotionState.lockon : LocomotionState.normal;
    }

    private void HandleJumping() {
        if (grounded && jumpInput) {
            rigidbody.velocity = Vector3.zero;
            rigidbody.AddForce(movement + (Vector3.up * jumpHeight), ForceMode.VelocityChange);
            jumpInput = false;
            jumping = true;

            if(model.anim != null)
                model.anim.CrossFade("Jump", 0.2f);
        }
    }

    private void HandleCamera() {
        Cinemachine.CinemachineFreeLook cam = freeLookCamera.GetComponent<Cinemachine.CinemachineFreeLook>();
        Cinemachine.CinemachineBrain brain = Camera.main.transform.GetComponent<Cinemachine.CinemachineBrain>();

        bool lButton = locomotionState == LocomotionState.lockon;
        cam.m_RecenterToTargetHeading.m_enabled = lButton;
        //cam.m_BindingMode = lButton ? Cinemachine.CinemachineTransposer.BindingMode.LockToTarget : Cinemachine.CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
    }

    private void HandleMovement() {
        Vector3 moveDirection = CameraRelativeLeftJoystick();

        if (model.anim != null) {
            Vector3 physicsInput = rigidbody.velocity;
            physicsInput.y = 0;
            float targetX = Vector3.ClampMagnitude(LeftJoystick(), 1).x;
            float targetY = Vector3.ClampMagnitude(LeftJoystick(), 1).z;
            // Set moveSpeed in animator
            model.anim.SetFloat(StaticStrings.anim_moveSpeed, Vector3.ClampMagnitude((physicsInput / moveSpeed) * 2, 1).magnitude);
            // Set horizontal in animator
            model.anim.SetFloat(StaticStrings.anim_horizontal, targetX);
            // Set vertical in animator
            model.anim.SetFloat(StaticStrings.anim_vertical, targetY);
            // Set lockon in animator
            model.anim.SetBool(StaticStrings.anim_lockon, locomotionState == LocomotionState.lockon);
        }

        Vector3 lookPos = locomotionState == LocomotionState.normal ? transform.position + moveDirection - transform.position : transform.forward;
        if (lookPos.magnitude <= 0.01f)
            return;

        lookPos.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        Quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveDirection.magnitude * rotateSpeed);

        if(showInput) Debug.DrawRay(groundNormalTransform.position, Vector3.ClampMagnitude(moveDirection, 1), Color.white);

        float maxMagnitude = ((transform.position + (Vector3.up * stepHeight)) - (GetForwardHit().point)).magnitude - 0.1f; 

        movement = Vector3.ClampMagnitude(moveDirection, 1) * moveSpeed;
        model.useIK = moveDirection.magnitude < moveInputThreshold;

        if (moveDirection.magnitude > moveInputThreshold && grounded) {
            rigidbody.rotation = rotation;
            rigidbody.velocity = movement;
        }


        RaycastHit forwardGroundPoint = GetForwardGroundHit();
        if ((forwardGroundPoint.point.y <= transform.position.y - forwardGroundDistanceToJump) && moveDirection.magnitude > 0.8f) {
            if(Flatness(forwardGroundPoint.normal) >= 0.1f)
                jumpInput = true;
        }
    }

    private void HandleVaulting() {
        if (jumpToLedge){

            return;
        }
        else if (mount){

            return;
        }
        else if (hop) {
            transform.position = vaultPos;
            return;
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

    public void CheckGrounding() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);

        // Turn off step adjusting if we're jumping this frame
        stepAdjust = !jumping;
        // Total raycast check length
        float dist = stepAdjust ? stepHeight + stepCorrectHeight : stepHeight - 0.02f;

        grounded = Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, dist, gameObject.layer);

        if (model.anim != null)
            model.anim.SetBool("grounded", grounded);

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
            jumping = false;
        }
        else {
            rigidbody.drag = 0;
            rigidbody.useGravity = true;

            if (model.anim != null)
                model.anim.CrossFade("Falling", 0.02f);
        }

        if (showGroundCheckRay) Debug.DrawRay(castPoint, Vector3.down * stepHeight);

    }

    public void CheckVaulting() {
        Vector3 ledgePos = GetVaultHit(100, ref shouldVault).point;
        hop = ledgePos.y > (transform.position + Vector3.up * (hopHeight)).y;
        mount = ledgePos.y > (transform.position + Vector3.up * (mountHeight)).y;
        jumpToLedge = ledgePos.y > (transform.position + Vector3.up * (jumpToLedgeGrabHeight)).y;

        vaultPos = ledgePos;
    }

    public bool IsGroundFlat(Vector3 checkPos) {
        Vector3 normal = GameEngine.GetGroundHit(checkPos).normal;
        return Flatness(normal) >= 0.95;
    }

    public float Flatness(Vector3 normal) {
        float flatness = Vector3.Dot(Vector3.up, normal);
        return flatness;
    }

    private Vector3 CameraRelativeLeftJoystick() {
        Vector3 _horizontal = Input.GetAxisRaw(StaticStrings.horizontal) * groundNormalTransform.right;
        Vector3 _vertical = Input.GetAxisRaw(StaticStrings.vertical) * groundNormalTransform.forward;
        
        Vector3 r = _horizontal + _vertical;
        return r;
    }
    private Vector3 LeftJoystick()
    {
        float _horizontal = Input.GetAxisRaw(StaticStrings.horizontal);
        float _vertical = Input.GetAxisRaw(StaticStrings.vertical);

        Vector3 r = new Vector3(_horizontal, 0,  _vertical);
        return r;
    }
    private Vector3 RightJoystick() {
        float x = Input.GetAxisRaw(StaticStrings.r_horizontal);
        float y = Input.GetAxisRaw(StaticStrings.r_vertical);
        Vector3 r = new Vector3(x, y, 0);
        return r;
    }

    public RaycastHit GetForwardHit() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);
        Physics.Raycast(new Ray(castPoint, movement), out hit, 1f, gameObject.layer);

        return hit;
    }
    public RaycastHit GetGroundHit() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);
        Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 1f, gameObject.layer);

        return hit;
    }
    public RaycastHit GetForwardGroundHit()
    {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (movement * 0.02f) + (Vector3.up * 10);
        Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 100f, gameObject.layer);
        GameEngine.DrawZPlaneCrossGizmo(hit.point, widthCheck);
        return hit;
    }
    public RaycastHit GetLedgeGrabHit() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * ledgeGrabHeight) + (transform.forward * widthCheck);
        shouldLedgeGrab = Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 1f, gameObject.layer);
        if (showLedgeGrabRay)
            Debug.DrawRay(castPoint, Vector3.down, Color.green);
        return hit;
    }
    public RaycastHit GetVaultHit(float vaultHeight, ref bool canDo) {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * vaultHeight) + (transform.forward * widthCheck);
        bool didHit = Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 1000f, gameObject.layer);
        Vector3 normal = hit.normal;
        float flatness = Vector3.Dot(Vector3.up, normal);
        canDo = flatness >= 0.95 && didHit;

        if (showVaultRay)
        {
            GameEngine.DrawZPlaneCrossGizmo(castPoint, widthCheck);
            Debug.DrawRay(castPoint, Vector3.down, Color.blue);
        }
        return hit;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if(groundNormalTransform != null && showInput) Gizmos.DrawSphere(groundNormalTransform.position, 1);
    }
}
