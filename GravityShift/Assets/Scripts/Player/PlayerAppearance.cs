using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerAppearance : MonoBehaviour
{
    public Renderer bodyRenderer;
    public TextMeshPro nameText;

    private Dictionary<int, Color> teamColors = new Dictionary<int, Color>()
    {
        { 1, Color.red },
        { 2, Color.blue },
        { 3, Color.green },
        { 4, Color.yellow }
    };

    public void SetTeamColor(int team)
    {
        Color colorToApply = Color.white;

        if (teamColors.ContainsKey(team))
            colorToApply = teamColors[team];
        
        if (bodyRenderer != null)
            bodyRenderer.material.color = colorToApply;

        if (nameText != null)
            nameText.color = colorToApply;
    }
}
