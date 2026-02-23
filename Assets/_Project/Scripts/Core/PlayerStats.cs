using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Cultivation Progress")]
    public int totalQi;
    public int cultivationLevel = 1;

    [Header("UI References")]
    public TextMeshProUGUI qiText;

    private void Start()
    {
        UpdateQiUI();
    }

    private void UpdateQiUI()
    {
        if (qiText != null)
        {
            qiText.text = "Qi: " + totalQi.ToString();
        }
    }

    public void AddQi(int amount)
    {
        totalQi += amount;

        UpdateQiUI();

        Debug.Log($"Qi gespeichert! Aktueller Stand: {totalQi}");

        // Hier könntest du später Logik für ein "Level Up" einbauen
    }
}