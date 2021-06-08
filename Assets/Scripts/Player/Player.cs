using UnityEngine;

struct Timer {
    float length, current;
    Timer(float l){ length = l; current = 0; }
}

public class Player : MonoBehaviour
{
    private enum LocomotionState { normal, lockon }
    private enum ActionStatus { none, leapingToLedge, ledgeGrab, pullingUp, blocking, jumping }

    [Header("Settings")]
    [SerializeField, Tooltip("How far the player needs to move the joystick before any movement input is recived. Recess input is used for rotation")]
    private float moveInputThreshold = 0.12f;
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float rotateSpeed = 30;
    [SerializeField] private float animationBlendSmoothing = 5f;
    [SerializeField] private float maxHeadLookUp = 1.5f, maxHeadLookDown = -.09f;
    [SerializeField] private LocomotionState locomotionState;
    [SerializeField] private ActionStatus actionState;
    [SerializeField] private ActionStatus previousState;

    [Header("Vaulting Settings")]
    [SerializeField] private Vector3 vaultPos;
    [SerializeField] private float ledgeGrabHeight = 0.3f;
    [SerializeField] private float jumpToLedgeGrabHeight = 1.7f;
    [SerializeField] private float mountHeight = 0.55f;
    [SerializeField] private float hopHeight = 0.32f;
    [SerializeField] private float leapSpeed = 2.0f;
    [SerializeField] private float widthCheck = 0.32f;
    [SerializeField] private float midairWidthCheck = 0.7f;
    [SerializeField] private float vaultDelay = 1.0f;
    [SerializeField] private float pullUpDelay = 0.9f;
    [SerializeField] private float armHeight = 0.5f;
    private float vaultTimer = 0.0f;
    

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 30;
    [SerializeField] private float jumpForwardSpeed = 10;
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
    public bool shouldVault;
    public bool canJumpToLedgeGrab;
    public bool canMount;
    public bool canHop;

    [Header("States")]
    public bool jumping;
    public bool vaulting;
    public bool hanging;

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
    [SerializeField] private bool showLedgeHitPoint;
    [SerializeField] public Vector3 movement;

    private new Rigidbody rigidbody;
    private bool stepAdjust = true;
    private Vector3 headLookTarget;

    // Called on Start in PlayerManager
    public void Init() {
        rigidbody = GetComponent<Rigidbody>();
        if (model == null) model = GetComponentInChildren<Model>();
        if (model != null){
            model.stepHeight = stepHeight;
            model.Init();
        }
        UpdateGroundNormalTransform();
    }

    // Called on Update in PlayerManager
    public void Tick() {
        switch(actionState){
            case ActionStatus.none:
                CheckForActionInputs();
                CheckLedge();
                UpdateGroundNormalTransform();
                CheckGrounding();
            break;
            case ActionStatus.leapingToLedge:
            break;
            case ActionStatus.ledgeGrab:
                
            break;
        }
    }

    // Called on FixedUpdate in PlayerManager
    public void FixedTick() {
        headLookTarget = transform.position + transform.forward;
        float _targetY = transform.position.y;
        if(jumpToLedge || mount)
            _targetY = vaultPos.y - 1.1f;
        else
            _targetY = vaultPos.y;


        headLookTarget = new Vector3(vaultPos.x, Mathf.Clamp(_targetY, transform.position.y - maxHeadLookDown, transform.position.y + maxHeadLookUp), vaultPos.z)+transform.forward;

        model.lookPos = headLookTarget;
        switch(actionState){
            case ActionStatus.none:
                HandleLocomotionState();
                HandleCamera();
                HandleMovement();
                HandleJumping();
                HandleVaulting();
            break;
            case ActionStatus.leapingToLedge:
                HandleLeapToLedge();
            break;
            case ActionStatus.ledgeGrab:
                HandleLedgeGrabState();
            break;
            case ActionStatus.pullingUp:
                HandlePullUpState();
            break;
            case ActionStatus.jumping:
            break;
        }
    }

