using System.Collections.Generic;
using UnityEngine;

/// <summary>Bước hoàn thành khi người chơi chọn được ít nhất một unit (click hoặc kéo hộp chọn).</summary>
public class SelectUnitsStep : TutorialStep
{
    private UnitController controller;

    protected override void StartWatching()
    {
        controller = FindFirstObjectByType<UnitController>();
        if (controller != null)
        {
            controller.SelectionChanged += HandleSelectionChanged;
        }
    }

    protected override void StopWatching()
    {
        if (controller != null)
        {
            controller.SelectionChanged -= HandleSelectionChanged;
        }
    }

    private void HandleSelectionChanged(IReadOnlyList<Unit> units)
    {
        if (units != null && units.Count > 0)
        {
            NotifyComplete();
        }
    }
}
