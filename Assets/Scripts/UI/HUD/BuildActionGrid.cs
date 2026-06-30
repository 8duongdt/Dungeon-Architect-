using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lưới ô lệnh xây dựng ở trung tâm bảng điều khiển. Nhân bản một ô mẫu cho mỗi loại công trình,
/// nối click + phím tắt vào <see cref="GridBuildingSystem.SetActiveBuildingType"/>, và làm mờ ô khi
/// không đủ tài nguyên hoặc công trình chưa có prefab (placeholder).
/// </summary>
public class BuildActionGrid : MonoBehaviour
{
    [SerializeField] private GridBuildingSystem buildingSystem;
    [SerializeField] private BuildActionSlotView slotTemplate;
    [SerializeField] private List<PlacedObjectTypeSO> types = new List<PlacedObjectTypeSO>();

    private readonly List<SlotBinding> bindings = new List<SlotBinding>();

    private struct SlotBinding
    {
        public BuildActionSlotView Slot;
        public PlacedObjectTypeSO Type;
        public Key Hotkey;
        public bool HasHotkey;
        public bool IsPlaceholder; // chưa có prefab -> luôn vô hiệu
    }

    private void Start()
    {
        if (buildingSystem == null || slotTemplate == null)
        {
            return;
        }

        slotTemplate.gameObject.SetActive(false);
        foreach (PlacedObjectTypeSO type in types)
        {
            if (type != null)
            {
                CreateSlot(type);
            }
        }
    }

    private void Update()
    {
        RefreshAffordability();
        ReadHotkeys();
    }

    private void CreateSlot(PlacedObjectTypeSO type)
    {
        BuildActionSlotView slot = Instantiate(slotTemplate, slotTemplate.transform.parent);
        slot.gameObject.SetActive(true);
        slot.name = $"BuildSlot_{type.nameString}";
        slot.Bind(type, () => Select(type));

        bindings.Add(new SlotBinding
        {
            Slot = slot,
            Type = type,
            Hotkey = ParseHotkey(type.hotkey, out bool parsed),
            HasHotkey = parsed,
            IsPlaceholder = type.prefab == null,
        });
    }

    private void Select(PlacedObjectTypeSO type)
    {
        // Công trình placeholder (chưa có prefab) thì không cho chọn.
        if (type.prefab == null)
        {
            return;
        }

        buildingSystem.SetActiveBuildingType(type);
    }

    private void RefreshAffordability()
    {
        foreach (SlotBinding binding in bindings)
        {
            bool canUse = !binding.IsPlaceholder && buildingSystem.CanAfford(binding.Type);
            binding.Slot.SetInteractable(canUse);
        }
    }

    private void ReadHotkeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        foreach (SlotBinding binding in bindings)
        {
            if (binding.HasHotkey && Keyboard.current[binding.Hotkey].wasPressedThisFrame)
            {
                Select(binding.Type);
            }
        }
    }

    private static Key ParseHotkey(string hotkey, out bool parsed)
    {
        if (!string.IsNullOrWhiteSpace(hotkey) && Enum.TryParse(hotkey.Trim(), true, out Key key) && key != Key.None)
        {
            parsed = true;
            return key;
        }

        parsed = false;
        return Key.None;
    }
}
