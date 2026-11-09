using System.Globalization;
using Aioros.Runtime.Slots;

namespace Aioros.Pictor.Text;

/// <summary>
/// A formatter's converter as the web's writer runs it: the converter a spec names by Aioros's plans file reference
/// (<c>type:Namespace.Type</c>, <c>static:Namespace.Type:Member</c>), rebuilt from the app's converters Aioros generates
/// at build time (<see cref="PlanConverters"/>) and run as a binding runs it (the value, text as the target, the
/// parameter, the current culture); nothing when the app has no generated entry for it.
/// </summary>
internal static class Converters
{
    public static Func<object?, object?, string?>? Of(string? reference) =>
        reference is not null && PlanConverters.Find(reference) is { } converter && PlanConverters.Code is { } code
            ? (value, parameter) => code.Convert(converter, value, typeof(string), parameter, CultureInfo.CurrentCulture) is { } text ? Convert.ToString(text, CultureInfo.CurrentCulture) : null
            : null;
}
