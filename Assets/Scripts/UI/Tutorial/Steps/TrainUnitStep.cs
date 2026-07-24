using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bước hoàn thành khi người chơi xếp huấn luyện một lính ở bất kỳ trại nào. Trại được đặt lúc chơi
/// nên có thể chưa tồn tại khi bước bắt đầu: quét và đăng ký các trại mới mỗi frame trong lúc canh.
/// </summary>
public class TrainUnitStep : TutorialStep
{
    private readonly List<UnitTrainingBuilding> subscribed = new List<UnitTrainingBuilding>();

    protected override void StartWatching()
    {
        RefreshSubscriptions();
    }

    protected override void StopWatching()
    {
        foreach (UnitTrainingBuilding barracks in subscribed)
        {
            if (barracks != null)
            {
                barracks.UnitEnqueued -= HandleUnitEnqueued;
            }
        }

        subscribed.Clear();
    }

    private void Update()
    {
        if (IsWatching)
        {
            RefreshSubscriptions();
        }
    }

    // Đăng ký lắng nghe cho các trại chưa từng đăng ký (trại mới xây trong màn).
    private void RefreshSubscriptions()
    {
        UnitTrainingBuilding[] allBarracks = FindObjectsByType<UnitTrainingBuilding>(FindObjectsSortMode.None);
        foreach (UnitTrainingBuilding barracks in allBarracks)
        {
            if (!subscribed.Contains(barracks))
            {
                barracks.UnitEnqueued += HandleUnitEnqueued;
                subscribed.Add(barracks);
            }
        }
    }

    private void HandleUnitEnqueued()
    {
        NotifyComplete();
    }
}
