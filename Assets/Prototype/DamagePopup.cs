using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// [DamagePopup.cs]
/// 적이 피격 시 데미지 숫자가 위로 올라가며 사라지는 팝업입니다.
/// 
/// [수정 사항]
/// 1) 스폰 오프셋을 ±0.3 → ±0.8로 확대 (보스처럼 같은 위치에 데미지 누적될 때 분산)
/// 2) 위로만 올라가던 이동을 좌/우로도 분산 (대각선 이동)
/// 3) sortingLayer 명시로 다른 UI에 가려지지 않게 함
/// 
/// 사용법:
/// DamagePopup.Create(position, damage, isCritical);
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class DamagePopup : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 정적 팩토리 — 프리팹 생성
    // ─────────────────────────────────────────────
    private static GameObject prefab;

    /// <summary>월드 좌표에 데미지 팝업 생성</summary>
    public static void Create(Vector3 worldPos, float damage, bool isCritical = false)
    {
        if (prefab == null)
            prefab = Resources.Load<GameObject>("DamagePopup");

        // 스폰 위치 분산 (보스 위치에 다수 데미지 팝업이 겹치는 것을 방지)
        Vector3 spawnOffset = new Vector3(
            Random.Range(-0.8f, 0.8f),    // 가로 분산 폭 확대
            Random.Range(0.2f, 0.7f),     // 세로 약간 위
            0f);
        Vector3 spawnPos = worldPos + spawnOffset;

        if (prefab == null)
        {
            // 프리팹이 없으면 코드로 즉시 생성
            GameObject obj = new GameObject("DamagePopup");
            obj.transform.position = spawnPos;

            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            TMPFontFixer.Apply(tmp);   // 통일 폰트(Neo둥근모 기반) 배정
            ApplyTextStyle(tmp, damage, isCritical);

            // sortingLayer 최상단으로 (기차/적에 가려지지 않게)
            tmp.sortingOrder = 100;

            DamagePopup popup = obj.AddComponent<DamagePopup>();
            popup.Setup(damage, isCritical);
            return;
        }

        GameObject popupObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        popupObj.GetComponent<DamagePopup>()?.Setup(damage, isCritical);
    }

    /// <summary>TextMeshPro에 데미지 숫자 스타일 적용 (재사용)</summary>
    private static void ApplyTextStyle(TextMeshPro tmp, float damage, bool isCritical)
    {
        tmp.text = isCritical ? "!" + (int)damage : ((int)damage).ToString();
        tmp.fontSize = isCritical ? 5f : 3.5f;
        tmp.color = isCritical
            ? new Color(1f, 0.3f, 0f)    // 크리티컬: 주황
            : new Color(1f, 1f, 0.3f);   // 일반: 노랑
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = isCritical ? FontStyles.Bold : FontStyles.Normal;
        tmp.sortingOrder = 100;
    }

    // ─────────────────────────────────────────────
    // 인스턴스 변수
    // ─────────────────────────────────────────────
    private TextMeshPro tmp;
    private float moveSpeedY = 1.5f;       // 위로 올라가는 속도
    private float moveSpeedX = 0f;         // 좌/우 분산 속도 (Setup에서 결정)
    private float lifetime = 0.8f;
    private float elapsed = 0f;
    private Color startColor;

    public void Setup(float damage, bool isCritical)
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null) tmp = gameObject.AddComponent<TextMeshPro>();

        TMPFontFixer.Apply(tmp);   // 통일 폰트(Neo둥근모 기반) 배정
        ApplyTextStyle(tmp, damage, isCritical);
        startColor = tmp.color;

        // 좌/우 분산 이동 — 동일 좌표에서 여러 팝업이 떠도 서로 다른 방향으로 흩어짐
        moveSpeedX = Random.Range(-1.0f, 1.0f);

        // 크리티컬은 더 크게 시작해서 줄어드는 연출
        if (isCritical) StartCoroutine(CriticalScaleEffect());
    }

    private IEnumerator CriticalScaleEffect()
    {
        transform.localScale = Vector3.one * 1.5f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // 대각선 이동 (위 + 좌/우 분산)
        Vector3 move = new Vector3(moveSpeedX, moveSpeedY, 0f) * Time.deltaTime;
        transform.position += move;

        // 페이드 아웃
        float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
        if (tmp != null)
        {
            Color c = startColor;
            c.a = alpha;
            tmp.color = c;
        }

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
