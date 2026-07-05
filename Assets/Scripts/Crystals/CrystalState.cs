// Trạng thái của một tinh thể.
//   Inactive -> màu xám, chưa thuộc về ai, chưa dùng được (chưa cho xây/kích hoạt tiến trình).
//   Active   -> màu theo loại, thuộc về người chơi, dùng được bình thường.
//   Captured -> màu đỏ, quái đã chiếm, ngừng hoạt động tới khi bị tái chiếm.
public enum CrystalState
{
    Inactive,
    Active,
    Captured
}
