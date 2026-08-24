using UnityEngine;

/// <summary>
/// [PickupFX.cs] v1
/// 적 처치 시 재료 조각이 튀어나와 기차로 빨려 들어가는 연출.
/// 재료는 조각이 기차에 "도착하는 순간" 실제로 인벤토리에 들어간다.
///
/// 사용법: Enemy.Die에서 PickupFX.Spawn(위치, 재료타입, 수량) 호출.
/// 씬 세팅 불필요 (스프라이트도 코드로 생성).
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class PickupFX : MonoBehaviour
{
    private static Sprite cachedSprite;

    // v2: 폭식 페이즈 (디 오리지널 P2) - 이 보스가 살아 있으면 조각 쟁탈전이 벌어진다
    // BossEnemy가 페이즈 진입/이탈 시 설정/해제
    public static BossEnemy FeedingBoss = null;

    private MaterialType mat;
    private Transform target;        // 기차
    private Vector3 popDir;          // 초기 튀어나가는 방향
    private float t = 0f;
    private bool collected = false;
    private bool contested = false;  // v2: 보스와 쟁탈 대상인 조각인가

    private const float POP_TIME = 0.28f;   // 튀어나가는 시간
    private const float MAX_LIFE = 4f;      // 안전장치 (이 시간 넘으면 강제 수급)

    /// <summary>처치 위치에서 재료 조각 amount개 생성</summary>
    public static void Spawn(Vector3 from, MaterialType matType, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject go = new GameObject("PickupFX_" + matType);
            go.transform.position = from + (Vector3)(Random.insideUnitCircle * 0.3f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite();
            sr.color = ColorOf(matType);
            sr.sortingOrder = 60;   // 적/이펙트보다 위
            go.transform.localScale = Vector3.one * 0.32f;

            PickupFX fx = go.AddComponent<PickupFX>();
            fx.mat = matType;
            fx.popDir = Random.insideUnitCircle.normalized;

            GameObject train = GameObject.FindGameObjectWithTag("Train");
            fx.target = train != null ? train.transform : null;

            // v2: 폭식 페이즈 중 생성된 조각은 일정 확률로 쟁탈 대상 (보스도 노린다)
            if (FeedingBoss != null && FeedingBoss.IsAlive
                && Random.value < GameBalance.FeedContestChance)
            {
                fx.contested = true;
                fx.transform.localScale = Vector3.one * 0.4f;   // 쟁탈 조각은 살짝 크게 (식별)
            }
        }
    }

    private void Update()
    {
        t += Time.deltaTime;

        if (t < POP_TIME)
        {
            // 1단계: 사방으로 살짝 튀어나감 (점점 감속)
            transform.position += popDir * 3.5f * (1f - t / POP_TIME) * Time.deltaTime;
        }
        else
        {
            // 2단계: 흡입 - 기본은 기차로, 쟁탈 조각은 "더 가까운 쪽"으로 (폭식 페이즈)
            if (target == null) { Collect(); return; }

            Vector3 destination = target.position;
            bool towardBoss = false;

            if (contested && FeedingBoss != null && FeedingBoss.IsAlive)
            {
                float dTrain = Vector3.Distance(transform.position, target.position);
                float dBoss = Vector3.Distance(transform.position, FeedingBoss.transform.position);
                if (dBoss < dTrain)
                {
                    destination = FeedingBoss.transform.position;
                    towardBoss = true;
                }
            }

            Vector3 toTarget = destination - transform.position;
            if (toTarget.magnitude < 0.7f)
            {
                if (towardBoss) { EatenByBoss(); return; }
                Collect();
                return;
            }

            float speed = 6f + (t - POP_TIME) * 26f;   // 갈수록 빨라짐
            transform.position += toTarget.normalized * speed * Time.deltaTime;

            // 흡입될수록 작아짐
            float shrink = Mathf.Max(0.14f, 0.32f - (t - POP_TIME) * 0.12f);
            transform.localScale = Vector3.one * shrink;
        }

        // 안전장치: 오래 살아있으면 그냥 수급 처리
        if (t > MAX_LIFE) Collect();
    }

    /// <summary>실제 재료 지급 + 제거</summary>
    private void Collect()
    {
        if (!collected)
        {
            collected = true;
            SoundManager.Play("sfx_pickup", 0.7f, 0.12f);   // 흡수음 (연속 수급 시 피치 변주)
            if (MaterialInventory.Instance != null)
                MaterialInventory.Instance.Add(mat, 1);
        }
        Destroy(gameObject);
    }

    /// <summary>v2: 보스가 먹어치움 - 플레이어는 재료를 잃는다 (폭식 페이즈)</summary>
    private void EatenByBoss()
    {
        collected = true;   // OnDestroy 보장 지급도 막는다 (도둑맞은 조각)
        if (FeedingBoss != null && FeedingBoss.IsAlive)
            FeedingBoss.EatFragment();
        Destroy(gameObject);
    }

    /// <summary>혹시 수급 전에 파괴되면 재료는 보장 지급</summary>
    private void OnDestroy()
    {
        if (!collected && MaterialInventory.Instance != null)
        {
            collected = true;
            MaterialInventory.Instance.Add(mat, 1);
        }
    }

    /// <summary>재료 타입별 색 (포탑 이펙트 색과 통일). P1: GameFeel 처치 팝도 이 색을 씀 (public)</summary>
    public static Color ColorOf(MaterialType t)
    {
        string key = t.ToString().ToLower();
        if (key == "meat") return TurretAttackExecutor.TagColor(FoodTag.Phys);
        if (key == "elec") return TurretAttackExecutor.TagColor(FoodTag.Elec);
        if (key == "fire") return TurretAttackExecutor.TagColor(FoodTag.Fire);
        if (key == "ice") return TurretAttackExecutor.TagColor(FoodTag.Ice);
        if (key == "poison") return TurretAttackExecutor.TagColor(FoodTag.Poison);
        return new Color(0.75f, 0.7f, 0.55f);   // 등심(장갑)
    }

    /// <summary>원형 스프라이트 1회 생성 후 캐시</summary>
    private static Sprite GetSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, d <= 6.5f ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        cachedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return cachedSprite;
    }
}
