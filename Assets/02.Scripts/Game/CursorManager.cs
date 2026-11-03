using UnityEngine;

// 마우스 커서를 화면 가운데에 잠그고 숨긴다.
// 게임 중에 왼쪽 Alt를 누르고 있는 동안에는 커서를 풀어서 UI(미니맵 버튼 등)를 누를 수 있다.
// 게임 중에 잠금이 풀려 있으면(에디터의 ESC, 창 전환 등) 다시 잠근다.
public class CursorManager : MonoBehaviour
{
    private void Awake()
    {
        LockCursor();
    }

    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 누른 순간/뗀 순간만 보지 않고, 매 프레임 "지금 있어야 할 상태"로 맞춘다.
        // 에디터에서는 ESC를 누르면 Unity가 커서 잠금을 마음대로 풀어 버리는데,
        // 이렇게 하면 계속하기 뒤에 잠금이 풀려 있어도 다시 잠근다.
        // (일시정지 중에 Alt를 떼서 GetKeyUp을 놓친 경우도 함께 해결된다.)
        bool wantUnlock = Input.GetKey(KeyCode.LeftAlt);

        if (wantUnlock)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                UnlockCursor();
            }
        }
        else if (Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 화면 가운데에 고정
        Cursor.visible = false;                   // 커서 숨기기
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
