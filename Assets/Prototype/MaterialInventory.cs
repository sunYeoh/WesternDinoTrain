using System;
using System.Collections.Generic;
using UnityEngine;

// [MaterialInventory.cs] v1.1 (v9.31 2026-10-21 급식 조리: CanAfford / TryConsume 에 몫 수(sets) - 재료 두 개씩 x sets)
// 재료 6종 보유량 관리 (싱글톤)
// 적 처치 시 드롭 -> Add, 조리 시작 시 -> TryConsume
public class MaterialInventory : MonoBehaviour
{
    public static MaterialInventory Instance { get; private set; }

    // 재료별 보유 수량
    private Dictionary<MaterialType, int> counts = new Dictionary<MaterialType, int>();

    // 수량 변경 시 UI 갱신용 이벤트
    public event Action OnChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 6종 전부 0으로 초기화
        foreach (MaterialType t in Enum.GetValues(typeof(MaterialType)))
            counts[t] = 0;
    }

    public int Get(MaterialType t)
    {
        return counts[t];
    }

    public void Add(MaterialType t, int n)
    {
        counts[t] += n;
        if (OnChanged != null) OnChanged();
    }

    // 레시피(재료 2개)를 만들 수 있는지 확인 - 같은 재료 2개도 가능
    public bool CanAfford(MaterialType a, MaterialType b)
    {
        return CanAfford(a, b, 1);
    }

    /// <summary>v1.1: sets 인분 - 재료가 각각 sets 개씩 (같은 재료면 2 x sets) 있나</summary>
    public bool CanAfford(MaterialType a, MaterialType b, int sets)
    {
        if (sets < 1) sets = 1;
        if (a == b) return counts[a] >= 2 * sets;
        return counts[a] >= sets && counts[b] >= sets;
    }

    // 조리 시작 시 재료 소모. 성공하면 true
    public bool TryConsume(MaterialType a, MaterialType b)
    {
        return TryConsume(a, b, 1);
    }

    /// <summary>v1.1: sets 인분 소모 (각각 sets 개씩). 모자라면 아무것도 안 빼고 false</summary>
    public bool TryConsume(MaterialType a, MaterialType b, int sets)
    {
        if (sets < 1) sets = 1;
        if (!CanAfford(a, b, sets)) return false;
        counts[a] -= sets;
        counts[b] -= sets;
        if (OnChanged != null) OnChanged();
        return true;
    }
}