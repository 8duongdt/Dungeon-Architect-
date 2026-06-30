/// <summary>
/// Dữ liệu một ô lưới: biết tọa độ (x, y) của mình và đang bị công trình nào chiếm dụng (nếu có).
/// Ô trống khi <c>placedObject == null</c>.
/// </summary>
public class GridObject
{
    private readonly GridXY<GridObject> grid;
    private readonly int x;
    private readonly int y;
    private PlacedObject placedObject;

    public GridObject(GridXY<GridObject> grid, int x, int y)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
    }

    public bool CanBuild()
    {
        return placedObject == null;
    }

    public void SetPlacedObject(PlacedObject placedObject)
    {
        this.placedObject = placedObject;
    }

    public PlacedObject GetPlacedObject()
    {
        return placedObject;
    }

    public void ClearPlacedObject()
    {
        placedObject = null;
    }

    public override string ToString()
    {
        return $"({x}, {y})";
    }
}
