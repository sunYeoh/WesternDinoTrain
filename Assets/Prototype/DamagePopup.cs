using UnityEngine;
using TMPro;

/// <summary>
/// [DamagePopup.cs] v1.6 (v9.35.4 2026-10-26 유저 "작다는 말이 폰트였어 - 크기인지 색인지 글꼴인지, 눈에 잘 안 들어오네" (기차 곁 손님의 "n 저항" 숫자): 크기 3.5 -> GameBalance.DmgPopupFontSize 4.4 (저항 숫자도 같은 크기 - 예전 3), 모든 숫자에 어두운 외곽선 (같은 글을 어둡게 부풀린(SDF _FaceDilate) 뒷판 한 장 - 셰이더 키워드 없이, 재질 하나를 같이 쓴다), 저항 색을 밝게, 떠 있는 시간 0.8 -> DmgPopupLifeSec 1.0) / v1.5 (v9.35.3 2026-10-26 부식으로 커진 타격 = 연두 숫자 (CreateFor(..., corroded) - 부식의 정수 오라 안 손님. 무엇 때문에 커졌는지 숫자 색으로) / v1.4 (v9.35.2 2026-10-26 숫자 오브젝트는 TMPFontFixer.NewWorldText 로 - 글꼴이 든 본을 복제 (매번 AddComponent 하면 TMP 가 기본 글꼴을 찾다 경고 - Player.log 10-26 2,148줄)) / v1.3 (v9.18 2026-10-06: 합산은 같은 포탑이 낸 것끼리만 - CreateFor(..., source). 구: 포탑이 달라도 한 손님에게 0.1초 안에 들어오면 한 숫자 -> 약한 포탑의 숫자가 센 포탑 것과 합쳐져 보였다) /
/// v1.2 (v9.17 2026-10-06 화면 손맛 2차 A4: CreateFor(손님) - 숫자 크기 = 기본 x (1 + 피해 / 손님 최대 HP), 최대 2배 /
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
    private static Material shadeMat;          // v1.6: 뒷판 재질 (글꼴 재질 복제 + _FaceDilate 로 부풀림) - 뒷판이 전부 같이 쓴다
    private static bool shadeTried = false;
    private static readonly Color SHADE_COLOR = new Color(0.09f, 0.06f, 0.05f, 1f);   // 어두운 갈색 (땅·손님 위에서 밝은 숫자를 띄운다)

    /// <summary>
    /// v1.6: 뒷판 재질 - 글꼴(TMPFontFixer 가 만든 SDF 글꼴)의 재질을 한 번 복제해 _FaceDilate 로 글자를 부풀린다 (어두운 색으로 뒤에 깔면 외곽선이 된다).
    /// 키워드가 필요 없는 속성이라 빌드에서도 그대로 (OUTLINE_ON 같은 shader_feature 는 그 키워드를 켠 재질이 프로젝트에 없으면 빌드에서 잘린다)
    /// </summary>
    private static Material ShadeMaterial(TextMeshPro t)
    {
        // 글꼴이 새로 만들어졌으면 (아틀라스가 다른 텍스처) 옛 재질을 버리고 다시 - 옛 텍스처를 붙잡은 뒷판은 깨진 글자가 된다
        if (shadeMat != null && t != null && t.font != null && t.font.atlasTexture != null && shadeMat.mainTexture != t.font.atlasTexture) { shadeMat = null; shadeTried = false; }
        if (shadeMat == null && !shadeTried)
        {
            shadeTried = true;   // 글꼴 재질이 없으면 (TMP 글꼴을 못 만든 경우) 다시 시도하지 않는다
            if (t == null || t.font == null || t.font.material == null) return null;
            shadeMat = new Material(t.font.material);
            shadeMat.name = "DamagePopupShade";
            shadeMat.SetFloat("_FaceDilate", Mathf.Clamp01(GameBalance.DmgPopupShade));
        }
        return shadeMat;
    }

    /// <summary>v1.6: 에디터에서 도메인 리로드를 꺼 둔 채 다시 Play 하면 static 이 죽은 객체를 붙잡고 남는다 - 플레이마다 비운다</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        shadeMat = null; shadeTried = false;
        prefab = null; prefabLooked = false;
    }

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
    public static void CreateFor(Enemy e, float damage, bool isCritical, bool resisted) { CreateFor(e, damage, isCritical, resisted, 0); }

    /// <summary>v1.3: source = 쏜 포탑 (TurretSlot 출처 번호, 0 = 모름). 앞 숫자와 출처가 같을 때만 더한다 (GameBalance.DmgPopupMergePerSource)</summary>
    public static void CreateFor(Enemy e, float damage, bool isCritical, bool resisted, int source) { CreateFor(e, damage, isCritical, resisted, source, false); }

    /// <summary>v1.5 (v9.35.3): corroded = 부식(부식의 정수 오라)으로 커진 타격 - 연두 숫자</summary>
    public static void CreateFor(Enemy e, float damage, bool isCritical, bool resisted, int source, bool corroded)
    {
        if (e == null) return;
        float maxHP = Mathf.Max(1f, e.scaledMaxHP);
        bool sameSource = !GameBalance.DmgPopupMergePerSource || e.lastPopupSource == source;
        if (GameBalance.DmgPopupMergeSec > 0f && sameSource && e.lastPopup != null && Time.time - e.lastPopupTime <= GameBalance.DmgPopupMergeSec)
        {
            e.lastPopup.AddDamage(damage, isCritical, resisted, corroded);
            return;
        }
        e.lastPopup = Spawn(e.transform.position, damage, isCritical, resisted, maxHP, corroded);
        e.lastPopupTime = Time.time;
        e.lastPopupSource = source;
    }

    private static DamagePopup Spawn(Vector3 worldPos, float damage, bool isCritical, bool resisted, float maxHP) { return Spawn(worldPos, damage, isCritical, resisted, maxHP, false); }

    private static DamagePopup Spawn(Vector3 worldPos, float damage, bool isCritical, bool resisted, float maxHP, bool corroded)
    {
        if (!prefabLooked) { prefab = Resources.Load<GameObject>("DamagePopup"); prefabLooked = true; }

        // 스폰 위치 분산 (보스처럼 한 자리에 숫자가 몰릴 때 겹치지 않게)
        Vector3 spawnPos = worldPos + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.2f, 0.7f), 0f);

        GameObject obj;
        if (prefab != null && prefab.GetComponent<TextMeshPro>() != null) obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        else obj = TMPFontFixer.NewWorldText("DamagePopup", spawnPos).gameObject;   // v1.4: 글꼴이 든 본을 복제 (경고 없음)

        DamagePopup popup = obj.GetComponent<DamagePopup>();
        if (popup == null) popup = obj.AddComponent<DamagePopup>();
        popup.Setup(damage, isCritical, resisted, maxHP);
        if (corroded) popup.MarkCorroded();   // v1.5
        return popup;
    }

    // ─────────────────────────────────────────────
    // 인스턴스
    // ─────────────────────────────────────────────
    private TextMeshPro tmp;
    private TextMeshPro shade;         // v1.6: 어두운 뒷판 (외곽선) - 글·크기·자리를 tmp 와 같게 맞춘다
    private float total = 0f;          // 합산된 피해
    private bool crit = false;         // 합산된 명중 중 하나라도 치명타
    private bool resistedAll = false;  // 합산된 명중이 전부 저항
    private bool corrodedAny = false;  // v1.5: 합산된 명중 중 하나라도 부식으로 커졌다 (연두)
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
        // v1.6: 어두운 뒷판 (같은 글을 부풀려 뒤에) - 외곽선
        if (GameBalance.DmgPopupShade > 0f)
        {
            if (shade == null)
            {
                shade = TMPFontFixer.NewWorldText("Shade", transform.position);
                if (shade != null)
                {
                    shade.transform.SetParent(transform, false);
                    shade.transform.localPosition = Vector3.zero;
                    shade.transform.localScale = Vector3.one;
                }
            }
            if (shade != null)
            {
                Material m = ShadeMaterial(shade);
                if (m != null && shade.fontSharedMaterial != m) shade.fontSharedMaterial = m;
            }
        }

        total = damage; crit = isCritical; resistedAll = resisted; maxHP = enemyMaxHP;
        corrodedAny = false;
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

    /// <summary>v1.5: 부식으로 커진 타격이다 - 색만 바꾼다 (Spawn 이 Setup 다음에 부른다)</summary>
    public void MarkCorroded()
    {
        corrodedAny = true;
        ApplyStyle();
    }

    /// <summary>v1.2: 같은 손님의 다음 명중을 이 숫자에 더한다. 숫자가 다시 한 번 튄다</summary>
    public void AddDamage(float damage, bool isCritical, bool resisted) { AddDamage(damage, isCritical, resisted, false); }

    /// <summary>v1.5: corroded = 부식으로 커진 타격 (하나라도 있으면 연두)</summary>
    public void AddDamage(float damage, bool isCritical, bool resisted, bool corroded)
    {
        if (corroded) corrodedAny = true;
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

        float size = GameBalance.DmgPopupFontSize > 0f ? GameBalance.DmgPopupFontSize : (resistedAll ? FONT_RESIST : FONT_NORMAL);   // v1.6: 저항 숫자도 같은 크기
        if (maxHP > 0f && GameBalance.DmgPopupScaleOn && GameBalance.GameFeelMaster > 0f)
            size *= Mathf.Clamp(1f + total / maxHP, 1f, Mathf.Max(1f, GameBalance.DmgPopupMaxScale));
        tmp.fontSize = size;

        startColor = resistedAll ? new Color(0.78f, 0.84f, 0.98f)  // 저항: 회청색 (v1.6: 밝게 - 외곽선이 생겨 밝아도 읽힌다)
            : crit ? new Color(1f, 0.3f, 0f)                       // 크리: 주황
            : corrodedAny ? new Color(0.62f, 1f, 0.42f)            // v1.5 부식: 연두 (부식의 정수 오라 안 - 피해 +20%)
            : new Color(1f, 1f, 0.3f);                             // 일반: 노랑
        tmp.color = startColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = crit ? FontStyles.Bold : FontStyles.Normal;
        tmp.sortingOrder = 100;   // 기차·손님에 가려지지 않게
        if (shade != null)
        {
            // v1.6: 뒷판은 글·크기·굵기·칸·줄바꿈·정렬 층을 본 글과 같게 (프리팹으로 만든 본 글과 어긋나지 않게), 색만 어둡게, 한 단계 뒤에
            shade.text = tmp.text;
            shade.fontSize = size;
            shade.alignment = TextAlignmentOptions.Center;
            shade.fontStyle = tmp.fontStyle;
            shade.enableWordWrapping = tmp.enableWordWrapping;
            shade.rectTransform.pivot = tmp.rectTransform.pivot;
            shade.rectTransform.sizeDelta = tmp.rectTransform.sizeDelta;
            shade.sortingLayerID = tmp.sortingLayerID;
            shade.color = SHADE_COLOR;
            shade.sortingOrder = 99;
        }
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

        // 페이드 아웃 (뒤 절반에서). v1.6: 떠 있는 시간 GameBalance.DmgPopupLifeSec (크리 +0.1)
        float life = GameBalance.DmgPopupLifeSec > 0f ? GameBalance.DmgPopupLifeSec + (crit ? 0.1f : 0f) : (crit ? LIFE_CRIT : LIFE);
        if (tmp != null)
        {
            Color c = startColor;
            c.a = Mathf.Clamp01((life - elapsed) / (life * 0.5f));
            tmp.color = c;
            if (shade != null) { Color sc = SHADE_COLOR; sc.a = c.a; shade.color = sc; }   // v1.6: 뒷판도 같이 흐려진다
        }
        if (elapsed >= life) Destroy(gameObject);
    }
}
