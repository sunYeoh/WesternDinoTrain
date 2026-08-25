using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [KitchenEvents.cs] v1
/// 주방 돌발 이벤트 인터페이스 + 4종 구현체 (기획 B-4)
///
/// 새 조작키를 만들지 않고 기존 조작만 재활용한다
///  - 몬스터 침입 : E 연타      (조리 상호작용 키)
///  - 기구 고장   : 방향키 커맨드 (볶기 미니게임 조작)
///  - 주방 화재   : E 홀드       (조리 상호작용 키)
///  - 재료 흘림   : 마우스 좌클릭 (슬롯 마커 조작)
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public interface IKitchenEvent
{
    /// <summary>배너 제목</summary>
    string Title { get; }
    /// <summary>배너 조작 안내 (매 프레임 갱신되므로 실시간 상태 표시에 써도 된다)</summary>
    string Guide { get; }
    /// <summary>제한 시간(초)</summary>
    float TimeLimit { get; }
    /// <summary>진행도 0~1 (게이지 표시용)</summary>
    float Progress { get; }

    /// <summary>이벤트 시작. difficulty는 0~1 (누적될수록 증가)</summary>
    void OnStart(KitchenEventManager mgr, float difficulty);

    /// <summary>매 프레임 호출. 반환값 true면 종료, success에 성공 여부를 담는다</summary>
    bool OnUpdate(float dt, out bool success);

    /// <summary>종료 처리 (보상 / 페널티)</summary>
    void OnEnd(bool success);
}


// ======================================================================
//  1. 몬스터 침입 - E 연타로 격퇴
// ======================================================================
public class MonsterIntrusionEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private float gauge;              // 현재 격퇴 게이지
    private float needGauge;          // 목표치
    private float decayPerSec = 14f;  // 가만히 있으면 게이지가 줄어든다
    private float gainPerPress = 9f;  // E 한 번당 상승량
    private RectTransform intruderIcon;
    private float shakeTimer;

    public string Title { get { return "침입자! 주방에 랩터가 들어왔다"; } }
    public string Guide { get { return "[E] 연타해서 몰아내라!   " + Mathf.RoundToInt(gauge) + " / " + Mathf.RoundToInt(needGauge); } }
    public float TimeLimit { get { return 6.5f; } }
    public float Progress { get { return needGauge > 0f ? gauge / needGauge : 0f; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        gauge = 0f;
        needGauge = 90f * (1f + difficulty);   // 난이도에 따라 최대 180

        // 침입자 표시용 아이콘 (화면 중앙 약간 아래)
        intruderIcon = KitchenEventManager.MakeBox(mgr.CustomRoot, "Intruder", new Color(0.75f, 0.2f, 0.18f, 0.92f));
        intruderIcon.anchorMin = new Vector2(0.5f, 0.5f);
        intruderIcon.anchorMax = new Vector2(0.5f, 0.5f);
        intruderIcon.anchoredPosition = new Vector2(0f, -40f);
        intruderIcon.sizeDelta = new Vector2(150f, 150f);
        intruderIcon.GetComponent<Image>().raycastTarget = false;

        Text label = KitchenEventManager.MakeText(intruderIcon, "Label", "침입자", 24, Color.white);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        gauge -= decayPerSec * dt;
        if (gauge < 0f) gauge = 0f;

        if (Input.GetKeyDown(KeyCode.E))
        {
            gauge += gainPerPress;
            shakeTimer = 0.12f;   // 때린 느낌으로 아이콘을 흔든다
        }

        // 아이콘 흔들기 + 게이지에 따라 작아지는 연출
        if (intruderIcon != null)
        {
            float t = Mathf.Clamp01(Progress);
            float size = Mathf.Lerp(150f, 60f, t);
            intruderIcon.sizeDelta = new Vector2(size, size);

            if (shakeTimer > 0f)
            {
                shakeTimer -= dt;
                intruderIcon.anchoredPosition = new Vector2(Random.Range(-9f, 9f), -40f + Random.Range(-9f, 9f));
            }
            else
            {
                intruderIcon.anchoredPosition = new Vector2(0f, -40f);
            }
        }

        if (gauge >= needGauge)
        {
            success = true;
            return true;
        }
        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            manager.HealTrain(25f);
            // 보상 다양화 (v3): 격퇴한 침입자가 재료를 떨군다
            if (MaterialInventory.Instance != null)
                MaterialInventory.Instance.Add(MaterialType.Meat, 1);
            Debug.Log("[주방이벤트] 침입자 격퇴 성공 - 기차 25 회복 + 고기 1");
        }
        else
        {
            manager.DamageTrain(60f);   // HP 500 기준 조정 (기존 90)
            Debug.Log("[주방이벤트] 침입자 격퇴 실패 - 기차 60 피해");
        }
    }
}


