using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// TEMP-DIAG: script chẩn đoán tạm cho bug "2 trại lính chỉ spawn ở trại đầu" - sẽ xóa sau khi xong.
public static class TwoBarracksDiag
{
    private static PlacedObject[] placed;
    private static PlacedObjectTypeSO barracksType;

    public static string PlaceAndEnqueue()
    {
        var log = new List<string>();
        GridBuildingSystem system = Object.FindAnyObjectByType<GridBuildingSystem>();
        if (system == null) return "FAIL: no GridBuildingSystem";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        object grid = typeof(GridBuildingSystem).GetField("grid", flags).GetValue(system);
        if (grid == null) return "FAIL: grid not built yet";

        var typeList = (List<PlacedObjectTypeSO>)typeof(GridBuildingSystem)
            .GetField("placedObjectTypeList", flags).GetValue(system);
        barracksType = null;
        foreach (PlacedObjectTypeSO t in typeList)
        {
            if (t != null && t.prefab != null && t.prefab.GetComponent<UnitTrainingBuilding>() != null)
            {
                barracksType = t;
                break;
            }
        }
        if (barracksType == null) return "FAIL: no barracks type";
        log.Add("type=" + barracksType.name + " " + barracksType.width + "x" + barracksType.height);

        Vector2Int? spotA = null, spotB = null;
        for (int x = 0; x < system.GridWidth && spotB == null; x++)
        {
            for (int y = 0; y < system.GridHeight && spotB == null; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!system.CanBuildAt(barracksType, cell)) continue;
                if (spotA == null) { spotA = cell; continue; }
                if (Vector2Int.Distance(spotA.Value, cell) > 15f) spotB = cell;
            }
        }
        if (spotA == null || spotB == null) return "FAIL: build spots A=" + spotA + " B=" + spotB;

        MethodInfo getGridObject = grid.GetType().GetMethod("GetGridObject", new[] { typeof(Vector2Int) });
        placed = new PlacedObject[2];
        Vector2Int[] spots = { spotA.Value, spotB.Value };
        for (int i = 0; i < 2; i++)
        {
            Vector3 worldPos = system.GetPlacementWorldPosition(barracksType, spots[i]);
            placed[i] = PlacedObject.Create(worldPos, spots[i], barracksType);
            foreach (Vector2Int cell in barracksType.GetGridPositionList(spots[i]))
            {
                object gridObject = getGridObject.Invoke(grid, new object[] { cell });
                gridObject.GetType().GetMethod("SetPlacedObject").Invoke(gridObject, new object[] { placed[i] });
            }
            log.Add("#" + (i + 1) + " id=" + placed[i].GetInstanceID() + " origin=" + spots[i] + " world=" + worldPos);
        }

        bool lookupOk = true;
        for (int i = 0; i < 2; i++)
        {
            foreach (Vector2Int cell in barracksType.GetGridPositionList(spots[i]))
            {
                object gridObject = getGridObject.Invoke(grid, new object[] { cell });
                var found = (PlacedObject)gridObject.GetType().GetMethod("GetPlacedObject").Invoke(gridObject, null);
                if (found != placed[i])
                {
                    lookupOk = false;
                    log.Add("MISMATCH at " + cell + " expected #" + (i + 1) + " got "
                        + (found == null ? "null" : found.GetInstanceID().ToString()));
                }
            }
        }
        log.Add("TEST1 lookup: " + (lookupOk ? "OK" : "MISMATCH"));

        var panel = Object.FindAnyObjectByType<ConstructStatusPanelView>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.Show(placed[1]);
            var bound = (UnitTrainingBuilding)typeof(ConstructStatusPanelView)
                .GetField("currentTraining", flags).GetValue(panel);
            var expected = placed[1].GetComponent<UnitTrainingBuilding>();
            log.Add("TEST2 panel bind after Show(#2): match=" + (bound == expected));
        }
        else
        {
            log.Add("TEST2 skipped: no panel");
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddGold(10000);
            ResourceManager.Instance.AddMana(10000);
        }
        for (int i = 0; i < 2; i++)
        {
            var training = placed[i].GetComponent<UnitTrainingBuilding>();
            bool queued = training.TryEnqueue(0);
            log.Add("TEST3 enqueue #" + (i + 1) + " id=" + training.GetInstanceID() + " -> " + queued);
        }
        return string.Join("\n", log);
    }

    public static string CheckSpawns()
    {
        if (placed == null || placed[0] == null || placed[1] == null) return "FAIL: run PlaceAndEnqueue first";

        var log = new List<string>();
        Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < 2; i++)
        {
            int nearby = 0;
            foreach (Unit unit in units)
            {
                if (Vector2.Distance(unit.transform.position, placed[i].transform.position) < 4f) nearby++;
            }
            var training = placed[i].GetComponent<UnitTrainingBuilding>();
            var pending = (List<int>)typeof(UnitTrainingBuilding)
                .GetField("pendingQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(training);
            var alive = (int)typeof(UnitTrainingBuilding)
                .GetField("currentUnitCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(training);
            log.Add("#" + (i + 1) + " unitsNearby=" + nearby + " queueLeft=" + pending.Count + " aliveCounter=" + alive);
        }
        return string.Join("\n", log);
    }
}
