﻿using Godot;

public class CustomPopupMenu : CustomWindow
{
    [Export]
    public NodePath? PanelPath;

    [Export]
    public NodePath ContainerPath = null!;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private Panel panel = null!;
    private Container container = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private Vector2 cachedMinSize;

    private bool closing;

    public override void _Ready()
    {
        panel = GetNode<Panel>(PanelPath);
        container = GetNode<Container>(ContainerPath);

        cachedMinSize = RectMinSize;
        RectMinSize = Vector2.Zero;

        ResolveNodeReferences();
        RemapDynamicChildren();
    }

    public void RemapDynamicChildren()
    {
        foreach (Control child in GetChildren())
        {
            if (child.Equals(panel))
                continue;

            child.ReParent(container);
        }
    }

    public override void Open()
    {
        base.Open();

        CreateTween().TweenProperty(this, "rect_size", cachedMinSize, 0.2f)
            .From(Vector2.Zero)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
    }

    public override void Close()
    {
        if (closing || !Visible)
            return;

        closing = true;

        var tween = CreateTween();
        tween.TweenProperty(this, "rect_size", Vector2.Zero, 0.15f)
            .From(cachedMinSize)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(this, nameof(OnFolded));
    }

    protected virtual void ResolveNodeReferences()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PanelPath != null)
            {
                PanelPath.Dispose();
                ContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnFolded()
    {
        closing = false;
        Hide();
    }
}
