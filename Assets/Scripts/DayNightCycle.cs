using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class DayNightCycle : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("하루가 지나는 속도 (값이 클수록 빠름")]
    [SerializeField] float DayTime;
    [Header("시간대별 Kelvin 프리셋")]
    [SerializeField] float DayKelvin;
    [SerializeField] float SunsetKelvin;
    [SerializeField] float SunriseKelvin;
    [SerializeField] float NightKelvin;
    [Header("스카이박스")]
    [SerializeField] Material DaySkyBox;
    [SerializeField] Material NightSkyBox;
    [SerializeField] Material SunsetSkyBox;
    [SerializeField] Material SunriseSkyBox;
    [Header("렌즈 플레어 / Light 밝기 목표치")]
    [SerializeField] float LightDayIntensity;
    [SerializeField] float LightNightIntensity;
    [SerializeField] float LightSunsetIntensity;
    [SerializeField] float LensDayIntensity;
    [SerializeField] float LensSunsetIntensity;
    [SerializeField] float LensNightIntensity;

    LensFlareComponentSRP SunLensFlare;
    Light MainLight;
    float CurrentAngle;
    float StartY;
    float StartZ;
    enum TimePhase { Sunrise, Day, Sunset, Night, None}
    TimePhase CurrentPhase = TimePhase.None;

    private void Start()
    {
        MainLight = GetComponent<Light>();
        SunLensFlare = GetComponent<LensFlareComponentSRP>();

        MainLight.useColorTemperature = true;
        //시작할 때 새벽부터 시작
        RenderSettings.skybox = SunriseSkyBox;
        MainLight.intensity = LightNightIntensity;
        MainLight.colorTemperature = 20000f;
        transform.rotation = Quaternion.Euler(0f, -88f, 0f);
        //시작 회전값 설정 및 변수에 저장
        StartZ = 0f;
        StartY = -88f;
        CurrentAngle = 0f;
    }

    private void Update()
    {
        //시간에 따라 회전값 증가
        CurrentAngle += DayTime * Time.deltaTime;
        if (CurrentAngle > 360f)
        {
            CurrentAngle = 0f;
        }
        transform.rotation = Quaternion.Euler(CurrentAngle, StartY, StartZ);
        //현재 각도를 계산하여 낮/밤 체크
        CheckTimeOfDay(CurrentAngle);
    }
    //스카이박스를 변경시 1번만 실행하는 함수
    private void ChangePhase(TimePhase phase, Material skybox)
    {
        if(CurrentPhase != phase)
        {
            CurrentPhase = phase;
            RenderSettings.skybox = skybox;
        }
    }

    private void CheckTimeOfDay(float angle)
    {
        // 새벽 ~ 아침: 0~20
        if(angle >= 0f && angle < 25f)
        {
            SunLensFlare.enabled = true;
            ChangePhase(TimePhase.Sunrise, SunriseSkyBox);
            //시간에 따른 빛의 변화
            float progress = Mathf.InverseLerp(0f, 25f, angle);
            MainLight.intensity = Mathf.Lerp(LightNightIntensity, LightSunsetIntensity, progress);
            MainLight.colorTemperature = Mathf.Lerp(SunriseKelvin, DayKelvin, progress);
            SunLensFlare.intensity = Mathf.Lerp(LensNightIntensity, LensSunsetIntensity, progress);
        }
        // 낮 ~ 저녁
        else if (angle >= 25f && angle < 155f)
        {
            ChangePhase(TimePhase.Day, DaySkyBox);
            float progress = Mathf.InverseLerp(25f, 155f, angle);
            MainLight.intensity = Mathf.Lerp(LightSunsetIntensity, LightDayIntensity, progress);
            MainLight.colorTemperature = Mathf.Lerp(DayKelvin, SunsetKelvin, progress);
            SunLensFlare.intensity = Mathf.Lerp(LensSunsetIntensity, LensDayIntensity, progress);
        }
        // 저녁/노울
        else if (angle >= 155f && angle < 180f)
        {
            ChangePhase(TimePhase.Sunset, SunsetSkyBox);
            float progress = Mathf.InverseLerp(155f, 180f, angle);
            MainLight.intensity = Mathf.Lerp(LightDayIntensity, LightSunsetIntensity, progress);
            MainLight.colorTemperature = Mathf.Lerp(SunsetKelvin, NightKelvin, progress);
            SunLensFlare.intensity = Mathf.Lerp(LensDayIntensity, LensSunsetIntensity, progress);
        }
        // 밤 ~ 아침
        else
        {
            SunLensFlare.enabled = false;
            ChangePhase(TimePhase.Night, NightSkyBox);
            float progress = Mathf.InverseLerp(180f, 360f, angle);
            MainLight.intensity = Mathf.Lerp(LightNightIntensity, LightSunsetIntensity, progress);
        }
    }
}
