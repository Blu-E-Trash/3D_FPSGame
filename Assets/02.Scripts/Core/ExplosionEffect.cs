using UnityEngine;

// 폭발 이펙트를 실제 폭발 범위(반경)만큼 크게 만들어서 생성한다. (폭탄, 드럼통이 같이 쓴다)
public static class ExplosionEffect
{
    // radius: 실제 대미지가 들어가는 반경
    // baseRadius: 이펙트 프리팹을 스케일 1로 터뜨렸을 때 눈에 보이는 폭발 반경
    public static ParticleSystem Spawn(ParticleSystem prefab, Vector3 position, float radius, float baseRadius)
    {
        ParticleSystem effect = Object.Instantiate(prefab, position, Quaternion.identity);

        // 파티클 시스템은 기본 설정(Shape)이면 오브젝트 크기를 키워도 "나오는 범위"만 커지고 파티클 크기는 그대로다.
        // Hierarchy로 바꾸면 오브젝트 크기를 키운 만큼 파티클 크기, 퍼지는 거리까지 전부 같이 커진다.
        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = particle.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        effect.transform.localScale = Vector3.one * (radius / baseRadius);
        effect.Play();
        return effect;
    }
}
