using System.Collections;
using UnityEngine;

// 부서진 상자 조각. 잠시 남아 있다가 작아지면서 사라진다.
public class BoxPiece : MonoBehaviour
{
    public void Init(float lifeTime, float shrinkTime)
    {
        StartCoroutine(Disappear_Coroutine(lifeTime, shrinkTime));
    }

    private IEnumerator Disappear_Coroutine(float lifeTime, float shrinkTime)
    {
        // 1. 바닥에 잠시 남아 있는다.
        yield return new WaitForSeconds(lifeTime);

        // 2. 점점 작아진다. (툭 하고 사라지지 않게)
        Vector3 startScale = transform.localScale;
        float timer = 0f;
        while (timer < shrinkTime)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / shrinkTime);
            yield return null;
        }

        // 3. 사라진다.
        Destroy(gameObject);
    }
}
