/// <summary>
/// Gives every target the functions and constants that only some targets declare, such as SDL's
/// <c>#ifdef SDL_PLATFORM_ANDROID</c> blocks. Such a declaration must be identical wherever it is declared, and becomes
/// <c>[SupportedOSPlatform]</c> for exactly the platforms of those targets, so the platform availability comes from the
/// headers rather than from config. Everything else must still be identical on every target (<see cref="VarianceCheck"/>).
/// </summary>
internal static class PlatformMerge
{
    /// <summary>Runs the merge.</summary>
    /// <param name="targets">The per-target results.</param>
    /// <param name="report">Receives one line per merged declaration.</param>
    /// <returns>The results with the merged models, and the errors.</returns>
    public static (IReadOnlyList<TargetModel> Targets, IReadOnlyList<string> Errors) Run(IReadOnlyList<TargetModel> targets, List<string> report)
    {
        var errors = new List<string>();
        var functionOrder = MergedOrder(targets.Select(t => t.Model.Functions.Select(f => f.NativeName).ToList()).ToList());
        var constantOrder = MergedOrder(targets.Select(t => t.Model.Constants.Select(c => c.NativeName).ToList()).ToList());

        var functions = Partial(targets, functionOrder, m => m.Functions, f => f.NativeName, f => f.Describe(), report, errors, (donor, platforms) =>
        {
            if (donor.SupportedPlatforms.Count > 0)
            {
                errors.Add($"{donor.NativeName} is only declared on some targets, which already gives its platforms: remove it from SupportedPlatforms.");
            }

            return new FunctionModel
            {
                NativeName = donor.NativeName,
                Name = donor.Name,
                Return = donor.Return,
                Parameters = donor.Parameters,
                Group = donor.Group,
                Documentation = donor.Documentation,
                SupportedPlatforms = platforms,
                UnsupportedPlatforms = donor.UnsupportedPlatforms,
            };
        });
        var constants = Partial(targets, constantOrder, m => m.Constants, c => c.NativeName, c => c.Describe(), report, errors, (donor, platforms) => new ConstantModel
        {
            NativeName = donor.NativeName,
            Name = donor.Name,
            Group = donor.Group,
            Type = donor.Type,
            Integer = donor.Integer,
            Float = donor.Float,
            Text = donor.Text,
            Documentation = donor.Documentation,
            SupportedPlatforms = platforms,
            UnsupportedPlatforms = donor.UnsupportedPlatforms,
        });

        var result = new List<TargetModel>(targets.Count);
        foreach (var target in targets)
        {
            var model = target.Model;
            var enums = model.Enums.ToList();
            var structs = model.Structs.ToList();
            var handles = model.Handles.ToList();
            var callbacks = new Dictionary<string, Documentation>(model.Callbacks, StringComparer.Ordinal);
            var functionList = Merge(functionOrder, functions, model.Functions, f => f.NativeName);
            var constantList = Merge(constantOrder, constants, model.Constants, c => c.NativeName);
            foreach (var function in functions.Values)
            {
                var donor = targets.First(t => t.Model.Functions.Any(f => f.NativeName == function.NativeName)).Model;
                var types = new List<TypeRef> { function.Return };
                types.AddRange(function.Parameters.Select(p => p.Type));
                CopyTypes(function.NativeName, types, donor, enums, structs, handles, callbacks, errors);
            }

            foreach (var constant in constants.Values)
            {
                var donor = targets.First(t => t.Model.Constants.Any(c => c.NativeName == constant.NativeName)).Model;
                CopyTypes(constant.NativeName, [constant.Type], donor, enums, structs, handles, callbacks, errors);
            }

            result.Add(new TargetModel
            {
                Target = target.Target,
                Model = new LibraryModel
                {
                    Name = model.Name,
                    Namespace = model.Namespace,
                    FunctionsClass = model.FunctionsClass,
                    Groups = model.Groups,
                    Functions = functionList,
                    Enums = [.. enums.OrderBy(e => e.Name, StringComparer.Ordinal)],
                    Structs = [.. structs.OrderBy(s => s.Name, StringComparer.Ordinal)],
                    Handles = [.. handles.OrderBy(h => h.Name, StringComparer.Ordinal)],
                    Constants = constantList,
                    Callbacks = callbacks,
                    StringView = model.StringView,
                },
                Layouts = target.Layouts,
                PointerSize = target.PointerSize,
                LongSize = target.LongSize,
                ExcludedFunctions = target.ExcludedFunctions,
                UsedConfigKeys = target.UsedConfigKeys,
            });
        }

        return (result, errors.Distinct(StringComparer.Ordinal).ToList());
    }

