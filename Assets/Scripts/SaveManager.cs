using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// 저장 데이터를 관리하는 싱글톤 매니저
/// </summary>
public class SaveManager : MonoBehaviour
{
    [SerializeField, Min(1)] int maxSaveSlots = 5;
    
    // 다른 스크립트에서 접근할 수 있는 싱글톤 인스턴스
    public static SaveManager Instance {get; private set;}

    // 저장 데이터 버전
    const int CurrentSaveVersion = 1;

    // 저장 파일이 위치할 폴더
    string SaveFolderPath => Path.Combine(Application.persistentDataPath, "Saves");

    // 현재 메모리에 유지하고 있는 저장 데이터
    public SaveData CurrentData {get; private set;}

    // Inspector에서 설정된 최대 저장 슬롯 개수
    public int MaxSaveSlots => Mathf.Max(1, maxSaveSlots);

    void Awake()
    {
        // 이미 다른 SaveManager가 존재하면 중복 개체 제거
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        // 저장 폴더가 없는 경우 생성
        EnsureSaveFolderExists();
    }

    /// <summary>
    /// 저장 폴더 생성
    /// </summary>
    private void EnsureSaveFolderExists()
    {
        try
        {
            if(!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }
        }
        catch(Exception e)
        {
            Debug.LogError($"저장 폴더 생성 실패: {e.Message}");
        }
    }

    /// <summary>
    /// SaveSlotId를 기반으로 저장 파일 경로 반환
    /// </summary>
    private string GetSaveFilePath(string saveSlotId)
    {
        return Path.Combine(SaveFolderPath, $"save_{saveSlotId}.json");
    }

    public bool CreateNewSave(string playerName, string initialScene = "")
    {
        // 닉네임이 비어있는 경우 생성 거부
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("플레이어 닉네임이 비어 있습니다.");
            return false;
        }

        if (IsPlayerNameDuplicate(playerName))
        {
            Debug.LogWarning("이미 사용 중인 플레이어 닉네임입니다.");
            return false;
        }

        // 최대 저장 슬롯 개수 도달 시 신규 생성 거부
        if (!CanCreateNewSave())
        {
            Debug.LogWarning($"최대 저장 슬롯 개수({MaxSaveSlots})에 도달하여 새 저장 데이터를 생성할 수 없습니다.");
            return false;
        }

