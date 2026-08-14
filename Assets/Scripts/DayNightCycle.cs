using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("하루가 지나는 실제 시간 (분 단위)")]
    [SerializeField] float DayDurationInMinutes;
    [Header("시작 시간대 설정")]
    [SerializeField] TimePhase StartTimePhase;
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
    [Header("렌즈 플레어 / Light 설정값")]
    [SerializeField] float LightDayIntensity;
    [SerializeField] float LightNightIntensity;
    [SerializeField] float LightSunsetIntensity;
    [SerializeField] float LensDayIntensity;
    [SerializeField] float LensSunsetIntensity;
    [SerializeField] float LensNightIntensity;
    [SerializeField] float LensDayScale;
    [SerializeField] float LensSunsetScale;
    [SerializeField] float LensNightScale;

    //환경광 색상 설정(Source : Color 일 때)
    Color SunriseColor;
    Color DayColor;
    Color SunsetColor;
    Color NightColor;

    LensFlareComponentSRP SunLensFlare;
    Light MainLight;
    float CurrentAngle;
    float StartY;
    float StartZ;
    enum TimePhase { Sunrise, Day, Sunset, Night, None}
    TimePhase CurrentPhase = TimePhase.None;

    private void Awake()
    {
        MainLight = GetComponent<Light>();
        SunLensFlare = GetComponent<LensFlareComponentSRP>();
    }

    private void Start()
    {
        MainLight.useColorTemperature = true;
        //시작할 때 인스펙터 설정한 시간대부터 시작
        CurrentAngle = GetStartAngle(StartTimePhase);
        CheckTimeOfDay(CurrentAngle);
        //시작 회전값 설정 및 변수에 저장
        transform.rotation = Quaternion.Euler(0f, -88f, 0f);
        StartZ = 0f;
        StartY = -88f;
        //설정한 Kelvin값 color값으로 변환해서 저장
        SunriseColor = Mathf.CorrelatedColorTemperatureToRGB(SunriseKelvin);
        DayColor = Mathf.CorrelatedColorTemperatureToRGB(DayKelvin);
        SunsetColor = Mathf.CorrelatedColorTemperatureToRGB(SunsetKelvin);
        NightColor = Mathf.CorrelatedColorTemperatureToRGB((NightKelvin));
    }
    private float GetStartAngle(TimePhase phase)
    {
        switch(phase)
        {
            case TimePhase.Sunrise: return 0f;
            case TimePhase.Day: return 25f;
            case TimePhase.Sunset: return 155f;
            case TimePhase.Night: return 180f;
            default: return 25f;
        }
    }

    private void Update()
    {
        //하루(360도)를 (분 * 60초)로 나누어 초당 회전 각도 계산
        float daytime = 360f / (DayDurationInMinutes * 60f);
        //시간에 따라 회전값 증가
        CurrentAngle += daytime * Time.deltaTime;
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
        // 하루 360도 기준
        // 새벽 ~ 아침
        if(angle >= 0f && angle < 25f)
        {
            ChangePhase(TimePhase.Sunrise, SunriseSkyBox);
        }
        // 아침 ~ 낮
        else if(angle >= 25f && angle < 155f)
        {
            ChangePhase(TimePhase.Day, DaySkyBox);
            float progress = Mathf.InverseLerp(25f, 55f, angle);
            RenderSettings.ambientSkyColor = Color.Lerp(SunriseColor, DayColor, progress);
            MainLight.intensity = Mathf.Lerp(LightSunsetIntensity, LightDayIntensity, progress);
            MainLight.colorTemperature = Mathf.Lerp(SunriseKelvin, DayKelvin, progress);
            SunLensFlare.intensity = Mathf.Lerp(LensSunsetIntensity, LensDayIntensity, progress);
            SunLensFlare.scale = Mathf.Lerp(LensSunsetScale, LensDayScale, progress);
        }
        // 저녁/노울
        else if (angle >= 155f && angle < 180f)
        {
            ChangePhase(TimePhase.Sunset, SunsetSkyBox);
            float progress = Mathf.InverseLerp(155f, 180f, angle);
            RenderSettings.ambientSkyColor = Color.Lerp(DayColor, SunsetColor, progress);
            MainLight.intensity = Mathf.Lerp(LightDayIntensity, LightSunsetIntensity, progress);
            MainLight.colorTemperature = Mathf.Lerp(DayKelvin, SunsetKelvin, progress);
            SunLensFlare.intensity = Mathf.Lerp(LensDayIntensity, LensSunsetIntensity, progress);
            SunLensFlare.scale = Mathf.Lerp(LensDayScale, LensSunsetScale, progress);
        }
        else
        {
            ChangePhase(TimePhase.Night, NightSkyBox);
            //노을에서 밤변경
            if (angle >= 180f && angle < 205f)
            {
                float progress = Mathf.InverseLerp(180f, 205f, angle);
                RenderSettings.ambientSkyColor = Color.Lerp(SunsetColor, NightColor, progress);
                MainLight.colorTemperature = Mathf.Lerp(SunsetKelvin, NightKelvin, progress);
                MainLight.intensity = Mathf.Lerp(LightSunsetIntensity, LightNightIntensity, progress);
                SunLensFlare.intensity = Mathf.Lerp(LensSunsetIntensity, LensNightIntensity, progress);
                SunLensFlare.scale = Mathf.Lerp(LensSunsetScale, LensNightScale, progress);
            }
            //새벽에서 아침
            else if (angle > 330f && angle < 360f)
            {
                float progress = Mathf.InverseLerp(330f, 360f, angle);
                //Lighting AmbientColor 설정
                RenderSettings.ambientSkyColor = Color.Lerp(NightColor, SunriseColor, progress);
                MainLight.colorTemperature = Mathf.Lerp(NightKelvin, SunriseKelvin, progress);
                //Light 설정
                MainLight.intensity = Mathf.Lerp(LightNightIntensity, LightSunsetIntensity, progress);
                //LensFlare 설정
                SunLensFlare.intensity = Mathf.Lerp(LensNightIntensity, LensSunsetIntensity, progress);
                SunLensFlare.scale = Mathf.Lerp(LensNightScale, LensSunsetScale, progress);
            }
        }
    }
}
