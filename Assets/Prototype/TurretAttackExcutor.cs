using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [TurretAttackExecutor.cs] v5.12 (v9.25 2026-10-16: DealDamage 에 싸움 유물 배율 ItemManager.RelicDamageMul (굶주린 화덕·짝꿍 숟가락·빈 접시) + 단골 주문 버프 OrderSystem.DamageMulFor (그 계열 +40%) / 폭발 반경 x ItemManager.ExplodeRadiusMul (무거운 냄비) / 화상·독 x ItemManager.DotMul (독한 소스)) / v5.11 (v9.24.1 2026-10-16 5차 피드백 5번 "탄환 증발": 탄이 손님을 따라간다 (GameBalance.ProjectileHoming - 예전엔 쏜 순간의 자리로 날아가 손님이 움직이면 빈자리에 떨어졌다). 날아가는 동안 표적이 죽으면 직격 탄은 탄 자리에서 가장 가까운 손님(ProjectileRetargetRadius 4u)으로 방향을 틀고, 폭발·장판은 마지막 자리에 떨어진다. 떨어진 자리의 판정은 LandShot 하나로) / v5.10 (v9.23 2026-10-14 보스 새 패턴: 탄·관통·부채꼴이 손님의 AimPoint 를 겨눈다 - 디 오리지널은 페이즈마다 약점 칸(머리 / 맨 뒤 화물칸 / 심장 칸)으로 탄이 모인다) / v5.9 (v9.21 2026-10-12 보스 1단계: 범위 판정(폭발·장판·동상 파편·마지막 서비스·대체 표적)의 거리를 Enemy.DistanceFrom 으로 - 큰 보스는 몸 가장자리까지 재서 9 ~ 20u 몸의 끝에 떨어진 폭발이 비껴가지 않는다) / v5.8 (v9.19.3 2026-10-07: 증강의 타격당 회복을 1초에 GameBalance.LifestealHitsPerSec 번까지만 센다 - LifestealReady. 타격마다 전부 세면 손님이 늘고 단단해진 만큼 회복이 불어나 기차 HP 가 닳지 않았다) / v5.7 (v9.19.2 2026-10-07: v5.6 의 "타격당 회복 x 손님의 몫"을 되돌렸다 - 손님이 제 힘으로 오니 때린 만큼 그대로 회복한다) / v5.6 (v9.19.1: 증강의 타격당 회복에 맞은 손님의 몫을 곱했다) / v5.5 (v9.18 2026-10-06: 피해 출처(CurrentSource = 쏜 포탑) - 데미지 숫자를 포탑별로 합산하게 HitFeel 에 같이 넘긴다. 날아가는 탄은 도착할 때 출처를 다시 건다) / v5.4 (v9.17 2026-10-06: v9.14 머리말에 적고 본문에 빠져 있던 것을 실제로 넣음 - 투사체가 날아가는 동안 Enemy.IncomingDamage 예약·도착 때 해제 / 도착했을 때 표적이 죽었으면 ProjectileRetargetRadius 안의 다른 손님을 맞힌다. + A5: 물리 단발이 맞으면 HitFeel.Knock(그림만 움찔)) / v5.3 (v9.16 2026-09-29 손맛 2차 - 소리: 발사음 = 요리 속성·티어·모양별(SoundKeys.Shot, 포탑 위치에서 PlayAt) / 폭발 착탄 sfx_explosion / 장판 sfx_field / 연쇄 번개 튈 때마다 sfx_chain / 증강 폭발(동상 파편·마지막 서비스)도 폭발음 / HitFeel.NextHit 에 요리 속성을 같이 넘겨 명중음에 속성 겹침이 얹힌다) / v5.2 (v9.14 2026-09-28 테스터 "하나 점사해서 잡으면 나머지가 다 빗나감": 투사체가 도착했을 때 표적이 이미 죽었으면 그 자리 근처의 다른 손님을 맞힌다(ProjectileRetargetRadius) + 날아가는 동안 Enemy.IncomingDamage 예약 - 포탑이 곧 죽을 손님을 건너뛴다) / v5.1 (v9.11 2026-09-22 타격감: DealDamage 가 HitFeel.NextHit(속성색·크리) 를 걸고 때린다) / v5
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

    // v5.8: 증강의 타격당 회복을 다음에 셀 수 있는 시각 (Time.time)
    private static float nextLifestealTime = 0f;

    /// <summary>
    /// v5.8: 증강의 타격당 회복을 지금 세도 되는가. 세면 다음 허용 시각을 한 간격 뒤로 민다.
    /// 간격 = 1 / GameBalance.LifestealHitsPerSec 초 (0 이하면 제한 없음 - 타격마다 센다).
    /// </summary>
    private static bool LifestealReady()
    {
        float perSec = GameBalance.LifestealHitsPerSec;
        if (perSec <= 0f) return true;

        float gap = 1f / perSec;
        float now = Time.time;
        // 허용 시각이 한 간격 + 1초보다 멀리 있으면 지난 실행에서 남은 값이다 (static 은 남는데 Time.time 은 0 부터 다시 셀 수 있다) - 무시하고 센다.
        // 여유 1초는 부동소수 오차 때문이다: 딱 한 간격으로 비교하면 같은 프레임의 두 번째 타격이 "남은 값"으로 잘못 읽힐 수 있다
        bool stale = nextLifestealTime - now > gap + 1f;
        if (now < nextLifestealTime && !stale) return false;

        nextLifestealTime = now + gap;
        return true;
    }

    /// <summary>
    /// v5.5: 지금 처리 중인 발사를 쏜 포탑 (TurretSlot 의 출처 번호, 0 = 모름).
    /// TurretSlot.TickFire 가 Execute 앞뒤로 걸고 푼다. 날아가는 탄은 Execute 때 값을 잡아 뒀다가 도착 판정 동안 다시 건다.
    /// 쓰는 곳: 데미지 숫자 합산 (같은 포탑이 낸 것끼리만 한 숫자로 - GameBalance.DmgPopupMergePerSource)
    /// </summary>
    public static int CurrentSource = 0;

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

        // v5.3: 발사음 - 속성·티어·모양별. 포탑 자리에서 (화면 밖 포탑은 작게)
        SoundManager.PlayAt(SoundKeys.Shot(r, shape), origin);

        switch (shape)
        {
            case AttackShape.Projectile:
            case AttackShape.Explode:
            case AttackShape.Field:
                if (vfx != null)
                {
                    // 투사체가 날아가서 도달하면 판정 (폭발형 크게, 일반형 작게)
                    Vector3 targetPos = target.AimPoint;   // v5.10 (v9.23): 겨누는 자리 - 손님은 몸 가운데, 디 오리지널은 페이즈마다 약점 칸 (탄이 그 칸에 모인다)
                    Enemy capturedTarget = target;
                    float projSpeed = r.projectileSpeed > 0f ? r.projectileSpeed * 0.03f : 13f;
                    float projSize = r.explodeRadius > 0f ? 0.5f : 0.32f;

                    // v5.4: 날아가는 동안 이 손님에게 들어갈 피해를 예약 - TurretSlot 이 "곧 죽을 손님"을 건너뛴다 (여러 포탑이 한 마리에 몰려 허공에 쏘던 것)
                    float reserved = 0f;
                    if (GameBalance.AvoidOverkillTargeting)
                    {
                        reserved = EstimateDamage(r, capturedTarget, damage);
                        capturedTarget.IncomingDamage += reserved;
                    }

                    int shotSource = CurrentSource;   // v5.5: 이 탄을 쏜 포탑 (도착할 때는 TickFire 가 이미 끝나 있다)
                    // 대체 표적은 직격 탄만 찾는다 - 폭발·장판은 제자리에 터지거나 깔리니 옆 손님에게 옮겨 붙으면 어색하다.
                    //   마지막 식사 장면에선 찾지 않는다 - 물러난 손님을 겨눴던 탄이 식사 중인 보스에게 옮겨 붙는다
                    bool canRetarget = r.shape == AttackShape.Projectile && GameBalance.ProjectileRetargetRadius > 0f;

                    if (GameBalance.ProjectileHoming)
                    {
                        // v5.11 (v9.24.1): 탄이 손님을 따라간다. chase = 지금 쫓는 손님, lastAim = 마지막으로 겨눈 자리
                        Enemy chase = capturedTarget;
                        Vector3 lastAim = targetPos;
                        int retargetsLeft = 2;          // 갈아탈 수 있는 횟수 (찾았을 때만 깎는다)
                        float nextSearchAt = 0f;        // 못 찾았으면 0.15초 뒤 다시 (매 프레임 찾지 않게. 시간이 멈추면 같이 멈춘다)
                        // 너무 오래 쫓으면 그 자리에서 맞는다 - 상한은 ProjectileMaxFlight 와 "곧장 가는 시간의 두 배" 중 긴 쪽 (사거리 증강으로 멀리 쏜 느린 탄이 상한에 걸려 빗나가지 않게)
                        float maxFlight = Mathf.Max(GameBalance.ProjectileMaxFlight, Vector3.Distance(origin, targetPos) / Mathf.Max(1f, projSpeed) * 2f);
                        vfx.ProjectileFollow(origin, delegate (Vector3 shotPos)
                        {
                            if (chase != null && chase.IsAlive) { lastAim = chase.AimPoint; return lastAim; }
                            // 날아가는 동안 표적이 죽었다: 직격 탄은 그 손님이 죽은 자리(lastAim) 곁의 다른 손님으로 방향을 튼다 (탄 곁에서 찾으면 뒤쪽 손님으로 되돌아 날았다).
                            //   없거나 폭발·장판이면 마지막 자리로
                            if (canRetarget && retargetsLeft > 0 && Time.time >= nextSearchAt && !BossEnemy.LastSupperServing)
                            {
                                nextSearchAt = Time.time + 0.15f;
                                Enemy next = FindNearestAlive(lastAim, GameBalance.ProjectileRetargetRadius);
                                if (next != null) { chase = next; lastAim = next.AimPoint; retargetsLeft--; }
                            }
                            return lastAim;
                        }, col, projSpeed, projSize, maxFlight, delegate (Vector3 hitPos)
                        {
                            int outerSource = CurrentSource;
                            CurrentSource = shotSource;
                            if (reserved > 0f && capturedTarget != null)
                                capturedTarget.IncomingDamage = Mathf.Max(0f, capturedTarget.IncomingDamage - reserved);

                            // 떨어진 순간 쫓던 손님이 살아 있고 가까우면 그 손님 (2초 상한에 걸려 조금 못 미쳤어도 - 쫓던 탄이 빗나가 보이지 않게).
                            //   상한에 걸려 멀리 떨어졌으면 그 자리 기준으로 다시 찾는다 (직격만 먼 손님에게 들어가고 폭발은 딴 데서 터지지 않게)
                            Enemy hitTarget = chase;
                            if (hitTarget != null && hitTarget.IsAlive && hitTarget.DistanceFrom(hitPos) > Mathf.Max(1.5f, GameBalance.ProjectileRetargetRadius))
                                hitTarget = null;
                            if ((hitTarget == null || !hitTarget.IsAlive) && canRetarget && !BossEnemy.LastSupperServing)
                                hitTarget = FindNearestAlive(hitPos, GameBalance.ProjectileRetargetRadius);
                            LandShot(r, origin, hitPos, hitTarget, damage, col, vfx);
                            CurrentSource = outerSource;
                        });
                    }
                    else
                    {
                        // 예전 탄 (스위치를 끈 때): 쏜 순간의 자리로 날아간다
                        vfx.Projectile(origin, targetPos, col, projSpeed, projSize, delegate
                        {
                            int outerSource = CurrentSource;
                            CurrentSource = shotSource;
                            if (reserved > 0f && capturedTarget != null)
                                capturedTarget.IncomingDamage = Mathf.Max(0f, capturedTarget.IncomingDamage - reserved);

                            // 도달 시점 판정. v5.4: 표적이 그사이 죽었으면 착탄 자리 근처의 다른 손님을 맞힌다 (탄이 허공에 사라지지 않는다)
                            Enemy hitTarget = capturedTarget;
                            if ((hitTarget == null || !hitTarget.IsAlive) && canRetarget && !BossEnemy.LastSupperServing)
                                hitTarget = FindNearestAlive(targetPos, GameBalance.ProjectileRetargetRadius);
                            LandShot(r, origin, targetPos, hitTarget, damage, col, vfx);
                            CurrentSource = outerSource;
                        });
                    }
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

    /// <summary>v5.4: 예약용 어림 피해 - 전역·증강 공격 배율과 손님 방어/저항만 본다. 치명타·조건부 보너스는 빼서 낮게 잡는다 (낮게 잡으면 덜 건너뛸 뿐, 높게 잡으면 살 손님을 건너뛴다)</summary>
    private static float EstimateDamage(RecipeData r, Enemy en, float damage)
    {
        float stat = r.damageType == DamageType.Magic ? en.resistance : en.defense;
        return damage * GameBalance.TurretDamageMul * AugmentManager.AtkMul * 50f / (50f + Mathf.Max(0f, stat));
    }

    /// <summary>
    /// v5.11 (v9.24.1): 탄이 떨어진 자리의 판정 - 따라가는 탄과 예전 탄이 같이 쓴다.
    /// hitTarget 이 살아 있으면 직격, 폭발형이면 hitPos 에 폭발 (직격 손님은 폭발에서 뺀다), 장판형이면 hitPos 에 장판
    /// </summary>
    private static void LandShot(RecipeData r, Vector3 origin, Vector3 hitPos, Enemy hitTarget, float damage, Color col, AttackVFX vfx)
    {
        if (hitTarget != null && hitTarget.IsAlive)
        {
            HitSingle(r, hitTarget, damage);
            // v5.4 (A5): 물리 단발 - 손님 그림이 탄이 날아온 반대쪽으로 움찔한다 (판정 위치는 그대로)
            if (r.damageType == DamageType.Phys && r.shape == AttackShape.Projectile)
                HitFeel.Knock(hitTarget, hitPos - origin);
        }

        if (r.shape == AttackShape.Explode)
        {
            float radius = r.explodeRadius * 0.06f * AugmentManager.ExplodeRadiusMul * ItemManager.ExplodeRadiusMul;   // v5.12: 유물 무거운 냄비
            vfx.Explosion(hitPos, col, radius);
            SoundManager.PlayAt("sfx_explosion", hitPos);   // v5.3: 착탄음 (큰 소리 - 잦은 소리를 0.3초 덕킹)
            // P1 게임필: 폭발 미세 럼블 - 쿨타임 채널 방식 (연사돼도 2.5초에 1번만)
            GameFeel.Shake(GameBalance.ShakeExplosion, "explosion", GameBalance.ShakeExplosionCooldown);
            HitExplosionArea(r, hitPos, damage, hitTarget, radius);

            // 프리즘 증강 '메아리치는 폭발': 60% 데미지로 한 번 더 (중심 대상 포함)
            if (AugmentManager.DoubleExplosion)
            {
                float radius2 = radius * 1.2f;
                vfx.Explosion(hitPos, col, radius2);
                HitExplosionArea(r, hitPos, damage * 0.6f, null, radius2);
            }
        }
        if (r.shape == AttackShape.Field)
        {
            float radius = (r.fieldBig ? 130f : 90f) * 0.06f;
            vfx.Field(hitPos, col, radius, 4f);
            SoundManager.PlayAt("sfx_field", hitPos);   // v5.3: 장판 깔림
            HitFieldArea(r, hitPos, radius);
        }
    }

    // v5.11 (v9.24.1): 이번 프레임의 손님 목록 - 날던 탄 여럿이 한꺼번에 표적을 잃어도 FindObjectsByType 은 프레임당 한 번
    private static Enemy[] enemyCache;
    private static int enemyCacheFrame = -1;

    private static Enemy[] EnemiesThisFrame()
    {
        if (enemyCache == null || enemyCacheFrame != Time.frameCount)
        {
            enemyCache = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            enemyCacheFrame = Time.frameCount;
        }
        return enemyCache;
    }

    /// <summary>v5.4: pos 에서 radius 안의 살아 있는 손님 중 가장 가까운 것 (없으면 null). v5.11: 목록은 프레임 캐시 (그 프레임에 지워진 손님은 건너뛴다)</summary>
    private static Enemy FindNearestAlive(Vector3 pos, float radius)
    {
        Enemy best = null;
        float bestDist = radius;
        Enemy[] all = EnemiesThisFrame();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].IsAlive) continue;
            float d = all[i].DistanceFrom(pos);   // v5.9: 보스는 몸 가장자리까지
            if (d < bestDist) { bestDist = d; best = all[i]; }
        }
        return best;
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

        // v5.12 (v9.25 B5): 싸움 유물 - 굶주린 화덕(기차 HP 절반 아래) / 짝꿍 숟가락(같은 요리 포탑 둘 이상) / 빈 접시(빈 칸마다)
        finalDamage *= ItemManager.RelicDamageMul(r);
        // v5.12 (v9.25 B4): 단골 주문 완료 버프 - 그 계열 포탑 피해 +40% (20초)
        finalDamage *= OrderSystem.DamageMulFor(r.tag);

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
        bool critHit = false;   // v5.1: 타격감(흰 조각·큰 찌그러짐)용
        if (AugmentManager.CritChanceAdd > 0f && Random.value < AugmentManager.CritChanceAdd)
        {
            finalDamage *= 1.5f + AugmentManager.CritDamageAdd;
            critHit = true;
        }

        // 약점 파고들기: 도트 걸린 적에게 추가 데미지
        if (AugmentManager.DotTargetBonus > 0f && AugmentHooks.HasDotTracked(en))
            finalDamage *= 1f + AugmentManager.DotTargetBonus;

        // 사냥꾼의 본능: 슬로우/스턴 적에게 추가 데미지
        if (AugmentManager.ControlTargetBonus > 0f && AugmentHooks.IsControlled(en))
            finalDamage *= 1f + AugmentManager.ControlTargetBonus;

        float hpBefore = en.currentHP;   // Phase 2-3: 초과 데미지(옆 테이블 계산서) 판정용

        HitFeel.NextHit(TagColor(r.tag), critHit, r.tag, CurrentSource);   // v5.1: 이번 명중의 스파크 색 = 요리 속성색 / v5.3: 속성 -> 명중음 겹침 / v5.5: 쏜 포탑
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

        // 흡혈: 타격당 기차 회복. v5.8: 1초에 LifestealHitsPerSec 번까지만 (포탑이 몇 기든, 관통·폭발로 여럿을 맞혀도 합쳐서)
        if (AugmentManager.LifestealPerHit > 0f && LifestealReady())
        {
            if (cachedTrain == null) cachedTrain = Object.FindFirstObjectByType<TrainManager>();
            if (cachedTrain != null) cachedTrain.Heal(AugmentManager.LifestealPerHit);
        }

        // 붉은 주방: 모든 타격이 화상 1스택 (원시 화력과는 양립 불가 - 선택 단계에서 차단됨)
        if (AugmentManager.RedKitchen && !AugmentManager.PrimalPower)
        {
            en.ApplyBurn(1, AugmentManager.DotMul * ItemManager.DotMul);
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
        SoundManager.PlayAt("sfx_explosion", pos, 0.7f);   // v5.3

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == center || !all[i].IsAlive) continue;
            if (all[i].DistanceFrom(pos) <= radius)   // v5.9
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
        SoundManager.PlayAt("sfx_explosion", pos, 0.7f);   // v5.3

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == center || !all[i].IsAlive) continue;
            if (all[i].DistanceFrom(pos) <= radius)   // v5.9
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
            SoundManager.PlayAt("sfx_chain", next.transform.position, 0.8f);   // v5.3

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
            if (all[i].DistanceFrom(center) <= radius)   // v5.9: 보스는 몸 가장자리까지
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
            if (all[i].DistanceFrom(center) <= radius)   // v5.9
                ApplyEffects(r, all[i]);
        }
    }

    /// <summary>관통(레일) 공격: 직선 경로상의 적 전부 풀 데미지</summary>
    private static void HitPierce(RecipeData r, Vector3 origin, Enemy target, float damage, Color col)
    {
        Vector3 dir = (target.AimPoint - origin).normalized;   // v5.10 (v9.23)
        float railLength = 20f;
        float railWidth = 1.2f;

        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Beam(origin, origin + dir * railLength, col, 0.35f);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            Vector3 toEnemy = all[i].AimPoint - origin;   // v5.10: 겨눈 자리로 잰다 (손님은 몸 가운데 그대로 - 보스 약점 칸을 겨눈 줄기가 비껴가지 않게)
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
        Vector3 dir = (target.AimPoint - origin).normalized;   // v5.10 (v9.23)
        float coneRange = 7f;
        float coneHalfAngle = 26f;

        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Cone(origin, dir, coneRange, coneHalfAngle, col);

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            Vector3 toEnemy = all[i].AimPoint - origin;   // v5.10: 겨눈 자리로 잰다
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
            SoundManager.PlayAt("sfx_chain", next.transform.position);   // v5.3: 튈 때마다

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
            en.ApplyBurn(r.burnStack, AugmentManager.DotMul * ItemManager.DotMul);
            AugmentHooks.RegisterDot(en, 3f);
        }
        if (r.poisonStack > 0)
        {
            en.ApplyPoison(r.poisonStack, AugmentManager.DotMul * ItemManager.DotMul);
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
