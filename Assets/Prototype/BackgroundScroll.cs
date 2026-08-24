using UnityEngine;

/// <summary>
/// [BackgroundScroll.cs]
/// 기차는 제자리에 있고 배경이 오른쪽→왼쪽으로 스크롤됩니다.
/// 화면 밖으로 나간 배경은 오른쪽으로 재배치됩니다.
/// 배경 오브젝트들에 이 스크립트를 붙이세요.
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// 
/// 사용법:
/// 1. 배경 Sprite 오브젝트를 2~3개 만들기 (가로로 나란히 배치)
/// 2. 각 배경 오브젝트에 이 스크립트 붙이기
/// 3. scrollSpeed, backgroundWidth 설정
/// </summary>
public class BackgroundScroll : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector 설정
    // ─────────────────────────────────────────────
    [Header("─ 스크롤 설정 ─")]
    public float scrollSpeed = 2f;   // 스크롤 속도 (기차 이동 속도에 맞춤)
    public float backgroundWidth = 20f; // 배경 하나의 너비 (World 단위)

    [Header("─ 재배치 기준점 ─")]
    public float resetPositionX = -20f; // 이 X 좌표 왼쪽으로 나가면 재배치
    public float spawnPositionX = 20f; // 재배치될 X 좌표

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private bool isScrolling = false;

    // ─────────────────────────────────────────────
    // 매 프레임: 전투 중에만 스크롤
    // ─────────────────────────────────────────────
    private void Update()
    {
        // 전투 중일 때만 배경 스크롤
        isScrolling = (GameManager.Instance != null &&
                       GameManager.Instance.currentState == GameManager.GameState.Battle);

        if (!isScrolling) return;

        // 왼쪽으로 이동
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        // 화면 왼쪽 밖으로 나가면 오른쪽으로 재배치
        if (transform.position.x < resetPositionX)
        {
            Vector3 newPos = transform.position;
            newPos.x += backgroundWidth * GetSiblingCount();
            transform.position = newPos;
        }
    }

    // ─────────────────────────────────────────────
    // 같은 부모의 자식 수 반환 (재배치 거리 계산용)
    // ─────────────────────────────────────────────
    private int GetSiblingCount()
    {
        if (transform.parent != null)
            return transform.parent.childCount;
        return 1;
    }

    // ─────────────────────────────────────────────
    // 스크롤 속도 외부 설정 (난이도 조절용)
    // ─────────────────────────────────────────────
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }
}
