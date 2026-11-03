using UnityEngine;
using UnityEngine.EventSystems;

// 일시정지 창: ESC를 누르면 GameManager가 Show()로 띄우고, Hide()로 닫는다.
// 창 안의 버튼(계속하기, 다시 시작, 게임 종료)을 누르면 GameManager에게 알린다.
// (게임 상태와 시간을 멈추는 일은 GameManager가, 창을 보여주는 일은 이 스크립트가 맡는다)
public class UI_PausePopup : MonoBehaviour
{
    // 창을 띄운다.
    public void Show()
    {
        gameObject.SetActive(true);
    }

    // 창을 닫는다.
    public void Hide()
    {
        Deselect();
        gameObject.SetActive(false);
    }

    // 계속하기 버튼의 On Click에 연결
    public void OnClickContinue()
    {
        GameManager.Instance.Continue();
    }

    // 다시 시작 버튼의 On Click에 연결
    public void OnClickRestart()
    {
        GameManager.Instance.Restart();
    }

    // 게임 종료 버튼의 On Click에 연결
    public void OnClickQuit()
    {
        GameManager.Instance.QuitGame();
    }

    // 버튼이 선택된 채로 남아 있으면 다음에 창을 열었을 때 그 버튼이 눌린 색으로 보이고,
    // Space(점프)가 Submit으로 들어가 버튼이 또 눌릴 수 있다. 그래서 창을 닫을 때 선택을 풀어 준다.
    private void Deselect()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
