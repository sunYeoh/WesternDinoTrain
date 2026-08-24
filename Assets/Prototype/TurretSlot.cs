using UnityEngine;

/// <summary>
/// [TurretSlot.cs] v3 (v3: 보스 낙뢰 패턴용 슬롯 마비 추가)
/// 포탑 슬롯 1개. 요리를 투입하면 포탑으로 가동한다.
/// - 같은 요리 반복 투입 -> 레벨업 (Lv1=C, 2=B, 3~4=A, 5+=S)
/// - 발사형이면 쿨다운마다 가장 가까운 적 공격
/// - 패시브/버프/오라는 TurretSlotManager가 일괄 처리
/// - v2 변경점: 증강 연동 (공격속도 AspdMul / 사거리 RangeMul)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TurretSlot : MonoBehaviour
{
    [Header("─ 슬롯 상태 (런타임) ─")]
    public string recipeId = "";   // 투입된 요리 키 ("" = 빈 슬롯)
    public int level = 0;          // 현재 레벨
    public bool isLocked = false;  // 잠금 슬롯 (증강 '증축된 주방 칸'으로 해금)

    [Header("─ 발사 설정 ─")]
    public float targetRange = 15f;    // 타겟 탐색 사거리
    public Transform firePoint;        // 발사 위치 (없으면 자기 위치)

    private float cooldownTimer = 0f;

    // ── v3: 슬롯 마비 (보스 '낙뢰 폭격' 패턴) ──
    // 마비 중에는 발사 정지. 마커 클릭 한 번으로 즉시 해제(감전 털어내기)
    private float stunUntil = 0f;

    public bool IsStunned { get { return Time.time < stunUntil; } }

    /// <summary>슬롯 마비 (보스 패턴이 호출)</summary>
    public void StunSlot(float seconds) { stunUntil = Time.time + seconds; }

    /// <summary>마비 즉시 해제 (마커 클릭 재가동)</summary>
    public void ClearStun() { stunUntil = 0f; }

    // 현재 투입된 레시피 데이터 (없으면 null)
    public RecipeData Recipe
    {
        get { return string.IsNullOrEmpty(recipeId) ? null : RecipeDatabase.Get(recipeId); }
    }

    public bool IsEmpty { get { return string.IsNullOrEmpty(recipeId); } }

    // 레벨 배율: 1 + 0.6 * (Lv-1)  (프로토타입 v3 검증값)
    public float LevelMult
    {
        get { return level <= 0 ? 1f : 1f + 0.6f * (level - 1); }
    }

    // 등급 문자열 (UI용)
    public string GradeName
    {
        get
        {
            if (level >= 5) return "S";
            if (level >= 3) return "A";
            if (level >= 2) return "B";
            return "C";
        }
    }

    /// <summary>요리 투입 시도. 성공하면 true</summary>
    public bool TryInsertFood(string id)
    {
        // 잠금 슬롯에는 투입 불가
        if (isLocked)
        {
            Debug.Log("[TurretSlot] 잠긴 슬롯 - 증강 '증축된 주방 칸'으로 해금 필요");
            return false;
        }

        // 빈 슬롯이거나 같은 요리만 가능
        if (!IsEmpty && recipeId != id) return false;

        RecipeData r = RecipeDatabase.Get(id);
        if (r == null) return false;

        recipeId = id;
        level += 1;

        // 최대HP형 패시브는 즉시 기차에 적용
        if (r.passiveType == "maxhp" || r.passiveType == "omega")
        {
            TrainManager tm = FindFirstObjectByType<TrainManager>();
            if (tm != null) tm.AddMaxHP(r.passiveType == "omega" ? 120f : r.passiveValue);
        }

        Debug.Log("[TurretSlot] " + r.displayName + " 투입! " + GradeName + "등급 Lv" + level);
        return true;
    }

    /// <summary>슬롯 비우기 (합체 재료로 소모 - 환급 없음)</summary>
    public void ClearSlot()
    {
        recipeId = "";
        level = 0;
        cooldownTimer = 0f;
    }

    /// <summary>포탑 직접 설정 (합체 진화 결과용). 최대HP형 패시브는 1회 적용</summary>
    public void SetTurret(string id, int newLevel)
    {
        RecipeData r = RecipeDatabase.Get(id);
        if (r == null) return;

        recipeId = id;
        level = Mathf.Max(1, newLevel);
        cooldownTimer = 0f;

        if (r.passiveType == "maxhp" || r.passiveType == "omega")
        {
            TrainManager tm = FindFirstObjectByType<TrainManager>();
            if (tm != null) tm.AddMaxHP(r.passiveType == "omega" ? 120f : r.passiveValue);
        }

        Debug.Log("[TurretSlot] 합체 결과: " + r.displayName + " " + GradeName + "등급 Lv" + level);
    }

    /// <summary>슬롯 비우기 (폐기). 반환값: 환급 재료 수</summary>
    public int Scrap()
    {
        if (IsEmpty) return 0;
        int refund = Mathf.Max(1, level);
        Debug.Log("[TurretSlot] " + Recipe.displayName + " 폐기, 재료 " + refund + "개 환급");
        recipeId = "";
        level = 0;
        cooldownTimer = 0f;
        return refund;
    }

    /// <summary>매 프레임 발사 처리 (TurretSlotManager가 호출)</summary>
    public void TickFire(float deltaTime, float buffAttackSpeed, float buffDamage)
    {
        if (isLocked) return;
        if (IsStunned) return;   // v3: 낙뢰 마비 중 발사 정지
        RecipeData r = Recipe;
        if (r == null) return;
        if (r.shape == AttackShape.Passive || r.shape == AttackShape.Aura) return;
        if (!string.IsNullOrEmpty(r.buffType)) return; // 버프형은 발사 안 함

        cooldownTimer -= deltaTime;
        if (cooldownTimer > 0f) return;

        // 가장 가까운 적 탐색
        Enemy target = FindNearestEnemy();
        if (target == null) return;

        // 쿨다운 리셋 (인접 버프 + 증강 공속 반영)
        cooldownTimer = r.cooldown / ((1f + buffAttackSpeed) * AugmentManager.AspdMul);

        // 최종 데미지 = 기본 x 레벨배율 x (1+버프)
        // (전역 배율/증강 데미지는 TurretAttackExecutor.DealDamage에서 적용)
        float finalDamage = r.damage * LevelMult * (1f + buffDamage);

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        TurretAttackExecutor.Execute(r, origin, target, finalDamage);
    }

    private Enemy FindNearestEnemy()
    {
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy best = null;
        // 증강 사거리 배율 반영
        float bestDist = targetRange * AugmentManager.RangeMul;
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            float d = Vector3.Distance(transform.position, all[i].transform.position);
            if (d < bestDist) { bestDist = d; best = all[i]; }
        }
        return best;
    }
}
