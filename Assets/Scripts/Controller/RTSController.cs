using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RTSController : MonoBehaviour
{
    [SerializeField] private Transform selectionBoxTransform; // Kéo UI vùng chọn vào đây
    private Vector3 startMousePosition;
    private List<UnitRTS> selectedUnitList;

    private void Awake()
    {
        selectedUnitList = new List<UnitRTS>();
        selectionBoxTransform.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleSelectionInputs();
        HandleMovementCommand();
    }

    // Xử lý logic click và kéo chuột để chọn lính
    private void HandleSelectionInputs()
    {
        // 1. Nhấn chuột trái
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            startMousePosition = MouseUtils.GetMouseWorldPosition();
            selectionBoxTransform.gameObject.SetActive(true);
        }

        // 2. Giữ chuột trái
        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 currentMousePosition = MouseUtils.GetMouseWorldPosition();
            UpdateSelectionBoxVisual(startMousePosition, currentMousePosition);
        }

        // 3. Nhả chuột trái
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            selectionBoxTransform.gameObject.SetActive(false);
            
            foreach (UnitRTS unit in selectedUnitList)
            {
                unit.SetSelectedVisible(false);
            }
            selectedUnitList.Clear();

            Vector3 endMousePosition = MouseUtils.GetMouseWorldPosition();
            selectedUnitList = GetUnitsInSelectionArea(startMousePosition, endMousePosition);
            
            foreach (UnitRTS unit in selectedUnitList)
            {
                unit.SetSelectedVisible(true);
            }
        }
    }

    // Xử lý logic click chuột phải để di chuyển
    private void HandleMovementCommand()
    {
        // Nhấn chuột phải
        if (Mouse.current.rightButton.wasPressedThisFrame && selectedUnitList.Count > 0)
        {
            Vector3 targetPosition = MouseUtils.GetMouseWorldPosition();
            List<Vector3> formationPositions = CalculateFormationPositions(targetPosition, selectedUnitList.Count);

            for (int i = 0; i < selectedUnitList.Count; i++)
            {
                selectedUnitList[i].MoveTo(formationPositions[i]);
            }
        }
    }

    // Cập nhật vị trí và kích thước UI Vùng Chọn
    private void UpdateSelectionBoxVisual(Vector3 startPos, Vector3 currentPos)
    {
        Vector3 lowerLeft = new Vector3(Mathf.Min(startPos.x, currentPos.x), Mathf.Min(startPos.y, currentPos.y));
        Vector3 upperRight = new Vector3(Mathf.Max(startPos.x, currentPos.x), Mathf.Max(startPos.y, currentPos.y));
        
        selectionBoxTransform.position = lowerLeft;
        selectionBoxTransform.localScale = upperRight - lowerLeft;
    }

    private List<UnitRTS> GetUnitsInSelectionArea(Vector3 startPos, Vector3 endPos)
    {
        List<UnitRTS> units = new List<UnitRTS>();
        Collider2D[] colliders = Physics2D.OverlapAreaAll(startPos, endPos);
        
        foreach (Collider2D col in colliders)
        {
            UnitRTS unit = col.GetComponentInParent<UnitRTS>();
            if (unit != null)
            {
                units.Add(unit);
            }
        }
        return units;
    }

    // Tính toán đội hình di chuyển (vòng tròn)
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
                if (unitsPlaced >= unitCount) return positions;

                float angle = j * (360f / unitsPerRing[i]);
                Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                positions.Add(targetPos + direction * ringDistances[i]);
                unitsPlaced++;
            }
        }
        return positions;
    }
}