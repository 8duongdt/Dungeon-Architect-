/// <summary>
/// Cơ chế thi triển của một kỹ năng. Giá trị gán tường minh vì asset lưu số nguyên:
/// KHÔNG đổi thứ tự/giá trị — asset cũ thiếu field sẽ mặc định 0 = InstantStrike (hành vi cũ).
/// </summary>
public enum SkillMechanic
{
    InstantStrike = 0, // đánh tức thời trên mục tiêu (VFX + damage ngay)
    Projectile = 1,    // đạn bay thẳng, phát nổ khi trúng kẻ địch ĐẦU TIÊN
    ChainStrike = 2,   // đánh tức thời + nảy sang các kẻ địch đứng gần
    AreaStrike = 3,    // nổ diện rộng quanh vị trí mục tiêu + đẩy lùi (tùy chọn)
    DamageZone = 4,    // vùng gây sát thương theo nhịp trong một khoảng thời gian (+ choáng tùy chọn)
    Trap = 5,          // bẫy đặt tại chỗ mục tiêu, kích hoạt khi kẻ địch dẫm lên
    PullVortex = 6,    // hút mọi kẻ địch trong phạm vi về tâm và trói chân (ultimate)
    SelfShield = 7,    // khiên máu ảo bao bọc CHÍNH người thi triển
    ExecuteStrike = 8, // đòn kết liễu: kết liễu ngay mục tiêu dưới ngưỡng máu + thưởng vàng
    ChargeDash = 9,    // lao thẳng tới mục tiêu, gây sát thương + đẩy lùi quanh điểm chạm
}
