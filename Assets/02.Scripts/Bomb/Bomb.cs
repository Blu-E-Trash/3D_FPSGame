using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem _explosionEffectPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        ParticleSystem effect = _explosionEffectPrefab;
        // 폭발 효과 생성
        if (effect != null)
        {
            Instantiate(effect, transform.position, Quaternion.identity);
        }
        // 폭발 효과 재생
        if (effect != null)
        {
            effect.Play();
        }

        // Todo: 오브젝트 풀링 적용하기
        // 폭탄 제거
        Destroy(gameObject);
    }
}
