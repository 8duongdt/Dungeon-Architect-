using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Bar : MonoBehaviour
{
    [field: SerializeField]
    public int MaxValue { get; private set; }
    [field: SerializeField]
    public int Value { get; private set; }

    [SerializeField]
    private Image _topBarImage;
    [SerializeField]
    private Image _middleBarImage;

    [SerializeField]
    private float _animationspeed = 5f;
    [SerializeField]
    private bool _enableMouseDebugControls;

    private float TargetFillAmount => MaxValue > 0 ? (float)Value / MaxValue : 0f;

    private Coroutine _adjustBarFillCoroutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        Value = Mathf.Clamp(Value, 0, MaxValue);
        SetBarFill(_topBarImage, TargetFillAmount);
        SetBarFill(_middleBarImage, TargetFillAmount);
    }

    private void Update()
    {
        if (!_enableMouseDebugControls || Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Change(20);
        }
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Change(-20);
        }
    }

    private IEnumerator AdjustBarFill(int amount)
    {
        Image suddenChange = amount >= 0 ? _middleBarImage : _topBarImage;
        Image slowChange = amount >= 0 ? _topBarImage : _middleBarImage;

        SetBarFill(suddenChange, TargetFillAmount);
        while (slowChange != null && Mathf.Abs(slowChange.fillAmount - TargetFillAmount) > 0.01f)
        {
            SetBarFill(slowChange, Mathf.Lerp(slowChange.fillAmount, TargetFillAmount, Time.deltaTime * _animationspeed));
            yield return null;
        }

        SetBarFill(slowChange, TargetFillAmount);
    }

    public void Change(int amount)
    {
        Value = Mathf.Clamp(Value + amount, 0, MaxValue);
        if (_adjustBarFillCoroutine != null)
        {
            StopCoroutine(_adjustBarFillCoroutine);
        }
        _adjustBarFillCoroutine = StartCoroutine(AdjustBarFill(amount));
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        MaxValue = Mathf.Max(0, Mathf.RoundToInt(maxHealth));
        Value = Mathf.Clamp(Mathf.RoundToInt(currentHealth), 0, MaxValue);
        float fillAmount = maxHealth > 0f ? currentHealth / maxHealth : 0f;

        if (_adjustBarFillCoroutine != null)
        {
            StopCoroutine(_adjustBarFillCoroutine);
        }

        SetBarFill(_topBarImage, fillAmount);
        SetBarFill(_middleBarImage, fillAmount);
    }

    private static void SetBarFill(Image barImage, float fillAmount)
    {
        if (barImage == null)
        {
            return;
        }

        barImage.fillAmount = Mathf.Clamp01(fillAmount);
    }

    private void ResolveReferences()
    {
        if (_middleBarImage != null)
        {
            return;
        }

        Transform middleBar = transform.Find("Bar");
        if (middleBar != null)
        {
            _middleBarImage = middleBar.GetComponent<Image>();
        }
    }
}
