using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class FaceExpressionController : MonoBehaviour
{
    [Serializable]
    public class FaceExpressionOffset
    {
        public string f_name;
        public Vector2 f_offest;
    }

    [Header("Face Material")]
    public Renderer faceRenderer;  // 캐릭터 얼굴의 Renderer
    public int faceMaterialIndex = 0;
    [SerializeField]
    private Material faceMaterial;

    [Header("Texture Atlas Settings")]
    public List<FaceExpressionOffset> faces = new();

    public Vector2 startOffset;
    public Vector2 cellSize; // 한 칸의 UV 크기

    private Vector2 currentOffset; // 현재 표정 위치

    void Start()
    {
        // 머티리얼 인스턴스 복제 (공유 머티리얼 오염 방지)
        Material[] mats = faceRenderer.materials;
        faceMaterial = new Material(mats[faceMaterialIndex]);
        mats[faceMaterialIndex] = faceMaterial;
        faceRenderer.materials = mats;

        SetExpression(0);
    }

    // 감정 이름으로 표정 변경
    public void SetExpression(int f_numer)
    {
        Vector2 f_offest = faces[f_numer].f_offest;
        currentOffset = new Vector2(startOffset.x + cellSize.x * f_offest.x, startOffset.y - cellSize.y * f_offest.y);

        faceMaterial.mainTextureOffset = currentOffset;
    }

    public int test_f_number = 0;
    [ContextMenu("Run Facial change")]
    public void Test_SetExpression()
    {
        SetExpression(test_f_number);
    }
}
