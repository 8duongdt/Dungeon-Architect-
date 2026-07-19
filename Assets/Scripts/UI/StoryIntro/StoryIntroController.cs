using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển màn cốt truyện mở đầu (sau khi bấm New Game): hiện màn trích dẫn tiếng Anh,
/// rồi lần lượt kể 3 phân cảnh có hình minh hoạ với hiệu ứng đánh máy (typewriter).
/// Nút Continue (góc dưới phải): lần bấm đầu hoàn tất dòng đang gõ, lần sau chuyển cảnh.
/// Nút Skip (góc trên phải): vào thẳng màn chơi. Hết cảnh cuối cũng vào màn chơi.
/// Toàn bộ chữ hiển thị là tiếng Anh; chỉ chú thích/tooltip là tiếng Việt.
/// </summary>
public class StoryIntroController : MonoBehaviour
{
    [Serializable]
    public class StoryPage
    {
        [Tooltip("Ảnh minh hoạ toàn màn hình cho phân cảnh này.")]
        public Sprite image;

        [TextArea(2, 6)]
        [Tooltip("Lời kể (tiếng Anh) hiện dần bằng hiệu ứng đánh máy.")]
        public string narration;

        [Tooltip("Giọng lồng tiếng cho phân cảnh này (để trống = không có, dùng tốc độ đánh máy mặc định).")]
        public AudioClip voiceClip;
    }

    [Header("Artwork")]
    [SerializeField] private Image backgroundArt;

    [Header("Quote screen (mở đầu)")]
    [SerializeField] private GameObject quoteScreen;
    [SerializeField] private TMP_Text quoteText;
    [SerializeField] private TMP_Text quoteSubText;

    [Header("Narration band (dưới)")]
    [SerializeField] private CanvasGroup textBand;
    [SerializeField] private TMP_Text narrationText;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipButton;

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float beatFadeSeconds = 0.4f;

    [Header("Typewriter")]
    [Tooltip("Tốc độ đánh máy dùng khi phân cảnh KHÔNG có voiceClip - có voiceClip thì tốc độ tự tính theo độ dài audio.")]
    [SerializeField] private float typewriterCharsPerSecond = 45f;

