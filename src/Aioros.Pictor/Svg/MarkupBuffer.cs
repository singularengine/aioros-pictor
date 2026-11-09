using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The markup writer every part of a chart writes through: it takes a large free block of the output (the output's
/// own array when it has one) and writes the small pieces markup is made of into it with a bounds check and a copy,
/// committing the bytes to the output in one advance when the block fills or the chart ends (<see cref="Flush"/>),
/// where the output's interface would cost a size request and an advance per piece. An output without an array behind
/// its memory gets the bytes copied from a pooled block.
/// </summary>
internal sealed class MarkupBuffer : IBufferWriter<byte>, IDisposable
{
    private const int Block = 4096;
    private readonly IBufferWriter<byte> _output;
    private byte[] _array = [];
    private int _start;
    private int _position;
    private int _end;
    private bool _rented;
    private int _committed;

    private MarkupBuffer(IBufferWriter<byte> output) => _output = output;

    /// <summary>The bytes written through this writer so far, committed or not.</summary>
    public int Length => _committed + _position - _start;

    /// <summary>The writer itself when it is one already (a part handed the chart's writer), otherwise a writer over it.</summary>
    public static MarkupBuffer Over(IBufferWriter<byte> output) => output as MarkupBuffer ?? new MarkupBuffer(output);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length <= _end - _position)
        {
            bytes.CopyTo(_array.AsSpan(_position));
            _position += bytes.Length;
            return;
        }

        WriteMore(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint >= _end - _position)
        {
            Renew(sizeHint);
        }

        return _array.AsSpan(_position, _end - _position);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (sizeHint >= _end - _position)
        {
            Renew(sizeHint);
        }

        return _array.AsMemory(_position, _end - _position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => _position += count;

    /// <summary>Commits the bytes written to the output; the output's written bytes are whole after it.</summary>
    public void Flush()
    {
        var pending = _position - _start;
        if (pending > 0)
        {
            if (_rented)
            {
                _array.AsSpan(_start, pending).CopyTo(_output.GetSpan(pending));
            }

            _output.Advance(pending);
            _committed += pending;
        }

        Release();
    }

    public void Dispose() => Flush();

    private void WriteMore(ReadOnlySpan<byte> bytes)
    {
        while (!bytes.IsEmpty)
        {
            if (_end == _position)
            {
                Renew(Math.Min(bytes.Length, Block));
            }

            var part = Math.Min(bytes.Length, _end - _position);
            bytes[..part].CopyTo(_array.AsSpan(_position));
            _position += part;
            bytes = bytes[part..];
        }
    }

    private void Renew(int sizeHint)
    {
        Flush();
        var size = Math.Max(sizeHint + 1, Block);
        if (MemoryMarshal.TryGetArray<byte>(_output.GetMemory(size), out var segment) && segment.Array is { } array && segment.Count >= size)
        {
            (_array, _start, _end, _rented) = (array, segment.Offset, segment.Offset + segment.Count, false);
        }
        else
        {
            (_array, _start, _end, _rented) = (ArrayPool<byte>.Shared.Rent(size), 0, 0, true);
            _end = _array.Length;
        }

        _position = _start;
    }

    private void Release()
    {
        if (_rented)
        {
            ArrayPool<byte>.Shared.Return(_array);
        }

        (_array, _start, _position, _end, _rented) = ([], 0, 0, 0, false);
    }
}
