using UnityEngine;

/// <summary>
/// [DevCheat.cs] v1.3 (v9.9: 견습 운행 중 무시) / v1.2 (v9.8.1: 빌드에서는 GameBalance.CheatsInBuild 가 true 일 때만 동작) / v1.1 (2026-09-14: F3 짧은 런 토글)
/// 개발 테스트용 치트 키 모음 (빌드 전 제거 또는 비활성화)
/// GameSystems 오브젝트에 부착
///
/// F5: 재료 전종 +10
/// F6: T1 요리 21종 전부 +2 (도감 전체 발견)
/// F7: T2 요리 21종 전부 +1 (도감 전체 발견)
/// F8: 슬롯 8개에 추천 조합 자동 세팅 (딜러+버퍼+서포트 밸런스)
/// F9: 슬롯 전체 비우기
/// F10: 기차 HP 풀 회복 (포만감 시스템은 v2에서 제거됨)
/// F3: 짧은 런 토글 (지역 길이 8 <-> 3, 로비에서 - 다음 런부터 적용) - 2026-09-14 교수 피드백 B6
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class DevCheat : MonoBehaviour
{
    [Header("치트 활성화 여부 (빌드 시 꺼두기)")]
    public bool cheatEnabled = true;

    void Update()
    {
        if (!cheatEnabled) return;
        if (!GameBalance.CheatsAllowed) return;   // v9.8.1: 빌드에서는 GameBalance.CheatsInBuild 가 true 일 때만
        if (TutorialDirector.Active) return;      // v9.9: 견습 운행 중엔 치트 없음 (단계 판정이 꼬인다)

        // F3: 짧은 런 토글 - 정식 길이(8)가 기본이므로 빠른 확인은 이 키로 (런 시작 때 WaveManager가 읽는다)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            // 런 도중에 바꾸면 보스 웨이브/최종전 판정이 즉시 어긋나므로(FinalWave가 실시간 계산) 로비에서만 받는다
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Lobby)
            {
                UIManager.Instance?.ShowStatChange("[치트] 지역 길이는 로비에서만 바꿀 수 있다 (런 포기 후 F3)");
            }
            else
            {
                bool toShort = GameBalance.RegionLength != GameBalance.ShortRunRegionLength;
                GameBalance.RegionLength = toShort ? GameBalance.ShortRunRegionLength : 8;
                string msg = "[치트] 지역 길이 " + GameBalance.RegionLength + " (보스 " + GameBalance.RegionLength + "/"
                    + (GameBalance.RegionLength * 2) + "/" + (GameBalance.RegionLength * 3) + ", 최종전 " + GameBalance.FinalWave + "웨이브)";
                UIManager.Instance?.ShowStatChange(msg);
                Debug.Log(msg);
            }
        }

        // F5: 재료 전종 +10
        if (Input.GetKeyDown(KeyCode.F5))
        {
            foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                MaterialInventory.Instance.Add(t, 10);
            Debug.Log("[치트] 재료 전종 +10");
        }

        // F6: T1 요리 전부 +2
        if (Input.GetKeyDown(KeyCode.F6))
        {
            foreach (RecipeData r in RecipeDatabase.All)
                if (r.tier == 1) FoodStock.Instance.Add(r.recipeId, 2);
            Debug.Log("[치트] T1 요리 21종 +2 (도감 발견)");
        }

        // F7: T2 요리 전부 +1
        if (Input.GetKeyDown(KeyCode.F7))
        {
            foreach (RecipeData r in RecipeDatabase.All)
                if (r.tier == 2) FoodStock.Instance.Add(r.recipeId, 1);
            Debug.Log("[치트] T2 요리 21종 +1 (도감 발견)");
        }

        // F8: 추천 조합 자동 세팅
        if (Input.GetKeyDown(KeyCode.F8))
        {
            AutoLoadout();
        }

        // F9: 슬롯 전체 비우기
        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (TurretSlotManager.Instance == null) return;
            for (int i = 0; i < 8; i++)
            {
                TurretSlot s = TurretSlotManager.Instance.slots[i];
                if (s != null && !s.IsEmpty) s.Scrap();
            }
            Debug.Log("[치트] 슬롯 전체 비움");
        }

        // F10: 기차 회복
        if (Input.GetKeyDown(KeyCode.F10))
        {
            TrainManager tm = FindFirstObjectByType<TrainManager>();
            if (tm != null)
            {
                tm.Heal(99999f);
                tm.FeedTrain(150f);
            }
            Debug.Log("[치트] 기차 HP 풀 회복");
        }
    }

    /// <summary>
    /// 슬롯 8개 자동 세팅 - 공격형태/역할이 골고루 보이는 테스트 조합
    /// [0][1]  개틀링(연사)     지휘관의만찬(물리버프)
    /// [2][3]  과부하코일(체인)  용암폭탄밥(폭발)
    /// [4][5]  플라즈마볶음(관통) 맹독화염방사(부채꼴)
    /// [6][7]  절대영도수프(장판) 해독스튜(리젠)
    /// </summary>
    private void AutoLoadout()
    {
        if (TurretSlotManager.Instance == null)
        {
            Debug.LogWarning("[치트] TurretSlotManager 없음");
            return;
        }

        string[] loadout = new string[]
        {
            "T2:elec+phys",                                            // 개틀링 티렉스
            RecipeDatabase.MakeTagKey(FoodTag.Def, FoodTag.Phys),      // 지휘관의 만찬
            RecipeDatabase.MakeKey(MaterialType.Elec, MaterialType.Elec),   // 과부하 코일
            RecipeDatabase.MakeKey(MaterialType.Fire, MaterialType.Fire),   // 용암 폭탄밥
            RecipeDatabase.MakeKey(MaterialType.Elec, MaterialType.Fire),   // 플라즈마 볶음
            RecipeDatabase.MakeKey(MaterialType.Fire, MaterialType.Poison), // 맹독 화염방사
            RecipeDatabase.MakeKey(MaterialType.Ice, MaterialType.Ice),     // 절대영도 수프
            RecipeDatabase.MakeKey(MaterialType.Armor, MaterialType.Poison) // 해독 스튜
        };

        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s == null) continue;

            // 기존 내용 비우고 새로 투입
            if (!s.IsEmpty) s.Scrap();

            RecipeData r = RecipeDatabase.Get(loadout[i]);
            if (r == null)
            {
                Debug.LogWarning("[치트] 레시피 못 찾음: " + loadout[i]);
                continue;
            }
            s.TryInsertFood(loadout[i]);
            FoodStock.Instance.Add(loadout[i], 0); // 도감 발견 처리용 (0개 추가 = 발견만)
        }
        Debug.Log("[치트] 슬롯 8개 자동 세팅 완료 (연사/버프/체인/폭발/관통/부채꼴/장판/리젠)");
    }
}