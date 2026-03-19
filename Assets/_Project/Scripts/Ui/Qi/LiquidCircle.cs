using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LiquidCircle : VisualElement
{
    private VisualElement fillElement;

    private float _progress = 0.5f;

    // Data-binding ready
    [UxmlAttribute]
    public float progress
    {
        get => _progress;
        set
        {
            _progress = Mathf.Clamp01(value);
            UpdateFill();
        }
    }

    [UxmlAttribute] public Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    [UxmlAttribute] public Color fillColor = Color.cyan;

    public LiquidCircle()
    {
        // 1. The Container (Acts as the circular mask)
        style.overflow = Overflow.Hidden;

        // This makes it a perfect circle as long as width == height in the UI Builder
        style.borderTopLeftRadius = Length.Percent(50);
        style.borderTopRightRadius = Length.Percent(50);
        style.borderBottomLeftRadius = Length.Percent(50);
        style.borderBottomRightRadius = Length.Percent(50);
        style.backgroundColor = emptyColor;

        // 2. The Fill (The "liquid" that rises from the bottom)
        fillElement = new VisualElement();
        fillElement.AddToClassList("liquid-circle__fill");
        fillElement.style.position = Position.Absolute;
        fillElement.style.bottom = 0; // Anchor to the bottom
        fillElement.style.left = 0;
        fillElement.style.right = 0;
        fillElement.style.backgroundColor = fillColor;

        // Add the fill into the circular container
        Add(fillElement);
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (fillElement != null)
        {
            // Adjust the height based on the progress percentage
            fillElement.style.height = Length.Percent(_progress * 100f);
        }
    }
}
