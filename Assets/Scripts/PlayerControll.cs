using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControll : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMove = new Vector2(0, -1); // Mặc định nhìn xuống dưới khi bắt đầu
    private Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Nếu dùng Rigidbody2D để di chuyển, nên để Gravity Scale = 0 
        // và đóng băng trục Z (Freeze Rotation Z) trong Inspector.
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        moveInput = Vector2.zero;
        if (keyboard.dKey.isPressed) moveInput.x = 1;
        else if (keyboard.aKey.isPressed) moveInput.x = -1;

        if (keyboard.wKey.isPressed) moveInput.y = 1;
        else if (keyboard.sKey.isPressed) moveInput.y = -1;

        //Di chuyển nhân vật
        Vector2 movement = moveInput.normalized;
        transform.position += (Vector3)movement * moveSpeed * Time.deltaTime;

        //Cập nhật Animator
        animator.SetFloat("Speed", moveInput.magnitude);

        if (moveInput != Vector2.zero)
        {
            //lưu hướng quay mặt cuối cùng
            lastMove = moveInput;

            // Gửi thông số hướng vào Animator
            animator.SetFloat("Horizontal", lastMove.x);
            animator.SetFloat("Vertical", lastMove.y);
        }
    }
}