// ======================================================================
//  2. 기구 고장 - 방향키 커맨드 입력으로 수리
// ======================================================================
public class EquipmentBreakEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private List<KeyCode> command = new List<KeyCode>();
    private int inputIndex;
    private Text commandText;
    private float wrongFlash;         // 오입력 시 빨갛게 깜빡이는 시간

    public string Title { get { return "조리 기구 고장! 배선에서 불꽃이 튄다"; } }
    public string Guide { get { return "방향키를 순서대로 입력해 수리하라   " + inputIndex + " / " + command.Count; } }
    public float TimeLimit { get { return 7f; } }
    public float Progress { get { return command.Count > 0 ? (float)inputIndex / command.Count : 0f; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        inputIndex = 0;
        command.Clear();

        // 커맨드 길이 4 ~ 8개 (난이도에 비례)
        int length = 4 + Mathf.RoundToInt(difficulty * 4f);
        KeyCode[] pool = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
        for (int i = 0; i < length; i++)
            command.Add(pool[Random.Range(0, pool.Length)]);

        commandText = KitchenEventManager.MakeText(mgr.CustomRoot, "Command", "", 52, Color.white);
        RectTransform rt = commandText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -30f);
        rt.sizeDelta = new Vector2(700f, 80f);

        RefreshCommandText();
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        if (wrongFlash > 0f) wrongFlash -= dt;

        KeyCode pressed = ReadArrowKey();
        if (pressed != KeyCode.None)
        {
            if (pressed == command[inputIndex])
            {
                inputIndex++;
                if (inputIndex >= command.Count)
                {
                    success = true;
                    return true;
                }
            }
            else
            {
                // 틀리면 처음부터 다시
                inputIndex = 0;
                wrongFlash = 0.25f;
            }
            RefreshCommandText();
        }

        if (commandText != null)
            commandText.color = wrongFlash > 0f ? new Color(1f, 0.35f, 0.3f) : Color.white;

        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            manager.HealTrain(15f);
            Debug.Log("[주방이벤트] 기구 수리 성공");
        }
        else
        {
            manager.DamageTrain(40f);   // HP 500 기준 조정 (기존 60)
            Debug.Log("[주방이벤트] 기구 수리 실패 - 기차 40 피해");
        }
    }

    /// <summary>이번 프레임에 눌린 방향키 하나를 반환 (없으면 None)</summary>
    private KeyCode ReadArrowKey()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) return KeyCode.UpArrow;
        if (Input.GetKeyDown(KeyCode.DownArrow)) return KeyCode.DownArrow;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) return KeyCode.LeftArrow;
        if (Input.GetKeyDown(KeyCode.RightArrow)) return KeyCode.RightArrow;
        return KeyCode.None;
    }

    /// <summary>남은 커맨드는 흰색, 입력 완료분은 회색으로 표시</summary>
    private void RefreshCommandText()
    {
        if (commandText == null) return;
        string s = "";
        for (int i = 0; i < command.Count; i++)
        {
            string arrow = ArrowChar(command[i]);
            if (i < inputIndex) s += "<color=#555555>" + arrow + "</color> ";
            else if (i == inputIndex) s += "<color=#FFC94D>" + arrow + "</color> ";
            else s += arrow + " ";
        }
        commandText.supportRichText = true;
        commandText.text = s;
    }

    private string ArrowChar(KeyCode key)
    {
        if (key == KeyCode.UpArrow) return "↑";
        if (key == KeyCode.DownArrow) return "↓";
        if (key == KeyCode.LeftArrow) return "←";
        return "→";
    }
}


