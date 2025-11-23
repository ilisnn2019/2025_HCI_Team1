using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class UnderwaterEffectsManager : MonoBehaviour
{
    // 싱글톤 인스턴스 (씬 내에서 유일해야 함)
    public static UnderwaterEffectsManager Instance { get; private set; }

    #region Public Variables (Inspector Settings)

    [Header("Global Settings")]
    public bool EnableUnderwaterEffects = true;
    public float SeaLevel = 50f; // 수면 높이
    
    // 플레이어 카메라 (Inspector에서 수동 할당 권장)
    public Transform PlayerCamera; 
    
    [Tooltip("수중 전환 시 활성화될 파티클 시스템 (예: 물방울 버블)")]
    public GameObject UnderwaterParticles;
    
    [Tooltip("수중 전환 시 활성화될 Post-Processing 볼륨 (Built-in/URP용)")]
    public GameObject UnderwaterPostFX;
    
    [Tooltip("수면 근처 전환 효과용 Post-Processing 볼륨")]
    public GameObject UnderwaterTransitionPostFX; 

    public bool IsUnderwater { get; private set; }
    private bool m_isStartingUnderwater = false; // 시작 시 수중에 있는지 여부

    [Header("Caustics (물결 그림자)")]
    public bool UseCaustics = true;
    
    [Tooltip("씬의 주 광원 (Directional Light)")]
    public Light MainLight; 
    
    [Range(1f, 100f)]
    public float CausticSize = 15f; // 광원 쿠키 크기
    
    [Tooltip("물결 애니메이션에 사용될 텍스처 리스트")]
    public List<Texture2D> CausticTextures = new List<Texture2D>();
    public int FramesPerSecond = 24; // Caustics 애니메이션 재생 속도

    [Header("Fog (안개)")]
    public bool SupportFog = true;
    
    [Tooltip("깊이에 따른 안개 색상 변화 그라디언트 (0.0=깊은 곳, 1.0=표면)")]
    public Gradient FogColorGradient;
    
    public float FogDepth = 100f; // 안개 색상 변화가 적용될 최대 깊이
    public float FogDistance = 45f; // 수중 안개의 끝 거리 (End Distance)
    public float NearFogDistance = -4f; // 수중 안개의 시작 거리 (Start Distance)
    public float FogDensity = 0.045f; // 수중 안개 밀도
    public Color FogColorMultiplier = Color.black; // 안개 색상에 추가적으로 곱해지는 값

    [Header("Audio")]
    [Range(0f, 1f)]
    public float PlaybackVolume = 0.5f;
    public AudioClip SubmergeSoundFXDown; // 잠수 시 사운드
    public AudioClip SubmergeSoundFXUp;   // 부상 시 사운드
    public AudioClip UnderwaterSoundFX;   // 지속적인 수중 배경 사운드

    [Tooltip("수면 VFX (예: 물 튀김) 리스트. 수중 진입 시 비활성화됩니다.")]
    public List<VisualEffect> SurfaceVisualEffects = new List<VisualEffect>();

    #endregion

    #region Private Variables

    private int m_indexNumber = 0; // Caustics 텍스처 인덱스
    private AudioSource m_audioSource;
    private AudioSource m_audioSourceUnderwater;
    private ParticleSystem m_underwaterParticleSystem;
    private bool m_surfaceSetup = false;
    private bool m_underwaterSetup = false;

    // 표면 안개 설정을 저장할 변수 (수중 진입 전 상태 복원용)
    private Color m_surfaceFogColor;
    private float m_surfaceFogDensity;
    private float m_surfaceFogStartDistance;
    private float m_surfaceFogEndDistance = -99.0f; 

    #endregion

    #region Unity Functions

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 1. 카메라 찾기
        if (PlayerCamera == null)
        {
            if (Camera.main != null)
            {
                PlayerCamera = Camera.main.transform; // 유니티 표준 메인 카메라 사용
            }
        }
        
        // 2. 주 광원 찾기 (태그 또는 씬 전체 검색)
        if (MainLight == null)
        {
            MainLight = FindAnyObjectByType<Light>(); 
            // 💡 주: 실제 환경에서는 Directional Light에 태그를 달아 찾거나,
            // 별도의 환경 관리 컴포넌트에서 주입받는 것이 더 안정적입니다.
        }

        // 3. 초기 수중 상태 판별
        if (PlayerCamera != null)
        {
            m_isStartingUnderwater = PlayerCamera.position.y <= SeaLevel;
        }

        // 4. 오디오 소스 설정
        if (m_audioSource == null) m_audioSource = GetAudioSource();
        if (m_audioSourceUnderwater == null) m_audioSourceUnderwater = GetAudioSource();
        
        if (m_audioSourceUnderwater != null)
        {
            m_audioSourceUnderwater.clip = UnderwaterSoundFX;
            m_audioSourceUnderwater.loop = true;
            m_audioSourceUnderwater.volume = PlaybackVolume;
            m_audioSourceUnderwater.Stop();
        }

        // 5. 파티클 시스템 설정
        if (UnderwaterParticles != null)
        {
            m_underwaterParticleSystem = UnderwaterParticles.GetComponent<ParticleSystem>();
            if (m_underwaterParticleSystem != null)
            {
                m_underwaterParticleSystem.Stop();
            }
            UnderwaterParticles.SetActive(false);
        }

        // 6. 초기 표면 안개 설정 저장 및 초기 수중 시스템 설정
        UpdateSurfaceFogSettings();
        if (m_isStartingUnderwater)
        {
            IsUnderwater = SetupWaterSystems(true, m_isStartingUnderwater);
            SetVisualEffectsState(SurfaceVisualEffects);
            m_underwaterSetup = true;
            m_surfaceSetup = false;
        }
        else
        {
            // 시작 시 수면 위인 경우, 표면 설정을 준비합니다.
            m_underwaterSetup = false;
            m_surfaceSetup = true;
        }

        // 7. Post FX 오브젝트 초기 활성화/비활성화
        if (Application.isPlaying)
        {
            if (UnderwaterPostFX != null) UnderwaterPostFX.SetActive(IsUnderwater);
            if (UnderwaterTransitionPostFX != null) UnderwaterTransitionPostFX.SetActive(true);
        }
        
        // 8. 기본 그라디언트 설정이 없으면 생성
        if (FogColorGradient.colorKeys.Length == 0)
        {
            FogColorGradient = CreateDefaultGradient();
        }
    }

    private void OnEnable()
    {
        // 씬 로딩 시 인스턴스 재할당 방지
        if (Instance == null) Instance = this; 
        
        if (m_audioSource != null) m_audioSource.playOnAwake = false;
        if (m_audioSourceUnderwater != null) m_audioSourceUnderwater.playOnAwake = false;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // 카메라가 없으면 오류 메시지 출력 후 함수 종료
        if (PlayerCamera == null)
        {
            Debug.LogError("Player Camera is missing. Please assign the camera or ensure the main camera is tagged 'MainCamera'.");
            return;
        }

        if (EnableUnderwaterEffects)
        {
            if (PlayerCamera.position.y > SeaLevel)
            {
                // 1. 플레이어가 수면 위인 경우 (표면 설정)
                if (!m_surfaceSetup)
                {
                    IsUnderwater = SetupWaterSystems(false);
                    SetVisualEffectsState(SurfaceVisualEffects); // 수면 VFX 활성화
                    m_underwaterSetup = false;
                    m_surfaceSetup = true;
                }
            }
            else
            {
                // 2. 플레이어가 수면 아래인 경우 (수중 설정)
                if (!m_underwaterSetup)
                {
                    IsUnderwater = SetupWaterSystems(true);
                    SetVisualEffectsState(SurfaceVisualEffects); // 수면 VFX 비활성화
                    m_underwaterSetup = true;
                    m_surfaceSetup = false;
                }
                
                // 수중에서는 안개 및 Post FX를 매 프레임 업데이트하여 깊이에 따른 변화 적용
                UpdateUnderwaterFog();
                UpdateUnderwaterPostFX();
            }
        }
        else
        {
            // 효과가 비활성화된 경우, 모든 수중 FX를 비활성화합니다.
            DisableUnderwaterFX();
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 표면 안개 설정으로 복원
        UpdateSurfaceFog(false);
    }
    
    #endregion

    #region Functions (Logic)

    /// <summary>
    /// 수중/표면 전환 시 파티클, 오디오, Caustics를 설정합니다.
    /// </summary>
    /// <param name="isUnderwater">true면 수중 효과 활성화, false면 비활성화</param>
    private bool SetupWaterSystems(bool isUnderwater, bool startingUnderwater = false)
    {
        // 🚨 주: 에디터 실행 방지 (Update에서 Application.isPlaying을 이미 체크했으나, 안전을 위해 남겨둠)
        if (!Application.isPlaying) return false;

        // 시작 시스템이 아니거나(게임 중 전환), 시작 시 수중 상태가 아닌 경우에만 사운드 재생
        if (!startingUnderwater)
        {
            if (isUnderwater)
            {
                // 잠수: Caustics 시작, 파티클/오디오 활성화
                if (SubmergeSoundFXDown != null)
                {
                    m_audioSource.PlayOneShot(SubmergeSoundFXDown, PlaybackVolume);
                }

                if (UseCaustics && CausticTextures.Count > 0)
                {
                    // Caustics 애니메이션 반복 호출 시작
                    InvokeRepeating("CausticsAnimation", 0f, 1f / FramesPerSecond);
                }
                
                if (UnderwaterParticles != null)
                {
                    UnderwaterParticles.SetActive(true);
                    m_underwaterParticleSystem.Play();
                }
                if (m_audioSourceUnderwater != null) m_audioSourceUnderwater.Play();
                if (UnderwaterPostFX != null) UnderwaterPostFX.SetActive(true);
            }
            else
            {
                // 부상: Caustics 중지, 파티클/오디오 비활성화, 안개 복원
                if (SubmergeSoundFXUp != null)
                {
                    m_audioSource.PlayOneShot(SubmergeSoundFXUp, PlaybackVolume);
                }

                CancelInvoke("CausticsAnimation");
                
                if (MainLight != null) MainLight.cookie = null; // 쿠키 텍스처 제거

                if (SupportFog) UpdateSurfaceFog(); // 표면 안개로 복원

                if (m_underwaterParticleSystem != null) m_underwaterParticleSystem.Stop();
                if (UnderwaterParticles != null) UnderwaterParticles.SetActive(false);
                if (m_audioSourceUnderwater != null) m_audioSourceUnderwater.Stop();
                if (UnderwaterPostFX != null) UnderwaterPostFX.SetActive(false);
            }
        }

        return isUnderwater;
    }
    
    /// <summary>
    /// 수중 효과를 완전히 비활성화하고 표면 상태로 복원합니다. (EnableUnderwaterEffects = false일 때 호출)
    /// </summary>
    private void DisableUnderwaterFX()
    {
        // 플레이어가 수중일 때만 Submerge Up 사운드 재생
        if (PlayerCamera.position.y < SeaLevel && SubmergeSoundFXUp != null)
        {
            m_audioSource.PlayOneShot(SubmergeSoundFXUp, PlaybackVolume);
        }

        CancelInvoke("CausticsAnimation"); // Caustics 애니메이션 중지

        if (MainLight != null)
        {
            MainLight.cookie = null; // 광원 쿠키 제거
        }

        if (SupportFog)
        {
            UpdateSurfaceFog(); // 표면 안개 설정으로 복원
        }
        
        // VFX 및 Post FX 비활성화
        SetVisualEffectsState(SurfaceVisualEffects); // VFX 상태 복원 (수면 위 상태로)

        if (m_underwaterParticleSystem != null) m_underwaterParticleSystem.Stop();
        if (UnderwaterParticles != null) UnderwaterParticles.SetActive(false);
        if (m_audioSourceUnderwater != null) m_audioSourceUnderwater.Stop();
        
        if (UnderwaterPostFX != null) UnderwaterPostFX.SetActive(false);
        if (UnderwaterTransitionPostFX != null) UnderwaterTransitionPostFX.SetActive(false);
        
        IsUnderwater = false;
        m_underwaterSetup = false;
        m_surfaceSetup = true;
    }

    /// <summary>
    /// Caustics (물결 그림자) 애니메이션을 업데이트합니다.
    /// </summary>
    private void CausticsAnimation()
    {
        if (MainLight != null && CausticTextures.Count > 0)
        {
            MainLight.cookieSize = CausticSize;
            MainLight.cookie = CausticTextures[m_indexNumber];
            m_indexNumber++;

            if (m_indexNumber >= CausticTextures.Count)
            {
                m_indexNumber = 0;
            }
        }
    }

    /// <summary>
    /// 수중 안개 색상 및 밀도를 플레이어의 깊이에 따라 업데이트합니다. (URP/Built-in용)
    /// </summary>
    private void UpdateUnderwaterFog()
    {
        if (!SupportFog || FogColorGradient.colorKeys.Length == 0) return;

        // 수면으로부터 깊이를 0~1 사이 값으로 계산 (0.0=깊은 곳, 1.0=표면)
        float depthDistance = Mathf.Clamp01((SeaLevel - PlayerCamera.position.y) / FogDepth);
        
        // 1. 색상 계산
        Color fogColor = FogColorGradient.Evaluate(depthDistance);
        
        // 주 광원 색상 적용 (기존 Gaia 로직 단순화)
        if (MainLight != null)
        {
             fogColor *= MainLight.color;
        }
        
        // 색상 승수 적용 (Clamp01을 통해 색상 값을 0~1 범위로 유지)
        fogColor.r = Mathf.Clamp01(fogColor.r + FogColorMultiplier.r);
        fogColor.g = Mathf.Clamp01(fogColor.g + FogColorMultiplier.g);
        fogColor.b = Mathf.Clamp01(fogColor.b + FogColorMultiplier.b);

        // 2. Render Settings에 적용 (Built-in/URP 기본 안개)
        RenderSettings.fog = true; // 안개가 꺼져있을 경우를 대비하여 활성화
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.fogStartDistance = NearFogDistance;
        RenderSettings.fogEndDistance = FogDistance;
    }

    /// <summary>
    /// 수중 PostFX를 업데이트합니다. 
    /// </summary>
    private void UpdateUnderwaterPostFX()
    {
        // URP에서 Volume 컴포넌트의 Weight나 Post-Process 파라미터를 깊이에 따라
        // 동적으로 조절하는 로직이 필요할 수 있습니다. 현재는 활성화/비활성화만 유지합니다.
    }

    /// <summary>
    /// 현재 표면의 안개 설정을 저장합니다.
    /// </summary>
    public void UpdateSurfaceFogSettings()
    {
        m_surfaceFogColor = RenderSettings.fogColor;
        m_surfaceFogDensity = RenderSettings.fogDensity;
        m_surfaceFogStartDistance = RenderSettings.fogStartDistance;
        m_surfaceFogEndDistance = RenderSettings.fogEndDistance;
    }

    /// <summary>
    /// 저장된 표면 안개 설정으로 복원합니다.
    /// </summary>
    /// <param name="restore">true면 복원, false면 아무것도 하지 않음 (기존 로직 유지)</param>
    private void UpdateSurfaceFog(bool restore = true)
    {
        if (restore)
        {
            if (m_surfaceFogEndDistance == -99.0f) return; // 저장된 값이 없으면 복원하지 않음

            RenderSettings.fogColor = m_surfaceFogColor;
            RenderSettings.fogDensity = m_surfaceFogDensity;
            RenderSettings.fogStartDistance = m_surfaceFogStartDistance;
            RenderSettings.fogEndDistance = m_surfaceFogEndDistance;
        }
    }
    
    /// <summary>
    /// VFX 리스트의 상태를 수중 여부에 따라 변경합니다.
    /// </summary>
    /// <param name="visualEffects">제어할 VisualEffect 리스트</param>
    public void SetVisualEffectsState(List<VisualEffect> visualEffects)
    {
        if (visualEffects.Count > 0)
        {
            foreach (VisualEffect visualEffect in visualEffects)
            {
                if (visualEffect != null)
                {
                    if (IsUnderwater)
                    {
                        visualEffect.enabled = false;
                        visualEffect.Stop();
                    }
                    else
                    {
                        visualEffect.enabled = true;
                        visualEffect.Play();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 이 GameObject에 오디오 소스를 추가하여 반환합니다.
    /// </summary>
    private AudioSource GetAudioSource()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        return audioSource;
    }
    
    /// <summary>
    /// 기본 안개 그라디언트를 생성합니다. (0.0=깊은 곳, 1.0=표면)
    /// </summary>
    private Gradient CreateDefaultGradient()
    {
        Gradient gradient = new Gradient();

        // 0C233A (깊은 바다), 5686BC (중간), 5C9BE0 (표면 근처) 색상 값
        Color deepColor = new Color(0.047f, 0.137f, 0.227f);
        Color midColor = new Color(0.337f, 0.525f, 0.737f);
        Color surfaceColor = new Color(0.36f, 0.607f, 0.882f);
        
        // 0.0f = 가장 깊은 곳, 1.0f = 가장 얕은 곳 (SeaLevel 근처)
        GradientColorKey[] colorKey = new GradientColorKey[3];
        colorKey[0].color = deepColor;  
        colorKey[0].time = 0.0f;
        colorKey[1].color = midColor;
        colorKey[1].time = 0.5f;
        colorKey[2].color = surfaceColor; 
        colorKey[2].time = 1f;

        // 알파는 모두 1.0f (불투명)
        GradientAlphaKey[] alphaKey = new GradientAlphaKey[3];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 0.5f;
        alphaKey[2].alpha = 1.0f;
        alphaKey[2].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);
        return gradient;
    }
    
    #endregion
}