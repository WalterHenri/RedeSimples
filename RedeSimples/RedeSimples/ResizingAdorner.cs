// RedeSimples/ResizingAdorner.cs
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

public class ResizingAdorner : Adorner
{
    private readonly Thumb _topLeft, _topRight, _bottomLeft, _bottomRight;
    private readonly VisualCollection _visualChildren;

    public ResizingAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _visualChildren = new VisualCollection(this);
        _topLeft = CreateThumb(Cursors.SizeNWSE, HorizontalAlignment.Left, VerticalAlignment.Top);
        _topRight = CreateThumb(Cursors.SizeNESW, HorizontalAlignment.Right, VerticalAlignment.Top);
        _bottomLeft = CreateThumb(Cursors.SizeNESW, HorizontalAlignment.Left, VerticalAlignment.Bottom);
        _bottomRight = CreateThumb(Cursors.SizeNWSE, HorizontalAlignment.Right, VerticalAlignment.Bottom);

        _topLeft.DragDelta += HandleTopLeft;
        _topRight.DragDelta += HandleTopRight;
        _bottomLeft.DragDelta += HandleBottomLeft;
        _bottomRight.DragDelta += HandleBottomRight;
    }

    private Thumb CreateThumb(Cursor cursor, HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        var thumb = new Thumb
        {
            Cursor = cursor,
            Width = 10,
            Height = 10,
            Background = new SolidColorBrush(Colors.DodgerBlue),
            BorderBrush = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(1)
        };
        _visualChildren.Add(thumb);
        return thumb;
    }

    private void HandleTopLeft(object sender, DragDeltaEventArgs args)
    {
        if (AdornedElement is not FrameworkElement adornedElement) return;

        double newWidth = adornedElement.Width - args.HorizontalChange;
        double newHeight = adornedElement.Height - args.VerticalChange;

        if (newWidth > adornedElement.MinWidth)
        {
            adornedElement.Width = newWidth;
            Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) + args.HorizontalChange);
        }
        if (newHeight > adornedElement.MinHeight)
        {
            adornedElement.Height = newHeight;
            Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) + args.VerticalChange);
        }
    }

    private void HandleTopRight(object sender, DragDeltaEventArgs args)
    {
        if (AdornedElement is not FrameworkElement adornedElement) return;

        double newWidth = adornedElement.Width + args.HorizontalChange;
        double newHeight = adornedElement.Height - args.VerticalChange;

        if (newWidth > adornedElement.MinWidth) adornedElement.Width = newWidth;
        if (newHeight > adornedElement.MinHeight)
        {
            adornedElement.Height = newHeight;
            Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) + args.VerticalChange);
        }
    }

    private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
    {
        if (AdornedElement is not FrameworkElement adornedElement) return;

        double newWidth = adornedElement.Width - args.HorizontalChange;
        double newHeight = adornedElement.Height + args.VerticalChange;

        if (newWidth > adornedElement.MinWidth)
        {
            adornedElement.Width = newWidth;
            Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) + args.HorizontalChange);
        }
        if (newHeight > adornedElement.MinHeight) adornedElement.Height = newHeight;
    }

    private void HandleBottomRight(object sender, DragDeltaEventArgs args)
    {
        if (AdornedElement is not FrameworkElement adornedElement) return;

        double newWidth = adornedElement.Width + args.HorizontalChange;
        double newHeight = adornedElement.Height + args.VerticalChange;

        if (newWidth > adornedElement.MinWidth) adornedElement.Width = newWidth;
        if (newHeight > adornedElement.MinHeight) adornedElement.Height = newHeight;
    }

    protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
    {
        double desireWidth = AdornedElement.DesiredSize.Width;
        double desireHeight = AdornedElement.DesiredSize.Height;
        double adornerWidth = DesiredSize.Width;
        double adornerHeight = DesiredSize.Height;

        _topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
        _topRight.Arrange(new Rect(desireWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
        _bottomLeft.Arrange(new Rect(-adornerWidth / 2, desireHeight - adornerHeight / 2, adornerWidth, adornerHeight));
        _bottomRight.Arrange(new Rect(desireWidth - adornerWidth / 2, desireHeight - adornerHeight / 2, adornerWidth, adornerHeight));

        return finalSize;
    }

    protected override int VisualChildrenCount => _visualChildren.Count;
    protected override Visual GetVisualChild(int index) => _visualChildren[index];
}