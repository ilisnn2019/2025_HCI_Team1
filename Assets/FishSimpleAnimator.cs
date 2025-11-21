using UnityEngine;

public enum SwimMode
{
    Fish,       // 물고기 (좌우 S자 유영)
    Dolphin     // 돌고래 (상하 웨이브 + 지느러미 웨이브)
}

public class FishSimpleAnimator : MonoBehaviour
{
    [Header("모드 설정")]
    public SwimMode currentMode = SwimMode.Fish;

    [Header("공통 (몸통) 설정")]
    public float swimSpeed = 3.0f;
    public float rotationAngle = 15.0f;
    public float waveOffset = 0.5f;

    [Header("몸통 뼈대 (순서대로)")]
    public Transform[] frontBones;
    public Transform[] backBones;

    [Header("돌고래 지느러미 설정")]
    [Tooltip("지느러미 뼈들을 몸통 쪽에서 바깥쪽 순서로 넣으세요")]
    public Transform[] leftFinBones;  // 왼쪽 지느러미 뼈 배열
    [Tooltip("지느러미 뼈들을 몸통 쪽에서 바깥쪽 순서로 넣으세요")]
    public Transform[] rightFinBones; // 오른쪽 지느러미 뼈 배열
    
    [Tooltip("지느러미가 펄럭이는 각도")]
    public float finRotationAngle = 25.0f;
    [Tooltip("지느러미 파동 속도 배율")]
    public float finSpeedMultiplier = 1.2f;
    [Tooltip("지느러미 뼈 사이의 굴곡 정도 (부드러움 조절)")]
    public float finWaveOffset = 0.3f; 

    // 초기 회전값 저장용
    private Quaternion[] frontInitialRotations;
    private Quaternion[] backInitialRotations;
    private Quaternion[] leftFinInitialRotations;
    private Quaternion[] rightFinInitialRotations;

    void Start()
    {
        // 1. 몸통 초기값 저장
        frontInitialRotations = new Quaternion[frontBones.Length];
        for (int i = 0; i < frontBones.Length; i++)
            if (frontBones[i] != null) frontInitialRotations[i] = frontBones[i].localRotation;

        backInitialRotations = new Quaternion[backBones.Length];
        for (int i = 0; i < backBones.Length; i++)
            if (backBones[i] != null) backInitialRotations[i] = backBones[i].localRotation;

        // 2. 지느러미 초기값 저장 (배열 처리)
        leftFinInitialRotations = new Quaternion[leftFinBones.Length];
        for (int i = 0; i < leftFinBones.Length; i++)
            if (leftFinBones[i] != null) leftFinInitialRotations[i] = leftFinBones[i].localRotation;

        rightFinInitialRotations = new Quaternion[rightFinBones.Length];
        for (int i = 0; i < rightFinBones.Length; i++)
            if (rightFinBones[i] != null) rightFinInitialRotations[i] = rightFinBones[i].localRotation;
    }

    void Update()
    {
        if (currentMode == SwimMode.Fish)
        {
            AnimateFish();
        }
        else if (currentMode == SwimMode.Dolphin)
        {
            AnimateDolphin();
        }
    }

    // 물고기: 좌우(X축) S자
    void AnimateFish()
    {
        int globalIndex = 0;

        // Front
        for (int i = 0; i < frontBones.Length; i++)
        {
            if (frontBones[i] == null) continue;
            float angle = Mathf.Sin(Time.time * swimSpeed + globalIndex * waveOffset) * rotationAngle;
            frontBones[i].localRotation = frontInitialRotations[i] * Quaternion.Euler(angle/4, 0, angle);
            globalIndex++;
        }

        // Back
        for (int i = 0; i < backBones.Length; i++)
        {
            if (backBones[i] == null) continue;
            float angle = Mathf.Sin(Time.time * swimSpeed + globalIndex * waveOffset) * rotationAngle;
            backBones[i].localRotation = backInitialRotations[i] * Quaternion.Euler(-angle/4, 0, -angle);
            globalIndex++;
        }
    }

    // 돌고래: 상하(Z축) 웨이브 + 지느러미(X축) 웨이브
    void AnimateDolphin()
    {
        int globalIndex = 0;

        // 1. 몸통 (상하)
        for (int i = 0; i < frontBones.Length; i++)
        {
            if (frontBones[i] == null) continue;
            float angle = Mathf.Sin(Time.time * swimSpeed + globalIndex * waveOffset) * rotationAngle;
            frontBones[i].localRotation = frontInitialRotations[i] * Quaternion.Euler(angle, 0, angle/4);
            globalIndex++;
        }

        for (int i = 0; i < backBones.Length; i++)
        {
            if (backBones[i] == null) continue;
            float angle = Mathf.Sin(Time.time * swimSpeed + globalIndex * waveOffset) * rotationAngle;
            backBones[i].localRotation = backInitialRotations[i] * Quaternion.Euler(-angle, 0, -angle/4);
            globalIndex++;
        }

        // 2. 지느러미 (X축 회전 - Flapping)
        // 지느러미도 뼈마다 index를 줘서 끝부분이 늦게 따라오게 만듭니다.
        
        // Left Fins
        for (int i = 0; i < leftFinBones.Length; i++)
        {
            if (leftFinBones[i] == null) continue;
            
            // 몸통과는 다른 박자(Cos), 조금 더 빠른 속도, 뼈마다 딜레이(i * finWaveOffset)
            float finWave = Mathf.Cos(Time.time * swimSpeed * finSpeedMultiplier + i * finWaveOffset);
            float finAngle = finWave * finRotationAngle;

            // Z가 아래를 봄 -> X축 회전
            leftFinBones[i].localRotation = leftFinInitialRotations[i] * Quaternion.Euler(finAngle, 0, 0);
        }

        // Right Fins
        for (int i = 0; i < rightFinBones.Length; i++)
        {
            if (rightFinBones[i] == null) continue;

            float finWave = Mathf.Cos(Time.time * swimSpeed * finSpeedMultiplier + i * finWaveOffset);
            float finAngle = finWave * finRotationAngle;

            // Z가 위를 봄 -> X축 회전 (부호 반대)
            rightFinBones[i].localRotation = rightFinInitialRotations[i] * Quaternion.Euler(-finAngle, 0, 0);
        }
    }
}