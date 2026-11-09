namespace Aioros.Pictor;

/// <summary>
/// The children whose changes an owner follows: after every change of its list, each child the list holds is followed
/// once and each child it no longer holds is let go, whatever the list's notification carried (a reset carries none).
/// </summary>
internal sealed class ChildChanges<T>(Action<T> follow, Action<T> leave)
    where T : class
{
    private HashSet<T> _followed = new(ReferenceEqualityComparer.Instance);

    public void Follow(IEnumerable<T> children)
    {
        var next = new HashSet<T>(children, ReferenceEqualityComparer.Instance);
        foreach (var child in _followed.Where(child => !next.Contains(child)))
        {
            leave(child);
        }

        foreach (var child in next.Where(child => !_followed.Contains(child)))
        {
            follow(child);
        }

        _followed = next;
    }
}
