using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LiquidCircle : VisualElement
{
    private VisualElement _fill;
    private float _progress = 0f;

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
        style.overflow = Overflow.Hidden;
        style.borderTopLeftRadius = Length.Percent(50);
        style.borderTopRightRadius = Length.Percent(50);
        style.borderBottomLeftRadius = Length.Percent(50);
        style.borderBottomRightRadius = Length.Percent(50);
        style.backgroundColor = emptyColor;

        _fill = new VisualElement();
        _fill.AddToClassList("liquid-circle__fill");
        _fill.style.position = Position.Absolute;
        _fill.style.bottom = 0;
        _fill.style.left = 0;
        _fill.style.right = 0;
        _fill.style.backgroundColor = fillColor;

        Add(_fill);
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (_fill != null)
            _fill.style.height = Length.Percent(_progress * 100f);
    }
}
