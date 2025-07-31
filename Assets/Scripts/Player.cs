using UnityEngine;
public class Player
{
    public readonly string name;
    public readonly Color color;
    public readonly Gradient colorGradient = new();
    public Player(string name, Color color)
    {
        this.name = name;
        this.color = color;
        SetColorGradient();
    }

    private void SetColorGradient()
    {
        GradientColorKey[] colorKeys = new GradientColorKey[1];
        colorKeys[0].color = color;
        colorKeys[0].time = 0f;
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = 1.0f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 0f;
        alphaKeys[1].time = 0.32f;
        colorGradient.SetKeys(colorKeys, alphaKeys);
    }
}
