using UnityEngine;
using DG.Tweening; // DOTween 에셋이 필요합니다.
using System.Collections;
using Random = UnityEngine.Random; // System.Random과 충돌 방지

/// <summary>
/// [수정됨] 카드 프리팹에 부착하여,
/// isOctopus 플래그에 따라 '물고기' 또는 '문어'의 움직임을 수행합니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CardBehavior : MonoBehaviour
{
    [Header("공통 동작 설정")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip siruSound;
    [SerializeField] private ParticleSystem spawnParticle;
    [SerializeField] private GameObject informationUI;
    [SerializeField] private Transform fish; // 물고기/문어의 Transform

    // [신규] 물고기/문어 동작 모드 선택
    [Header("동작 모드")]
    [Tooltip("true이면 문어 움직임(상하+스케일), false이면 물고기 움직임(3D+회전)을 합니다.")]
    [SerializeField] private bool isOctopus = false;

    [Header("물고기 움직임 설정 (isOctopus = false)")]
    [Tooltip("물고기가 움직일 수 있는 최대 반경 (X: 좌우, Y: 상하, Z: 앞뒤)")]
    [SerializeField] private Vector3 swimAreaSize = new Vector3(3f, 3f, 3f);
    [Tooltip("한 번 움직이는 데 걸리는 최소/최대 시간")]
    [SerializeField] private float minSwimDuration = 1.0f;
    [SerializeField] private float maxSwimDuration = 2.5f;
    [Tooltip("상하 이동 시 위/아래를 보는 최대 각도 (X축 Pitch)")]
    [SerializeField] private float maxPitchAngle = 10f;
    [Tooltip("방향을 트는(Roll) 데 걸리는 시간")]
    [SerializeField] private float turnDuration = 0.5f;

    [Header("문어 움직임 설정 (isOctopus = true)")]
    [Tooltip("문어가 아래로 내려가는 시간")]
    [SerializeField] private float moveDownDuration = 1.5f;
    [Tooltip("문어가 위로 올라오는 시간")]
    [SerializeField] private float moveUpDuration = 0.2f;
    [Tooltip("문어가 작아졌을 때의 크기 배율 (예: 0.7)")]
    [SerializeField] private float shrinkScale = 0.7f;
    // (참고: swimAreaSize.y 값이 문어가 내려가는 깊이로 재활용됩니다)

    private AudioSource audioSource;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale; // [신규] 문어 크기 조절을 위해 추가
    private bool isInitialized = false;
    private Coroutine _behaviorCoroutine; // [이름 변경] _swimCoroutine -> _behaviorCoroutine

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!isInitialized)
        {
            initialLocalPosition = fish.localPosition;
            initialLocalRotation = fish.localRotation;
            initialLocalScale = fish.localScale; // [신규] 초기 스케일 저장
            isInitialized = true;
        }
    }

    void OnEnable()
    {
        // 0. 모든 트윈과 코루틴을 안전하게 정리
        StopAllCoroutines();
        fish.DOKill(); 

        // 1. 소리 재생
        StartCoroutine(PlayAudio());

        // 2. 파티클 출력
        if (spawnParticle != null)
        {
            spawnParticle.Play();
        }
        if( informationUI != null)
            informationUI.SetActive(true);

        // 3. 위치/회전/크기를 초기 상태로 리셋
        fish.localPosition = initialLocalPosition;
        fish.localRotation = initialLocalRotation;
        fish.localScale = initialLocalScale; // [신규] 스케일 리셋

        // 4. [핵심 수정] 모드에 맞는 동작 코루틴 시작
        if (isOctopus)
        {
            _behaviorCoroutine = StartCoroutine(OctopusLoop());
        }
        else
        {
            _behaviorCoroutine = StartCoroutine(SwimLoop());
        }
    }

    void OnDisable()
    {
        // 1. 코루틴 중지
        if (_behaviorCoroutine != null)
        {
            StopCoroutine(_behaviorCoroutine);
            _behaviorCoroutine = null;
        }
        StopAllCoroutines(); // PlayAudio 코루틴도 중지

        // 2. 파티클 중지
        if (spawnParticle != null)
        {
            spawnParticle.Stop();
        }
        if( informationUI != null)
            informationUI.SetActive(false);

        // 3. 모든 DOTween 중지 및 리셋
        fish.DOKill(); 
        fish.localPosition = initialLocalPosition;
        fish.localRotation = initialLocalRotation;
        fish.localScale = initialLocalScale; // [신규] 스케일 리셋
    }

    IEnumerator PlayAudio()
    {
        if (spawnSound != null)
        {
            audioSource.PlayOneShot(spawnSound);
            yield return new WaitForSeconds(spawnSound.length - 1f);
        }
        if (siruSound != null)
        {
            audioSource.PlayOneShot(siruSound);
        }
    }

    #region --- 물고기 움직임 (isOctopus = false) ---

    /// <summary>
    /// [물고기 모드] 3D 이동과 Z축(Roll)/X축(Pitch) 회전을 순차적으로 반복
    /// (이 코드는 변경되지 않았습니다)
    /// </summary>
    private IEnumerator SwimLoop()
    {
        while (true)
        {
            float newX = initialLocalPosition.x + Random.Range(-swimAreaSize.x, swimAreaSize.x);
            float newY = initialLocalPosition.y + Random.Range(-swimAreaSize.y, swimAreaSize.y);
            float newZ = initialLocalPosition.z + Random.Range(-swimAreaSize.z, swimAreaSize.z);
            float duration = Random.Range(minSwimDuration, maxSwimDuration);
            Vector3 targetPos = new Vector3(newX, newY, newZ);
            Vector3 currentPos = fish.localPosition;
            
            Vector2 directionXZ = new Vector2(targetPos.x - currentPos.x, targetPos.z - currentPos.z);
            float targetZRoll = initialLocalRotation.eulerAngles.z;
            if (directionXZ.sqrMagnitude > 0.01f)
            {
                float worldYawAngle = Mathf.Atan2(directionXZ.x, directionXZ.y) * Mathf.Rad2Deg;
                targetZRoll = (180f + worldYawAngle + 360f) % 360f; 
            }

            float directionY = targetPos.y - currentPos.y;
            float distanceXZ = directionXZ.magnitude;
            float targetXPitch = initialLocalRotation.eulerAngles.x;
            if (distanceXZ > 0.01f && Mathf.Abs(directionY) > 0.01f)
            {
                float pitchAngle = Mathf.Atan2(directionY, distanceXZ) * Mathf.Rad2Deg;
                targetXPitch = -90f + Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
            }
            else if (Mathf.Abs(directionY) > 0.01f)
            {
                float pitchAngle = Mathf.Sign(directionY) * maxPitchAngle;
                targetXPitch = -90f + Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
            }
            
            Quaternion targetRotation = Quaternion.Euler(targetXPitch, 0, targetZRoll);

            Sequence swimSequence = DOTween.Sequence();
            swimSequence.Insert(0, fish.DOLocalMove(targetPos, duration).SetEase(Ease.InOutSine));
            swimSequence.Insert(0, fish.DOLocalRotateQuaternion(targetRotation, turnDuration).SetEase(Ease.OutSine));
            swimSequence.Insert(duration - turnDuration, fish.DOLocalRotateQuaternion(initialLocalRotation, turnDuration).SetEase(Ease.InSine));
            
            yield return swimSequence.WaitForCompletion();
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }
    
    #endregion
    
    #region --- 문어 움직임 (isOctopus = true) ---

    /// <summary>
    /// [신규 - 문어 모드] Y축 이동(상하)과 크기(Scale) 조절을 반복합니다.
    /// </summary>
    private IEnumerator OctopusLoop()
    {
        // Y축 이동과 스케일을 위한 목표값 설정
        Vector3 upPos = initialLocalPosition;
        // swimAreaSize.y를 '내려가는 깊이'로 재활용합니다.
        Vector3 downPos = initialLocalPosition - new Vector3(0, swimAreaSize.y, 0); 
        Vector3 bigScale = initialLocalScale;
        Vector3 smallScale = initialLocalScale * shrinkScale;

        // OnDisable에 의해 이 코루틴이 중지될 때까지 무한 반복
        while (true)
        {
            // --- 1. 아래로 내려가면서 작아지기 (1.5초) ---
            Sequence downSequence = DOTween.Sequence();
            downSequence.Insert(0, 
                fish.DOLocalMove(downPos, moveDownDuration)
                    .SetEase(Ease.InOutSine) // 천천히
            );
            downSequence.Insert(0, 
                fish.DOScale(smallScale, moveDownDuration)
                    .SetEase(Ease.InOutSine)
            );
            
            yield return downSequence.WaitForCompletion(); // 1.5초 대기

            // --- 2. 위로 올라오면서 커지기 (0.2초) ---
            Sequence upSequence = DOTween.Sequence();
            upSequence.Insert(0, 
                fish.DOLocalMove(upPos, moveUpDuration)
                    .SetEase(Ease.OutQuad) // 쑥 올라오는 느낌
            );
            upSequence.Insert(0, 
                fish.DOScale(bigScale, moveUpDuration)
                    .SetEase(Ease.OutQuad)
            );
            
            yield return upSequence.WaitForCompletion(); // 0.2초 대기
            
            // --- (선택) 다음 루프 전 잠시 대기 ---
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }

    #endregion
}