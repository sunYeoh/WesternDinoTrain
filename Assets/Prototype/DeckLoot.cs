using UnityEngine;

/// <summary>
/// [DeckLoot.cs] v1.1 (v9.9.2 2026-09-16: 4모서리 배치의 남쪽 포탑 자리를 피해 떨어진다 - 상자가 포탑 위에 그려지던 것) / v1 (신규 파일) - B-2: 갑판 전리품 상자 (방향결정 2026-08-31)
///
/// 아이템(유물) 획득이 즉시 지급 대신 "갑판에 떨어진 상자"가 된다.
/// 셰프가 걸어가서 밟으면 회수 - 걷는 것 자체가 보상 행위가 되게.
///  - 대상: 적 처치 아이템 드랍 / 폐역 선로 아이템 (희귀한 순간만 상자로)
///  - 비대상: 재료 보장드랍·비상 식량 창고·침입자 드랍·행상인 구매 = 기존 즉시 지급
///    (기본 경제까지 상자로 만들면 보상이 노동이 된다 - 안티프러스트레이션 헌법)
///  - GameBalance.DeckLootEnabled = false 면 전부 즉시 지급으로 복귀
///
/// 상자는 씬 오브젝트라 런 재시작(씬 리로드) 시 자연 소멸 - 안전.
/// 사용법: 없음! Enemy/WaveManager가 SpawnItemCrate로 생성한다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class DeckLoot : MonoBehaviour
{
    private string sourceLabel = "";   // 회수 시 알림에 쓸 출처 문구
    private Transform chefTransform;
    private float bobPhase;

    /// <summary>
    /// 아이템 상자 생성. nearX 근처의 갑판 위에 떨어진다 (활동 범위로 클램프).
    /// DeckLootEnabled가 꺼져 있으면 상자 없이 즉시 지급.
    /// </summary>
    public static void SpawnItemCrate(float nearX, string sourceLabel)
    {
        if (!GameBalance.DeckLootEnabled)
        {
            ItemManager.GrantRandom(sourceLabel);
            return;
        }

        float x = Mathf.Clamp(nearX, GameBalance.TrainWalkMinX + 0.5f, GameBalance.TrainWalkMaxX - 0.5f);
        x = AvoidSouthSlots(x);   // v1.1

        GameObject go = new GameObject("DeckLoot");
        go.transform.position = new Vector3(x, GameBalance.DeckLootY, 0f);
        DeckLoot loot = go.AddComponent<DeckLoot>();
        loot.sourceLabel = sourceLabel;
        loot.BuildVisual();

        UIManager.Instance?.ShowStatChange("[전리품] 갑판에 상자가 떨어졌다 - 밟아서 회수하라!");
        SoundManager.Play("sfx_pickup");
        Debug.Log("[DeckLoot] 상자 생성 (x " + x.ToString("F1") + ") / 출처: " + sourceLabel);
    }

    /// <summary>v1.1: 남쪽 포탑 자리(x ±0.9) 를 피해 가장 가까운 x 로 (0.7u 씩 좌우로 번갈아 벌려 본다). 1열 배치면 그대로</summary>
    private static float AvoidSouthSlots(float x)
    {
        if (!GameBalance.SlotCornerLayout) return x;
        float minX = GameBalance.TrainWalkMinX + 0.5f, maxX = GameBalance.TrainWalkMaxX - 0.5f;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float cand = attempt == 0 ? x : x + ((attempt % 2 == 1) ? 1f : -1f) * 0.7f * ((attempt + 1) / 2);
            cand = Mathf.Clamp(cand, minX, maxX);
            if (ClearOfSouthSlots(cand)) return cand;
        }
        return x;
    }

    private static bool ClearOfSouthSlots(float x)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!GameBalance.IsSouthSlot(i)) continue;
            if (Mathf.Abs(GameBalance.SlotPosition(i).x - x) < 0.9f) return false;
        }
        return true;
    }

    // ─────────────────────────────────────────────
    // 비주얼 (코드 도형 - 아트 단계에서 나무 상자 스프라이트로 교체)
    // ─────────────────────────────────────────────
    private void BuildVisual()
    {
        // 상자 본체 (호박색)
        SpriteRenderer body = gameObject.AddComponent<SpriteRenderer>();
        body.sprite = TrainDeck.GetWhiteSprite();
        body.color = new Color(0.85f, 0.6f, 0.22f);
        body.sortingOrder = 58;   // 처치 팝과 같은 층 (셰프보다 위 아님)
        transform.localScale = new Vector3(0.55f, 0.45f, 1f);

        // 띠 장식 (검정 포인트)
        GameObject strap = new GameObject("Strap");
        strap.transform.SetParent(transform, false);
        strap.transform.localScale = new Vector3(1f, 0.22f, 1f);
        SpriteRenderer strapSr = strap.AddComponent<SpriteRenderer>();
        strapSr.sprite = TrainDeck.GetWhiteSprite();
        strapSr.color = new Color(0.16f, 0.11f, 0.08f);
        strapSr.sortingOrder = 59;

        bobPhase = Random.Range(0f, 6.28f);
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 살짝 들썩이며 셰프를 기다린다
    // ─────────────────────────────────────────────
    private void Update()
    {
        // 들썩임 (여기 있어! 하는 존재감)
        bobPhase += Time.deltaTime * 3f;
        Vector3 p = transform.position;
        p.y = GameBalance.DeckLootY + Mathf.Abs(Mathf.Sin(bobPhase)) * 0.12f;
        transform.position = p;

        if (chefTransform == null)
        {
            GameObject chefObj = GameObject.Find("Chef");
            if (chefObj != null) chefTransform = chefObj.transform;
            if (chefTransform == null) return;
        }

        // 밟으면 회수
        if (Vector2.Distance(chefTransform.position, transform.position) <= GameBalance.DeckLootPickupRange)
            Collect();
    }

    private void Collect()
    {
        ItemManager.GrantRandom(sourceLabel);
        GameFeel.DeathPop(transform.position, new Color(1f, 0.85f, 0.4f), 0.6f);
        SoundManager.Play("sfx_pickup");
        Destroy(gameObject);
    }
}
