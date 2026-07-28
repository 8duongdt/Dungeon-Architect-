using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bước hoàn thành khi người chơi bấm MỘT trong các phím cấu hình (đọc trực tiếp
/// <see cref="Keyboard.current"/> theo convention Input System của dự án). Dùng cho "di chuyển bằng
/// WASD" (liệt kê W/A/S/D) hay "dùng skill" (liệt kê Digit1...).
/// </summary>
public class KeyPressStep : TutorialStep
{
    [Tooltip("Bấm bất kỳ phím nào trong danh sách là qua bước.")]
    [SerializeField] private List<Key> acceptedKeys = new List<Key>();

    protected override void StartWatching()
    {
        // Canh bằng poll trong Update (dựa vào IsWatching) - không có event bàn phím để đăng ký.
    }

    protected override void StopWatching()
    {
    }

    private void Update()
    {
        if (!IsWatching)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        foreach (Key key in acceptedKeys)
        {
            if (keyboard[key].wasPressedThisFrame)
            {
                NotifyComplete();
                return;
            }
        }
    }
}
