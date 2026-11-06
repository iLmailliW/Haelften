using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Camera camera;
    private Transform cameraTransform;

    private const float CAMERA_MAX_X_ANGLE = 89.0f;
    private float cameraXRotation = 0.0f;

    [SerializeField]
    private float cameraSense;


    [SerializeField]
    private float walkSpeed;

    private CharacterController characterController;

    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction flyAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        moveAction = InputSystem.actions.FindAction("Move");
        flyAction = InputSystem.actions.FindAction("Fly");
        cameraTransform = camera.GetComponent<Transform>();
        characterController = gameObject.GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        gameObject.transform.Rotate(new Vector3(0.0f, mouseDelta.x * cameraSense, 0.0f), Space.World);
        float cameraMaxXRotation = CAMERA_MAX_X_ANGLE - cameraXRotation;
        float cameraMinXRotation = -CAMERA_MAX_X_ANGLE - cameraXRotation;

        cameraTransform.Rotate(new Vector3(math.clamp(-mouseDelta.y * cameraSense, cameraMinXRotation, cameraMaxXRotation), 0.0f, 0.0f), Space.Self);
        cameraXRotation += math.clamp(-mouseDelta.y * cameraSense, cameraMinXRotation, cameraMaxXRotation);

        Vector2 moveDelta = moveAction.ReadValue<Vector2>();
        if (moveDelta.SqrMagnitude() != 0)
        {
            moveDelta.Normalize();
        }
        float flyDelta = flyAction.ReadValue<float>();
        characterController.Move((transform.forward * moveDelta.y + transform.right * moveDelta.x + transform.up * flyDelta) * walkSpeed * Time.deltaTime);

    }
}
