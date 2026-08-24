using System;
using System.Collections.Generic;
using UnityEngine;

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
        if (a == b) return counts[a] >= 2;
        return counts[a] >= 1 && counts[b] >= 1;
    }

    // 조리 시작 시 재료 소모. 성공하면 true
    public bool TryConsume(MaterialType a, MaterialType b)
    {
        if (!CanAfford(a, b)) return false;
        counts[a] -= 1;
        counts[b] -= 1;
        if (OnChanged != null) OnChanged();
        return true;
    }
}