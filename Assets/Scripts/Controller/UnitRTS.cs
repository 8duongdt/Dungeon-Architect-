using UnityEngine;

public class UnitRTS : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f; // Tốc độ di chuyển
    private GameObject selectedVisual; // Vòng tròn hiển thị dưới chân
    private Vector3 targetPosition;

    private void Awake()
    {
        // Tìm object con có tên "Selected" (nhớ tạo một sprite con tên này trong lính của bạn)
        Transform selectedTransform = transform.Find("Selected");
        if (selectedTransform != null)
        {
            selectedVisual = selectedTransform.gameObject;
        }
        
        targetPosition = transform.position; // Đứng yên lúc mới sinh ra
        SetSelectedVisible(false);
    }

    private void Update()
    {
        MoveTowardsTarget();
    }

    // Bật/tắt hiển thị vòng tròn
    public void SetSelectedVisible(bool visible)
    {
        if (selectedVisual != null)
        {
            selectedVisual.SetActive(visible);
        }
    }

    // Nhận lệnh di chuyển từ Controller
    public void MoveTo(Vector3 targetPos)
    {
        this.targetPosition = targetPos;
    }

    // Tự động di chuyển mỗi khung hình
    private void MoveTowardsTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }
}