    // -------------------------------------------

    private void CheckForActionInputs() {
        jumpInput = Input.GetButtonDown(StaticStrings.jump);

        if(jumpInput) jumpInput = true;
    }

    private void HandlePullUpState(){
        SetAnimatorBool("pullUp", true);
        rigidbody.position = Vector3.Lerp(rigidbody.position, vaultPos, Time.deltaTime * 80);
        _pullUpTimer += Time.deltaTime;
        if(_pullUpTimer >= _pullUpTime) {
            _pullUpTimer = 0;
            jumping = false;
            SetAnimatorBool("pullUp", false);
            SetAnimatorBool("hanging", false);
            ChangeState(ActionStatus.none);     ///---> Exits state
        }
    }

    float _waitToLeap = 0;
    private void HandleLeapToLedge() {
        _waitToLeap += Time.deltaTime;
        if(_waitToLeap >= 0.5f){

            FaceObjectInFront();

            rigidbody.velocity = Vector3.up * leapSpeed;
            if(rigidbody.position.y + (jumpToLedgeGrabHeight + armHeight) >= vaultPos.y){
                SetAnimatorTrigger("grabLedge");
                _waitToLeap = 0;
                ChangeState(ActionStatus.ledgeGrab);    ///---> Exits State
            }
        }
    }

    float _pullUpTime = 1.0f;
    float _pullUpTimer = 0;
    float _hlgsTimer = 0;

    private void HandleLedgeGrabState() {       // On a ledge
        SetAnimatorBool("hanging", true);
        // Snap position to ledge anchored at the armHeight
        // rigidbody.position = new Vector3(vaultPos.x, vaultPos.y - (jumpToLedgeGrabHeight - armHeight), vaultPos.z) + (GetForwardHit().normal*0.1f);
        rigidbody.position = new Vector3(rigidbody.position.x, vaultPos.y - (jumpToLedgeGrabHeight - armHeight), rigidbody.position.z);
        _hlgsTimer += Time.deltaTime;

        FaceObjectInFront();

        if(LeftJoystick().magnitude >= 0.5f && _hlgsTimer >= pullUpDelay){
            _hlgsTimer = 0;
            ChangeState(ActionStatus.pullingUp);
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
            rigidbody.drag = 0;
            rigidbody.useGravity = true;

            rigidbody.AddForce(new Vector3(movement.x, 0, movement.z).normalized * jumpForwardSpeed + (Vector3.up * jumpHeight), ForceMode.VelocityChange);
            jumpInput = false;
            jumping = true;

            PlayAnimation(StaticStrings.anim_jump, 0.2f);
        }
    }

    private void HandleCamera() {
        Cinemachine.CinemachineFreeLook cam = freeLookCamera.GetComponent<Cinemachine.CinemachineFreeLook>();
        Cinemachine.CinemachineBrain brain = Camera.main.transform.GetComponent<Cinemachine.CinemachineBrain>();

        bool lButton = locomotionState == LocomotionState.lockon;
        //cam.m_RecenterToTargetHeading.m_enabled = lButton;
        //cam.m_BindingMode = lButton ? Cinemachine.CinemachineTransposer.BindingMode.LockToTarget : Cinemachine.CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
    }

