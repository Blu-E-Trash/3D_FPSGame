using System.Collections;
using UnityEngine;

// 게임의 진행 상태
public enum GameState
{
    Ready,      // 준비: 시작 전 (조작 불가)
    Playing,    // 진행 중
    GameOver    // 게임 오버
}

// 게임 전체의 상태를 관리한다. 씬에 하나만 존재한다. (싱글톤)
public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    private void Awake()
    {
        // 이미 게임 매니저가 있으면 나는 사라진다. (하나만 존재하도록)
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    // 잠깐 준비 시간을 가진 뒤 게임을 시작한다.
    private IEnumerator StartSequence_Coroutine()
    {
        yield return new WaitForSeconds(2f);
        _state = GameState.Playing;
    }
}
