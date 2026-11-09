using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>Recharts' <c>getAttrsOfCartesianLabel</c>: where a label sits around a box and how it is anchored.</summary>
internal static class LabelPlacement
{
    public const double Start = 0.71;
    public const double Middle = 0.355;
    public const double End = 0;

    public static (double X, double Y, TextAnchor Anchor, double Dy) Of(LabelPosition position, double x, double y, double width, double height, double offset)
    {
        var ySign = height >= 0 ? 1 : -1;
        var xSign = width >= 0 ? 1 : -1;
        var vo = ySign * offset;
        var ho = xSign * offset;
        var cx = x + (width / 2);
        var cy = y + (height / 2);
        var down = ySign > 0;
        var ahead = xSign > 0 ? TextAnchor.Start : TextAnchor.End;
        var behind = xSign > 0 ? TextAnchor.End : TextAnchor.Start;
        var above = down ? End : Start;
        var below = down ? Start : End;
        return position switch
        {
            LabelPosition.Top => (cx, y - vo, TextAnchor.Middle, above),
            LabelPosition.Bottom => (cx, y + height + vo, TextAnchor.Middle, below),
            LabelPosition.Left => (x - ho, cy, behind, Middle),
            LabelPosition.Right => (x + width + ho, cy, ahead, Middle),
            LabelPosition.InsideLeft => (x + ho, cy, ahead, Middle),
            LabelPosition.InsideRight => (x + width - ho, cy, behind, Middle),
            LabelPosition.InsideTop => (cx, y + vo, TextAnchor.Middle, below),
            LabelPosition.InsideBottom => (cx, y + height - vo, TextAnchor.Middle, above),
            LabelPosition.InsideTopLeft => (x + ho, y + vo, TextAnchor.Start, Start),
            LabelPosition.InsideTopRight => (x + width - ho, y + vo, TextAnchor.End, Start),
            LabelPosition.InsideBottomLeft => (x + ho, y + height - vo, TextAnchor.Start, End),
            LabelPosition.InsideBottomRight => (x + width - ho, y + height - vo, TextAnchor.End, End),
            LabelPosition.Start => (x, cy, TextAnchor.End, Middle),
            LabelPosition.End => (x + width, cy, TextAnchor.Start, Middle),
            _ => (cx, cy, TextAnchor.Middle, Middle),
        };
    }
}
