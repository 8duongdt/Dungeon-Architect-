using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitController : MonoBehaviour
{
    [SerializeField] private Transform selectionBoxTransform;
    [SerializeField] private float dragSelectThreshold = 0.1f;
    [SerializeField] private float clickSelectRadius = 0.25f;

    private Vector3 startMousePosition;
    private List<Unit> selectedUnitList;

    /// <summary>Các unit đang được chọn (chỉ đọc) - để HUD hiển thị thông tin đơn vị.</summary>
    public IReadOnlyList<Unit> SelectedUnits => selectedUnitList;

    /// <summary>Bắn mỗi khi tập hợp unit được chọn thay đổi (chọn mới hoặc bỏ chọn).</summary>
    public event Action<IReadOnlyList<Unit>> SelectionChanged;

    private void Awake()
    {
        selectedUnitList = new List<Unit>();

        if (selectionBoxTransform != null)
        {
            selectionBoxTransform.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        HandleSelectionInputs();
        HandleMovementCommand();
    }

    private void HandleSelectionInputs()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            startMousePosition = MouseUtils.GetMouseWorldPosition();
            ShowSelectionBox(true);
            UpdateSelectionBoxVisual(startMousePosition, startMousePosition);
        }

        if (Mouse.current.leftButton.isPressed)
        {
            UpdateSelectionBoxVisual(startMousePosition, MouseUtils.GetMouseWorldPosition());
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ShowSelectionBox(false);
            ClearSelection();

            Vector3 endMousePosition = MouseUtils.GetMouseWorldPosition();
            bool isDragSelection = Vector3.Distance(startMousePosition, endMousePosition) > dragSelectThreshold;
            List<Unit> units = isDragSelection
                ? GetUnitsInSelectionArea(startMousePosition, endMousePosition)
                : GetUnitsAtPoint(endMousePosition);

            SelectUnits(units);
        }
    }

    private void HandleMovementCommand()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && selectedUnitList.Count > 0)
        {
            Vector3 targetPosition = MouseUtils.GetMouseWorldPosition();
            List<Vector3> formationPositions = CalculateFormationPositions(targetPosition, selectedUnitList.Count);
            AssignNearestFormationSlots(formationPositions);
        }
    }

    private void ShowSelectionBox(bool visible)
    {
        if (selectionBoxTransform != null)
        {
            selectionBoxTransform.gameObject.SetActive(visible);
        }
    }

    private void ClearSelection()
    {
        foreach (Unit unit in selectedUnitList)
        {
            if (unit != null)
            {
                unit.SetSelectedVisible(false);
            }
        }

        selectedUnitList.Clear();
    }

    private void SelectUnits(List<Unit> units)
    {
        selectedUnitList = units;

        foreach (Unit unit in selectedUnitList)
        {
            if (unit != null)
            {
                unit.SetSelectedVisible(true);
            }
        }

        // Chọn xong (kể cả khi danh sách rỗng) thì báo cho HUD cập nhật.
        SelectionChanged?.Invoke(selectedUnitList);
    }

    private void UpdateSelectionBoxVisual(Vector3 startPos, Vector3 currentPos)
    {
        if (selectionBoxTransform == null)
        {
            return;
        }

        Vector3 lowerLeft = new Vector3(Mathf.Min(startPos.x, currentPos.x), Mathf.Min(startPos.y, currentPos.y));
        Vector3 upperRight = new Vector3(Mathf.Max(startPos.x, currentPos.x), Mathf.Max(startPos.y, currentPos.y));

        selectionBoxTransform.position = lowerLeft;
        selectionBoxTransform.localScale = upperRight - lowerLeft;
    }

    private List<Unit> GetUnitsAtPoint(Vector3 point)
    {
        List<Unit> units = new List<Unit>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, clickSelectRadius);

        AddUnitsFromColliders(colliders, units);
        return units;
    }

    private List<Unit> GetUnitsInSelectionArea(Vector3 startPos, Vector3 endPos)
    {
        List<Unit> units = new List<Unit>();
        Vector2 lowerLeft = new Vector2(Mathf.Min(startPos.x, endPos.x), Mathf.Min(startPos.y, endPos.y));
        Vector2 upperRight = new Vector2(Mathf.Max(startPos.x, endPos.x), Mathf.Max(startPos.y, endPos.y));
        Collider2D[] colliders = Physics2D.OverlapAreaAll(lowerLeft, upperRight);

        AddUnitsFromColliders(colliders, units);
        return units;
    }

    private void AddUnitsFromColliders(Collider2D[] colliders, List<Unit> units)
    {
        foreach (Collider2D col in colliders)
        {
            Unit unit = col.GetComponentInParent<Unit>();
            if (unit != null && !units.Contains(unit))
            {
                units.Add(unit);
            }
        }
    }

    // Gán mỗi unit tới ô đội hình GẦN nó nhất để các unit không cắt ngang đường
    // nhau khi tiến vào vòng tròn, giảm va chạm và đứng đúng vị trí riêng.
    private void AssignNearestFormationSlots(List<Vector3> formationPositions)
    {
        bool[] slotTaken = new bool[formationPositions.Count];

        foreach (Unit unit in selectedUnitList)
        {
            if (unit == null)
            {
                continue;
            }

            int bestSlot = -1;
            float bestSqrDistance = float.MaxValue;
            for (int slot = 0; slot < formationPositions.Count; slot++)
            {
                if (slotTaken[slot])
                {
                    continue;
                }

                float sqrDistance = (formationPositions[slot] - unit.transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestSlot = slot;
                }
            }

            if (bestSlot >= 0)
            {
                slotTaken[bestSlot] = true;
                unit.MoveTo(formationPositions[bestSlot]);
            }
        }
    }

    private List<Vector3> CalculateFormationPositions(Vector3 targetPos, int unitCount)
    {
        List<Vector3> positions = new List<Vector3>();
        if (unitCount == 1)
        {
            positions.Add(targetPos);
            return positions;
        }

        float[] ringDistances = { 1.5f, 3f, 4.5f, 6f };
        int[] unitsPerRing = { 5, 10, 15, 20 };

        positions.Add(targetPos);
        int unitsPlaced = 1;

        for (int i = 0; i < ringDistances.Length; i++)
        {
            for (int j = 0; j < unitsPerRing[i]; j++)
            {
                if (unitsPlaced >= unitCount)
                {
                    return positions;
                }

                float angle = j * (360f / unitsPerRing[i]);
                Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                positions.Add(targetPos + direction * ringDistances[i]);
                unitsPlaced++;
            }
        }

        return positions;
    }
}
