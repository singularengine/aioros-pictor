using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>Where an element's spec stands among its chart's: by reference first, so two elements written alike (two <c>&lt;Bar DataKey="uv"/&gt;</c>) keep their own places.</summary>
internal static class SpecIndex
{
    /// <summary>The index of the spec itself, or, for a copy of it (a frame of an animation built from another chart's specs), of the first equal one; -1 when none.</summary>
    public static int Of<T>(ImmutableArray<T> specs, T spec)
        where T : class
    {
        for (var i = 0; i < specs.Length; i++)
        {
            if (ReferenceEquals(specs[i], spec))
            {
                return i;
            }
        }

        return specs.IndexOf(spec);
    }
}
