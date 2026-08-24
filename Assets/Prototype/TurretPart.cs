using UnityEngine;

/// <summary>
/// [TurretPart.cs]
/// 포탑을 만드는 데 사용되는 부품 데이터를 정의합니다.
/// 부품은 프레임 / 구동부 / 코어 3종류로 나뉩니다.
/// 3가지를 하나씩 조합하면 C등급 기초 포탑이 완성됩니다.
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class TurretPart : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 부품 카테고리 열거형
    // ─────────────────────────────────────────────
    public enum PartCategory
    {
        Frame,  // 프레임: 공격 방식 결정 (연사/범위/투척)
        Motor,  // 구동부: 주요 스탯 결정 (ASPD/ATK/CRT)
        Core    // 코어:   에너지원 및 기초 속성 결정
    }

    // ─────────────────────────────────────────────
    // 부품 데이터 구조체
    // ─────────────────────────────────────────────
    [System.Serializable]
    public struct PartData
    {
        public string partName;    // 부품 이름
        public PartCategory category;  // 카테고리
        public string description; // 설명
        public float statBonus;   // 스탯 보너스
        public string attribute;   // 속성 (물리/화염/전기 등)
    }

    // ─────────────────────────────────────────────
    // 프레임 부품 데이터
    // ─────────────────────────────────────────────
    public static PartData LightFrame = new PartData
    {
        partName = "단신형 총몸",
        category = PartCategory.Frame,
        description = "연사 공격 방식",
        statBonus = 1.0f,
        attribute = "물리"
    };

    public static PartData HeavyBarrel = new PartData
    {
        partName = "중량형 포신",
        category = PartCategory.Frame,
        description = "범위 공격 방식",
        statBonus = 1.5f,
        attribute = "물리"
    };

    public static PartData SpringLauncher = new PartData
    {
        partName = "스프링 발사대",
        category = PartCategory.Frame,
        description = "투척 공격 방식",
        statBonus = 1.2f,
        attribute = "물리"
    };

    // ─────────────────────────────────────────────
    // 구동부 부품 데이터
    // ─────────────────────────────────────────────
    public static PartData HighSpeedMotor = new PartData
    {
        partName = "고속 회전 모터",
        category = PartCategory.Motor,
        description = "ASPD 대폭 상승",
        statBonus = 2.0f,
        attribute = "기계"
    };

    public static PartData ReinforcedCylinder = new PartData
    {
        partName = "강화 실린더",
        category = PartCategory.Motor,
        description = "ATK 대폭 상승",
        statBonus = 2.5f,
        attribute = "기계"
    };

    public static PartData PrecisionLens = new PartData
    {
        partName = "정밀 렌즈",
        category = PartCategory.Motor,
        description = "CRT 대폭 상승",
        statBonus = 1.8f,
        attribute = "기계"
    };

    // ─────────────────────────────────────────────
    // 코어 부품 데이터
    // ─────────────────────────────────────────────
    public static PartData CopperCore = new PartData
    {
        partName = "구리 증기 코어",
        category = PartCategory.Core,
        description = "기본 물리 속성",
        statBonus = 1.0f,
        attribute = "물리"
    };

    public static PartData FlameCore = new PartData
    {
        partName = "화염 연소석",
        category = PartCategory.Core,
        description = "화염 속성 부여",
        statBonus = 1.3f,
        attribute = "화염"
    };

    public static PartData StaticCore = new PartData
    {
        partName = "정전기 발생기",
        category = PartCategory.Core,
        description = "전기 속성 부여",
        statBonus = 1.3f,
        attribute = "전기"
    };
}
