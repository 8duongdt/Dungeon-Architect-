using System;
using UnityEngine;

/// <summary>
/// Lớp cơ sở cho MỘT bước hướng dẫn tương tác. Mỗi bước là một component con của
/// <see cref="TutorialSequence"/>: nó hiện một câu chỉ dẫn (tiếng Anh) rồi "canh" một thao tác của
/// người chơi, khi làm đúng thì gọi <see cref="NotifyComplete"/> để chuỗi chuyển sang bước kế.
///
/// Mô hình giống các State của UnitAI: <see cref="Begin"/>/<see cref="End"/> bật/tắt việc canh
/// (<see cref="StartWatching"/>/<see cref="StopWatching"/>). Bước dạng "chỉ đọc" (<see cref="isInfoOnly"/>)
/// không canh gì - chuỗi cho qua bằng nút Next trên overlay.
/// </summary>
public abstract class TutorialStep : MonoBehaviour
{
    [TextArea(2, 5)]
    [Tooltip("Câu chỉ dẫn hiển thị cho người chơi (tiếng Anh).")]
    [SerializeField] protected string instruction;

    [Tooltip("Bước chỉ để đọc: không canh thao tác, người chơi bấm Next để qua.")]
    [SerializeField] private bool isInfoOnly;

    [Tooltip("Mục tiêu trong world để mũi tên chỉ vào (vd một tinh thể) - để trống thì không có mũi tên.")]
    [SerializeField] private Transform worldHighlightTarget;

    [Tooltip("Mục tiêu UI để mũi tên chỉ vào (vd một nút) - để trống thì không có mũi tên.")]
    [SerializeField] private RectTransform uiHighlightTarget;

    public string Instruction => instruction;
    public bool IsInfoOnly => isInfoOnly;
    public Transform WorldHighlightTarget => worldHighlightTarget;
    public RectTransform UiHighlightTarget => uiHighlightTarget;

    /// <summary>Bắn đúng một lần khi người chơi hoàn thành thao tác của bước này.</summary>
    public event Action Completed;

    private bool isComplete;

    /// <summary>Bước con đang trong trạng thái canh thao tác (dùng cho bước poll trong Update).</summary>
    protected bool IsWatching { get; private set; }

    /// <summary>Bắt đầu canh thao tác của bước.</summary>
    public void Begin()
    {
        isComplete = false;
        IsWatching = true;
        StartWatching();
    }

    /// <summary>Ngừng canh (khi qua bước hoặc kết thúc/skip tutorial).</summary>
    public void End()
    {
        IsWatching = false;
        StopWatching();
    }

    /// <summary>Bước con gọi khi thao tác đã hoàn thành - chỉ có tác dụng một lần.</summary>
    protected void NotifyComplete()
    {
        if (isComplete)
        {
            return;
        }

        isComplete = true;
        Completed?.Invoke();
    }

    // Đăng ký lắng nghe sự kiện game mà bước này quan tâm.
    protected abstract void StartWatching();

    // Hủy đăng ký để không rò rỉ handler khi qua bước.
    protected abstract void StopWatching();

    /// <summary>
    /// Mục tiêu world cho mũi tên, tính LẠI mỗi frame. Mặc định trả về mục tiêu gán sẵn trong
    /// Inspector; bước liên quan tinh thể ghi đè để tự tìm tinh thể gần nhất lúc chơi (tinh thể được
    /// rải ngẫu nhiên nên không gán cứng được).
    /// </summary>
    public virtual Transform ResolveHighlightTarget()
    {
        return worldHighlightTarget;
    }

    // Tinh thể gần người chơi nhất khớp loại (null = mọi loại) và trạng thái (null = mọi trạng thái).
    protected static Transform FindNearestCrystalTransform(CrystalType? type, CrystalState? requiredState)
    {
        Vector3 origin = ReferencePosition();
        CrystalNode nearest = null;
        float nearestSqrDistance = float.MaxValue;

        foreach (CrystalNode node in CrystalNodeRegistry.All)
        {
            if (node == null)
            {
                continue;
            }
            if (type.HasValue && node.Type != type.Value)
            {
                continue;
            }
            if (requiredState.HasValue && node.State != requiredState.Value)
            {
                continue;
            }

            float sqrDistance = (node.transform.position - origin).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = node;
            }
        }

        return nearest != null ? nearest.transform : null;
    }

    // Điểm tham chiếu để đo "gần nhất": avatar người chơi nếu có, không thì camera chính.
    private static Vector3 ReferencePosition()
    {
        PlayerControll avatar = FindFirstObjectByType<PlayerControll>();
        if (avatar != null)
        {
            return avatar.transform.position;
        }

        return Camera.main != null ? Camera.main.transform.position : Vector3.zero;
    }
}
