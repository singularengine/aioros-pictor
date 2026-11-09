using System.Buffers;
using Aioros.Pictor.Motion;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The update animation an SVG chart declares for Aioros's morph: the pending series layer's settings, written on the
/// next animated layer, and the pending element's numbers, written on the next shape or circle (the series' data key
/// on its first one).
/// </summary>
internal struct MorphWriter
{
    private MorphLayer? _layer;
    private MorphMark _mark;
    private MorphKind _kind;
    private byte _curve;
    private uint? _key;

    public void Layer(in MorphLayer layer) => _layer = layer;

    public void Mark(in MorphMark mark) => _mark = mark;

    /// <summary>Writes the pending layer's settings on the animated layer being opened, or forgets the previous layer's.</summary>
    public void Open(MarkupBuffer output)
    {
        if (_layer is { } layer)
        {
            SvgMotion.MorphLayer(output, layer);
            (_kind, _curve, _key, _layer) = (layer.Kind, layer.Curve, layer.Key, null);
            return;
        }

        (_kind, _key) = (MorphKind.None, null);
    }

    /// <summary>Writes the pending element's numbers on the element being opened.</summary>
    public void Take(MarkupBuffer output)
    {
        if (_mark.Kind == MorphKind.None)
        {
            return;
        }

        SvgMotion.Morph(output, _mark, _kind, _curve, _key);
        (_key, _mark) = (null, default);
    }
}
