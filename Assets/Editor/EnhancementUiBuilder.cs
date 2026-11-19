#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity 에디터 상단 메뉴에서 클릭 한 번으로 무기 강화 UI 전체 계층 구조와 슬롯 프리팹을 자동 생성하고,
/// 컴포넌트의 모든 필드(SerializedField)를 연결해주는 에디터 유틸리티 빌더 클래스입니다.
/// </summary>
public static class EnhancementUiBuilder
{
    // 프로젝트 기본 TMP 한글 폰트 캐시
    private static TMP_FontAsset cachedFont;

    /// <summary>
    /// 에디터 상단 메뉴: Tools > 강화 UI 생성 (Build Enhancement UI)
    /// 씬 내에 캔버스를 확인/생성하고, 프리팹 생성, 패널 생성, 위젯 배치, 참조 연결을 순차적으로 수행합니다.
    /// </summary>
    [MenuItem("Tools/강화 UI 생성 (Build Enhancement UI)")]
    public static void BuildEnhancementUI()
    {
        // 1. 프로젝트 내의 기본 한글 TMP 폰트 로드 (Maplestory OTF Light SDF)
        cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Maplestory OTF Light SDF.asset");
        if (cachedFont == null)
        {
            // 경로에 없다면 프로젝트 내 다른 TMP 폰트 검색 후 대체
            string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (fontGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }

        // 2. 씬에 배치된 Canvas 탐색 (없을 경우 1920x1080 기준 캔버스 신규 생성)
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Main Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // 3. 무기 슬롯 프리팹 생성 또는 기존 프리팹 로드 (Assets/Prefabs/EnhanceWeaponSlotPrefab.prefab)
        GameObject slotPrefab = CreateOrUpdateSlotPrefab();

        // 4. 강화 UI 메인 패널 생성 및 크기/앵커 설정 (940 x 600, 화면 중앙 배치)
        Transform parentTransform = canvas.transform;
        UiManager uiManager = Object.FindAnyObjectByType<UiManager>();
        
        GameObject panelObj = new GameObject("EnhancementPanel", typeof(RectTransform), typeof(Image), typeof(EnhancementUi));
        panelObj.transform.SetParent(parentTransform, false);
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Enhancement UI");

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(940, 600);
        panelRect.anchoredPosition = Vector2.zero;

        // 메인 패널 배경 색상 (세련된 판타지 다크 차콜 톤)
        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.15f, 0.96f);

        EnhancementUi enhancementUi = panelObj.GetComponent<EnhancementUi>();

        // 5. 상단 헤더 영역 생성 (타이틀 '무기 강화' + 서브타이틀 + 닫기 'X' 버튼)
        CreateHeader(panelObj.transform, enhancementUi, out Button btnClose);

        // 6. 좌측 영역 생성 (보유 무기 목록 스크롤뷰, Content, 빈 목록 안내)
        CreateLeftPanel(panelObj.transform, slotPrefab, out Transform slotParent, out GameObject emptyNotice);

        // 7. 우측 영역 생성 (선택 무기 프리뷰, 공격력 비교, 성공 확률 게이지, 재료 정보)
        CreateRightPanel(panelObj.transform, out GameObject detailGroup,
            out Image weaponIcon, out TextMeshProUGUI weaponName, out TextMeshProUGUI weaponDesc,
            out TextMeshProUGUI levelText, out TextMeshProUGUI curAtkText, out TextMeshProUGUI nextAtkText,
            out TextMeshProUGUI rateText, out Slider rateSlider,
            out Image matIcon, out TextMeshProUGUI matName, out TextMeshProUGUI matCount,
            out GameObject matBadge, out TextMeshProUGUI matStatus);

        // 8. 하단 영역 생성 (결과 안내 텍스트 + 대형 [강화하기] 버튼 + 주의사항 안내문)
        CreateBottomBar(panelObj.transform, out Button btnEnhance, out TextMeshProUGUI btnEnhanceText, out TextMeshProUGUI resultText);

        // 9. EnhancementData 에셋 로드 및 EnhancementUi의 SerializedField 자동 연결
        EnhancementData dataAsset = AssetDatabase.LoadAssetAtPath<EnhancementData>("Assets/Scripts/SoData/EnhancementData.asset");

        SerializedObject so = new SerializedObject(enhancementUi);
        so.FindProperty("enhancementData").objectReferenceValue = dataAsset;
        so.FindProperty("WeaponSlotParent").objectReferenceValue = slotParent;
        so.FindProperty("WeaponSlotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("EmptyWeaponListNotice").objectReferenceValue = emptyNotice;

        so.FindProperty("WeaponDetailGroup").objectReferenceValue = detailGroup;
        so.FindProperty("SelectedWeaponIcon").objectReferenceValue = weaponIcon;
        so.FindProperty("SelectedWeaponNameText").objectReferenceValue = weaponName;
        so.FindProperty("SelectedWeaponDescText").objectReferenceValue = weaponDesc;
        so.FindProperty("CurrentEnhanceLevelText").objectReferenceValue = levelText;
        so.FindProperty("CurrentAtkText").objectReferenceValue = curAtkText;
        so.FindProperty("NextAtkText").objectReferenceValue = nextAtkText;
        so.FindProperty("SuccessRateText").objectReferenceValue = rateText;
        so.FindProperty("SuccessRateSlider").objectReferenceValue = rateSlider;

        so.FindProperty("MaterialIcon").objectReferenceValue = matIcon;
        so.FindProperty("MaterialNameText").objectReferenceValue = matName;
        so.FindProperty("MaterialCountText").objectReferenceValue = matCount;
        so.FindProperty("MaterialStatusBadge").objectReferenceValue = matBadge;
        so.FindProperty("MaterialStatusText").objectReferenceValue = matStatus;

        so.FindProperty("BtnEnhance").objectReferenceValue = btnEnhance;
        so.FindProperty("BtnEnhanceText").objectReferenceValue = btnEnhanceText;
        so.FindProperty("BtnClose").objectReferenceValue = btnClose;
        so.FindProperty("ResultNoticeText").objectReferenceValue = resultText;
        so.ApplyModifiedProperties();

        // 10. 씬의 UiManager에 EnhancementPanel 등록
        if (uiManager != null)
        {
            SerializedObject uiso = new SerializedObject(uiManager);
            SerializedProperty prop = uiso.FindProperty("EnhancementPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = panelObj;
                uiso.ApplyModifiedProperties();
                Debug.Log("[EnhancementUiBuilder] UiManager의 EnhancementPanel에 연결되었습니다.");
            }
        }

        // 11. 씬에 EnhancementSystem이 없다면 자동 생성하여 데이터 연결
        EnhancementSystem sys = Object.FindAnyObjectByType<EnhancementSystem>();
        if (sys == null)
        {
            GameObject sysObj = new GameObject("EnhancementSystem", typeof(EnhancementSystem));
            sys = sysObj.GetComponent<EnhancementSystem>();
            SerializedObject sysSo = new SerializedObject(sys);
            sysSo.FindProperty("defaultEnhancementData").objectReferenceValue = dataAsset;
            sysSo.ApplyModifiedProperties();
            Undo.RegisterCreatedObjectUndo(sysObj, "Create EnhancementSystem");
            Debug.Log("[EnhancementUiBuilder] 씬에 EnhancementSystem을 새로 생성하고 데이터를 연결했습니다.");
        }

        Selection.activeGameObject = panelObj;
        EditorUtility.SetDirty(panelObj);
        Debug.Log("<color=#00FF66><b>[EnhancementUiBuilder]</b> 무기 강화 UI가 성공적으로 생성되었습니다!</color>");
    }

    /// <summary>
    /// 무기 목록에서 사용할 개별 슬롯 프리팹을 구성하고 에셋으로 저장합니다.
    /// (아이콘, 무기명, 강화수치, 장착태그, 선택체크마크 포함)
    /// </summary>
    private static GameObject CreateOrUpdateSlotPrefab()
    {
        string dir = "Assets/Prefabs";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string prefabPath = Path.Combine(dir, "EnhanceWeaponSlotPrefab.prefab");

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            return existing;
        }

        // 슬롯 루트 오브젝트 (250 x 64 크기 버튼 컨테이너)
        GameObject root = new GameObject("EnhanceWeaponSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EnhanceWeaponSlotUi));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 64);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        Button btn = root.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.32f, 1f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        btn.colors = colors;

        // 좌측 아이콘 박스 (50 x 50) 및 무기 아이콘 Image
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(Image));
        iconBox.transform.SetParent(root.transform, false);
        RectTransform iconBoxRt = iconBox.GetComponent<RectTransform>();
        iconBoxRt.anchorMin = new Vector2(0f, 0.5f);
        iconBoxRt.anchorMax = new Vector2(0f, 0.5f);
        iconBoxRt.pivot = new Vector2(0f, 0.5f);
        iconBoxRt.anchoredPosition = new Vector2(7, 0);
        iconBoxRt.sizeDelta = new Vector2(50, 50);
        iconBox.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 1f);

