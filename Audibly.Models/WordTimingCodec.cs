using System.Buffers.Binary;

namespace Audibly.Models;

/// <summary>
///     Per-word timing within a <see cref="TranscriptSegment" />.
///     Offsets index into <see cref="TranscriptSegment.Text" />; times are relative
///     to <see cref="TranscriptSegment.StartMs" />. All values fit ushort because the
///     sentence assembler caps sentences at 350 characters / 30 seconds.
/// </summary>
public readonly record struct WordTiming(ushort CharOffset, ushort CharLength, ushort StartMsRel, ushort DurationMs);

/// <summary>
///     Packs word timings into the compact blob stored in <see cref="TranscriptSegment.WordTimings" />:
///     [version:byte][count:ushort][count x (charOffset:ushort, charLength:ushort, startMsRel:ushort, durationMs:ushort)],
///     all little-endian. ~8 bytes per word vs ~30 as JSON.
/// </summary>
public static class WordTimingCodec
{
    private const byte Version = 1;
    private const int HeaderSize = 3;
    private const int WordSize = 8;

    public static byte[] Encode(IReadOnlyList<WordTiming> words)
    {
        if (words.Count > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(words), "Too many words for one segment.");

        var blob = new byte[HeaderSize + words.Count * WordSize];
        blob[0] = Version;
        BinaryPrimitives.WriteUInt16LittleEndian(blob.AsSpan(1), (ushort)words.Count);

        for (var i = 0; i < words.Count; i++)
        {
            var span = blob.AsSpan(HeaderSize + i * WordSize);
            BinaryPrimitives.WriteUInt16LittleEndian(span, words[i].CharOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(span[2..], words[i].CharLength);
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], words[i].StartMsRel);
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..], words[i].DurationMs);
        }

        return blob;
    }

    public static WordTiming[] Decode(byte[] blob)
    {
        if (blob.Length < HeaderSize || blob[0] != Version)
            return [];

        var count = BinaryPrimitives.ReadUInt16LittleEndian(blob.AsSpan(1));
        if (blob.Length < HeaderSize + count * WordSize)
            return [];

        var words = new WordTiming[count];
        for (var i = 0; i < count; i++)
        {
            var span = blob.AsSpan(HeaderSize + i * WordSize);
            words[i] = new WordTiming(
                BinaryPrimitives.ReadUInt16LittleEndian(span),
                BinaryPrimitives.ReadUInt16LittleEndian(span[2..]),
                BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
                BinaryPrimitives.ReadUInt16LittleEndian(span[6..]));
        }

        return words;
    }
}
