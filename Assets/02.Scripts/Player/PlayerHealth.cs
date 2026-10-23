using System;
using UnityEngine;

// 플레이어의 체력을 관리한다. 대미지를 받을 수 있다(IDamageable).
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;

    private float _currentHealth;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float Ratio => _currentHealth / _maxHealth; // 0 ~ 1, 체력바에 쓴다.
    public bool IsDead => _currentHealth <= 0f;

    // 대미지를 받았을 때 호출된다. (화면 피격 효과 등에 쓴다)
    public event Action<DamageInfo> Damaged;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        // 이미 죽었으면 더 맞지 않는다.
        if (IsDead)
            return;

        _currentHealth = Mathf.Max(_currentHealth - damageInfo.Value, 0f);
        GameLogger.Log($"플레이어가 {damageInfo.Value} 대미지를 받았다. 남은 체력: {_currentHealth} / {_maxHealth}");
        Damaged?.Invoke(damageInfo);

        if (IsDead)
        {
            // Todo. 게임 오버 처리
            Debug.Log("플레이어가 죽었다.");
        }
    }
}