        GameObject iconObj = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(iconBox.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = new Vector2(-6, -6);
        iconRt.anchoredPosition = Vector2.zero;
        Image weaponIconImg = iconObj.GetComponent<Image>();
        weaponIconImg.preserveAspect = true;

        // 무기 이름 텍스트
        GameObject nameObj = new GameObject("WeaponNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(65, -12);
        nameRt.sizeDelta = new Vector2(-95, 20);
        TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(nameText);
        nameText.text = "무기 이름";
        nameText.fontSize = 15;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;

        // 강화 단계 텍스트 (+0, 주황/금색)
        GameObject levelObj = new GameObject("EnhanceLevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        levelObj.transform.SetParent(root.transform, false);
        RectTransform levelRt = levelObj.GetComponent<RectTransform>();
        levelRt.anchorMin = new Vector2(0f, 0f);
        levelRt.anchorMax = new Vector2(1f, 0f);
        levelRt.pivot = new Vector2(0f, 0f);
        levelRt.anchoredPosition = new Vector2(65, 12);
        levelRt.sizeDelta = new Vector2(-95, 20);
        TextMeshProUGUI levelText = levelObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(levelText);
        levelText.text = "+0";
        levelText.fontSize = 14;
        levelText.fontStyle = FontStyles.Bold;
        levelText.color = new Color(1f, 0.75f, 0.2f);

        // 선택 하이라이트 및 체크마크 (SelectedHighlight)
        GameObject highlightObj = new GameObject("SelectedHighlight", typeof(RectTransform), typeof(Image));
        highlightObj.transform.SetParent(root.transform, false);
        RectTransform hlRt = highlightObj.GetComponent<RectTransform>();
        hlRt.anchorMin = Vector2.zero;
        hlRt.anchorMax = Vector2.one;
        hlRt.sizeDelta = Vector2.zero;
        Image hlImg = highlightObj.GetComponent<Image>();
        hlImg.color = new Color(1f, 0.8f, 0.2f, 0.2f);
        hlImg.raycastTarget = false;

        GameObject checkObj = new GameObject("CheckMark", typeof(RectTransform), typeof(TextMeshProUGUI));
        checkObj.transform.SetParent(highlightObj.transform, false);
        RectTransform checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(1f, 0.5f);
        checkRt.anchorMax = new Vector2(1f, 0.5f);
        checkRt.pivot = new Vector2(1f, 0.5f);
        checkRt.anchoredPosition = new Vector2(-10, 0);
        checkRt.sizeDelta = new Vector2(24, 24);
        TextMeshProUGUI checkText = checkObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(checkText);
        checkText.text = "✔";
        checkText.fontSize = 18;
        checkText.color = new Color(1f, 0.85f, 0.3f);
        highlightObj.SetActive(false);

        // '장착' 태그 뱃지 (EquippedTag)
        GameObject equippedTag = new GameObject("EquippedTag", typeof(RectTransform), typeof(Image));
        equippedTag.transform.SetParent(root.transform, false);
        RectTransform eqRt = equippedTag.GetComponent<RectTransform>();
        eqRt.anchorMin = new Vector2(1f, 1f);
        eqRt.anchorMax = new Vector2(1f, 1f);
        eqRt.pivot = new Vector2(1f, 1f);
        eqRt.anchoredPosition = new Vector2(-6, -6);
        eqRt.sizeDelta = new Vector2(46, 18);
        equippedTag.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 0.85f);

        GameObject eqTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        eqTextObj.transform.SetParent(equippedTag.transform, false);
        RectTransform eqTextRt = eqTextObj.GetComponent<RectTransform>();
        eqTextRt.anchorMin = Vector2.zero;
        eqTextRt.anchorMax = Vector2.one;
        eqTextRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI eqText = eqTextObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(eqText);
        eqText.text = "장착";
        eqText.fontSize = 11;
        eqText.alignment = TextAlignmentOptions.Center;
        eqText.color = Color.white;
        equippedTag.SetActive(false);

        // EnhanceWeaponSlotUi 스크립트 필드 바인딩
        EnhanceWeaponSlotUi slotUi = root.GetComponent<EnhanceWeaponSlotUi>();
        SerializedObject so = new SerializedObject(slotUi);
        so.FindProperty("WeaponIcon").objectReferenceValue = weaponIconImg;
        so.FindProperty("WeaponNameText").objectReferenceValue = nameText;
        so.FindProperty("EnhanceLevelText").objectReferenceValue = levelText;
        so.FindProperty("EquippedTag").objectReferenceValue = equippedTag;
        so.FindProperty("SelectedHighlight").objectReferenceValue = highlightObj;
        so.FindProperty("SlotButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        // 프리팹 에셋으로 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>
    /// 상단 헤더 영역(타이틀 및 닫기 버튼)을 생성합니다.
    /// </summary>
    private static void CreateHeader(Transform parent, EnhancementUi ui, out Button btnClose)
    {
        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(parent, false);
        RectTransform rt = headerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0, 60);
        rt.anchoredPosition = Vector2.zero;

        // 메인 타이틀: "무기 강화"
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(0f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(30, 6);
        titleRt.sizeDelta = new Vector2(300, 30);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(titleText);
        titleText.text = "무기 강화";
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.95f, 0.88f, 0.72f);

        // 영문 서브타이틀: "WEAPON ENHANCEMENT"
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0.5f);
        subRt.anchorMax = new Vector2(0f, 0.5f);
        subRt.pivot = new Vector2(0f, 0.5f);
        subRt.anchoredPosition = new Vector2(30, -14);
        subRt.sizeDelta = new Vector2(300, 18);
        TextMeshProUGUI subText = subObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(subText);
        subText.text = "WEAPON ENHANCEMENT";
        subText.fontSize = 11;
        subText.color = new Color(0.6f, 0.6f, 0.65f);

        // 우측 상단 닫기 [X] 버튼
        GameObject closeObj = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(headerObj.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 0.5f);
        closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-20, 0);
        closeRt.sizeDelta = new Vector2(36, 36);

        Image closeBg = closeObj.GetComponent<Image>();
        closeBg.color = new Color(0.25f, 0.15f, 0.15f, 0.9f);
        btnClose = closeObj.GetComponent<Button>();

        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextObj.transform.SetParent(closeObj.transform, false);
        RectTransform ctRt = closeTextObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI ct = closeTextObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(ct);
        ct.text = "✕";
        ct.fontSize = 20;
        ct.alignment = TextAlignmentOptions.Center;
        ct.color = new Color(0.9f, 0.7f, 0.7f);
    }

