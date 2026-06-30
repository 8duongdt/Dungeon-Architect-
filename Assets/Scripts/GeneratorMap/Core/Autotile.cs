/// <summary>
/// Cờ trạng thái theo 8 hướng quanh một ô (4 cạnh + 4 chéo) - dùng để chọn tile khi autotile một
/// vùng địa hình. Với viền bờ quanh nước: true = phía đó LÀ nước, false = đất/biên.
/// </summary>
public readonly struct TileNeighbors
{
    public readonly bool North;
    public readonly bool East;
    public readonly bool South;
    public readonly bool West;
    public readonly bool NorthEast;
    public readonly bool NorthWest;
    public readonly bool SouthEast;
    public readonly bool SouthWest;

    public TileNeighbors(
        bool north, bool east, bool south, bool west,
        bool northEast, bool northWest, bool southEast, bool southWest)
    {
        North = north;
        East = east;
        South = south;
        West = west;
        NorthEast = northEast;
        NorthWest = northWest;
        SouthEast = southEast;
        SouthWest = southWest;
    }
}
