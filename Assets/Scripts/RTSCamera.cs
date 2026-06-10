using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCamera : MonoBehaviour
{
    const float FixedZ = -10f;

    [Header("Movement")]
    public float moveSpeed = 20f;
    public float screenEdgeThickness = 10f;
    public bool useScreenEdge = true;

    [Header("Map Bounds")]
    public Vector2 minMaxX = new Vector2(-50f, 50f);
    public Vector2 minMaxY = new Vector2(-50f, 50f);

    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minHeight = 5f;
    public float maxHeight = 40f;

    Camera cameraComponent;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        LockZPosition();
    }

    void Update()
    {
        Move();
        Zoom();
    }

    void LateUpdate()
    {
        LockZPosition();
    }

    void Move()
    {
        Vector2 keyboardMove = GetKeyboardMoveInput();
        Vector3 moveDirection = new Vector3(keyboardMove.x, keyboardMove.y, 0f);

        Mouse mouse = Mouse.current;
        if (useScreenEdge && mouse != null)
        {
            Vector2 mousePosition = mouse.position.ReadValue();

            if (mousePosition.y >= Screen.height - screenEdgeThickness)
            {
                moveDirection += Vector3.up;
            }
            if (mousePosition.y <= screenEdgeThickness)
            {
                moveDirection -= Vector3.up;
            }
            if (mousePosition.x >= Screen.width - screenEdgeThickness)
            {
                moveDirection += Vector3.right;
            }
            if (mousePosition.x <= screenEdgeThickness)
            {
                moveDirection -= Vector3.right;
            }
        }

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, minMaxX.x, minMaxX.y);
        newPosition.y = Mathf.Clamp(newPosition.y, minMaxY.x, minMaxY.y);
        newPosition.z = FixedZ;

        transform.position = newPosition;
    }

    Vector2 GetKeyboardMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float moveX = 0f;
        float moveY = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            moveX -= 1f;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            moveX += 1f;
        }
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            moveY -= 1f;
        }
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            moveY += 1f;
        }

        return new Vector2(moveX, moveY);
    }

    void Zoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float scroll = mouse.scroll.ReadValue().y / 120f;
        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        Camera targetCamera = cameraComponent != null ? cameraComponent : Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        float zoomDelta = scroll * zoomSpeed * 10f * Time.deltaTime;
        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize - zoomDelta,
                minHeight,
                maxHeight);
        }
        else
        {
            targetCamera.fieldOfView = Mathf.Clamp(
                targetCamera.fieldOfView - zoomDelta,
                minHeight,
                maxHeight);
        }
    }

    void LockZPosition()
    {
        Vector3 position = transform.position;
        if (Mathf.Approximately(position.z, FixedZ))
        {
            return;
        }

        position.z = FixedZ;
        transform.position = position;
    }
}
