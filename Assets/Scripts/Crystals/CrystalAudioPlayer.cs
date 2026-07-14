using UnityEngine;

/// <summary>
/// Âm thanh chiếm đóng tinh thể: nghe sự kiện tĩnh <see cref="CrystalNode.AnyStateChanged"/> nên
/// MỘT component đặt ở đâu đó trong scene là đủ cho mọi tinh thể trên map, không cần gắn riêng
/// từng prefab tinh thể.
/// </summary>
[DisallowMultipleComponent]
public class CrystalAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] capturedClips;
    [SerializeField] private AudioClip[] activatedClips;

    private void OnEnable()
    {
        CrystalNode.AnyStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        CrystalNode.AnyStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(CrystalNode crystal)
    {
        if (crystal.State == CrystalState.Captured)
        {
            AudioManager.Instance.PlayRandom(capturedClips);
        }
        else if (crystal.State == CrystalState.Active)
        {
            AudioManager.Instance.PlayRandom(activatedClips);
        }
    }
}