    private void HandleMovement() {
        Vector3 moveDirection = CameraRelativeLeftJoystick();

        Vector3 physicsInput = rigidbody.velocity;
        physicsInput.y = 0;
        float targetX = Vector3.ClampMagnitude(LeftJoystick(), 1).x;
        float targetY = Vector3.ClampMagnitude(LeftJoystick(), 1).z;
        float groundAngle = GetForwardAngle(transform.position);

        // Set moveSpeed in animator
        SetAnimatorFloat(StaticStrings.anim_moveSpeed, Vector3.ClampMagnitude((physicsInput / moveSpeed) * 2, 1).magnitude);
        // Set horizontal in animator
        SetAnimatorFloat(StaticStrings.anim_horizontal, targetX);
        // Set vertical in animator
        SetAnimatorFloat(StaticStrings.anim_vertical, targetY);
        // Set lockon in animator
        SetAnimatorBool(StaticStrings.anim_lockon, locomotionState == LocomotionState.lockon);
        // Set groundAngle in animator
        //SetAnimatorFloat("groundAngle", groundAngle);

        if (groundNormalAngleDebugTextbox != null) groundNormalAngleDebugTextbox.text = /*(slopeFlatness * 100)*/"GroundAngle: " + groundAngle.ToString("0.0");

        Vector3 lookPos = locomotionState == LocomotionState.normal 
            ? moveDirection //transform.position + moveDirection - transform.position 
            : CameraForward();                  /*transform.forward;*/

        if (lookPos.magnitude <= 0.01f)
            return;

        lookPos.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        Quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveDirection.magnitude * rotateSpeed);

        if(showInput) Debug.DrawRay(groundNormalTransform.position, Vector3.ClampMagnitude(moveDirection, 1), Color.white);

        float maxMagnitude = ((transform.position + (Vector3.up * stepHeight)) - (GetForwardHit().point)).magnitude - 0.1f; 

        movement = moveDirection.normalized * moveDirection.magnitude * moveSpeed;
        if (model != null) model.useIK = moveDirection.magnitude < moveInputThreshold;