    // The declarations missing on some targets, checked identical where present and rebuilt with their platforms.
    private static Dictionary<string, T> Partial<T>(IReadOnlyList<TargetModel> targets, List<string> order, Func<LibraryModel, IReadOnlyList<T>> select,
        Func<T, string> key, Func<T, string> describe, List<string> report, List<string> errors, Func<T, IReadOnlyList<string>, T> rebuild)
    {
        var partial = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var name in order)
        {
            var declaring = targets.Where(t => select(t.Model).Any(d => key(d) == name)).ToList();
            if (declaring.Count == targets.Count)
            {
                continue;
            }

            var versions = declaring.Select(t => select(t.Model).First(d => key(d) == name)).ToList();
            if (versions.Select(describe).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                errors.Add($"{name} is declared differently on {string.Join(", ", declaring.Select(t => t.Target.Rid))}.");
                continue;
            }

            var platforms = declaring.Select(t => t.Target.Platform).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
            partial[name] = rebuild(versions[0], platforms);
            report.Add($"  {name}: declared for {string.Join(", ", platforms)} only ({declaring.Count} of {targets.Count} targets).");
        }

        return partial;
    }

    private static List<T> Merge<T>(List<string> order, Dictionary<string, T> partial, IReadOnlyList<T> own, Func<T, string> key)
    {
        var list = new List<T>();
        foreach (var name in order)
        {
            if (partial.TryGetValue(name, out var merged))
            {
                list.Add(merged);
            }
            else if (own.FirstOrDefault(d => key(d) == name) is { } declaration)
            {
                list.Add(declaration);
            }
        }

        return list;
    }

    // Every target lists declarations in header order, so one missing from the first target goes right after the one
    // that precedes it where it is declared.
    private static List<string> MergedOrder(List<List<string>> lists)
    {
        var order = lists[0].ToList();
        var known = order.ToHashSet(StringComparer.Ordinal);
        foreach (var list in lists.Skip(1))
        {
            string? previous = null;
            foreach (var name in list)
            {
                if (known.Add(name))
                {
                    order.Insert(previous is null ? 0 : order.IndexOf(previous) + 1, name);
                }

                previous = name;
            }
        }

        return order;
    }

    // Types only a platform declaration uses exist only where it is declared. Opaque ones and enums carry over as they
    // are; a struct used by value would need its layout checked on targets that do not declare it.
    private static void CopyTypes(string owner, List<TypeRef> types, LibraryModel donor, List<EnumModel> enums, List<StructModel> structs, List<HandleModel> handles,
        Dictionary<string, Documentation> callbacks, List<string> errors)
    {
        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];
            switch (type.Kind)
            {
                case TypeKind.Pointer or TypeKind.FixedArray:
                    types.Add(type.Element!);
                    break;

                case TypeKind.FunctionPointer:
                    types.Add(type.Return!);
                    types.AddRange(type.Parameters);
                    if (type.Alias is { } alias && donor.Callbacks.TryGetValue(alias, out var documentation))
                    {
                        callbacks.TryAdd(alias, documentation);
                    }

                    break;

                case TypeKind.Named:
                    var name = type.Name!;
                    if (enums.Any(e => e.NativeName == name) || structs.Any(s => s.NativeName == name) || handles.Any(h => h.NativeName == name))
                    {
                        break;
                    }

                    if (donor.Enums.FirstOrDefault(e => e.NativeName == name) is { } enumModel)
                    {
                        enums.Add(enumModel);
                    }
                    else if (donor.Handles.FirstOrDefault(h => h.NativeName == name) is { } handle)
                    {
                        handles.Add(handle);
                    }
                    else if (donor.Structs.FirstOrDefault(s => s.NativeName == name) is { IsOpaque: true } opaque)
                    {
                        structs.Add(opaque);
                    }
                    else
                    {
                        errors.Add($"{owner}: {name} is only declared where it is, and only enums, handles and opaque structs can be carried to other targets.");
                    }

                    break;
            }
        }
    }
}
