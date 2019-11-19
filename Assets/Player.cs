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
    [SerializeField] private float jumpForce = 30;
    [SerializeField] private LocomotionState locomotionState;
    [Header("Grounding")]
    [SerializeField] private float groundNormalSmoothTime = 200;
    [SerializeField, Range(0.1f, 0.9f)] private float slopeAngleLimit;
    [SerializeField] private bool grounded = false;
    [Header("Attachments")]
    [SerializeField] private GameObject freeLookCamera;
    [SerializeField] private Transform groundNormalTransform;


    private new Rigidbody rigidbody;

    public Transform headTransform;
    public Transform headHeightTransform;
    public TMPro.TextMeshProUGUI groundNormalAngleDebugTextbox;

    public float stepHeight = 0.1f;


    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        UpdateGroundNormalTransform();
        HandleLocomotionState();
        HandleCamera();
        HandleMovement();
        HandleJumping();
    }

    private void HandleLocomotionState()
    {
        bool lButton = Input.GetButton(StaticStrings.leftBumper);
        locomotionState = lButton == true ? LocomotionState.lockon : LocomotionState.normal;
    }

    private void HandleJumping() {
        if (Input.GetButtonDown(StaticStrings.jump) && grounded) {
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleMovement() {
        Vector3 moveDirection = LeftJoystick();

        if (anim != null) {
            if (moveDirection.magnitude > moveInputThreshold) {
                anim.SetFloat(StaticStrings.anim_moveSpeed, moveDirection.magnitude);
                anim.SetFloat(StaticStrings.anim_horizontal, moveDirection.normalized.x * moveDirection.magnitude);
                anim.SetFloat(StaticStrings.anim_vertical, moveDirection.normalized.z * moveDirection.magnitude);
                anim.SetBool(StaticStrings.anim_lockon, locomotionState == LocomotionState.lockon);
            }
            else {
                anim.SetFloat(StaticStrings.anim_moveSpeed, 0);
                anim.SetFloat(StaticStrings.anim_horizontal, 0);
                anim.SetFloat(StaticStrings.anim_vertical, 0);
                anim.SetBool(StaticStrings.anim_lockon, false);
            }
        }

        Vector3 lookPos = locomotionState == LocomotionState.normal ? transform.position + moveDirection - transform.position : transform.forward;
        //Quaternion headRotation = Quaternion.LookRotation();
        if (lookPos.magnitude <= 0.01f)
            return;

        headTransform.LookAt(new Vector3(lookPos.x, lookPos.y + headHeightTransform.localPosition.y, lookPos.z));

        lookPos.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        Quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveDirection.magnitude * rotateSpeed);

        Debug.DrawRay(groundNormalTransform.position, Vector3.ClampMagnitude(moveDirection, 1), Color.white);

        rigidbody.rotation = rotation;
        if(moveDirection.magnitude > moveInputThreshold)
            rigidbody.position += Vector3.ClampMagnitude(moveDirection, 1) * Time.deltaTime * moveSpeed;
    }

    private void UpdateGroundNormalTransform() {
        Vector3 groundNormal = GetGroundNormal();
        Vector3 parallel = new Vector3();
        float groundSlopeAngle = Vector3.Dot(Vector3.up, groundNormal);
        if(groundNormalAngleDebugTextbox != null) groundNormalAngleDebugTextbox.text = groundSlopeAngle.ToString("0.0");
        if (groundSlopeAngle < slopeAngleLimit) {
            parallel = Vector3.Cross(transform.forward, groundNormal);
        }
        else {
            parallel = Vector3.Cross(Camera.main.transform.right, groundNormal);
        }

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

        groundNormalTransform.rotation = groundAngleRotation;
        groundNormalTransform.position = GetGroundHit().point;

        Debug.DrawRay(groundNormalTransform.position, groundNormal, Color.magenta, 5f);
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

    public RaycastHit GetGroundHit()
    {
        RaycastHit hit;
        grounded = Physics.Raycast(new Ray(transform.position + (Vector3.up * stepHeight), Vector3.down), out hit, 0.4f, gameObject.layer);

        return hit;
    }

    public Vector3 GetGroundNormal()
    {
        RaycastHit hit;
        Physics.Raycast(new Ray(transform.position + (Vector3.up * stepHeight), Vector3.down), out hit, 5f, gameObject.layer);

        return hit.normal;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if(groundNormalTransform != null) Gizmos.DrawSphere(groundNormalTransform.position, 1);
    }
}
