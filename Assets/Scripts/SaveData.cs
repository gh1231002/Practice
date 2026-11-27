using UnityEngine;
using System;

// 저장 파일에 기록될 데이터만 관리하는 클래스
[Serializable]
public class SaveData
{
    // 저장 슬롯을 구분하기 위한 고유 ID
    public string SaveSlotId;

    // 플레이어가 입력한 닉네임
    public string PlayerName;

    // 저장 데이터가 최초 생성된 날짜
    public string CreatedAt;

    // 마지막으로 저장된 날짜
    public string LastSavedAt;

    // 저장 데이터 구조 버전
    public int SaveVersion;

    // 저장 당시 씬 이름
    public string CurrentScene;

    // 저장 당시 플레이어 위치
    public Vector3 PlayerPos;

    // 저장 당시 플레이어 회전
    public Quaternion PlayerRot;
}
