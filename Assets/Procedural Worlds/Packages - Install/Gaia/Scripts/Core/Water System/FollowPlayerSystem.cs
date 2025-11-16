using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerSystem : MonoBehaviour
{
    [Header("Global Settings")]
    public bool m_followPlayer = true;
    public bool m_isWaterObject = false;
    public List<GameObject> m_particleObjects = new List<GameObject>();
    public Transform m_player;

    public bool m_useOffset = false;
    public float m_xoffset = 1250f;
    public float m_zoffset = 300f;
    public float m_yOffset = 200f;

    public bool m_useScale = false;
    public Vector3 m_scaleSize = new Vector3(1f, 1f, 1f);

    private bool m_playerExists = false;
    private List<Transform> m_particleObjectTransforms = new List<Transform>();

    private void Start()
    {
        if (m_player == null)
        {
            // GaiaUtils 대신 유니티 표준 메인 카메라를 사용합니다.
            if (Camera.main != null)
            {
                m_player = Camera.main.transform;
            }
        }

        m_playerExists = m_player != null;

        if (m_particleObjects.Count > 0)
        {
            m_particleObjectTransforms.Clear();
            foreach (GameObject particleObject in m_particleObjects)
            {
                // 파티클 오브젝트의 Transform을 캐싱합니다.
                m_particleObjectTransforms.Add(particleObject.transform);
            }
        }
        
        if (m_useScale)
        {
            // 이 컴포넌트가 붙은 GameObject의 스케일을 설정합니다.
            gameObject.transform.localScale = m_scaleSize;
        }
    }

    private void LateUpdate()
    {
        if (m_followPlayer && m_player != null)
        {
            if (m_particleObjectTransforms.Count > 0)
            {
                // 모든 추적 대상 오브젝트의 위치를 업데이트합니다.
                foreach (Transform particleTransform in m_particleObjectTransforms)
                {
                    if (!m_useOffset)
                    {
                        // 오프셋을 사용하지 않으면 플레이어와 동일한 위치로 설정합니다.
                        particleTransform.position = m_player.position;
                    }
                    else
                    {
                        // 오프셋을 사용하는 경우
                        if (m_isWaterObject)
                        {
                            // 물 오브젝트 특수 로직: 플레이어의 Y 위치에 따라 높이 오프셋이 달라집니다.
                            if (m_player.position.y < 1f)
                            {
                                particleTransform.position = new Vector3(
                                    m_player.position.x + m_xoffset, 
                                    m_player.position.y + 70f - m_yOffset, // 플레이어 Y < 1f 일 때의 높이 설정
                                    m_player.position.z - m_zoffset
                                );
                            }
                            else
                            {
                                particleTransform.position = new Vector3(
                                    m_player.position.x + m_xoffset, 
                                    m_player.position.y + 10 - m_yOffset, // 플레이어 Y >= 1f 일 때의 높이 설정
                                    m_player.position.z - m_zoffset
                                );
                            }
                        }
                        else
                        {
                            // 일반 오프셋 로직
                            particleTransform.position = new Vector3(
                                m_player.position.x + m_xoffset, 
                                m_player.position.y - m_yOffset, 
                                m_player.position.z - m_zoffset
                            );
                        }
                    }
                }
            }
        }
    }
}