using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [RecipeText.cs] v1.7 (v9.35.3 2026-10-26 오라 셋이 하는 일대로 (AuraWord - 예전엔 셋 다 "가까이 온 손님이 계속 맞는다": 빙벽 스튜·부식의 정수는 안 때린다) / 부식의 정수 = 방어·저항 깎기 + 부식 (맞는 피해 +n%) / 부채꼴 = 닿는 거리 7, 멀면 기다린다 / 낱말 요약에 오라 효과) / v1.6 (v9.35.2 2026-10-26 장판 줄 - n초 동안 그 위 손님이 계속 느려진다 (독 장판은 독도, GameBalance.FieldLingerSec) / 부식의 정수 줄 - 방어·저항이 깎인다 (AuraShredReal)) / v1.5 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v1.4 (v9.19.4 2026-10-07: 낱말 요약 Brief·ShapeWord·PassiveScales - 운행 중 포탑 정보창용. 유저 "포탑 설명이 너무 길다, 겜하면서 필요한 내용도 아니다") / v1.3 (v9.14 2026-09-28: 설명 짧게 - 재료 줄 제외, "잘 박힌다" 줄 간결 / RecipeHoverRelay.onClick) / v1.2 (v9.12 2026-09-22: 용어 "지속 피해") / v1.1 (v9.11.1 2026-09-22: 재료·조리대 줄(Source) 추가, 무방비 표현) / v1 (신규, v9.10 2026-09-17) - 요리(포탑) 설명을 일상어로 만드는 한 곳
///
/// 테스터 피드백: "도감에 요리를 눌렀을 때 뭔 요린지 모르니까 만들지 말지도 모르겠음", "포탑 효과를 읽을 시간이 없음",
/// "모르겠는 말(공명·인퓨징·DPS) 쓰지 말기". RecipeData 의 수치 필드(형태·속성·도트·감속·폭발·체인·회복·버프·패시브)를 그대로 읽어
/// "무엇을 하나 / 어떤 손님에 잘 박히나 / 언제 쓰나" 세 줄 + 숫자 한 줄을 만든다. 문구는 코드 필드에서만 나오므로 레시피를 고쳐도 같이 맞는다.
///
/// 사용법:
///   RecipeText.What(r)   - "가까운 손님 하나를 노려 쏜다. 화상(지속 피해)." (한 줄)
///   RecipeText.Against(r) - "물리 - 저항 높은 손님(날개 달린 것들)에 잘 박힌다" (한 줄)
///   RecipeText.When(r)   - "작은 손님이 무리로 올 때" (한 줄)
///   RecipeText.Numbers(r, levelMult) - "공격 26  1.0초마다  (초당 26)" (한 줄, 패시브면 "")
///   RecipeText.Full(r, levelMult)    - 위 넷을 줄바꿈으로 (툴팁·도감 상세용)
///   RecipeText.RoleWord(r) - "단일 화력 / 범위 / 제어 / 지원 / 약화 / 기차 강화 / 이웃 강화" (짧은 역할 낱말 - 카드 한 줄용)
///   RecipeText.Brief(r)  - "단일 · 물리" / "폭발 · 속성 · 화상" (낱말 요약 한 줄 - 운행 중 포탑 정보창용. 문장은 Full, 멈춰서 읽는 도감·조리 창에만)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class RecipeText
{
    /// <summary>짧은 역할 낱말 (카드·이름표 한 줄용)</summary>
    public static string RoleWord(RecipeData r)
    {
        if (r == null) return "";
        if (r.shape == AttackShape.Passive) return "기차 강화";
        if (!string.IsNullOrEmpty(r.buffType)) return "이웃 강화";   // v1.4: "곁 포탑 강화" -> 속성 줄 설명·문구원칙의 용어와 같게
        if (r.role == TurretRole.CC || r.slowLevel > 0 || r.stunSec > 0f) return "제어";
        if (r.role == TurretRole.Debuffer || r.shredDef > 0 || r.shredRes > 0) return "약화";
        if (r.role == TurretRole.Support || r.healOnHit > 0f) return "지원";
        if (r.shape == AttackShape.Explode || r.shape == AttackShape.Cone || r.shape == AttackShape.Field
            || r.shape == AttackShape.Chain || r.shape == AttackShape.Pierce || r.shape == AttackShape.Aura) return "범위";
        return "단일 화력";
    }

    /// <summary>v1.4: 공격 형태 낱말 (Brief 와 합체 미리보기가 같이 쓴다 - 용어는 문구원칙의 표: 직선 관통 · 연쇄 번개)</summary>
    public static string ShapeWord(RecipeData r)
    {
        if (r == null) return "";
        switch (r.shape)
        {
            case AttackShape.Pierce: return "직선 관통";
            case AttackShape.Cone: return "부채꼴";
            case AttackShape.Explode: return "폭발";
            case AttackShape.Chain: return "연쇄 번개 " + Mathf.Max(1, r.chainCount);
            case AttackShape.Field: return r.fieldBig ? "넓은 장판" : "장판";
            case AttackShape.Aura: return "기차 주변";
            case AttackShape.Passive: return "쏘지 않는다";
            default: return "단일";
        }
    }

    /// <summary>
    /// v1.4: 낱말 요약 한 줄 (운행 중 포탑 정보창용) - 문장이 아니라 "형태 · 물리/속성 · 덤 효과".
    /// 예: "단일 · 물리" / "폭발 · 속성 · 화상" / "연쇄 번개 3 · 속성 · 감속 50%".
    /// 쏘지 않는 요리(기차 강화 · 이웃 강화)는 레시피에 적힌 한 줄을 그대로 쓴다 ("[상시] 기차 HP 초당 +2").
    /// </summary>
    public static string Brief(RecipeData r)
    {
        if (r == null) return "";
        if (r.shape == AttackShape.Passive || !string.IsNullOrEmpty(r.buffType))
            return string.IsNullOrEmpty(r.description) ? RoleWord(r) : r.description;

        string s = ShapeWord(r);
        if (r.shape == AttackShape.Aura) return s + " · " + AuraBrief(r);   // v1.7: 오라는 하는 일 하나 (쏘지 않는다)
        if (r.damage > 0f) s += " · " + (r.damageType == DamageType.Magic ? "속성" : "물리");
        if (r.burnStack > 0) s += " · 화상";
        if (r.poisonStack > 0 || r.fieldPoison) s += " · 독";
        if (r.slowLevel > 0) s += " · 감속 " + (r.slowLevel >= 2 ? "70%" : "50%");
        if (r.stunSec > 0f) s += " · 마비 " + r.stunSec.ToString("F1") + "초";
        if (r.shredDef > 0) s += " · 방어 감소";
        if (r.shredRes > 0) s += " · 저항 감소";
        if (r.healOnHit > 0f) s += " · 맞출 때마다 기차 HP +" + r.healOnHit.ToString("F0");
        return s;
    }

    /// <summary>
    /// v1.4: 쏘지 않는 요리의 효과가 포탑 레벨을 따라 커지는가.
    /// 커지는 것 = 이웃 강화 / 회복(regen·omega 의 회복) / 받는 피해 감소(dr) / 되받기(thorns) / 불 오라(auraBurn).
    /// 안 커지는 것 = 최대 HP(maxhp - TurretSlot.ApplyMaxHPPassive "레벨 무관") / 감속 오라 · 방어 감소 오라 (TurretSlotManager.TickAuras 의 고정 값)
    /// </summary>
    public static bool PassiveScales(RecipeData r)
    {
        if (r == null) return false;
        if (!string.IsNullOrEmpty(r.buffType)) return true;
        string p = r.passiveType ?? "";
        return p == "regen" || p == "omega" || p == "dr" || p == "thorns" || p == "auraBurn";
    }

    /// <summary>무엇을 하나 - 형태 + 부가 효과</summary>
    public static string What(RecipeData r)
    {
        if (r == null) return "";
        string s;
        switch (r.shape)
        {
            case AttackShape.Pierce: s = "직선으로 뚫고 지나간다 - 줄지어 오는 손님을 한꺼번에"; break;
            case AttackShape.Cone: s = ConeWord(); break;
            case AttackShape.Explode: s = "적중한 곳에서 터진다" + (r.explodeRadius > 0f ? "(반경 " + r.explodeRadius.ToString("F0") + ")" : "") + " - 주변 손님도 같이 맞는다"; break;
            case AttackShape.Chain: s = "맞은 손님에서 " + Mathf.Max(1, r.chainCount) + "마리로 튄다 - 몰려 있을수록 좋다"; break;
            case AttackShape.Field: s = FieldWord(r); break;
            case AttackShape.Aura: s = AuraWord(r); break;
            case AttackShape.Passive: s = PassiveWord(r); break;
            default: s = "가까운 손님 하나를 노려 쏜다"; break;
        }
        if (!string.IsNullOrEmpty(r.buffType)) s = "쏘지 않는다. 가로·세로 이웃 포탑을 강화한다 (" + BuffWord(r) + ")";

        string extra = "";
        if (r.burnStack > 0) extra += ", 화상(불붙어 지속 피해)";
        if (r.poisonStack > 0 || r.fieldPoison) extra += ", 독(지속 피해)";
        if (r.slowLevel > 0) extra += ", 감속 " + (r.slowLevel >= 2 ? "70%" : "50%");
        if (r.stunSec > 0f) extra += ", " + r.stunSec.ToString("F1") + "초 마비";
        if (r.shredDef > 0) extra += ", 방어 깎기";
        if (r.shredRes > 0) extra += ", 저항 깎기";
        if (r.healOnHit > 0f) extra += ", 맞출 때마다 기차 HP +" + r.healOnHit.ToString("F0");
        if (extra.Length > 0) s += ". 덤으로" + extra.Substring(1);
        return s + ".";
    }

    /// <summary>어떤 손님에 잘 박히나 - 물리/속성(마법)</summary>
    public static string Against(RecipeData r)
    {
        if (r == null || r.shape == AttackShape.Passive || !string.IsNullOrEmpty(r.buffType)) return "";
        if (r.damage <= 0f) return "";
        return r.damageType == DamageType.Magic
            ? "속성 피해 - 두꺼운 손님(거북·아르마딜로·강철)에 잘 박힌다"
            : "물리 피해 - 날개 달린 손님(프테라·플라이)에 잘 박힌다";
    }

    /// <summary>언제 쓰나 - 역할 휴리스틱</summary>
    public static string When(RecipeData r)
    {
        if (r == null) return "";
        if (r.shape == AttackShape.Passive) return "칸 하나를 화력 대신 기차 자체에 쓰고 싶을 때";
        if (!string.IsNullOrEmpty(r.buffType)) return "이미 좋은 포탑 곁에 두어 더 세게 만들 때";
        if (r.role == TurretRole.Debuffer || r.shredDef > 0 || r.shredRes > 0)
            return "두꺼운 손님·보스 (보스가 무방비일 때 [F] 로 던지는 요리)";
        if (r.role == TurretRole.CC || r.slowLevel > 0 || r.stunSec > 0f) return "손님이 기차에 붙기 전에 늦추고 싶을 때";
        if (r.healOnHit > 0f || r.role == TurretRole.Support) return "기차가 자주 다칠 때 - 쏘면서 조금씩 고친다";
        switch (r.shape)
        {
            case AttackShape.Explode:
            case AttackShape.Cone:
            case AttackShape.Field:
            case AttackShape.Chain:
            case AttackShape.Aura:
                return "작은 손님이 무리로 올 때";
            case AttackShape.Pierce:
                return "손님이 한 줄로 몰려올 때";
            default:
                return r.damage >= 20f ? "두꺼운 손님 하나를 오래 때릴 때" : "기본 화력 - 어디에나";
        }
    }

    /// <summary>숫자 한 줄: 공격 / 발사 간격 / 초당. 패시브·버프는 ""</summary>
    public static string Numbers(RecipeData r, float levelMult)
    {
        if (r == null || r.damage <= 0f || r.cooldown <= 0f) return "";
        float dmg = r.damage * Mathf.Max(0.01f, levelMult);
        return "공격 " + dmg.ToString("F0") + "  " + r.cooldown.ToString("F1") + "초마다  (초당 " + (dmg / r.cooldown).ToString("F0") + ")";
    }

    /// <summary>재료·조리대 한 줄: "고기 + 고기 · 굽기(그릴)" / 전설 요리는 "전설 요리 - 기본 포탑 둘을 합쳐 진화"</summary>
    public static string Source(RecipeData r)
    {
        if (r == null) return "";
        if (r.tier >= 2 || string.IsNullOrEmpty(r.recipeId) || r.recipeId.IndexOf('+') < 0) return "전설 요리 - 기본 포탑 둘을 합쳐 진화";
        return MaterialNames.PairKor(r.recipeId) + " · " + MethodWord(r);
    }

    /// <summary>툴팁·도감 상세용 전체 (줄바꿈). 순서 = 무엇을 하나 / 어떤 손님에 / 언제 / 숫자. v1.3 (v9.14): 재료 줄은 뺐다 (도감 머리줄이 보여 준다) - 테스터 "길다"</summary>
    public static string Full(RecipeData r, float levelMult)
    {
        if (r == null) return "";
        string s = What(r);
        string ag = Against(r); if (ag.Length > 0) s += "\n" + ag;
        string wh = When(r); if (wh.Length > 0) s += "\n쓰는 때: " + wh;
        string num = Numbers(r, levelMult); if (num.Length > 0) s += "\n" + num;
        return s;
    }

    /// <summary>조리법 낱말 (KitchenPanel.MethodOf 와 같은 규칙 - 이름 키워드)</summary>
    public static string MethodWord(RecipeData r)
    {
        if (r == null) return "";
        string n = r.displayName ?? "";
        if (n.Contains("수프") || n.Contains("스튜") || n.Contains("탕")) return "끓이기(솥)";
        if (n.Contains("볶음")) return "볶기(팬)";
        if (n.Contains("육포") || n.Contains("구이") || n.Contains("립")) return "굽기(그릴)";
        return "조리대";
    }

    /// <summary>v1.6 (v9.35.2): 장판 한 줄 - 장판이 일하는 시간(GameBalance.FieldLingerSec) 동안 그 위 손님이 계속 느려진다 (독 장판은 독도). 0 이면 떨어질 때 한 번</summary>
    private static string FieldWord(RecipeData r)
    {
        string size = r.fieldBig ? "넓은" : "작은";
        if (GameBalance.FieldLingerSec <= 0f) return size + " 장판을 깐다 - 떨어진 자리의 손님에게 덤 효과";
        return size + " 장판을 깐다 - " + Mathf.RoundToInt(GameBalance.FieldLingerSec) + "초 동안 그 위를 지나는 손님이 계속 " + (r.fieldPoison && GameBalance.FieldPoisonOn ? "느려지고 독에 걸린다" : "느려진다");
    }

    /// <summary>v1.7 (v9.35.3): 부채꼴 한 줄 - 닿는 거리(GameBalance.ConeReach)를 쓴다. 조준이 그 안만 보면 "멀면 기다린다"</summary>
    private static string ConeWord()
    {
        string reach = Mathf.RoundToInt(GameBalance.ConeReach).ToString();
        if (GameBalance.ConeAimWithinReach) return "가까이 온 손님에게 부채꼴로 흩뿌린다 - 닿는 거리 " + reach + ", 멀면 기다린다";
        return "앞쪽 부채꼴로 흩뿌린다 - 닿는 거리 " + reach;
    }

    /// <summary>v1.7 (v9.35.3): 오라 한 줄 - 오라마다 하는 일 (TurretSlotManager.TickAuras 와 같은 말). 예전엔 셋 다 "가까이 온 손님이 계속 맞는다"</summary>
    private static string AuraWord(RecipeData r)
    {
        string where = GameBalance.AuraFromTrainBody ? "기차에서 " + Mathf.RoundToInt(GameBalance.AuraReach) + " 안에 들어온 손님" : "기차 가까이 온 손님";
        switch (r.passiveType ?? "")
        {
            case "auraBurn": return "쏘지 않는다. " + where + "이 계속 불탄다 (0.5초마다, 레벨이 오르면 세진다)";
            case "auraSlow": return "쏘지 않는다. " + where + "이 느려진다 (50%)";
            case "auraShred":
                if (!GameBalance.AuraShredReal) return "쏘지 않는다. " + where + "이 조금 느려진다";
                return "쏘지 않는다. " + where + "의 방어·저항이 깎인다"
                    + (GameBalance.CorrodeVulnBonus > 0f ? ". 부식된 손님은 포탑에 " + Mathf.RoundToInt(GameBalance.CorrodeVulnBonus * 100f) + "% 더 아프게 맞는다 (숫자가 연두색, 화상·독은 그대로)" : "");
            default: return "쏘지 않는다. " + where + "에게 덤 효과";
        }
    }

    /// <summary>v1.7: 오라 낱말 요약 (포탑 정보창) - "화상" / "감속 50%" / "방어·저항 깎기 + 부식 +20%"</summary>
    private static string AuraBrief(RecipeData r)
    {
        switch (r.passiveType ?? "")
        {
            case "auraBurn": return "화상";
            case "auraSlow": return "감속 50%";
            case "auraShred":
                if (!GameBalance.AuraShredReal) return "감속 15%";
                return "방어·저항 깎기" + (GameBalance.CorrodeVulnBonus > 0f ? " + 부식 +" + Mathf.RoundToInt(GameBalance.CorrodeVulnBonus * 100f) + "%" : "");
            default: return "덤 효과";
        }
    }

    /// <summary>RecipeDatabase 의 passiveType 키: regen / maxhp / dr / thorns / auraBurn / auraSlow / auraShred / omega</summary>
    private static string PassiveWord(RecipeData r)
    {
        switch (r.passiveType ?? "")
        {
            case "regen": return "쏘지 않는다. 기차 HP 가 조금씩 저절로 찬다";
            case "maxhp": return "쏘지 않는다. 기차 최대 HP 가 오른다";
            case "dr": return "쏘지 않는다. 기차가 받는 피해가 줄어든다";
            case "thorns": return "쏘지 않는다. 기차를 문 손님이 되받는다";
            case "auraBurn": return "쏘지 않는다. 기차 주변 손님이 계속 불탄다";
            case "auraSlow": return "쏘지 않는다. 기차 주변 손님이 느려진다";
            case "auraShred": return GameBalance.AuraShredReal ? "쏘지 않는다. 기차 주변 손님의 방어·저항이 깎인다" : "쏘지 않는다. 기차 주변 손님이 조금 느려진다";
            case "omega": return "쏘지 않는다. 기차 전체를 크게 강화한다";
            default: return "쏘지 않는다. 기차 자체를 강화한다";
        }
    }

    /// <summary>RecipeDatabase 의 buffType 키: pd(물리 공격) / md(속성 공격) / as(발사 속도)</summary>
    private static string BuffWord(RecipeData r)
    {
        switch (r.buffType ?? "")
        {
            case "pd": return "물리 공격력";
            case "md": return "속성 공격력";
            case "as": return "발사 속도";
            default: return r.buffType;
        }
    }
}

/// <summary>요리 이름/카드의 마우스 이벤트 중계 - onHover(스침) / onClick(클릭). v1.3: 도감은 onClick 만 쓴다 (스침으로 바뀌면 읽는 도중 사라진다)</summary>
public class RecipeHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public RecipeData recipe;
    public System.Action<RecipeData> onHover;
    public System.Action<RecipeData> onClick;
    public void OnPointerEnter(PointerEventData e) { if (onHover != null) onHover(recipe); }
    public void OnPointerClick(PointerEventData e) { if (onClick != null) onClick(recipe); else if (onHover != null) onHover(recipe); }
}
