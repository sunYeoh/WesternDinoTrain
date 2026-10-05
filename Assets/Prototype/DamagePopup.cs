using UnityEngine;
using TMPro;

/// <summary>
/// [DamagePopup.cs] v1.2 (v9.17 2026-10-06 화면 손맛 2차 A4: CreateFor(손님) - 숫자 크기 = 기본 x (1 + 피해 / 손님 최대 HP), 최대 2배 /
///   같은 손님이 0.1초 안에 또 맞으면 새 숫자 대신 앞 숫자에 더한다 / 등장할 때 1.25배에서 제자리로, 크리는 굵게 + 1.6배에서 0.1초 더 길게 + 더 높이 /
///   크리 여부는 Enemy 가 진짜 치명타만 넘긴다) / v1.1 (v9.15 2026-09-29: resisted 팝업 - 회청색 "n 저항")
/// 손님이 맞으면 피해 숫자가 위로 떠오르며 사라진다.
///
/// 사용법: DamagePopup.CreateFor(enemy, damage, isCritical, resisted);   // 손님 (크기 비례·합산)
///         DamagePopup.Create(position, damage, isCritical);               // 자리만 아는 경우 (고정 크기)
/// 스위치: GameBalance.DmgPopupScaleOn / DmgPopupMaxScale / DmgPopupMergeSec
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const float FONT_NORMAL = 3.5f, FONT_RESIST = 3f;
    private const float POP_AMT = 0.25f, POP_SEC = 0.1f;            // 일반: 1.25배 -> 1.0
    private const float POP_AMT_CRIT = 0.6f, POP_SEC_CRIT = 0.2f;   // 크리: 1.6배 -> 1.0, 0.1초 더 길게
    private const float LIFE = 0.8f, LIFE_CRIT = 0.9f;
    private const float RISE = 1.5f, RISE_CRIT = 2.1f;

    private static GameObject prefab;
    private static bool prefabLooked = false;

    // ─────────────────────────────────────────────
    // 만들기
    // ─────────────────────────────────────────────
    /// <summary>월드 좌표에 피해 숫자 (고정 크기)</summary>
    public static void Create(Vector3 worldPos, float damage, bool isCritical) { Create(worldPos, damage, isCritical, false); }
    public static void Create(Vector3 worldPos, float damage) { Create(worldPos, damage, false, false); }

    /// <summary>v1.1: resisted = 방어·저항으로 크게 깎인 타격 ("저항" 표기)</summary>
    public static void Create(Vector3 worldPos, float damage, bool isCritical, bool resisted)
    {
        Spawn(worldPos, damage, isCritical, resisted, 0f);
    }

    /// <summary>
    /// v1.2: 손님이 맞았다. 크기는 피해 / 최대 HP 에 비례하고, DmgPopupMergeSec 안에 같은 손님이 또 맞으면 앞 숫자에 더한다
    /// (합산 창은 첫 숫자가 뜬 때부터 센다 - 연사 포탑이 숫자 하나를 끝없이 붙들지 않는다)
    /// </summary>
    public static void CreateFor(Enemy e, float damage, bool isCritical, bool resisted)
    {
        if (e == null) return;
        float maxHP = Mathf.Max(1f, e.scaledMaxHP);
        if (GameBalance.DmgPopupMergeSec > 0f && e.lastPopup != null && Time.time - e.lastPopupTime <= GameBalance.DmgPopupMergeSec)
        {
            e.lastPopup.AddDamage(damage, isCritical, resisted);
            return;
        }
        e.lastPopup = Spawn(e.transform.position, damage, isCritical, resisted, maxHP);
        e.lastPopupTime = Time.time;
    }

    private static DamagePopup Spawn(Vector3 worldPos, float damage, bool isCritical, bool resisted, float maxHP)
    {
        if (!prefabLooked) { prefab = Resources.Load<GameObject>("DamagePopup"); prefabLooked = true; }

        // 스폰 위치 분산 (보스처럼 한 자리에 숫자가 몰릴 때 겹치지 않게)
        Vector3 spawnPos = worldPos + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.2f, 0.7f), 0f);

        GameObject obj;
        if (prefab != null) obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        else { obj = new GameObject("DamagePopup"); obj.transform.position = spawnPos; }

        DamagePopup popup = obj.GetComponent<DamagePopup>();
        if (popup == null) popup = obj.AddComponent<DamagePopup>();
        popup.Setup(damage, isCritical, resisted, maxHP);
        return popup;
    }

    // ─────────────────────────────────────────────
    // 인스턴스
    // ─────────────────────────────────────────────
    private TextMeshPro tmp;
    private float total = 0f;          // 합산된 피해
    private bool crit = false;         // 합산된 명중 중 하나라도 치명타
    private bool resistedAll = false;  // 합산된 명중이 전부 저항
    private float maxHP = 0f;          // 0 = 크기 고정
    private float moveSpeedX = 0f;
    private float elapsed = 0f;
    private float popT = 0f;
    private Color startColor;

    public void Setup(float damage, bool isCritical) { Setup(damage, isCritical, false, 0f); }
    public void Setup(float damage, bool isCritical, bool resisted) { Setup(damage, isCritical, resisted, 0f); }

    public void Setup(float damage, bool isCritical, bool resisted, float enemyMaxHP)
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null) tmp = gameObject.AddComponent<TextMeshPro>();
        TMPFontFixer.Apply(tmp);   // 통일 폰트

        total = damage; crit = isCritical; resistedAll = resisted; maxHP = enemyMaxHP;
        moveSpeedX = Random.Range(-1f, 1f);   // 같은 자리에서 여러 숫자가 떠도 서로 다른 쪽으로 흩어진다
        elapsed = 0f; popT = 0f;
        ApplyStyle();
        transform.localScale = Vector3.one * (1f + PopAmount());
    }

    /// <summary>등장 튀기 양 (연출 전체 끔 GameFeelMaster 0 이면 0 - 숫자는 그냥 뜬다)</summary>
    private float PopAmount()
    {
        if (GameBalance.GameFeelMaster <= 0f) return 0f;
        return crit ? POP_AMT_CRIT : POP_AMT;
    }

    /// <summary>v1.2: 같은 손님의 다음 명중을 이 숫자에 더한다. 숫자가 다시 한 번 튄다</summary>
    public void AddDamage(float damage, bool isCritical, bool resisted)
    {
        total += damage;
        if (isCritical) crit = true;
        if (!resisted) resistedAll = false;
        ApplyStyle();
        popT = 0f;
        elapsed = Mathf.Min(elapsed, 0.1f);   // 막 더해진 숫자가 바로 흐려지지 않게
    }

    /// <summary>글자·색·굵기·크기 (합산될 때마다 다시)</summary>
    private void ApplyStyle()
    {
        if (tmp == null) return;
        int shown = Mathf.Max(1, Mathf.RoundToInt(total));
        tmp.text = resistedAll ? shown + " 저항" : crit ? "!" + shown : shown.ToString();

        float size = resistedAll ? FONT_RESIST : FONT_NORMAL;
        if (maxHP > 0f && GameBalance.DmgPopupScaleOn && GameBalance.GameFeelMaster > 0f)
            size *= Mathf.Clamp(1f + total / maxHP, 1f, Mathf.Max(1f, GameBalance.DmgPopupMaxScale));
        tmp.fontSize = size;

        startColor = resistedAll ? new Color(0.62f, 0.7f, 0.85f)   // 저항: 회청색
            : crit ? new Color(1f, 0.3f, 0f)                       // 크리: 주황
            : new Color(1f, 1f, 0.3f);                             // 일반: 노랑
        tmp.color = startColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = crit ? FontStyles.Bold : FontStyles.Normal;
        tmp.sortingOrder = 100;   // 기차·손님에 가려지지 않게
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        // 등장 튀기: (1 + 양) -> 1 (easeOut)
        float popSec = crit ? POP_SEC_CRIT : POP_SEC;
        if (popT < popSec)
        {
            popT += dt;
            float k = Mathf.Clamp01(popT / popSec);
            float w = (1f - k) * (1f - k);
            transform.localScale = Vector3.one * (1f + PopAmount() * w);
        }

        // 대각선으로 떠오른다 (크리는 더 높이)
        transform.position += new Vector3(moveSpeedX, crit ? RISE_CRIT : RISE, 0f) * dt;

        // 페이드 아웃 (뒤 절반에서)
        float life = crit ? LIFE_CRIT : LIFE;
        if (tmp != null)
        {
            Color c = startColor;
            c.a = Mathf.Clamp01((life - elapsed) / (life * 0.5f));
            tmp.color = c;
        }
        if (elapsed >= life) Destroy(gameObject);
    }
}
