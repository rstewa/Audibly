using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Counts the logical processors that share a last-level (L3) cache. On Intel hybrid
///     CPUs with an SoC-tile low-power island (Meteor Lake, Arrow Lake) the LP-E cores
///     have no L3, are effectively parked during normal use, and are indistinguishable
///     from regular E-cores by efficiency class — but Environment.ProcessorCount still
///     counts them, so "ProcessorCount - 1" oversubscribes the cores that actually run.
/// </summary>
public static class CpuTopology
{
    private const int RelationCache = 2;

    /// <summary>
    ///     Logical processors participating in any L3 cache; 0 when the topology cannot be
    ///     determined (VMs, exotic hardware) — callers fall back to ProcessorCount.
    /// </summary>
    public static int GetLastLevelCacheLogicalProcessorCount()
    {
        try
        {
            uint length = 0;
            GetLogicalProcessorInformationEx(RelationCache, null, ref length);
            if (length == 0) return 0;

            var buffer = new byte[length];
            if (!GetLogicalProcessorInformationEx(RelationCache, buffer, ref length)) return 0;

            // OR the group masks of all L3 entries together, per processor group
            var masksByGroup = new Dictionary<int, ulong>();
            var offset = 0;
            while (offset + 8 <= length)
            {
                var relationship = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset));
                var size = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset + 4));
                if (size <= 0) break;

                if (relationship == RelationCache)
                {
                    // CACHE_RELATIONSHIP at offset+8: Level(1) Assoc(1) LineSize(2) CacheSize(4)
                    // Type(4) Reserved(18) GroupCount(2) GroupMasks[](16 each, at +32 from its start)
                    var cache = offset + 8;
                    var level = buffer[cache];
                    if (level == 3)
                    {
                        int groupCount = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(cache + 30));
                        if (groupCount == 0) groupCount = 1; // pre-20H2 layout: single GroupMask
                        for (var g = 0; g < groupCount; g++)
                        {
                            var affinity = cache + 32 + g * 16;
                            if (affinity + 10 > offset + size) break;
                            var mask = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(affinity));
                            int group = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(affinity + 8));
                            masksByGroup[group] = masksByGroup.GetValueOrDefault(group) | mask;
                        }
                    }
                }

                offset += size;
            }

            var count = 0;
            foreach (var mask in masksByGroup.Values) count += BitOperations.PopCount(mask);
            return count;
        }
        catch
        {
            return 0;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(int relationshipType, byte[]? buffer,
        ref uint returnedLength);
}
