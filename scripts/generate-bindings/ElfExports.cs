using System.Buffers.Binary;
using System.Text;

/// <summary>Reads the names a shared library exports from its ELF dynamic symbol table (<c>.dynsym</c>).</summary>
internal static class ElfExports
{
    private const uint DynamicSymbolTable = 11;
    private const int GlobalBinding = 1;
    private const int WeakBinding = 2;
    private const int HiddenVisibility = 2;
    private const int InternalVisibility = 1;

    /// <summary>Returns the defined global and weak symbols of an ELF shared library, as <c>nm -D --defined-only</c> lists them.</summary>
    /// <param name="path">The library.</param>
    /// <returns>The exported names, or <see langword="null"/> if the file is not ELF.</returns>
    /// <exception cref="InvalidOperationException">The file is ELF but has no dynamic symbol table.</exception>
    public static IReadOnlySet<string>? Read(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 64 || data[0] != 0x7F || data[1] != (byte)'E' || data[2] != (byte)'L' || data[3] != (byte)'F')
        {
            return null;
        }

        var is64 = data[4] == 2;
        var little = data[5] == 1;
        var sectionOffset = (long)(is64 ? U64(data, 0x28, little) : U32(data, 0x20, little));
        var sectionSize = U16(data, is64 ? 0x3A : 0x2E, little);
        var sectionCount = U16(data, is64 ? 0x3C : 0x30, little);

        for (var i = 0; i < sectionCount; i++)
        {
            var header = (int)(sectionOffset + ((long)i * sectionSize));
            if (U32(data, header + 4, little) != DynamicSymbolTable)
            {
                continue;
            }

            var (offset, size, link, entrySize) = is64
                ? ((long)U64(data, header + 0x18, little), (long)U64(data, header + 0x20, little), U32(data, header + 0x28, little), (long)U64(data, header + 0x38, little))
                : (U32(data, header + 0x10, little), U32(data, header + 0x14, little), U32(data, header + 0x18, little), (long)U32(data, header + 0x24, little));
            var strings = (int)(is64 ? U64(data, (int)(sectionOffset + (link * sectionSize)) + 0x18, little) : U32(data, (int)(sectionOffset + (link * sectionSize)) + 0x10, little));

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var entry = offset; entry + entrySize <= offset + size; entry += entrySize)
            {
                var symbol = (int)entry;
                var name = (int)U32(data, symbol, little);
                var (info, other, section) = is64
                    ? (data[symbol + 4], data[symbol + 5], U16(data, symbol + 6, little))
                    : (data[symbol + 12], data[symbol + 13], U16(data, symbol + 14, little));
                var binding = info >> 4;
                var visibility = other & 3;
                if (section == 0 || binding is not (GlobalBinding or WeakBinding) || visibility is HiddenVisibility or InternalVisibility)
                {
                    continue;
                }

                var start = strings + name;
                var end = Array.IndexOf(data, (byte)0, start);
                names.Add(Encoding.ASCII.GetString(data, start, end - start));
            }

            return names;
        }

        throw new InvalidOperationException($"{path} has no dynamic symbol table.");
    }

    private static ushort U16(byte[] data, int offset, bool little) =>
        little ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)) : BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));

    private static uint U32(byte[] data, int offset, bool little) =>
        little ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)) : BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));

    private static ulong U64(byte[] data, int offset, bool little) =>
        little ? BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset)) : BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset));
}