// ======================================================================
//  3. 주방 화재 - E 홀드로 진화
// ======================================================================
public class KitchenFireEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private float gauge;
    private float needGauge = 100f;
    private float holdGain = 42f;     // E 누르고 있을 때 초당 상승
    private float releaseLoss = 22f;  // 떼면 초당 감소
    private float burnDamagePerSec;   // 진화 전까지 기차가 계속 입는 피해
    private float burnTickTimer;      // 도트 적용 주기 누적기 (0.5초 묶음)
    private RectTransform fireBox;
    private Text fireLabel;

    public string Title { get { return "주방 화재 발생! 기차가 계속 타들어간다"; } }
    public string Guide { get { return "[E] 꾹 눌러 불길을 잡아라   " + Mathf.RoundToInt(gauge) + "%"; } }
    public float TimeLimit { get { return 8f; } }
    public float Progress { get { return gauge / needGauge; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        gauge = 0f;
        burnTickTimer = 0f;
        burnDamagePerSec = 5f + difficulty * 5f;   // 난이도에 따라 5 ~ 10 (기차 HP 500 기준 조정)

        fireBox = KitchenEventManager.MakeBox(mgr.CustomRoot, "Fire", new Color(1f, 0.35f, 0.1f, 0.35f));
        fireBox.anchorMin = new Vector2(0.5f, 0.5f);
        fireBox.anchorMax = new Vector2(0.5f, 0.5f);
        fireBox.anchoredPosition = new Vector2(0f, -40f);
        fireBox.sizeDelta = new Vector2(300f, 190f);
        fireBox.GetComponent<Image>().raycastTarget = false;

        fireLabel = KitchenEventManager.MakeText(fireBox, "Label", "화재", 34, new Color(1f, 0.9f, 0.6f));
        RectTransform lrt = fireLabel.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;

        // 불이 꺼질 때까지 기차가 피해를 입는다
        // [수정] 매 프레임 잘게 넣으면 TrainManager 최소데미지(1) 보정 때문에
        // 실제 피해가 수 배로 뻥튀기됨 -> 0.5초 단위 묶음으로 적용
        burnTickTimer += dt;
        if (burnTickTimer >= 0.5f)
        {
            burnTickTimer -= 0.5f;
            manager.DamageTrain(burnDamagePerSec * 0.5f);
        }

        if (Input.GetKey(KeyCode.E)) gauge += holdGain * dt;
        else gauge -= releaseLoss * dt;
        gauge = Mathf.Clamp(gauge, 0f, needGauge);

        // 진화될수록 불길이 작아지고 옅어진다
        if (fireBox != null)
        {
            float t = Mathf.Clamp01(Progress);
            fireBox.sizeDelta = new Vector2(Mathf.Lerp(300f, 110f, t), Mathf.Lerp(190f, 70f, t));
            Image img = fireBox.GetComponent<Image>();
            float flicker = 0.28f + Mathf.PingPong(Time.time * 2.4f, 0.14f);
            img.color = new Color(1f, 0.35f, 0.1f, Mathf.Lerp(flicker, 0.08f, t));
        }

        if (gauge >= needGauge)
        {
            success = true;
            return true;
        }
        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            Debug.Log("[주방이벤트] 화재 진화 성공");
        }
        else
        {
            manager.DamageTrain(50f);   // 시간 초과 시 폭발 피해 (HP 500 기준 조정)
            Debug.Log("[주방이벤트] 화재 진화 실패 - 기차 50 추가 피해");
        }
    }
}


// ======================================================================
//  4. 재료 흘림 - 흩어진 재료를 마우스 좌클릭으로 줍기
// ======================================================================
public class MaterialSpillEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private int totalCount;
    private int pickedCount;
    private List<Button> items = new List<Button>();

    public string Title { get { return "기차 흔들림! 재료가 바닥에 쏟아졌다"; } }
    public string Guide { get { return "떨어진 재료를 [마우스 좌클릭]으로 전부 주워라   " + pickedCount + " / " + totalCount; } }
    public float TimeLimit { get { return 7.5f; } }
    public float Progress { get { return totalCount > 0 ? (float)pickedCount / totalCount : 0f; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        pickedCount = 0;
        items.Clear();

        totalCount = 4 + Mathf.RoundToInt(difficulty * 3f);   // 4 ~ 7개
        string[] names = { "고기", "등심", "전기 꼬리", "화염 꽃", "얼음꽃", "독침" };

        for (int i = 0; i < totalCount; i++)
        {
            string label = names[Random.Range(0, names.Length)];
            Vector2 pos = new Vector2(Random.Range(-620f, 620f), Random.Range(-330f, 60f));
            Button btn = KitchenEventManager.MakeButton(
                mgr.CustomRoot, label, new Color(0.55f, 0.42f, 0.24f, 0.95f), pos, new Vector2(110f, 72f));

            Button captured = btn;   // 클로저 캡처용 지역 변수 (C# 7.3 필수)
            btn.onClick.AddListener(delegate { OnPick(captured); });
            items.Add(btn);
        }
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        if (pickedCount >= totalCount)
        {
            success = true;
            return true;
        }
        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            manager.HealTrain(20f);
            // 보상 다양화 (v3): 바닥을 치우다 여분 재료를 발견
            if (MaterialInventory.Instance != null)
                MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
            Debug.Log("[주방이벤트] 재료 전부 회수 성공 + 여분 재료 1");
        }
        else
        {
            int lost = totalCount - pickedCount;
            manager.DamageTrain(10f * lost);   // 못 주운 재료가 기계에 끼어 피해 (HP 500 기준 조정)
            Debug.Log("[주방이벤트] 재료 회수 실패 - 미회수 " + lost + "개 / 기차 " + (10 * lost) + " 피해");
        }
    }

    /// <summary>재료 하나를 주웠을 때</summary>
    private void OnPick(Button btn)
    {
        if (btn == null || !btn.gameObject.activeSelf) return;
        btn.gameObject.SetActive(false);
        pickedCount++;
    }
}
