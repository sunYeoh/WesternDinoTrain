using UnityEngine;

/// <summary>
/// [TrainFeel.cs] v1 (신규, v9.11 2026-09-22) - 기차 피격 연출 (스펙 표 B1): 맞은 칸 흰 플래시
///
/// "내가 맞았다" 가 HP 숫자로만 보이던 것. TrainManager.TakeDamage 가 Hit() 를 부르면
/// 물린 칸(Enemy.AttackTrain 이 NextHitX 로 알려준 x 가 속한 칸, 모르면 기차 전체) 위에 흰 반투명 판을 0.08초 띄운다.
/// 흔들림은 기존 GameFeel.Shake(train_hit 채널, 1.5초 쿨) 그대로. 지연 체력바는 UIManager v?.? (같은 팩) 가 맡는다.
/// 사용법: 없음. GameBalance.TrainHitFlashOn = false 면 끔.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class TrainFeel
{
    /// <summary>다음 TakeDamage 한 번에 쓰일 피격 x (Enemy.AttackTrain 이 건다). 소비하면 지워진다</summary>
    public static float NextHitX = float.NaN;

    private const float CAR_TOP = 1.8f, CAR_BOTTOM = -1.75f;   // TrainDeck 칸 캔버스: 지붕 위 +1.8 / 남벽 아래 -1.75
    private const int SORT_ORDER = 4;                          // 포탑·조리대 위, 손님(5)·셰프(6) 아래
    private const float FLASH_SEC = 0.08f, FLASH_ALPHA = 0.38f;

    private static TrainFeelRunner runner;
    private static float lastFlashTime = -1f;

    /// <summary>기차가 맞았다. TrainManager.TakeDamage 에서</summary>
    public static void Hit()
    {
        if (!GameBalance.TrainHitFlashOn || GameBalance.GameFeelMaster <= 0f) return;
        float x = NextHitX; NextHitX = float.NaN;
        if (Time.time - lastFlashTime < 0.05f) return;   // 같은 순간 다중 피격은 한 번
        lastFlashTime = Time.time;

        float[] e = GameBalance.CarEdgesX;
        float x0 = e[0], x1 = e[e.Length - 1];
        if (!float.IsNaN(x))
        {
            // 물린 칸만 (가장 가까운 칸)
            int car = 0; float best = float.MaxValue;
            for (int i = 0; i < e.Length - 1; i++)
            {
                float mid = (e[i] + e[i + 1]) * 0.5f;
                float d = Mathf.Abs(x - mid);
                if (d < best) { best = d; car = i; }
            }
            x0 = e[car]; x1 = e[car + 1];
        }

        GameObject go = new GameObject("TrainFlash");
        go.transform.position = new Vector3((x0 + x1) * 0.5f, (CAR_TOP + CAR_BOTTOM) * 0.5f, 0f);
        go.transform.localScale = new Vector3(x1 - x0, CAR_TOP - CAR_BOTTOM, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TrainDeck.GetWhiteSprite();
        sr.sortingOrder = SORT_ORDER;
        sr.color = new Color(1f, 1f, 1f, FLASH_ALPHA * Mathf.Clamp01(GameBalance.GameFeelMaster));
        Runner().StartCoroutine(Runner().FlashRoutine(sr));
    }

    private static TrainFeelRunner Runner()
    {
        if (runner != null) return runner;
        GameObject go = new GameObject("TrainFeel");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<TrainFeelRunner>();
        return runner;
    }

    public class TrainFeelRunner : MonoBehaviour
    {
        public System.Collections.IEnumerator FlashRoutine(SpriteRenderer sr)
        {
            float t = 0f;
            Color c = sr.color; float a0 = c.a;
            while (t < FLASH_SEC && sr != null)
            {
                t += Time.deltaTime;
                c.a = a0 * (1f - t / FLASH_SEC);
                sr.color = c;
                yield return null;
            }
            if (sr != null) Destroy(sr.gameObject);
        }
    }
}
