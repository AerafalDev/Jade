using System.Collections.Frozen;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Dawn;

/// <summary>
/// The two <c>webgpu.h</c> variants that Dawn generates from <c>dawn.json</c>, and the platforms
/// each one serves.
/// </summary>
/// <remarks>
/// The rules follow <c>generator/dawn_json_generator.py</c> at the pinned commit: Dawn's own header
/// enables the <c>dawn</c>, <c>native</c> and <c>deprecated</c> tags and serves every native
/// platform; Emdawnwebgpu's header enables <c>emscripten</c> and serves the browser. Untagged items
/// are in both.
/// </remarks>
internal static class DawnVariants
{
    /// <summary>The tag that only selects items for Dawn's Kotlin bindings.</summary>
    private const string KotlinOnlyTag = "art_experimental";

    /// <summary>The tags that Dawn's own header enables.</summary>
    private static readonly FrozenSet<string> _nativeTags = FrozenSet.Create(StringComparer.Ordinal, "dawn", "native", "deprecated");

    /// <summary>The tags that Emdawnwebgpu's header enables.</summary>
    private static readonly FrozenSet<string> _browserTags = FrozenSet.Create(StringComparer.Ordinal, "emscripten");

    /// <summary>Gets the platforms whose header variant contains an item.</summary>
    /// <param name="tags">The tags of the entry, method or enum value; <see langword="null"/> when it has none.</param>
    /// <returns>The platforms the item is available on; <see cref="Platforms.None"/> for an item no variant contains.</returns>
    public static Platforms GetPlatforms(IReadOnlyList<string>? tags)
    {
        var platforms = Platforms.None;

        if (IsEnabled(tags, _nativeTags))
        {
            platforms |= Platforms.Native;
        }

        if (IsEnabled(tags, _browserTags))
        {
            platforms |= Platforms.Browser;
        }

        return platforms;
    }

    /// <summary>Tells whether a header variant contains an item, as <c>item_is_enabled</c> does.</summary>
    /// <param name="tags">The tags of the item.</param>
    /// <param name="enabledTags">The tags the variant enables.</param>
    /// <returns><see langword="true"/> when the item is untagged or has an enabled tag.</returns>
    /// <remarks>
    /// <c>art_experimental</c> is ignored, except that an item tagged with it alone is in no variant.
    /// </remarks>
    private static bool IsEnabled(IReadOnlyList<string>? tags, FrozenSet<string> enabledTags)
    {
        if (tags is null or [])
        {
            return true;
        }

        var relevantTags = tags.Where(static tag => tag != KotlinOnlyTag).ToList();

        return relevantTags.Count > 0 && relevantTags.Any(enabledTags.Contains);
    }
}
