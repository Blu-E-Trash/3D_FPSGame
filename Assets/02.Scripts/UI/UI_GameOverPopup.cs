using UnityEngine;
using UnityEngine.EventSystems;

// 게임오버 창: 플레이어가 죽으면 GameManager가 Show()로 띄운다.
// 창 안의 버튼(다시 시작, 게임 종료)을 누르면 GameManager에게 알린다.
// (게임 상태를 바꾸는 일은 GameManager가, 창을 보여주는 일은 이 스크립트가 맡는다)
public class UI_GameOverPopup : MonoBehaviour
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

    // 버튼이 선택된 채로 남아 있으면 Space(점프)가 Submit으로 들어가 버튼이 또 눌릴 수 있다.
    // 그래서 창을 닫을 때 선택을 풀어 준다.
    private void Deselect()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
