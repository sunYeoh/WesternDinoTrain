using UnityEngine;

/// <summary>
/// [Bullet.cs]
/// 포탑에서 발사된 총알의 이동과 적 충돌을 처리합니다.
/// 총알 프리팹에 이 스크립트를 붙이세요.
/// Turret.cs의 FireBullet()에서 Initialize()를 호출해 초기화합니다.
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class Bullet : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 런타임 상태
    // ─────────────────────────────────────────────
    private Enemy target;       // 추적할 타겟
    private float damage;       // 데미지
    private float speed;        // 이동 속도
    private bool isReady = false; // Initialize 완료 여부

    // 총알이 너무 오래 살아있으면 자동 삭제 (타겟이 죽었을 때 대비)
    private float lifeTime = 5f;
    private float lifeTimer = 0f;

    // ─────────────────────────────────────────────
    // 초기화 (Turret에서 호출)
    // ─────────────────────────────────────────────
    public void Initialize(Enemy targetEnemy, float bulletDamage, float bulletSpeed)
    {
        target = targetEnemy;
        damage = bulletDamage;
        speed = bulletSpeed;
        isReady = true;
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 타겟 추적 이동
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!isReady) return;

        lifeTimer += Time.deltaTime;

        // 수명 초과 시 삭제
        if (lifeTimer >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 타겟이 사라졌거나 죽었으면 삭제
        if (target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        // 타겟 방향으로 이동
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 타겟 방향으로 회전 (총알 스프라이트가 있을 때 자연스럽게 보이도록)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 타겟에 충분히 가까워지면 명중 처리
        float distanceToTarget = Vector2.Distance(transform.position, target.transform.position);
        if (distanceToTarget < 0.3f)
        {
            HitTarget();
        }
    }

    // ─────────────────────────────────────────────
    // 명중 처리
    // ─────────────────────────────────────────────
    private void HitTarget()
    {
        if (target != null && target.IsAlive)
        {
            target.TakeDamage(damage);
            // TODO: 명중 이펙트 생성
        }

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    // 2D Collider 충돌 처리 (Collider 사용 시)
    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy hitEnemy = other.GetComponent<Enemy>();
        if (hitEnemy != null && hitEnemy == target)
        {
            HitTarget();
        }
    }
}
