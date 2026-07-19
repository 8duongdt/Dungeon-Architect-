using UnityEngine;

/// <summary>
/// Đánh dấu GameObject là nhân vật chính do người chơi điều khiển (PlayerControll) - không mang
/// dữ liệu gì, chỉ để <see cref="AttackAreaBase.FindVisibleTarget"/> nhận ra và ưu tiên tấn công
/// nhân vật chính hơn các Unit RTS khác cùng phe khi quái phát hiện nhiều mục tiêu cùng lúc.
/// </summary>
public class PlayerAvatarMarker : MonoBehaviour
{
}
