using System;
using System.Collections.Generic;
using UnityEngine;

public class FaceExpressionController : MonoBehaviour
{
    [Serializable]
    public class FaceExpressionOffset
    {
        public string expressionName;
        public Vector2 offsetIndex;
    }

    [Serializable]
    public class FaceSheet
    {
        public string partName;
        public Material material;
        public Vector2 startOffset;
        public Vector2 cellSize;
        public List<FaceExpressionOffset> expressions = new();
        private Vector2 currentOffset;

        public void SetExpression(string expressionName)
        {
            int idx = expressions.FindIndex(e => e.expressionName == expressionName);
            if (idx == -1)
            {
                Debug.LogWarning($"'{expressionName}' not found in {partName} sheet");
                return;
            }

            Vector2 eOffset = expressions[idx].offsetIndex;
            currentOffset = new Vector2(
                startOffset.x + cellSize.x * eOffset.x,
                startOffset.y - cellSize.y * eOffset.y
            );
            Debug.Log(currentOffset);
            if (material != null)
                material.SetTextureOffset("_Texture2D", currentOffset);
            else
                Debug.LogError("Cannot find mat");
        }
    }

    [Serializable]
    public class FacePreset
    {
        public string presetName;

        [Serializable]
        public class PartExpression
        {
            public string partName;
            public string expressionName;
        }

        public List<PartExpression> parts = new();
    }

    [Header("Face Layers (Eye, Mouth, Cheek...)")]
    [SerializeField] private List<FaceSheet> faceSheets = new();

    [Header("Face Presets")]
    [SerializeField] private List<FacePreset> facePresets = new();

    void Start()
    {
        SetFullExpression("Neutral");
    }

    public void SetPartExpression(string partName, string expressionName)
    {
        var sheet = faceSheets.Find(s => s.partName == partName);
        if (sheet != null)
            sheet.SetExpression(expressionName);
        else
            Debug.LogWarning($"Part '{partName}' not found.");
    }

    public void SetFullExpression(string presetName)
    {
        var preset = facePresets.Find(p => p.presetName == presetName);
        if (preset == null)
        {
            Debug.LogWarning($"Preset '{presetName}' not found.");
            return;
        }

        foreach (var part in preset.parts)
            SetPartExpression(part.partName, part.expressionName);

        Debug.Log($"Full expression '{presetName}' applied.");
    }

    // For Timeline or UnityEvent (single string argument)
    // Example: "Preset:Cry" or "Eye:Blink"
    public void SetExpressionFromString(string formatted)
    {
        if (string.IsNullOrEmpty(formatted)) return;

        string[] parts = formatted.Split(':');
        if (parts.Length != 2)
        {
            Debug.LogWarning($"Invalid format: {formatted}");
            return;
        }

        string key = parts[0].Trim();
        string value = parts[1].Trim();

        if (key.Equals("Preset", StringComparison.OrdinalIgnoreCase))
            SetFullExpression(value);
        else
            SetPartExpression(key, value);
    }

    // -----------------------------
    // TEST FUNCTIONS (for Editor)
    // -----------------------------

    [ContextMenu("Test/Set Eye Blink")]
    private void TestSetEyeBlink()
    {
        Debug.Log("Testing Eye Blink...");
        SetPartExpression("Eye", "Blink");
    }

    [ContextMenu("Test/Set Mouth Talk")]
    private void TestSetMouthTalk()
    {
        Debug.Log("Testing Mouth Talk...");
        SetPartExpression("Mouth", "Talk");
    }

    [ContextMenu("Test/Set Full Cry Preset")]
    private void TestSetFullCry()
    {
        Debug.Log("Testing Full 'Cry' Preset...");
        SetFullExpression("Cry");
    }

    [ContextMenu("Test/Set Expression From String (Preset:Cry)")]
    private void TestExpressionFromString()
    {
        Debug.Log("Testing ExpressionFromString: 'Preset:Cry'");
        SetExpressionFromString("Preset:Cry");
    }
}
