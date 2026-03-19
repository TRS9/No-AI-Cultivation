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

    public LiquidCircle()
    {
        AddToClassList("liquid-circle");

        // Structural: circular mask
        style.overflow = Overflow.Hidden;
        style.borderTopLeftRadius = Length.Percent(50);
        style.borderTopRightRadius = Length.Percent(50);
        style.borderBottomLeftRadius = Length.Percent(50);
        style.borderBottomRightRadius = Length.Percent(50);

        // The Fill (liquid that rises from the bottom)
        fillElement = new VisualElement();
        fillElement.AddToClassList("liquid-circle__fill");
        fillElement.style.position = Position.Absolute;
        fillElement.style.bottom = 0;
        fillElement.style.left = 0;
        fillElement.style.right = 0;

        Add(fillElement);
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (fillElement != null)
        {
            fillElement.style.height = Length.Percent(_progress * 100f);
        }
    }
}
