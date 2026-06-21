// Phân loại ô trong lưới bản đồ do generator sinh ra.
// Đây là dữ liệu thuần (logic), không liên quan tới cách vẽ tile lên Tilemap.
public enum TileType
{
    Empty,      // ô trống / chưa xác định
    Floor,      // ô sàn (đi lại được)
    Wall,       // ô tường (vật cản)
    SwampWater, // vũng nước đầm lầy (địa hình nguy hiểm/làm chậm bên trong phòng)
    Gate        // cổng/portal - đánh dấu lối ra ở phòng cuối
}