    [Header("Voice")]
    [Tooltip("Nguồn phát giọng lồng tiếng - để trống thì tự thêm AudioSource lúc Awake.")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Content")]
    [TextArea(1, 3)]
    [SerializeField] private string quoteLine = "Every monster was once someone's hero.";
    [TextArea(1, 2)]
    [SerializeField] private string quoteSubLine = "— from the sealed chronicles of the Dungeon Core";
    [SerializeField] private StoryPage[] pages;

    [Header("Flow")]
    [SerializeField] private string gameplaySceneName = "Phase 1";

    private int currentPageIndex = -1;
    private bool isShowingQuote = true;
    private bool isTyping;
    private Coroutine typewriterRoutine;
    private string currentFullNarration = string.Empty;

    private void Awake()
    {
        ResolveVoiceSource();
    }

    private void Start()
    {
        BindButtons();
        ShowQuoteScreen();
    }

    private void ResolveVoiceSource()
    {
        if (voiceSource != null)
        {
            return;
        }

        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    private void Update()
    {
        ReadKeyboardShortcuts();
    }

    // ---------------------------------------------------------------- Setup

    private void BindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipPressed);
        }
    }

    private void UnbindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipPressed);
        }
    }

    private void ReadKeyboardShortcuts()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool pressedContinue = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
        if (pressedContinue)
        {
            OnContinuePressed();
        }
        else if (keyboard.escapeKey.wasPressedThisFrame)
        {
            OnSkipPressed();
        }
    }

    // ---------------------------------------------------------------- Quote screen

    private void ShowQuoteScreen()
    {
        isShowingQuote = true;
        SetQuoteText();
        SetQuoteScreenVisible(true);
        SetNarrationBandVisible(false);
    }

    private void SetQuoteText()
    {
        if (quoteText != null)
        {
            quoteText.text = quoteLine;
        }
        if (quoteSubText != null)
        {
            quoteSubText.text = quoteSubLine;
        }
    }

    private void SetQuoteScreenVisible(bool visible)
    {
        if (quoteScreen != null)
        {
            quoteScreen.SetActive(visible);
        }
    }

    private void SetNarrationBandVisible(bool visible)
    {
        if (textBand != null)
        {
            textBand.alpha = visible ? 1f : 0f;
        }
    }

    // ---------------------------------------------------------------- Continue / Skip

    private void OnContinuePressed()
    {
        if (isShowingQuote)
        {
            LeaveQuoteScreen();
            return;
        }

        if (isTyping)
        {
            CompleteTypewriterInstantly();
            return;
        }

        AdvanceToNextBeat();
    }

    private void OnSkipPressed()
    {
        LoadGameplay();
    }

    private void LeaveQuoteScreen()
    {
        isShowingQuote = false;
        SetQuoteScreenVisible(false);
        SetNarrationBandVisible(true);
        ShowBeat(0);
    }

    private void AdvanceToNextBeat()
    {
        int nextIndex = currentPageIndex + 1;
        bool reachedEnd = pages == null || nextIndex >= pages.Length;
        if (reachedEnd)
        {
            LoadGameplay();
            return;
        }

        ShowBeat(nextIndex);
    }

    // ---------------------------------------------------------------- Beat display

    private void ShowBeat(int index)
    {
        currentPageIndex = index;
        StoryPage page = pages[index];

        if (backgroundArt != null && page.image != null)
        {
            backgroundArt.sprite = page.image;
        }

        PlayVoiceLine(page.voiceClip);
        StartTypewriter(page.narration, page.voiceClip);
        PlayBeatFadeIn();
    }

    private void PlayVoiceLine(AudioClip clip)
    {
        if (voiceSource == null || clip == null)
        {
            return;
        }

        // Play() tự dừng clip đang phát trước đó trên cùng AudioSource - không cần gọi Stop() riêng.
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private void StopVoice()
    {
        if (voiceSource != null)
        {
            voiceSource.Stop();
        }
    }

    private void PlayBeatFadeIn()
    {
        if (fadeOverlay == null)
        {
            return;
        }

        StopCoroutine(nameof(FadeOverlayOut));
        fadeOverlay.alpha = 1f;
        StartCoroutine(FadeOverlayOut());
    }

    private IEnumerator FadeOverlayOut()
    {
        float elapsed = 0f;
        while (elapsed < beatFadeSeconds)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / beatFadeSeconds);
            yield return null;
        }
        fadeOverlay.alpha = 0f;
    }

    // ---------------------------------------------------------------- Typewriter

    private void StartTypewriter(string fullText, AudioClip voiceClip)
    {
        currentFullNarration = fullText ?? string.Empty;
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
        }

        float charsPerSecond = ComputeTypewriterSpeed(currentFullNarration, voiceClip);
        typewriterRoutine = StartCoroutine(RevealNarration(charsPerSecond));
    }

    // Có voiceClip thì chia đều tốc độ đánh máy theo đúng độ dài audio để chữ chạy xong đúng
    // lúc giọng đọc kết thúc; không có thì dùng tốc độ cố định (typewriterCharsPerSecond).
    private float ComputeTypewriterSpeed(string text, AudioClip voiceClip)
    {
        bool canSyncToVoice = voiceClip != null && voiceClip.length > 0f && text.Length > 0;
        return canSyncToVoice ? text.Length / voiceClip.length : typewriterCharsPerSecond;
    }

    private IEnumerator RevealNarration(float charsPerSecond)
    {
        isTyping = true;
        narrationText.text = string.Empty;

        var builder = new StringBuilder(currentFullNarration.Length);
        float secondsPerChar = 1f / Mathf.Max(1f, charsPerSecond);
        foreach (char character in currentFullNarration)
        {
            builder.Append(character);
            narrationText.text = builder.ToString();
            yield return new WaitForSeconds(secondsPerChar);
        }

        FinishTyping();
    }

    private void CompleteTypewriterInstantly()
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }
        narrationText.text = currentFullNarration;
        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        typewriterRoutine = null;
    }

    // ---------------------------------------------------------------- Scene flow

    private void LoadGameplay()
    {
        // Bấm Skip giữa chừng vẫn phải cắt tiếng ngay - không để giọng đọc kéo dài sang scene sau.
        StopVoice();

        // Lần đầu chơi đi thẳng từ intro vào Phase 1 - ghi checkpoint để Tiếp tục hoạt động.
        PlayerProgression.CurrentPhase = 1;
        SceneManager.LoadScene(gameplaySceneName);
    }
}
