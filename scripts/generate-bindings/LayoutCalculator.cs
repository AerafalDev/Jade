/// <summary>
/// Computes the layout the CLR gives a generated struct on one target: <c>LayoutKind.Sequential</c> with natural
/// alignment for structs, every field at offset 0 for unions. Comparing it with clang's layout on every target is
/// what proves that one C# definition fits all of them (ADR-0006); the generated layout tests check this model
/// against the real runtime on the test host.
/// </summary>
internal sealed class LayoutCalculator
{
    private readonly Dictionary<string, EnumModel> enums;
    private readonly Dictionary<string, StructModel> structs;
    private readonly HashSet<string> handles;
    private readonly Dictionary<string, RecordLayout> cache = new(StringComparer.Ordinal);
    private readonly int pointerSize;
    private readonly int longSize;

    /// <summary>Creates a calculator for one target.</summary>
    /// <param name="model">The model whose named types fields refer to.</param>
    /// <param name="pointerSize">The target's pointer size.</param>
    /// <param name="longSize">The target's C <c>long</c> size.</param>
    public LayoutCalculator(LibraryModel model, int pointerSize, int longSize)
    {
        enums = model.Enums.ToDictionary(e => e.NativeName, StringComparer.Ordinal);
        structs = model.Structs.ToDictionary(s => s.NativeName, StringComparer.Ordinal);
        handles = model.Handles.Select(h => h.NativeName).ToHashSet(StringComparer.Ordinal);
        this.pointerSize = pointerSize;
        this.longSize = longSize;
    }

    /// <summary>Returns the size of a scalar on a target; it is also its alignment.</summary>
    /// <param name="primitive">The scalar.</param>
    /// <param name="pointerSize">The target's pointer size.</param>
    /// <param name="longSize">The target's C <c>long</c> size.</param>
    /// <returns>The size in bytes.</returns>
    public static int SizeOf(PrimitiveType primitive, int pointerSize, int longSize) => primitive switch
    {
        PrimitiveType.Char or PrimitiveType.SByte or PrimitiveType.Byte => 1,
        PrimitiveType.Int16 or PrimitiveType.UInt16 => 2,
        PrimitiveType.Int32 or PrimitiveType.UInt32 or PrimitiveType.Single => 4,
        PrimitiveType.Int64 or PrimitiveType.UInt64 or PrimitiveType.Double => 8,
        PrimitiveType.NInt or PrimitiveType.NUInt => pointerSize,
        PrimitiveType.CLong or PrimitiveType.CULong => longSize,
        _ => throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null),
    };

    /// <summary>Computes the managed layout of a struct.</summary>
    /// <param name="model">A non-opaque struct of the model.</param>
    /// <returns>Its layout on this calculator's target.</returns>
    public RecordLayout Layout(StructModel model)
    {
        if (cache.TryGetValue(model.NativeName, out var cached))
        {
            return cached;
        }

        long offset = 0;
        long size = 0;
        long alignment = 1;
        var offsets = new List<long>(model.Fields.Count);
        foreach (var field in model.Fields)
        {
            var (fieldSize, fieldAlignment) = SizeAndAlignment(field.Type);
            alignment = Math.Max(alignment, fieldAlignment);
            if (model.IsUnion)
            {
                offsets.Add(0);
                size = Math.Max(size, fieldSize);
            }
            else
            {
                offset = Align(offset, fieldAlignment);
                offsets.Add(offset);
                offset += fieldSize;
                size = offset;
            }
        }

        var layout = new RecordLayout(Align(size, alignment), alignment, offsets);
        cache[model.NativeName] = layout;
        return layout;
    }

    private (long Size, long Alignment) SizeAndAlignment(TypeRef type)
    {
        switch (type.Kind)
        {
            case TypeKind.Primitive:
                var size = SizeOf(type.Primitive, pointerSize, longSize);
                return (size, size);

            case TypeKind.Pointer or TypeKind.FunctionPointer:
                return (pointerSize, pointerSize);

            case TypeKind.FixedArray:
                var (elementSize, elementAlignment) = SizeAndAlignment(type.Element!);
                return (elementSize * type.Length, elementAlignment);

            case TypeKind.Named when handles.Contains(type.Name!):
                return (pointerSize, pointerSize);

            case TypeKind.Named when enums.TryGetValue(type.Name!, out var enumModel):
                var enumSize = SizeOf(enumModel.Underlying, pointerSize, longSize);
                return (enumSize, enumSize);

            case TypeKind.Named when structs.TryGetValue(type.Name!, out var structModel) && !structModel.IsOpaque:
                var layout = Layout(structModel);
                return (layout.Size, layout.Alignment);

            default:
                throw new InvalidOperationException($"no managed layout for {type.Kind} {type.Name}.");
        }
    }

    private static long Align(long value, long alignment) => (value + alignment - 1) / alignment * alignment;
}