        if (moveDirection.magnitude > moveInputThreshold && grounded) {
            rigidbody.rotation = rotation;
            rigidbody.velocity = movement;
        }

        
        RaycastHit forwardGroundPoint = GetForwardGroundHit();
        if (((forwardGroundPoint.point.y <= transform.position.y - forwardGroundDistanceToJump) && moveDirection.magnitude > 0.8f) || GetForwardAngle(forwardGroundPoint.normal) > 135f) {
            jumpInput = true;
            //ChangeState(ActionStatus.jumping);
        }
    }

    private void HandleVaulting() {
        if (jumpToLedge){
            //if(shouldLedgeGrab){
            if (!grounded) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.useGravity = false;
                PlayAnimation("Hanging Idle");
                SetAnimatorTrigger("grabLedge");
                ChangeState(ActionStatus.ledgeGrab);
                vaultTimer = 0;
                return;
            }

            if(LeftJoystick().magnitude > 0.02f) {
                vaultTimer += Time.deltaTime;
                if(vaultTimer >= vaultDelay){
                    PlayAnimation("Idle to Hang");
                    ChangeState(ActionStatus.leapingToLedge);
                    vaultTimer = 0; 
                }
            }
            else 
                vaultTimer = 0;
            //}
            return;
        }
        else if (mount){
            if (!grounded) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.useGravity = false;
                PlayAnimation("Hanging Idle");
                SetAnimatorTrigger("grabLedge");
                ChangeState(ActionStatus.ledgeGrab);
                vaultTimer = 0;
                return;
            }


            if(LeftJoystick().magnitude > 0.02f) {
                vaultTimer += Time.deltaTime;
                if(vaultTimer >= vaultDelay){
                    FaceObjectInFront();
                    PlayAnimation("Hang Pull Up", 0);
                    ChangeState(ActionStatus.pullingUp);
                    vaultTimer = 0; 
                }
            }
            else 
                vaultTimer = 0;
            return;
        }
        else if (hop) {
              if (!grounded) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.useGravity = false;
                PlayAnimation("Hanging Idle");
                SetAnimatorTrigger("grabLedge");
                ChangeState(ActionStatus.ledgeGrab);
                vaultTimer = 0;
                return;
            }

            if(LeftJoystick().magnitude > 0.02f) {
                vaultTimer += Time.deltaTime;
                if(vaultTimer >= vaultDelay){
                    FaceObjectInFront();
                    PlayAnimation("Hang Pull Up", 0);
                    ChangeState(ActionStatus.pullingUp);
                    vaultTimer = 0; 
                }
            }
            else 
                vaultTimer = 0;
            return;
        }
    }

    private void UpdateGroundNormalTransform() {
        RaycastHit groundHit = GetGroundHit(transform.position);

        Vector3 groundNormal = groundHit.point != Vector3.zero ? groundHit.normal : Vector3.up;
        Vector3 forwardGroundNormal = GameEngine.GetGroundHit(transform.position + (Vector3.up * stepHeight) + movement).point;
        Vector3 cameraForward = Vector3.Cross(Camera.main.transform.right, groundNormal);

        float slopeFlatness = Vector3.Dot(Vector3.up, groundNormal);
        float forwardSlopeFlatness = Vector3.Dot(Vector3.up, forwardGroundNormal); // Only used for debug purposes at the moment
        
        if (forwardGroundNormalAngleDebugTextbox != null) forwardGroundNormalAngleDebugTextbox.text = (forwardSlopeFlatness * 100).ToString("0");

        Quaternion groundAngleTargetRotation = Quaternion.LookRotation(cameraForward, groundNormal);
        Quaternion groundAngleRotation = groundNormalTransform != null
            ? groundAngleRotation = Quaternion.Slerp(groundNormalTransform.rotation, groundAngleTargetRotation, Time.deltaTime * groundNormalSmoothTime)
            : Quaternion.identity;

        if (groundNormalTransform == null) {
            GameObject groundUpObject = new GameObject("groundUp");
            groundUpObject.transform.position = groundHit.point;
            groundUpObject.transform.rotation = groundAngleRotation;
            groundNormalTransform = groundUpObject.transform;
        }
        else {
            groundNormalTransform.position = groundHit.point;
            groundNormalTransform.rotation = groundAngleRotation;
        }

        groundNormalTransform.rotation = /* slopeFlatness < minSlopeFlatness ? Quaternion.LookRotation(Camera.main.transform.forward) : */ groundAngleRotation;
        groundNormalTransform.position = groundHit.point;

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
        SetAnimatorBool("grounded", grounded);
        
        if (grounded) {
            if (Mathf.Abs((transform.position - hit.point).magnitude) > 0.05f) {
                transform.position = Vector3.Lerp(transform.position, hit.point, Time.deltaTime * stepSmoothing);
            }
            else {
                transform.position = hit.point;
            }
            rigidbody.drag = 20;
            rigidbody.useGravity = false;
            jumping = false;
            //ChangeState(previousState);
            widthCheck = 0.45f;
        }
        else {
            widthCheck = .7f;
            if (!jumping){
                rigidbody.drag = 0;
                rigidbody.useGravity = true;

                PlayAnimation("Falling", 0.02f);
            }
        }

        if (showGroundCheckRay) Debug.DrawRay(castPoint, Vector3.down * stepHeight);

    }

    public bool IsGroundFlat(Vector3 checkPos) {
        Vector3 normal = GameEngine.GetGroundHit(checkPos).normal;
        return Flatness(normal) >= 0.95;
    }

    public float Flatness(Vector3 normal) {
        float flatness = Vector3.Dot(Vector3.up, normal);
        return flatness;
    }

    public float GetForwardAngle(Vector3 position) {
        RaycastHit groundHit = GetGroundHit(position);

        Vector3 groundNormal = groundHit.point != Vector3.zero ? groundHit.normal : Vector3.up;
        Vector3 playerForward = Vector3.Cross(transform.right, groundNormal);

        float angle = Vector3.Angle(playerForward, Vector3.up);

        return angle;
    }

    public float GetGroundAngle(Vector3 position) {
        RaycastHit groundHit = GetGroundHit(position);

        Vector3 groundNormal = groundHit.point != Vector3.zero ? groundHit.normal : Vector3.up;

        float angle = Vector3.Angle(groundNormal, Vector3.up);

        return angle;
    }

    private Vector3 CameraForward() {
        Camera c = PlayerManager.instance.playerCamera;
        return new Vector3(c.transform.forward.x, 0, c.transform.forward.z).normalized;
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

    public void CheckLedge() {
        Vector3 ledgePos = GetLedgeHit(ledgeGrabHeight, ref shouldVault).point;
        if (ledgePos.y <= ledgeGrabHeight + transform.position.y && shouldVault) {
            hop = ledgePos.y > (transform.position + Vector3.up * (hopHeight)).y;
            mount = ledgePos.y > (transform.position + Vector3.up * (mountHeight)).y;
            jumpToLedge = ledgePos.y > (transform.position + Vector3.up * (jumpToLedgeGrabHeight)).y;
        }
        else  {
            hop = false;
            mount = false;
            jumpToLedge = false;
        }

        vaultPos = ledgePos;
    }
    
    /// <summary> Shoots a ray forward and returns a point in front of the player, @stepHeight height </summary>
    public RaycastHit GetForwardHit() {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * stepHeight);
        Physics.Raycast(new Ray(castPoint, movement), out hit, 1f, gameObject.layer);

        return hit;
    }
    
    /// <summary> Shoots a ray down and returns a point below the player </summary>
    public RaycastHit GetGroundHit(Vector3 position) {
        RaycastHit hit;
        Vector3 castPoint = position + (Vector3.up * stepHeight);
        Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 1f, gameObject.layer);

        return hit;
    }

    /// <summary> Shoots a ray forward and down, and returns a point on the floor, slightly in-front of (.02m) the player </summary>
    public RaycastHit GetForwardGroundHit()
    {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (movement * 0.02f) + (Vector3.up * ledgeGrabHeight);
        Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 100f, gameObject.layer);
        GameEngine.DrawZPlaneCrossGizmo(hit.point, widthCheck);
        return hit;
    }

    /// <summary> Shoots a ray down from @p_vaultHeight height and in-front of the player @widthCheck width, and returns a point up to @p_vaultHeight below that point </summary>
    public RaycastHit GetLedgeHit(float vaultHeight, ref bool canDo) {
        RaycastHit hit;
        Vector3 castPoint = transform.position + (Vector3.up * vaultHeight) + (transform.forward * widthCheck);
        bool didHit = Physics.Raycast(new Ray(castPoint, Vector3.down), out hit, 1000f, gameObject.layer);
        float groundAngle = GetGroundAngle(castPoint + Vector3.down * vaultHeight * 0.5f);
        canDo = hit.point.y > transform.position.y && (groundAngle < 45);

        if (showLedgeHitPoint)
        {
            if(didHit) GameEngine.DrawZPlaneCrossGizmo(hit.point, widthCheck);
            Debug.DrawRay(castPoint, Vector3.down * vaultHeight, Color.blue);
        }
        return hit;
    }



    private void ChangeState(ActionStatus state){
        previousState = actionState;
        actionState = state;
    }

    private void PlayAnimation(string anim, float xFadeTime = (0.2f)){
        if(model != null){
            if(model.anim != null)
                model.anim.CrossFade(anim, xFadeTime);
        }
    }

    private void SetAnimatorTrigger(string anim){
        if(model != null){
            if(model.anim != null)
                model.anim.SetTrigger(anim);
        }
    }

    private void SetAnimatorBool(string n, bool val){
        if(model != null){
            if(model.anim != null){
                model.anim.SetBool(n, val);
            }
        }
    }

    private void SetAnimatorFloat(string n, float val){
        if(model != null){
            if(model.anim != null){
                model.anim.SetFloat(n, val);
            }
        }
    }

    private void FaceObjectInFront(){
        Vector3 _look = -GetForwardHit().normal;
        _look.y = 0;
        rigidbody.rotation = Quaternion.LookRotation(_look);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if(groundNormalTransform != null && showInput) Gizmos.DrawSphere(groundNormalTransform.position, 1);
    }
}
