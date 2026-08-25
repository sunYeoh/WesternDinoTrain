using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [TurretAttackExecutor.cs] v5
/// 포탑 공격 형태(8종)별 판정 및 이펙트 실행기
/// - v3: 모든 TakeDamage에 r.damageType 적용 (DEF/RES 계산)
/// - v4: 증강 시스템(AugmentManager) 연동
///     * 데미지 파이프라인: 전체 배율 / 도박사 탄환 / 치명타 / 조건부 보너스(도트, 제어)
///     * 프리즘 변형: 투사체->관통 / 투사체->산탄 / 이중 폭발 / 증폭 전이 / 번개 계승
///     * 타격 부가: 흡혈, 정전기 축적(N타 감전), 붉은 주방(전타격 화상)
/// - v5: 증강 추가분 반영
///     * 과열 기관(연속 사격 램핑) / 동상 파편(CC 적 타격 시 서리 폭발) / 개전 포격(웨이브 초반 2배)
///     * 폭발 전문가(스플래시 감쇄 제거) / 2연장 개조(확률로 한 발 더)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class TurretAttackExecutor
{
    // 정전기 축적용: 레시피별 타격 카운터
    private static Dictionary<RecipeData, int> staticHitCounter = new Dictionary<RecipeData, int>();

    // 과열 기관용: 레시피별 램핑 상태 (연속 사격 스택)
    private class RampState { public int stacks; public float lastTime; }
    private static Dictionary<RecipeData, RampState> rampStates = new Dictionary<RecipeData, RampState>();

    // 번개 계승 재귀 방지 (연쇄로 발생한 타격이 또 연쇄를 부르는 것 차단)
    private static bool inSubAttack = false;

    // 기차 참조 캐시 (흡혈용, 매 타격 Find 방지)
    private static TrainManager cachedTrain;

    /// <summary>요리 속성 태그별 이펙트 색상</summary>
    public static Color TagColor(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Phys: return new Color(1f, 0.55f, 0.35f);
            case FoodTag.Elec: return new Color(1f, 0.91f, 0.42f);
            case FoodTag.Fire: return new Color(1f, 0.29f, 0.16f);
            case FoodTag.Ice: return new Color(0.48f, 0.85f, 0.91f);
            case FoodTag.Poison: return new Color(0.68f, 0.45f, 0.91f);
            default: return new Color(0.55f, 0.75f, 0.45f);
        }
    }

    public static void Execute(RecipeData r, Vector3 origin, Enemy target, float damage)
    {
        Color col = TagColor(r.tag);
        AttackVFX vfx = AttackVFX.Instance;

        // ---- 프리즘 증강: 공격 형태 변환 ----
        AttackShape shape = r.shape;
        if (shape == AttackShape.Projectile)
        {
            if (AugmentManager.PierceConversion)
            {
                shape = AttackShape.Pierce;            // 열차포 개조: 투사체 -> 관통 레일
            }
            else if (AugmentManager.ConeConversion)
            {
                shape = AttackShape.Cone;              // 산탄 셰프: 투사체 -> 부채꼴 (데미지 -25%)
                damage *= 0.75f;
            }
        }

        switch (shape)
        {
            case AttackShape.Projectile:
            case AttackShape.Explode:
            case AttackShape.Field:
                if (vfx != null)
                {
                    // 투사체가 날아가서 도달하면 판정 (폭발형 크게, 일반형 작게)
                    Vector3 targetPos = target.transform.position;
                    Enemy capturedTarget = target;
                    float projSpeed = r.projectileSpeed > 0f ? r.projectileSpeed * 0.03f : 13f;
                    float projSize = r.explodeRadius > 0f ? 0.5f : 0.32f;

                    vfx.Projectile(origin, targetPos, col, projSpeed, projSize, delegate
                    {
                        // 도달 시점 판정
                        if (capturedTarget != null && capturedTarget.IsAlive)
                            HitSingle(r, capturedTarget, damage);

                        if (r.shape == AttackShape.Explode)
                        {
                            float radius = r.explodeRadius * 0.06f * AugmentManager.ExplodeRadiusMul;
                            vfx.Explosion(targetPos, col, radius);
                            // P1 게임필: 폭발 미세 럼블 - 쿨타임 채널 방식 (연사돼도 2.5초에 1번만)
                            GameFeel.Shake(GameBalance.ShakeExplosion, "explosion", GameBalance.ShakeExplosionCooldown);
                            HitExplosionArea(r, targetPos, damage, capturedTarget, radius);

                            // 프리즘 증강 '메아리치는 폭발': 60% 데미지로 한 번 더 (중심 대상 포함)
                            if (AugmentManager.DoubleExplosion)
                            {
                                float radius2 = radius * 1.2f;
                                vfx.Explosion(targetPos, col, radius2);
                                HitExplosionArea(r, targetPos, damage * 0.6f, null, radius2);
                            }
                        }
                        if (r.shape == AttackShape.Field)
                        {
                            float radius = (r.fieldBig ? 130f : 90f) * 0.06f;
                            vfx.Field(targetPos, col, radius, 4f);
                            HitFieldArea(r, targetPos, radius);
                        }
                    });
                }
                else
                {
                    // VFX 없으면 즉시 판정 (폴백)
                    HitSingle(r, target, damage);
                }
                break;

            case AttackShape.Pierce:
                HitPierce(r, origin, target, damage, col);
                break;

            case AttackShape.Cone:
                HitCone(r, origin, target, damage, col);
                break;

            case AttackShape.Chain:
                HitChain(r, target, damage, col, origin);
                break;
        }
    }

    // ==================================================================
    //  데미지 파이프라인 (모든 타격은 반드시 DealDamage를 거친다)
    // ==================================================================

    /// <summary>
    /// 증강 배율을 전부 적용해 실제 데미지를 넣고, 타격 부가효과를 처리한다.
    /// mul : 스플래시 0.8 같은 형태별 감쇄 계수
    /// </summary>
    private static void DealDamage(RecipeData r, Enemy en, float damage, float mul)
    {
        if (en == null || !en.IsAlive) return;

        // 동상 파편 판정용: 타격 전 CC 상태 기억
        bool wasControlled = AugmentHooks.IsControlled(en);

        // 전역 밸런스 배율 + 증강 배율
        float finalDamage = damage * mul * GameBalance.TurretDamageMul * AugmentManager.AtkMul;

        // P1+: 요리 숙련 - 평생 조리 횟수 티어에 따른 그 레시피 포탑 공격력 보너스 (영구)
        // Phase 2-2 증강 '단골 장부': 숙련 보너스 증폭 (MasteryAmp)
        float masteryAtk = MetaProgress.GetMasteryAtk(r.recipeId) * AugmentManager.MasteryAmp;
        if (masteryAtk > 0f)
            finalDamage *= 1f + masteryAtk;

        // 개전 포격: 웨이브 시작 8초간 데미지 2배
        if (AugmentManager.OpeningBarrage && Time.time - AugmentManager.WaveStartTime <= 8f)
            finalDamage *= 2f;

        // 과열 기관: 연속 사격 스택 (타격당 +5%, 최대 15스택 = +75%, 2.5초 쉬면 초기화)
        if (AugmentManager.RampAttack)
        {
            RampState rs;
            if (!rampStates.TryGetValue(r, out rs))
            {
                rs = new RampState();
                rampStates[r] = rs;
            }
            if (Time.time - rs.lastTime > 2.5f) rs.stacks = 0;
            rs.stacks = Mathf.Min(15, rs.stacks + 1);
            rs.lastTime = Time.time;
            finalDamage *= 1f + 0.05f * rs.stacks;
        }

        // 원시 화력: 상태이상 포기 대가로 순수 데미지 +80%
        if (AugmentManager.PrimalPower)
            finalDamage *= 1.8f;

        // ── Phase 2-3 신규 증강 배율 ──

        // 선대의 기본기: T1 포탑 강화 (T2 진화 봉인의 대가)
        if (AugmentManager.BasicsDoctrine && r.tier == 1)
            finalDamage *= 1f + GameBalance.BasicsT1Bonus;

        // 강철의 심장: 기차 최대 HP 100당 데미지 증가 (전체 상한 +100%)
        if (AugmentManager.SteelHeart && TrainManager.Instance != null)
            finalDamage *= 1f + Mathf.Min(1f,
                TrainManager.Instance.currentMaxHP / 100f * GameBalance.SteelHeartPer100);

        // 주방장은 하나다: 최고 레벨 포탑에 몰아주기 (처치 누적은 아래 처치 처리에서 오른다)
        string chefId = "";
        if (AugmentManager.OneChef && TurretSlotManager.Instance != null)
        {
            chefId = TurretSlotManager.Instance.GetChefRecipeId();
            if (chefId == r.recipeId && chefId != "")
                finalDamage *= 1f + GameBalance.OneChefBonus
                    + GameBalance.OneChefPerKill * AugmentManager.OneChefKillStacks;
            else if (chefId != "")
                finalDamage *= 1f - GameBalance.OneChefOthersPenalty;
        }

        // 골동품 감정가: 보유 아이템 1개당 데미지 증가 (동적 계산)
        finalDamage *= AugmentManager.CollectorMul;

        // 도박사의 성배: [도박] 증강 1개당 +10% (동적 계산)
        finalDamage *= AugmentManager.ChaliceMul;

        // 도박사 스피노의 탄환: 확률로 2배 / 절반 (도박 증강 수만큼 2배 확률 상승)
        if (AugmentManager.GamblerBullet)
            finalDamage *= (Random.value < AugmentManager.GamblerWinChance) ? 2f : 0.5f;

        // 치명타: 기본 1.5배 + 치피 가산
        if (AugmentManager.CritChanceAdd > 0f && Random.value < AugmentManager.CritChanceAdd)
            finalDamage *= 1.5f + AugmentManager.CritDamageAdd;

        // 약점 파고들기: 도트 걸린 적에게 추가 데미지
        if (AugmentManager.DotTargetBonus > 0f && AugmentHooks.HasDotTracked(en))
            finalDamage *= 1f + AugmentManager.DotTargetBonus;

        // 사냥꾼의 본능: 슬로우/스턴 적에게 추가 데미지
        if (AugmentManager.ControlTargetBonus > 0f && AugmentHooks.IsControlled(en))
            finalDamage *= 1f + AugmentManager.ControlTargetBonus;

        float hpBefore = en.currentHP;   // Phase 2-3: 초과 데미지(옆 테이블 계산서) 판정용

        en.TakeDamage(finalDamage, r.damageType);

        // ── Phase 2-3: 처치 시 효과 (주방장 누적 / 마지막 서비스 / 옆 테이블 계산서) ──
        if (!en.IsAlive)
        {
            // 주방장은 하나다: 주방장 포탑이 처치할 때마다 데미지 누적 (런 한정)
            if (AugmentManager.OneChef && chefId != "" && chefId == r.recipeId)
                AugmentManager.OneChefKillStacks = Mathf.Min(
                    GameBalance.OneChefMaxStacks, AugmentManager.OneChefKillStacks + 1);

            // 마지막 서비스: 쓰러진 손님이 터진다
            // (직접 데미지 - 파이프라인 재적용/연쇄 폭발 없음, 동상 파편과 같은 방식)
            if (AugmentManager.CorpseService && !inSubAttack)
            {
                inSubAttack = true;
                CorpseBurst(r, en, finalDamage * GameBalance.CorpseServiceRatio);
                inSubAttack = false;
            }

            // 옆 테이블 계산서: 처치하고 남은 초과 데미지를 가장 가까운 적에게 청구
            // (적 방어 보정 전 수치 기준의 근사치 - 골드 증강다운 손맛 우선)
            if (AugmentManager.OverkillCarry && !inSubAttack)
            {
                float overkill = finalDamage - hpBefore;
                if (overkill > 1f)
                {
                    Enemy next = FindNearestOther(en, GameBalance.OverkillCarryRange);
                    if (next != null)
                    {
                        inSubAttack = true;
                        next.TakeDamage(overkill, r.damageType);
                        inSubAttack = false;
                    }
                }
            }
        }

        // 흡혈: 타격당 기차 회복
        if (AugmentManager.LifestealPerHit > 0f)
        {
            if (cachedTrain == null) cachedTrain = Object.FindFirstObjectByType<TrainManager>();
            if (cachedTrain != null) cachedTrain.Heal(AugmentManager.LifestealPerHit);
        }

        // 붉은 주방: 모든 타격이 화상 1스택 (원시 화력과는 양립 불가 - 선택 단계에서 차단됨)
        if (AugmentManager.RedKitchen && !AugmentManager.PrimalPower)
        {
            en.ApplyBurn(1, AugmentManager.DotMul);
            AugmentHooks.RegisterDot(en, 3f);
        }

        // 정전기 축적: 이 포탑(레시피)의 N번째 타격마다 감전
        if (AugmentManager.StaticNth > 0)
        {
            int count;
            staticHitCounter.TryGetValue(r, out count);
            count++;
            if (count >= AugmentManager.StaticNth)
            {
                count = 0;
                en.ApplyStun(0.4f);
                AugmentHooks.RegisterControl(en, 0.4f);
                if (AttackVFX.Instance != null)
                    AttackVFX.Instance.Lightning(en.transform.position + Vector3.up * 1.2f,
                        en.transform.position, TagColor(FoodTag.Elec));
            }
            staticHitCounter[r] = count;
        }

        // 번개 계승: 확률로 소형 연쇄 번개 (재귀 방지 가드)
        if (AugmentManager.ChainProcChance > 0f && !inSubAttack
            && Random.value < AugmentManager.ChainProcChance)
        {
            inSubAttack = true;
            MiniChain(r, en, finalDamage * 0.6f);
            inSubAttack = false;
        }

        // 동상 파편: CC 상태의 적 타격 시 30% 확률 서리 폭발 (주변 50% 데미지)
        if (AugmentManager.FrostShatter && wasControlled && !inSubAttack && Random.value < 0.3f)
        {
            inSubAttack = true;
            FrostShatterBurst(r, en, finalDamage * 0.5f);
            inSubAttack = false;
        }
    }

    /// <summary>동상 파편: 대상 주변 소범위 서리 폭발 (파이프라인 재적용 없이 직접 데미지)</summary>
    private static void FrostShatterBurst(RecipeData r, Enemy center, float damage)
    {
        if (center == null) return;
        Vector3 pos = center.transform.position;
        float radius = 1.8f;

        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Explosion(pos, TagColor(FoodTag.Ice), radius);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == center || !all[i].IsAlive) continue;
            if (Vector3.Distance(all[i].transform.position, pos) <= radius)
                all[i].TakeDamage(damage, r.damageType);
        }
    }

    /// <summary>
    /// Phase 2-3 '마지막 서비스': 처치한 적 위치에서 폭발 - 주변 적에게 직접 데미지.
    /// (동상 파편과 같은 방식: 파이프라인 재적용 없음 -> 연쇄 폭발 없음)
    /// </summary>
    private static void CorpseBurst(RecipeData r, Enemy center, float damage)
    {
        if (center == null) return;
        Vector3 pos = center.transform.position;
        float radius = GameBalance.CorpseServiceRadius;

        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Explosion(pos, new Color(1f, 0.75f, 0.35f), radius);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == center || !all[i].IsAlive) continue;
            if (Vector3.Distance(all[i].transform.position, pos) <= radius)
                all[i].TakeDamage(damage, r.damageType);
        }
    }

    /// <summary>Phase 2-3 '옆 테이블 계산서': 기준 적에서 가장 가까운 다른 생존 적</summary>
    private static Enemy FindNearestOther(Enemy from, float range)
    {
        if (from == null) return null;
        Vector3 pos = from.transform.position;
        Enemy best = null;
        float bestDist = range;

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == from || !all[i].IsAlive) continue;
            float d = Vector3.Distance(all[i].transform.position, pos);
            if (d < bestDist) { bestDist = d; best = all[i]; }
        }
        return best;
    }

    /// <summary>번개 계승용 소형 연쇄: 근처 적 2체로 전이</summary>
    private static void MiniChain(RecipeData r, Enemy from, float damage)
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy current = from;
        HashSet<Enemy> hit = new HashSet<Enemy>();
        hit.Add(from);
        Color col = TagColor(FoodTag.Elec);

        for (int jump = 0; jump < 2; jump++)
        {
            Enemy next = null;
            float bestDist = 5f;
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].IsAlive || hit.Contains(all[i])) continue;
                float d = Vector3.Distance(current.transform.position, all[i].transform.position);
                if (d < bestDist) { bestDist = d; next = all[i]; }
            }
            if (next == null) break;

            if (AttackVFX.Instance != null)
                AttackVFX.Instance.Lightning(current.transform.position, next.transform.position, col);

            DealDamage(r, next, damage, 1f);
            hit.Add(next);
            current = next;
        }
    }

    // ==================================================================
    //  형태별 판정
    // ==================================================================

    /// <summary>단일 대상 피격 처리</summary>
    private static void HitSingle(RecipeData r, Enemy en, float damage)
    {
        if (en == null || !en.IsAlive) return;
        DealDamage(r, en, damage, 1f);

        // 2연장 개조: 25% 확률로 즉시 한 발 더 (50% 데미지, 재귀 방지)
        if (AugmentManager.DoubleTapChance > 0f && !inSubAttack
            && Random.value < AugmentManager.DoubleTapChance && en.IsAlive)
        {
            inSubAttack = true;
            DealDamage(r, en, damage, 0.5f);
            inSubAttack = false;
        }

        ApplyEffects(r, en);
    }

    /// <summary>폭발 범위 피격 (exclude 대상 제외, 스플래시 80%)</summary>
    private static void HitExplosionArea(RecipeData r, Vector3 center, float damage, Enemy exclude, float radius)
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == exclude || !all[i].IsAlive) continue;
            if (Vector3.Distance(all[i].transform.position, center) <= radius)
            {
                // 폭발 전문가 증강: 스플래시 감쇄 제거 (0.8 -> 1.0)
                DealDamage(r, all[i], damage, AugmentManager.FullSplash ? 1f : 0.8f);
                ApplyEffects(r, all[i]);
            }
        }
    }

    /// <summary>장판 범위: 데미지 없이 상태이상 효과만 부여</summary>
    private static void HitFieldArea(RecipeData r, Vector3 center, float radius)
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            if (Vector3.Distance(all[i].transform.position, center) <= radius)
                ApplyEffects(r, all[i]);
        }
    }

    /// <summary>관통(레일) 공격: 직선 경로상의 적 전부 풀 데미지</summary>
    private static void HitPierce(RecipeData r, Vector3 origin, Enemy target, float damage, Color col)
    {
        Vector3 dir = (target.transform.position - origin).normalized;
        float railLength = 20f;
        float railWidth = 1.2f;

        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Beam(origin, origin + dir * railLength, col, 0.35f);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            Vector3 toEnemy = all[i].transform.position - origin;
            float proj = Vector3.Dot(toEnemy, dir);
            if (proj < 0f || proj > railLength) continue;
            float perp = (toEnemy - dir * proj).magnitude;
            if (perp <= railWidth)
            {
                DealDamage(r, all[i], damage, 1f);
                ApplyEffects(r, all[i]);
            }
        }
    }

    /// <summary>부채꼴(화염방사/산탄) 공격: 각도 내 적 전부 풀 데미지</summary>
    private static void HitCone(RecipeData r, Vector3 origin, Enemy target, float damage, Color col)
    {
        Vector3 dir = (target.transform.position - origin).normalized;
        float coneRange = 7f;
        float coneHalfAngle = 26f;

        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Cone(origin, dir, coneRange, coneHalfAngle, col);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            Vector3 toEnemy = all[i].transform.position - origin;
            if (toEnemy.magnitude > coneRange) continue;
            if (Vector3.Angle(dir, toEnemy) <= coneHalfAngle)
            {
                DealDamage(r, all[i], damage, 1f);
                ApplyEffects(r, all[i]);
            }
        }
    }

    /// <summary>연쇄 번개: 인접 적으로 전이. 기본 80%, 증폭 전이 증강 시 튕길수록 강해짐</summary>
    private static void HitChain(RecipeData r, Enemy first, float damage, Color col, Vector3 origin)
    {
        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Lightning(origin, first.transform.position, col);

        HitSingle(r, first, damage);

        Enemy current = first;
        HashSet<Enemy> hit = new HashSet<Enemy>();
        hit.Add(first);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        float chainRange = 5f;

        // 증강: 전이 횟수 가산
        int totalJumps = r.chainCount + AugmentManager.ChainCountAdd;

        for (int c = 0; c < totalJumps; c++)
        {
            Enemy next = null;
            float bestDist = chainRange;
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].IsAlive || hit.Contains(all[i])) continue;
                float d = Vector3.Distance(current.transform.position, all[i].transform.position);
                if (d < bestDist) { bestDist = d; next = all[i]; }
            }
            if (next == null) break;

            if (AttackVFX.Instance != null)
                AttackVFX.Instance.Lightning(current.transform.position, next.transform.position, col);

            // 증폭 전이: 튕길수록 +20% (기본은 80% 감쇄), 최대 2.2배
            float jumpMul = AugmentManager.ChainAmplify
                ? Mathf.Min(2.2f, 0.8f + 0.2f * (c + 1))
                : 0.8f;

            DealDamage(r, next, damage, jumpMul);
            ApplyEffects(r, next);
            hit.Add(next);
            current = next;
        }
    }

    // ==================================================================
    //  상태이상 부여
    // ==================================================================

    /// <summary>부가 효과 적용: 슬로우/스턴/도트/방깎마깎/흡혈. 증강 훅 기록 포함</summary>
    private static void ApplyEffects(RecipeData r, Enemy en)
    {
        if (en == null || !en.IsAlive) return;

        // 원시 화력: 모든 상태이상 포기 (타격 회복만 유지)
        if (AugmentManager.PrimalPower)
        {
            if (r.healOnHit > 0f)
            {
                TrainManager tmp = Object.FindFirstObjectByType<TrainManager>();
                if (tmp != null) tmp.Heal(r.healOnHit);
            }
            return;
        }

        // 슬로우 -> 얼음 심장 증강이 있으면 빙결(스턴)로 변환
        if (r.slowLevel >= 1)
        {
            if (AugmentManager.IceHeart)
            {
                en.ApplyStun(0.8f);
                AugmentHooks.RegisterControl(en, 0.8f);
            }
            else if (r.slowLevel >= 2)
            {
                en.ApplySpeedDebuff(0.3f, 3f);
                AugmentHooks.RegisterControl(en, 3f);
            }
            else
            {
                en.ApplySpeedDebuff(0.5f, 2f);
                AugmentHooks.RegisterControl(en, 2f);
            }
        }
        if (r.stunSec > 0f)
        {
            en.ApplyStun(r.stunSec);
            AugmentHooks.RegisterControl(en, r.stunSec);
        }

        // 도트 (화상 3초 / 중독 5초) - 증강 배율 적용 + 장부 기록
        if (r.burnStack > 0)
        {
            en.ApplyBurn(r.burnStack, AugmentManager.DotMul);
            AugmentHooks.RegisterDot(en, 3f);
        }
        if (r.poisonStack > 0)
        {
            en.ApplyPoison(r.poisonStack, AugmentManager.DotMul);
            AugmentHooks.RegisterDot(en, 5f);
        }

        // 방어력 깎기 / 마법저항 깎기 - 증강 가산 적용
        if (r.shredDef > 0) en.ApplyShredDef(r.shredDef + AugmentManager.ShredAdd);
        if (r.shredRes > 0) en.ApplyShredRes(r.shredRes + AugmentManager.ShredAdd);

        // 타격 시 기차 회복 (요리 고유 효과)
        if (r.healOnHit > 0f)
        {
            TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
            if (tm != null) tm.Heal(r.healOnHit);
        }
    }
}
