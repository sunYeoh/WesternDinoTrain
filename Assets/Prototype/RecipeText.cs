using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [RecipeText.cs] v1.1 (v9.11.1 2026-09-22: 재료·조리대 줄(Source) 추가, 무방비 표현) / v1 (신규, v9.10 2026-09-17) - 요리(포탑) 설명을 일상어로 만드는 한 곳
///
/// 테스터 피드백: "도감에 요리를 눌렀을 때 뭔 요린지 모르니까 만들지 말지도 모르겠음", "포탑 효과를 읽을 시간이 없음",
/// "모르겠는 말(공명·인퓨징·DPS) 쓰지 말기". RecipeData 의 수치 필드(형태·속성·도트·감속·폭발·체인·회복·버프·패시브)를 그대로 읽어
/// "무엇을 하나 / 어떤 손님에 잘 박히나 / 언제 쓰나" 세 줄 + 숫자 한 줄을 만든다. 문구는 코드 필드에서만 나오므로 레시피를 고쳐도 같이 맞는다.
///
/// 사용법:
///   RecipeText.What(r)   - "가까운 손님 하나를 노려 쏜다. 화상(계속 피해)." (한 줄)
///   RecipeText.Against(r) - "물리 - 저항 높은 손님(날개 달린 것들)에 잘 박힌다" (한 줄)
///   RecipeText.When(r)   - "작은 손님이 무리로 올 때" (한 줄)
///   RecipeText.Numbers(r, levelMult) - "공격 26  1.0초마다  (초당 26)" (한 줄, 패시브면 "")
///   RecipeText.Full(r, levelMult)    - 위 넷을 줄바꿈으로 (툴팁·도감 상세용)
///   RecipeText.RoleWord(r) - "단일 화력 / 범위 / 제어 / 지원 / 약화 / 기차 강화" (짧은 역할 낱말 - 카드 한 줄용)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class RecipeText
{
    /// <summary>짧은 역할 낱말 (카드·이름표 한 줄용)</summary>
    public static string RoleWord(RecipeData r)
    {
        if (r == null) return "";
        if (r.shape == AttackShape.Passive) return "기차 강화";
        if (!string.IsNullOrEmpty(r.buffType)) return "곁 포탑 강화";
        if (r.role == TurretRole.CC || r.slowLevel > 0 || r.stunSec > 0f) return "제어";
        if (r.role == TurretRole.Debuffer || r.shredDef > 0 || r.shredRes > 0) return "약화";
        if (r.role == TurretRole.Support || r.healOnHit > 0f) return "지원";
        if (r.shape == AttackShape.Explode || r.shape == AttackShape.Cone || r.shape == AttackShape.Field
            || r.shape == AttackShape.Chain || r.shape == AttackShape.Pierce || r.shape == AttackShape.Aura) return "범위";
        return "단일 화력";
    }

    /// <summary>무엇을 하나 - 형태 + 부가 효과</summary>
    public static string What(RecipeData r)
    {
        if (r == null) return "";
        string s;
        switch (r.shape)
        {
            case AttackShape.Pierce: s = "직선으로 뚫고 지나간다 - 줄지어 오는 손님을 한꺼번에"; break;
            case AttackShape.Cone: s = "앞쪽 부채꼴로 흩뿌린다 - 붙어 오는 손님 여럿"; break;
            case AttackShape.Explode: s = "적중한 곳에서 터진다" + (r.explodeRadius > 0f ? "(반경 " + r.explodeRadius.ToString("F0") + ")" : "") + " - 주변 손님도 같이 맞는다"; break;
            case AttackShape.Chain: s = "맞은 손님에서 " + Mathf.Max(1, r.chainCount) + "마리로 튄다 - 몰려 있을수록 좋다"; break;
            case AttackShape.Field: s = (r.fieldBig ? "넓은" : "작은") + " 장판을 깐다 - 그 위를 지나는 손님이 계속 맞는다"; break;
            case AttackShape.Aura: s = "기차 주변 오라 - 가까이 온 손님이 계속 맞는다"; break;
            case AttackShape.Passive: s = PassiveWord(r); break;
            default: s = "가까운 손님 하나를 노려 쏜다"; break;
        }
        if (!string.IsNullOrEmpty(r.buffType)) s = "쏘지 않는다. 가로·세로 이웃 포탑을 강화한다 (" + BuffWord(r) + ")";

        string extra = "";
        if (r.burnStack > 0) extra += ", 화상(불붙어 계속 피해)";
        if (r.poisonStack > 0 || r.fieldPoison) extra += ", 독(계속 피해)";
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
            ? "속성(마법) 피해 - 방어가 두꺼운 손님(거북·아르마딜로·강철)에 잘 박힌다"
            : "물리 피해 - 저항이 높은 손님(날개 달린 것들)에 잘 박힌다";
    }

    /// <summary>언제 쓰나 - 역할 휴리스틱</summary>
    public static string When(RecipeData r)
    {
        if (r == null) return "";
        if (r.shape == AttackShape.Passive) return "슬롯 하나를 화력 대신 기차 자체에 쓰고 싶을 때";
        if (!string.IsNullOrEmpty(r.buffType)) return "이미 좋은 포탑 곁에 두어 더 세게 만들 때";
        if (r.role == TurretRole.Debuffer || r.shredDef > 0 || r.shredRes > 0)
            return "두꺼운 손님·보스 - 보스가 무방비(그로기)일 때 [F] 로 던지는 요리이기도 하다";
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

    /// <summary>툴팁·도감 상세용 전체 (줄바꿈). 순서 = 무엇을 하나 / 어떤 손님에 / 언제 / 재료·조리대 / 숫자</summary>
    public static string Full(RecipeData r, float levelMult)
    {
        if (r == null) return "";
        string s = What(r);
        string ag = Against(r); if (ag.Length > 0) s += "\n" + ag;
        string wh = When(r); if (wh.Length > 0) s += "\n쓰는 때: " + wh;
        string so = Source(r); if (so.Length > 0) s += "\n재료: " + so;
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
            case "auraShred": return "쏘지 않는다. 기차 주변 손님의 방어가 깎인다";
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

/// <summary>요리 이름/카드 위에 마우스가 오면 onHover(레시피) - 로비 도감 등 어디서나 붙여 쓴다</summary>
public class RecipeHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public RecipeData recipe;
    public System.Action<RecipeData> onHover;
    public void OnPointerEnter(PointerEventData e) { if (onHover != null) onHover(recipe); }
    public void OnPointerClick(PointerEventData e) { if (onHover != null) onHover(recipe); }
}
