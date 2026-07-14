using UnityEngine;

/// <summary>
/// Điểm phát âm thanh (SFX) duy nhất cho toàn game: một AudioSource sống xuyên scene
/// (DontDestroyOnLoad) để tiếng vẫn phát trọn vẹn kể cả khi GameObject phát ra nó (unit chết,
/// công trình bị phá...) đã bị hủy ngay sau đó. Tự khởi tạo lazy - không cần đặt sẵn trong scene.
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource sfxSource;

    public static AudioManager Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<AudioManager>();
        if (instance != null || !Application.isPlaying)
        {
            return;
        }

        var audioManagerObject = new GameObject(nameof(AudioManager));
        instance = audioManagerObject.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSfxSource();
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
        {
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    /// <summary>Phát một clip; nhiều lời gọi cùng lúc chồng lên nhau bình thường (không cắt tiếng nhau).</summary>
    public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSfxSource();
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    /// <summary>Phát ngẫu nhiên một clip trong mảng - dùng cho các biến thể đánh số (punch_1..3...).</summary>
    public void PlayRandom(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        PlayOneShot(clips[Random.Range(0, clips.Length)], volumeScale);
    }
}