    /// <summary>
    /// 좌측 보유 무기 목록 스크롤뷰 영역을 생성합니다.
    /// </summary>
    private static void CreateLeftPanel(Transform parent, GameObject slotPrefab, out Transform slotParent, out GameObject emptyNotice)
    {
        GameObject leftPanel = new GameObject("LeftPanel_WeaponList", typeof(RectTransform), typeof(Image));
        leftPanel.transform.SetParent(parent, false);
        RectTransform lpRt = leftPanel.GetComponent<RectTransform>();
        lpRt.anchorMin = new Vector2(0f, 0.5f);
        lpRt.anchorMax = new Vector2(0f, 0.5f);
        lpRt.pivot = new Vector2(0f, 0.5f);
        lpRt.anchoredPosition = new Vector2(25, -20);
        lpRt.sizeDelta = new Vector2(280, 470);
        leftPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.85f);

        // 목록 타이틀
        GameObject titleObj = new GameObject("ListTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(leftPanel.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(15, -12);
        tRt.sizeDelta = new Vector2(-30, 24);
        TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(title);
        title.text = "보유 무기";
        title.fontSize = 17;
        title.fontStyle = FontStyles.Bold;
        title.color = Color.white;

        // 안내 서브타이틀
        GameObject subObj = new GameObject("ListSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(leftPanel.transform, false);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0f, 1f);
        sRt.anchoredPosition = new Vector2(15, -36);
        sRt.sizeDelta = new Vector2(-30, 18);
        TextMeshProUGUI sub = subObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(sub);
        sub.text = "강화할 무기를 선택하세요.";
        sub.fontSize = 12;
        sub.color = new Color(0.6f, 0.6f, 0.65f);

        // 스크롤뷰 (ScrollView)
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(leftPanel.transform, false);
        RectTransform sRect = scrollObj.GetComponent<RectTransform>();
        sRect.anchorMin = Vector2.zero;
        sRect.anchorMax = Vector2.one;
        sRect.pivot = new Vector2(0.5f, 0.5f);
        sRect.anchoredPosition = new Vector2(0, -30);
        sRect.sizeDelta = new Vector2(-20, -70);

        ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpRt;

        // Content (세로 자동 정렬 레이아웃)
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = cRt;
        slotParent = content.transform;

        // 무기가 없을 때 안내 텍스트
        GameObject emptyObj = new GameObject("EmptyNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
        emptyObj.transform.SetParent(leftPanel.transform, false);
        RectTransform emRt = emptyObj.GetComponent<RectTransform>();
        emRt.anchorMin = Vector2.zero;
        emRt.anchorMax = Vector2.one;
        emRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI emText = emptyObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(emText);
        emText.text = "강화 가능한 무기가 없습니다.";
        emText.fontSize = 14;
        emText.alignment = TextAlignmentOptions.Center;
        emText.color = new Color(0.6f, 0.6f, 0.65f);
        emptyNotice = emptyObj;
        emptyNotice.SetActive(false);
    }

    /// <summary>
    /// 우측 선택 무기 상세 정보 영역(프리뷰, 공격력 비교, 확률 게이지, 재료 정보)을 생성합니다.
    /// </summary>
    private static void CreateRightPanel(Transform parent, out GameObject detailGroup,
        out Image weaponIcon, out TextMeshProUGUI weaponName, out TextMeshProUGUI weaponDesc,
        out TextMeshProUGUI levelText, out TextMeshProUGUI curAtkText, out TextMeshProUGUI nextAtkText,
        out TextMeshProUGUI rateText, out Slider rateSlider,
        out Image matIcon, out TextMeshProUGUI matName, out TextMeshProUGUI matCount,
        out GameObject matBadge, out TextMeshProUGUI matStatus)
    {
        GameObject rightPanel = new GameObject("RightPanel_WeaponDetail", typeof(RectTransform), typeof(Image));
        rightPanel.transform.SetParent(parent, false);
        RectTransform rpRt = rightPanel.GetComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(1f, 0.5f);
        rpRt.anchorMax = new Vector2(1f, 0.5f);
        rpRt.pivot = new Vector2(1f, 0.5f);
        rpRt.anchoredPosition = new Vector2(-25, -20);
        rpRt.sizeDelta = new Vector2(580, 470);
        rightPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        detailGroup = rightPanel;

        // 상단 무기 프리뷰 영역 (대형 아이콘 + 무기명 + 강화도 + 설명)
        GameObject topSection = new GameObject("TopPreviewSection", typeof(RectTransform));
        topSection.transform.SetParent(rightPanel.transform, false);
        RectTransform tsRt = topSection.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 1f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.pivot = new Vector2(0.5f, 1f);
        tsRt.anchoredPosition = new Vector2(0, -15);
        tsRt.sizeDelta = new Vector2(-40, 90);

        // 대형 무기 아이콘 박스 (80 x 80)
        GameObject iconBox = new GameObject("LargeIconBox", typeof(RectTransform), typeof(Image));
        iconBox.transform.SetParent(topSection.transform, false);
        RectTransform ibRt = iconBox.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(10, 0);
        ibRt.sizeDelta = new Vector2(80, 80);
        iconBox.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 1f);

        GameObject iconImgObj = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
        iconImgObj.transform.SetParent(iconBox.transform, false);
        RectTransform iiRt = iconImgObj.GetComponent<RectTransform>();
        iiRt.anchorMin = Vector2.zero;
        iiRt.anchorMax = Vector2.one;
        iiRt.sizeDelta = new Vector2(-10, -10);
        weaponIcon = iconImgObj.GetComponent<Image>();
        weaponIcon.preserveAspect = true;

        // 무기 이름 텍스트
        GameObject nameObj = new GameObject("WeaponNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(topSection.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(110, -15);
        nRt.sizeDelta = new Vector2(-120, 30);
        weaponName = nameObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(weaponName);
        weaponName.text = "강철 장검";
        weaponName.fontSize = 22;
        weaponName.fontStyle = FontStyles.Bold;
        weaponName.color = Color.white;

        // 강화 단계 텍스트 (+5)
        GameObject lvObj = new GameObject("EnhanceLevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvObj.transform.SetParent(topSection.transform, false);
        RectTransform lRt = lvObj.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(1f, 1f);
        lRt.anchorMax = new Vector2(1f, 1f);
        lRt.pivot = new Vector2(1f, 1f);
        lRt.anchoredPosition = new Vector2(-10, -15);
        lRt.sizeDelta = new Vector2(100, 30);
        levelText = lvObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(levelText);
        levelText.text = "+5";
        levelText.fontSize = 22;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.Right;
        levelText.color = new Color(1f, 0.75f, 0.2f);

        // 무기 설명 텍스트
        GameObject descObj = new GameObject("WeaponDescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(topSection.transform, false);
        RectTransform dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0f);
        dRt.pivot = new Vector2(0f, 0f);
        dRt.anchoredPosition = new Vector2(110, 15);
        dRt.sizeDelta = new Vector2(-120, 24);
        weaponDesc = descObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(weaponDesc);
        weaponDesc.text = "견고한 강철로 만들어진 장검";
        weaponDesc.fontSize = 13;
        weaponDesc.color = new Color(0.65f, 0.65f, 0.7f);

        // 공격력 비교 박스 (현재 공격력 ▶ 강화 후 공격력)
        CreateStatBoxes(rightPanel.transform, out curAtkText, out nextAtkText);

        // 성공 확률 섹션 (게이지 바 + 퍼센트)
        CreateSuccessRateSection(rightPanel.transform, out rateText, out rateSlider);

        // 강화 재료 섹션 (재료 아이콘 + 재료명 + 보유/필요 수량 + 상태 뱃지)
        CreateMaterialSection(rightPanel.transform, out matIcon, out matName, out matCount, out matBadge, out matStatus);
    }

    /// <summary>
    /// 현재 공격력과 강화 후 공격력을 나란히 비교하는 스탯 박스를 생성합니다.
    /// </summary>
    private static void CreateStatBoxes(Transform parent, out TextMeshProUGUI curAtkText, out TextMeshProUGUI nextAtkText)
    {
        GameObject statGroup = new GameObject("StatComparisonGroup", typeof(RectTransform));
        statGroup.transform.SetParent(parent, false);
        RectTransform sgRt = statGroup.GetComponent<RectTransform>();
        sgRt.anchorMin = new Vector2(0.5f, 1f);
        sgRt.anchorMax = new Vector2(0.5f, 1f);
        sgRt.pivot = new Vector2(0.5f, 1f);
        sgRt.anchoredPosition = new Vector2(0, -120);
        sgRt.sizeDelta = new Vector2(520, 90);

        // 좌측: 현재 공격력 박스
        GameObject curBox = new GameObject("CurrentAtkBox", typeof(RectTransform), typeof(Image));
        curBox.transform.SetParent(statGroup.transform, false);
        RectTransform cbRt = curBox.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0f, 0.5f);
        cbRt.anchorMax = new Vector2(0f, 0.5f);
        cbRt.pivot = new Vector2(0f, 0.5f);
        cbRt.anchoredPosition = new Vector2(10, 0);
        cbRt.sizeDelta = new Vector2(220, 80);
        curBox.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        GameObject curLblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        curLblObj.transform.SetParent(curBox.transform, false);
        RectTransform clRt = curLblObj.GetComponent<RectTransform>();
        clRt.anchorMin = new Vector2(0f, 1f);
        clRt.anchorMax = new Vector2(1f, 1f);
        clRt.sizeDelta = new Vector2(0, 24);
        clRt.anchoredPosition = new Vector2(0, -10);
        TextMeshProUGUI curLbl = curLblObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(curLbl);
        curLbl.text = "현재 공격력";
        curLbl.fontSize = 13;
        curLbl.alignment = TextAlignmentOptions.Center;
        curLbl.color = new Color(0.65f, 0.65f, 0.7f);

        GameObject curValObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        curValObj.transform.SetParent(curBox.transform, false);
        RectTransform cvRt = curValObj.GetComponent<RectTransform>();
        cvRt.anchorMin = new Vector2(0f, 0f);
        cvRt.anchorMax = new Vector2(1f, 0f);
        cvRt.sizeDelta = new Vector2(0, 40);
        cvRt.anchoredPosition = new Vector2(0, 10);
        curAtkText = curValObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(curAtkText);
        curAtkText.text = "150";
        curAtkText.fontSize = 28;
        curAtkText.fontStyle = FontStyles.Bold;
        curAtkText.alignment = TextAlignmentOptions.Center;
        curAtkText.color = Color.white;

        // 중앙: 화살표 (▶)
        GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowObj.transform.SetParent(statGroup.transform, false);
        RectTransform aRt = arrowObj.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0.5f);
        aRt.anchorMax = new Vector2(0.5f, 0.5f);
        aRt.sizeDelta = new Vector2(40, 40);
        aRt.anchoredPosition = Vector2.zero;
        TextMeshProUGUI arrow = arrowObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(arrow);
        arrow.text = "▶";
        arrow.fontSize = 20;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.color = new Color(0.9f, 0.75f, 0.3f);

        // 우측: 강화 후 공격력 박스 (초록 틴트 배경)
        GameObject nextBox = new GameObject("NextAtkBox", typeof(RectTransform), typeof(Image));
        nextBox.transform.SetParent(statGroup.transform, false);
        RectTransform nbRt = nextBox.GetComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(1f, 0.5f);
        nbRt.anchorMax = new Vector2(1f, 0.5f);
        nbRt.pivot = new Vector2(1f, 0.5f);
        nbRt.anchoredPosition = new Vector2(-10, 0);
        nbRt.sizeDelta = new Vector2(220, 80);
        nextBox.GetComponent<Image>().color = new Color(0.12f, 0.20f, 0.16f, 1f);

        GameObject nextLblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        nextLblObj.transform.SetParent(nextBox.transform, false);
        RectTransform nlRt = nextLblObj.GetComponent<RectTransform>();
        nlRt.anchorMin = new Vector2(0f, 1f);
        nlRt.anchorMax = new Vector2(1f, 1f);
        nlRt.sizeDelta = new Vector2(0, 24);
        nlRt.anchoredPosition = new Vector2(0, -10);
        TextMeshProUGUI nextLbl = nextLblObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(nextLbl);
        nextLbl.text = "강화 후 공격력";
        nextLbl.fontSize = 13;
        nextLbl.alignment = TextAlignmentOptions.Center;
        nextLbl.color = new Color(0.65f, 0.85f, 0.7f);

        GameObject nextValObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nextValObj.transform.SetParent(nextBox.transform, false);
        RectTransform nvRt = nextValObj.GetComponent<RectTransform>();
        nvRt.anchorMin = new Vector2(0f, 0f);
        nvRt.anchorMax = new Vector2(1f, 0f);
        nvRt.sizeDelta = new Vector2(0, 40);
        nvRt.anchoredPosition = new Vector2(0, 10);
        nextAtkText = nextValObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(nextAtkText);
        nextAtkText.text = "160 <color=#66FF66>(+10)</color>";
        nextAtkText.fontSize = 28;
        nextAtkText.fontStyle = FontStyles.Bold;
        nextAtkText.alignment = TextAlignmentOptions.Center;
        nextAtkText.color = new Color(0.4f, 1f, 0.5f);
    }

    /// <summary>
    /// 다음 강화 성공 확률 표시 텍스트 및 게이지 바(Slider)를 생성합니다.
    /// </summary>
    private static void CreateSuccessRateSection(Transform parent, out TextMeshProUGUI rateText, out Slider rateSlider)
    {
        GameObject rateSection = new GameObject("SuccessRateSection", typeof(RectTransform));
        rateSection.transform.SetParent(parent, false);
        RectTransform rsRt = rateSection.GetComponent<RectTransform>();
        rsRt.anchorMin = new Vector2(0.5f, 1f);
        rsRt.anchorMax = new Vector2(0.5f, 1f);
        rsRt.pivot = new Vector2(0.5f, 1f);
        rsRt.anchoredPosition = new Vector2(0, -225);
        rsRt.sizeDelta = new Vector2(500, 50);

        // 라벨 텍스트
        GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblObj.transform.SetParent(rateSection.transform, false);
        RectTransform lRt = lblObj.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 1f);
        lRt.anchorMax = new Vector2(0.6f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.anchoredPosition = Vector2.zero;
        lRt.sizeDelta = new Vector2(0, 22);
        TextMeshProUGUI lbl = lblObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(lbl);
        lbl.text = "다음 강화 성공 확률";
        lbl.fontSize = 14;
        lbl.color = new Color(0.85f, 0.85f, 0.9f);

        // 확률 수치 텍스트 (금색 볼드)
        GameObject valObj = new GameObject("RateValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valObj.transform.SetParent(rateSection.transform, false);
        RectTransform vRt = valObj.GetComponent<RectTransform>();
        vRt.anchorMin = new Vector2(0.6f, 1f);
        vRt.anchorMax = new Vector2(1f, 1f);
        vRt.pivot = new Vector2(1f, 1f);
        vRt.anchoredPosition = Vector2.zero;
        vRt.sizeDelta = new Vector2(0, 24);
        rateText = valObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(rateText);
        rateText.text = "65%";
        rateText.fontSize = 20;
        rateText.fontStyle = FontStyles.Bold;
        rateText.alignment = TextAlignmentOptions.Right;
        rateText.color = new Color(1f, 0.8f, 0.25f);

        // 슬라이더 프로그레스 바
        GameObject sliderObj = new GameObject("RateSlider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(rateSection.transform, false);
        RectTransform slRt = sliderObj.GetComponent<RectTransform>();
        slRt.anchorMin = new Vector2(0f, 0f);
        slRt.anchorMax = new Vector2(1f, 0f);
        slRt.pivot = new Vector2(0.5f, 0f);
        slRt.anchoredPosition = Vector2.zero;
        slRt.sizeDelta = new Vector2(0, 16);

        rateSlider = sliderObj.GetComponent<Slider>();
        rateSlider.interactable = false;
        rateSlider.minValue = 0f;
        rateSlider.maxValue = 1f;
        rateSlider.value = 0.65f;

        // 슬라이더 배경
        GameObject sBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        sBg.transform.SetParent(sliderObj.transform, false);
        RectTransform sBgRt = sBg.GetComponent<RectTransform>();
        sBgRt.anchorMin = Vector2.zero;
        sBgRt.anchorMax = Vector2.one;
        sBgRt.sizeDelta = Vector2.zero;
        sBg.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

        // 슬라이더 채우기 영역 (금색 게이지)
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = new Vector2(-4, -4);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.GetComponent<RectTransform>();
        fRt.sizeDelta = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(1f, 0.75f, 0.2f, 1f);
        rateSlider.fillRect = fRt;
    }

    /// <summary>
    /// 강화에 필요한 재료 정보(아이콘, 이름, 보유량/필요량, 상태 뱃지)를 생성합니다.
    /// </summary>
    private static void CreateMaterialSection(Transform parent, out Image matIcon, out TextMeshProUGUI matName,
        out TextMeshProUGUI matCount, out GameObject matBadge, out TextMeshProUGUI matStatus)
    {
        GameObject matSection = new GameObject("MaterialSection", typeof(RectTransform));
        matSection.transform.SetParent(parent, false);
        RectTransform msRt = matSection.GetComponent<RectTransform>();
        msRt.anchorMin = new Vector2(0.5f, 1f);
        msRt.anchorMax = new Vector2(0.5f, 1f);
        msRt.pivot = new Vector2(0.5f, 1f);
        msRt.anchoredPosition = new Vector2(0, -300);
        msRt.sizeDelta = new Vector2(500, 100);

        // 헤더: "강화 재료"
        GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblObj.transform.SetParent(matSection.transform, false);
        RectTransform lRt = lblObj.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 1f);
        lRt.anchorMax = new Vector2(0.5f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.anchoredPosition = Vector2.zero;
        lRt.sizeDelta = new Vector2(0, 24);
        TextMeshProUGUI lbl = lblObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(lbl);
        lbl.text = "강화 재료";
        lbl.fontSize = 15;
        lbl.fontStyle = FontStyles.Bold;
        lbl.color = Color.white;

        // 우측 보유/필요 수량: "12 / 1"
        GameObject countObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        countObj.transform.SetParent(matSection.transform, false);
        RectTransform cRt = countObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0, 24);
        matCount = countObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(matCount);
        matCount.text = "<color=#00FF66>12</color> / 1";
        matCount.fontSize = 18;
        matCount.fontStyle = FontStyles.Bold;
        matCount.alignment = TextAlignmentOptions.Right;

        // 재료 카드 박스 배경
        GameObject card = new GameObject("MaterialCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(matSection.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0f, 0f);
        cardRt.anchorMax = new Vector2(1f, 0f);
        cardRt.pivot = new Vector2(0.5f, 0f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(0, 60);
        card.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

        // 재료 아이콘 박스 및 아이콘
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(Image));
        iconBox.transform.SetParent(card.transform, false);
        RectTransform ibRt = iconBox.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(10, 0);
        ibRt.sizeDelta = new Vector2(44, 44);
        iconBox.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

        GameObject iconObj = new GameObject("MaterialIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(iconBox.transform, false);
        RectTransform iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero;
        iRt.anchorMax = Vector2.one;
        iRt.sizeDelta = new Vector2(-6, -6);
        matIcon = iconObj.GetComponent<Image>();
        matIcon.preserveAspect = true;

        // 재료명 텍스트 ("강화석")
        GameObject nameObj = new GameObject("MaterialNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 0.5f);
        nRt.anchorMax = new Vector2(0f, 0.5f);
        nRt.pivot = new Vector2(0f, 0.5f);
        nRt.anchoredPosition = new Vector2(65, 0);
        nRt.sizeDelta = new Vector2(200, 30);
        matName = nameObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(matName);
        matName.text = "강화석";
        matName.fontSize = 16;
        matName.color = Color.white;

        // 상태 뱃지 ("[✔ 충분]")
        GameObject badge = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(card.transform, false);
        RectTransform bRt = badge.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 0.5f);
        bRt.anchorMax = new Vector2(1f, 0.5f);
        bRt.pivot = new Vector2(1f, 0.5f);
        bRt.anchoredPosition = new Vector2(-15, 0);
        bRt.sizeDelta = new Vector2(80, 28);
        badge.GetComponent<Image>().color = new Color(0.15f, 0.35f, 0.2f, 0.9f);
        matBadge = badge;

        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObj.transform.SetParent(badge.transform, false);
        RectTransform stRt = statusObj.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero;
        stRt.anchorMax = Vector2.one;
        stRt.sizeDelta = Vector2.zero;
        matStatus = statusObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(matStatus);
        matStatus.text = "✔ 충분";
        matStatus.fontSize = 13;
        matStatus.alignment = TextAlignmentOptions.Center;
        matStatus.color = new Color(0.4f, 1f, 0.5f);
    }

    /// <summary>
    /// 하단 결과 알림창과 [강화하기] 버튼 및 실패 패널티 안내문을 생성합니다.
    /// </summary>
    private static void CreateBottomBar(Transform parent, out Button btnEnhance, out TextMeshProUGUI btnEnhanceText, out TextMeshProUGUI resultText)
    {
        GameObject bottomBar = new GameObject("BottomBar", typeof(RectTransform));
        bottomBar.transform.SetParent(parent, false);
        RectTransform bbRt = bottomBar.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0f, 0f);
        bbRt.anchorMax = new Vector2(1f, 0f);
        bbRt.pivot = new Vector2(0.5f, 0f);
        bbRt.sizeDelta = new Vector2(0, 75);
        bbRt.anchoredPosition = Vector2.zero;

        // 좌측: 결과 알림창 및 텍스트
        GameObject resultBox = new GameObject("ResultNoticeBox", typeof(RectTransform), typeof(Image));
        resultBox.transform.SetParent(bottomBar.transform, false);
        RectTransform rbRt = resultBox.GetComponent<RectTransform>();
        rbRt.anchorMin = new Vector2(0f, 0.5f);
        rbRt.anchorMax = new Vector2(0f, 0.5f);
        rbRt.pivot = new Vector2(0f, 0.5f);
        rbRt.anchoredPosition = new Vector2(25, 6);
        rbRt.sizeDelta = new Vector2(400, 42);
        resultBox.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.8f);

        GameObject resTextObj = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
        resTextObj.transform.SetParent(resultBox.transform, false);
        RectTransform rtRt = resTextObj.GetComponent<RectTransform>();
        rtRt.anchorMin = Vector2.zero;
        rtRt.anchorMax = Vector2.one;
        rtRt.sizeDelta = new Vector2(-20, 0);
        resultText = resTextObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(resultText);
        resultText.text = "ⓘ 강화 결과가 여기에 표시됩니다.";
        resultText.fontSize = 13;
        resultText.alignment = TextAlignmentOptions.Left;
        resultText.color = new Color(0.7f, 0.7f, 0.75f);

        // 최하단 주의사항: "(i) 강화 실패 시 무기 파괴나 강화 단계 하락이 발생하지 않습니다."
        GameObject noticeObj = new GameObject("FailSafeNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
        noticeObj.transform.SetParent(bottomBar.transform, false);
        RectTransform nRt = noticeObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0.5f, 0f);
        nRt.anchorMax = new Vector2(0.5f, 0f);
        nRt.pivot = new Vector2(0.5f, 0f);
        nRt.anchoredPosition = new Vector2(0, 4);
        nRt.sizeDelta = new Vector2(600, 18);
        TextMeshProUGUI noticeText = noticeObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(noticeText);
        noticeText.text = "ⓘ 강화 실패 시 무기 파괴나 강화 단계 하락이 발생하지 않습니다.";
        noticeText.fontSize = 11;
        noticeText.alignment = TextAlignmentOptions.Center;
        noticeText.color = new Color(0.55f, 0.55f, 0.6f);

        // 우측: 대형 [강화하기] 버튼
        GameObject btnObj = new GameObject("BtnEnhance", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(bottomBar.transform, false);
        RectTransform bRt = btnObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 0.5f);
        bRt.anchorMax = new Vector2(1f, 0.5f);
        bRt.pivot = new Vector2(1f, 0.5f);
        bRt.anchoredPosition = new Vector2(-25, 6);
        bRt.sizeDelta = new Vector2(230, 48);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.85f, 0.55f, 0.12f, 1f);

        btnEnhance = btnObj.GetComponent<Button>();
        ColorBlock cb = btnEnhance.colors;
        cb.normalColor = new Color(0.85f, 0.55f, 0.12f, 1f);
        cb.highlightedColor = new Color(1f, 0.65f, 0.2f, 1f);
        cb.pressedColor = new Color(0.7f, 0.45f, 0.1f, 1f);
        cb.disabledColor = new Color(0.3f, 0.3f, 0.35f, 0.7f);
        btnEnhance.colors = cb;

        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRt = btnTextObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;
        btnEnhanceText = btnTextObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(btnEnhanceText);
        btnEnhanceText.text = "강화하기";
        btnEnhanceText.fontSize = 20;
        btnEnhanceText.fontStyle = FontStyles.Bold;
        btnEnhanceText.alignment = TextAlignmentOptions.Center;
        btnEnhanceText.color = Color.white;
    }

    /// <summary>
    /// TextMeshPro 텍스트 컴포넌트에 프로젝트 기본 한글 폰트를 적용합니다.
    /// </summary>
    private static void ApplyFont(TextMeshProUGUI text)
    {
        if (text != null && cachedFont != null)
        {
            text.font = cachedFont;
        }
    }
}
#endif