        try
        {
            EnsureSaveFolderExists();

            // 기존 파일과 충돌하지 않는 고유 ID 생성
            string saveSlotId;

            do
            {
                saveSlotId = Guid.NewGuid().ToString("N");
            }
            while (File.Exists(GetSaveFilePath(saveSlotId)));

            // 신규 저장 데이터 구성
            SaveData newData = new SaveData
            {
                SaveSlotId = saveSlotId,
                PlayerName = playerName,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                LastSavedAt = DateTime.UtcNow.ToString("O"),
                SaveVersion = CurrentSaveVersion,
                CurrentScene = initialScene,
                PlayerPos = Vector3.zero,
                PlayerRot = Quaternion.identity
            };

            // 생성한 데이터를 파일로 저장
            if (!WriteSaveFile(newData))
            {
                return false;
            }

            // 저장 성공 후 현재 데이터 갱신
            CurrentData = newData;

            Debug.Log($"신규 저장 데이터 생성 성공: {saveSlotId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"신규 저장 데이터 생성 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 지정한 저장 슬롯을 새 게임 데이터로 교체한다.
    /// </summary>
    public bool ReplaceSaveData(string saveSlotId, string playerName, string initialScene = "")
    {
        if (string.IsNullOrWhiteSpace(saveSlotId) || string.IsNullOrWhiteSpace(playerName))
        {
            return false;
        }

        List<SaveData> saveDataList = GetSaveDataList();
        bool targetExists = saveDataList.Exists(data => data.SaveSlotId == saveSlotId);
        if (!targetExists)
        {
            Debug.LogWarning("교체할 저장 데이터를 찾을 수 없습니다.");
            return false;
        }

        if (IsPlayerNameDuplicate(playerName, saveSlotId))
        {
            Debug.LogWarning("이미 사용 중인 플레이어 닉네임입니다.");
            return false;
        }

        try
        {
            string now = DateTime.UtcNow.ToString("O");
            SaveData replacementData = new SaveData
            {
                SaveSlotId = saveSlotId,
                PlayerName = playerName.Trim(),
                CreatedAt = now,
                LastSavedAt = now,
                SaveVersion = CurrentSaveVersion,
                CurrentScene = initialScene,
                PlayerPos = Vector3.zero,
                PlayerRot = Quaternion.identity
            };

            if (!WriteSaveFile(replacementData))
            {
                return false;
            }

            CurrentData = replacementData;
            Debug.Log($"저장 데이터 교체 성공: {saveSlotId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 데이터 교체 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 현재 데이터를 저장
    /// </summary>
    public bool SaveCurrentData()
    {
        if (CurrentData == null)
        {
            Debug.LogWarning("현재 저장 데이터가 존재하지 않습니다.");
            return false;
        }

        // 저장 시 마지막 저장 날짜 갱신
        CurrentData.LastSavedAt = DateTime.UtcNow.ToString("O");

        return WriteSaveFile(CurrentData);
    }

    /// <summary>
    /// 저장 데이터를 JSON 파일로 기록
    /// </summary>
    private bool WriteSaveFile(SaveData data)
    {
        if (!IsValidSaveData(data))
        {
            Debug.LogWarning("유효하지 않은 저장 데이터입니다.");
            return false;
        }

        try
        {
            EnsureSaveFolderExists();

            string filePath = GetSaveFilePath(data.SaveSlotId);
            string tempPath = filePath + ".tmp";

            // Unity JsonUtility를 이용해 JSON 변환
            string json = JsonUtility.ToJson(data, true);

            // 임시 파일에 먼저 기록
            File.WriteAllText(tempPath, json);

            // 임시 파일 기록 완료 후 기존 파일을 교체하거나 신규 파일 이동
            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 파일 기록 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 지정한 저장 슬롯의 데이터 불러오기
    /// </summary>
    public bool LoadSaveData(string saveSlotId)
    {
        if (string.IsNullOrWhiteSpace(saveSlotId))
        {
            return false;
        }

        try
        {
            string filePath = GetSaveFilePath(saveSlotId);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning("해당 저장 파일이 존재하지 않습니다.");
                return false;
            }

            string json = File.ReadAllText(filePath);
            SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

            // 읽어온 데이터 검증
            if (!IsValidSaveData(loadedData))
            {
                Debug.LogWarning("저장 데이터가 손상되었거나 유효하지 않습니다.");
                return false;
            }

            // 파일 이름과 데이터 내부 ID가 일치하는지 확인
            if (loadedData.SaveSlotId != saveSlotId)
            {
                Debug.LogWarning("저장 파일 ID가 일치하지 않습니다.");
                return false;
            }

            CurrentData = loadedData;

            Debug.Log($"저장 데이터 불러오기 성공: {saveSlotId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 데이터 불러오기 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 가장 최근에 저장된 데이터 불러오기
    /// </summary>
    public bool LoadLatestSave()
    {
        List<SaveData> saveDataList = GetSaveDataList();

        if (saveDataList.Count == 0)
        {
            Debug.LogWarning("불러올 저장 데이터가 없습니다.");
            return false;
        }

        // 마지막 저장 날짜 기준으로 최신 데이터 선택
        SaveData latestData = saveDataList[0];

        for (int i = 1; i < saveDataList.Count; i++)
        {
            if (DateTime.TryParse(
                    saveDataList[i].LastSavedAt,
                    out DateTime currentDate) &&
                DateTime.TryParse(
                    latestData.LastSavedAt,
                    out DateTime latestDate))
            {
                if (currentDate > latestDate)
                {
                    latestData = saveDataList[i];
                }
            }
        }

        return LoadSaveData(latestData.SaveSlotId);
    }

     /// <summary>
     /// 저장 데이터 목록 반환
     /// </summary>
    public List<SaveData> GetSaveDataList()
    {
        List<SaveData> saveDataList = new List<SaveData>();

        try
        {
            EnsureSaveFolderExists();

            string[] filePaths = Directory.GetFiles(SaveFolderPath, "save_*.json");

            foreach (string filePath in filePaths)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);

                    if (IsValidSaveData(data))
                    {
                        // 파일명과 데이터 내부 ID 일치 여부도 확인
                        string expectedPath = GetSaveFilePath(data.SaveSlotId);

                        if (Path.GetFullPath(filePath) ==
                            Path.GetFullPath(expectedPath))
                        {
                            saveDataList.Add(data);
                        }
                    }
                }
                catch (Exception e)
                {
                    // 개별 파일 오류가 다른 저장 데이터 조회에 영향을 주지 않도록 처리
                    Debug.LogWarning(
                        $"저장 파일 조회 실패: {Path.GetFileName(filePath)} - {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 데이터 목록 조회 실패: {e.Message}");
        }

        // 마지막 저장 날짜 기준 내림차순(최신순) 정렬
        saveDataList.Sort(CompareByLastSavedDescending);

        return saveDataList;
    }

    /// <summary>
    /// 현재 유효한 저장 데이터 개수 반환
    /// </summary>
    public int GetSaveCount()
    {
        return GetSaveDataList().Count;
    }

    /// <summary>
    /// 최대 저장 슬롯 제한 기준으로 신규 저장 데이터 생성 가능 여부 반환
    /// </summary>
    public bool CanCreateNewSave()
    {
        return GetSaveCount() < MaxSaveSlots;
    }

    /// <summary>
    /// 기존 저장 데이터 중 동일한 플레이어 닉네임 존재 여부 확인 (대소문자 구분 없음)
    /// </summary>
    public bool IsPlayerNameDuplicate(string playerName, string exceptSaveSlotId = null)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return false;
        }

        string trimmedName = playerName.Trim();
        List<SaveData> saveDataList = GetSaveDataList();

        for (int i = 0; i < saveDataList.Count; i++)
        {
            if (saveDataList[i].SaveSlotId == exceptSaveSlotId)
            {
                continue;
            }

            if (string.Equals(
                    saveDataList[i].PlayerName?.Trim(),
                    trimmedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 저장 데이터를 마지막 저장 시각 기준 최신순으로 정렬하기 위한 비교자
    /// </summary>
    private static int CompareByLastSavedDescending(SaveData a, SaveData b)
    {
        bool hasDateA = DateTime.TryParse(
            a.LastSavedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime dateA);
        bool hasDateB = DateTime.TryParse(
            b.LastSavedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime dateB);

        if (hasDateA && hasDateB)
        {
            return dateB.CompareTo(dateA);
        }

        if (hasDateA != hasDateB)
        {
            return hasDateB.CompareTo(hasDateA);
        }

        return string.CompareOrdinal(b.LastSavedAt, a.LastSavedAt);
    }

    /// <summary>
    /// 저장 데이터 존재 여부 확인
    /// </summary>
    public bool HasAnySaveData()
    {
        return GetSaveDataList().Count > 0;
    }

    /// <summary>
    /// 지정한 저장 데이터 삭제
    /// </summary>
    public bool DeleteSaveData(string saveSlotId)
    {
        if (string.IsNullOrWhiteSpace(saveSlotId))
        {
            return false;
        }

        try
        {
            string filePath = GetSaveFilePath(saveSlotId);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning("삭제할 저장 파일이 존재하지 않습니다.");
                return false;
            }

            File.Delete(filePath);

            // 현재 사용 중인 데이터가 삭제된 경우 메모리에서도 초기화
            if (CurrentData != null && CurrentData.SaveSlotId == saveSlotId)
            {
                CurrentData = null;
            }

            Debug.Log($"저장 데이터 삭제 성공: {saveSlotId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 데이터 삭제 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 저장 데이터 기본 검증
    /// </summary>
    private bool IsValidSaveData(SaveData data)
    {
        if (data == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(data.SaveSlotId) ||
            string.IsNullOrWhiteSpace(data.PlayerName) ||
            string.IsNullOrWhiteSpace(data.CreatedAt) ||
            string.IsNullOrWhiteSpace(data.LastSavedAt))
        {
            return false;
        }

        if (data.SaveVersion <= 0 ||
            data.SaveVersion > CurrentSaveVersion)
        {
            return false;
        }

        // 날짜 문자열이 정상적으로 파싱되는지 확인
        if (!DateTime.TryParse(data.CreatedAt, out _) ||
            !DateTime.TryParse(data.LastSavedAt, out _))
        {
            return false;
        }

        return true;
    }
}
