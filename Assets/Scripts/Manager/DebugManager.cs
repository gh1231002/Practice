using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class DebugManager : MonoBehaviour
{
    [SerializeField] GameObject debugPanel;
    [SerializeField] Key targetKey;

    [SerializeField] bool currentWeaponUnlock;
    [SerializeField] bool currentBossEnraged;

    public bool BossEnraged => currentBossEnraged;
    public bool WeaponUnlock => currentWeaponUnlock;

    public static DebugManager Instance { get; private set; }

    // 무기 언락 토글 상태가 바뀌었을 때 알리는 이벤트
    public event Action<bool> WeaponUnlockDebugChanged;
    // 보스 광폭화 토글이 변경되었을 때 알리는 이벤트
    public event Action<bool> BossEnragedDebugChanged;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        debugPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[targetKey].wasPressedThisFrame)
        {
            switch(debugPanel.activeSelf)
            {
                case true:
                    debugPanel.SetActive(false);
                    break;
                case false:
                    debugPanel.SetActive(true);
                    break;
            }
        }
    }

    // ui 토글 이벤트에 연결할 함수
    public void SetWeaponUnlock(bool isOn)
    {
        currentWeaponUnlock = isOn;
        WeaponUnlockDebugChanged?.Invoke(currentWeaponUnlock);
    }
    public void SetBossEnraged(bool isOn)
    {
        currentBossEnraged = isOn;
        BossEnragedDebugChanged?.Invoke(currentBossEnraged);
    }
}
