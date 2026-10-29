using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [EnemyHpBar.cs] v1.1 (v9.36 2026-10-29 그림 팩: 머리 위 상태 표시 fx_status_(burn·poison·slow·stun) - 걸린 동안 바 위(바가 숨어 있으면 그 자리)에 한 줄로, GameBalance.FxStatusSprite) / v1 (신규, v9.10 2026-09-17) - 손님 머리 위 얇은 HP 바
///
/// 테스터 피드백 "몬스터 체력도 위에 보여줬으면". 개정안 §6: "피해를 받은 적 위에 얇게, 모든 적의 수치를 상시 띄우지 않는다".
///  - 0.2초마다 씬의 Enemy 를 훑어 마커가 없는 손님에 바를 붙인다 (EnemySkin 과 같은 방식, 씬·프리팹 수정 0). 보스는 전용 HP 바가 있으니 제외.
///  - 바 = 검정 홈 + 초록→노랑→빨강 채움 (월드 스프라이트 2장, 정렬 = 적(5) 위 7). 폭 1.1u, 높이 0.12u, 스프라이트 위 0.15u.
///  - HP 가 줄어든 순간부터 GameBalance.EnemyHpBarHideSec 동안만 보인다 (가득 찬 손님은 안 보인다 = 혼잡 방지). 죽으면 같이 사라진다.
///  - GameBalance.EnemyHpBars = false 면 아무것도 안 만든다.
/// 사용법: 없음! 파일만 넣으면 자동 생성.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class EnemyHpBar : MonoBehaviour
{
    private const int SORT_ORDER = 7;
    private const float WIDTH = 1.1f, HEIGHT = 0.12f, ABOVE = 0.15f;

    private static EnemyHpBar scanner;
    private static Sprite whiteSprite;
    private float nextScan = 0f;

    // ── 바 1개 ──
    private Enemy enemy;
    private Transform bg, fill;
    private SpriteRenderer fillSr;
    private float lastRatio = 1f;
    private float showUntil = 0f;
    private float topOffset = 1.0f;

    // v1.1: 상태 표시 넷 (화상 · 독 · 감속 · 마비) - 바와 따로 산다, 바가 안 보여도 뜬다
    private static readonly string[] STATUS_NAMES = { "fx_status_burn", "fx_status_poison", "fx_status_slow", "fx_status_stun" };
    private Transform statusRoot;
    private SpriteRenderer[] statusSr;
    private bool statusTried;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (scanner != null || !GameBalance.EnemyHpBars) return;
        GameObject go = new GameObject("EnemyHpBar_Scanner");
        DontDestroyOnLoad(go);
        scanner = go.AddComponent<EnemyHpBar>();
    }

    private void Update()
    {
        if (enemy == null && this == scanner) { Scan(); return; }
        TickBar();
    }

    // ─────────────────────────────────────────────
    // 스캐너
    // ─────────────────────────────────────────────
    private void Scan()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.2f;
        if (!GameBalance.EnemyHpBars) return;
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Enemy e = all[i];
            if (e == null || e is BossEnemy) continue;
            if (e.GetComponent<EnemyHpBar>() != null) continue;
            EnemyHpBar bar = e.gameObject.AddComponent<EnemyHpBar>();
            bar.Attach(e);
        }
    }

    // ─────────────────────────────────────────────
    // 바
    // ─────────────────────────────────────────────
    private void Attach(Enemy e)
    {
        enemy = e;
        if (whiteSprite == null) whiteSprite = TrainDeck.GetWhiteSprite();

        // 스프라이트 위 끝을 찾아 그 위에 (없으면 1u 위)
        SpriteRenderer[] srs = e.GetComponentsInChildren<SpriteRenderer>();
        float top = float.MinValue;
        for (int i = 0; i < srs.Length; i++)
            if (srs[i] != null && srs[i].enabled && srs[i].sprite != null) top = Mathf.Max(top, srs[i].bounds.max.y - e.transform.position.y);
        topOffset = top > -100f ? top + ABOVE : 1.0f;

        GameObject bgGo = new GameObject("HpBarBg");
        bgGo.transform.SetParent(e.transform, false);
        SpriteRenderer bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = whiteSprite; bgSr.color = new Color(0.05f, 0.04f, 0.04f, 0.85f); bgSr.sortingOrder = SORT_ORDER;
        bg = bgGo.transform;

        GameObject fillGo = new GameObject("HpBarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        fillSr = fillGo.AddComponent<SpriteRenderer>();
        fillSr.sprite = whiteSprite; fillSr.sortingOrder = SORT_ORDER + 1;
        fill = fillGo.transform;

        lastRatio = e.HPRatio;
        bgGo.SetActive(false);
    }

    private void TickBar()
    {
        if (enemy == null || bg == null) { if (bg != null) Destroy(bg.gameObject); if (statusRoot != null) Destroy(statusRoot.gameObject); Destroy(this); return; }
        TickStatus();   // v1.1: 상태 표시는 바와 따로
        if (!enemy.IsAlive || !GameBalance.EnemyHpBars)
        {
            if (bg.gameObject.activeSelf) bg.gameObject.SetActive(false);
            return;
        }

        float ratio = Mathf.Clamp01(enemy.HPRatio);
        if (ratio < lastRatio - 0.001f) showUntil = Time.time + GameBalance.EnemyHpBarHideSec;   // 맞았다 - 잠시 보여준다
        lastRatio = ratio;

        bool show = ratio < 0.999f && Time.time < showUntil;
        if (bg.gameObject.activeSelf != show) bg.gameObject.SetActive(show);
        if (!show) return;

        // 적은 이동 방향으로 회전하므로 바는 월드 기준으로 세운다 (부모 회전·크기 상쇄)
        Vector3 ls = enemy.transform.lossyScale;
        float invX = ls.x != 0f ? 1f / Mathf.Abs(ls.x) : 1f, invY = ls.y != 0f ? 1f / Mathf.Abs(ls.y) : 1f;
        bg.rotation = Quaternion.identity;
        bg.position = enemy.transform.position + Vector3.up * topOffset;
        bg.localScale = new Vector3(WIDTH * invX, HEIGHT * invY, 1f);

        // 채움: 왼쪽 정렬 (비율만큼 폭, 왼쪽 끝 고정)
        fill.localScale = new Vector3(Mathf.Max(0.001f, ratio) * (1f - 0.08f), 0.6f, 1f);
        fill.localPosition = new Vector3(-0.5f * (1f - 0.08f) * (1f - ratio), 0f, -0.01f);
        fillSr.color = ratio > 0.5f ? Color.Lerp(new Color(0.95f, 0.85f, 0.2f), new Color(0.35f, 0.85f, 0.35f), (ratio - 0.5f) * 2f)
                                    : Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.2f), ratio * 2f);
    }

    /// <summary>v1.1: 상태 표시 - 처음 필요할 때 넷을 만들고, 걸린 것만 켜서 가운데 정렬로 한 줄. 바가 보이면 바 위, 아니면 바 자리</summary>
    private void TickStatus()
    {
        if (!GameBalance.FxStatusSprite || !enemy.IsAlive)
        {
            if (statusRoot != null && statusRoot.gameObject.activeSelf) statusRoot.gameObject.SetActive(false);
            return;
        }
        if (statusSr != null && statusRoot == null) { statusSr = null; statusTried = false; }   // 리뷰: 루트가 바깥에서 지워졌으면 다시 만든다
        if (statusSr == null)
        {
            if (statusTried) return;
            statusTried = true;
            if (!SpriteBank.Has(STATUS_NAMES[0])) return;   // 그림이 없으면 아무것도 안 만든다
            GameObject rootGo = new GameObject("StatusIcons");
            rootGo.transform.SetParent(enemy.transform, false);
            statusRoot = rootGo.transform;
            statusSr = new SpriteRenderer[STATUS_NAMES.Length];
            for (int i = 0; i < STATUS_NAMES.Length; i++)
            {
                GameObject go = new GameObject(STATUS_NAMES[i]);
                go.transform.SetParent(statusRoot, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Get(STATUS_NAMES[i]); sr.sortingOrder = SORT_ORDER + 2;
                sr.enabled = false;
                statusSr[i] = sr;
            }
        }
        bool b = enemy.HasBurn, p = enemy.HasPoison, s = enemy.IsSlowedNow, st = enemy.IsStunnedNow;
        int on = (b ? 1 : 0) + (p ? 1 : 0) + (s ? 1 : 0) + (st ? 1 : 0);
        if (on == 0) { if (statusRoot.gameObject.activeSelf) statusRoot.gameObject.SetActive(false); return; }
        if (!statusRoot.gameObject.activeSelf) statusRoot.gameObject.SetActive(true);

        // 월드 기준으로 세운다 (부모 회전·크기 상쇄 - 바와 같은 식). 그림은 16px = 0.5u (32ppu) - FxStatusSize 로 다시 잰다
        Vector3 ls = enemy.transform.lossyScale;
        float invX = ls.x != 0f ? 1f / Mathf.Abs(ls.x) : 1f, invY = ls.y != 0f ? 1f / Mathf.Abs(ls.y) : 1f;
        float barUp = (bg != null && bg.gameObject.activeSelf) ? HEIGHT + 0.08f : 0f;
        statusRoot.rotation = Quaternion.identity;
        statusRoot.position = enemy.transform.position + Vector3.up * (topOffset + barUp + GameBalance.FxStatusSize * 0.5f);
        float sz = GameBalance.FxStatusSize / 0.5f;
        statusRoot.localScale = new Vector3(sz * invX, sz * invY, 1f);
        float step = (GameBalance.FxStatusSize + GameBalance.FxStatusGap) / Mathf.Max(0.01f, GameBalance.FxStatusSize) * 0.5f;   // 자식 로컬 단위 (한 칸 = 0.5u)
        float x = -(on - 1) * step * 0.5f;
        bool[] flags = { b, p, s, st };
        for (int i = 0; i < statusSr.Length; i++)
        {
            bool show = flags[i] && statusSr[i].sprite != null;
            statusSr[i].enabled = show;
            if (!show) continue;
            statusSr[i].transform.localPosition = new Vector3(x, 0f, -0.01f);
            x += step;
        }
    }

    private void OnDestroy()
    {
        if (bg != null) Destroy(bg.gameObject);
        if (statusRoot != null) Destroy(statusRoot.gameObject);
    }
}
