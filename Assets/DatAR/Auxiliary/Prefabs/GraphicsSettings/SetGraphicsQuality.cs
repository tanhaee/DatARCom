using UnityEngine;

public class SetGraphicsQuality : MonoBehaviour
{
    [SerializeField] private int qualityLevel;
    
    public void SetQualityLevel()
    {
        // Check if valid number
        string[] names = QualitySettings.names;
        if (names.Length < qualityLevel || qualityLevel < 0)
        {
            Debug.LogError("Invalid input for graphics quality setter");
            return;
        }
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }
}