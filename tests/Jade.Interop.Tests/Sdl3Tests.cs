using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Jade.Interop.Sdl3;
using Shouldly;
using Xunit;

namespace Jade.Interop.Tests;

// SDL keeps global state (init ref-counts, the event queue), so every SDL test lives in this one class:
// xunit runs the tests of a class one after the other.
public sealed unsafe class Sdl3Tests
{
    private const string NotStaged = "jade_native is not staged for this RID; run `dotnet scripts/build-native.cs` and rebuild the tests.";

    private static readonly bool Staged = NativeLibrary.TryLoad("jade_native", typeof(Sdl).Assembly, null, out _);

    [Fact]
    public void GetVersion_returns_the_staged_SDL_version()
    {
        Assert.SkipUnless(Staged, NotStaged);
        var expected = StagedVersion("sdl3").Split('.').Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();

        var version = Sdl.GetVersion();

        version.ShouldBe((expected[0] * 1_000_000) + (expected[1] * 1_000) + expected[2]);
        Utf8(Sdl.GetRevision()).ShouldContain($"release-{StagedVersion("sdl3")}");
    }

    [Fact]
    public void Init_without_subsystems_succeeds_and_Quit_releases_everything()
    {
        Assert.SkipUnless(Staged, NotStaged);

        Sdl.Init(0).ShouldBe((byte)1, Utf8(Sdl.GetError()));
        Sdl.Quit();

        Sdl.WasInit(0).ShouldBe((InitFlags)0);
    }

    [Fact]
    public void A_pushed_quit_event_is_polled_back()
    {
        Assert.SkipUnless(Staged, NotStaged);
        Sdl.Init(InitFlags.Events).ShouldBe((byte)1, Utf8(Sdl.GetError()));
        try
        {
            var pushed = new Event { Quit = new QuitEvent { Type = EventType.Quit } };
            Sdl.PushEvent(ref pushed).ShouldBe((byte)1, Utf8(Sdl.GetError()));

            var polled = default(Event);
            var found = false;
            while (!found && Sdl.PollEvent(out polled) != 0)
            {
                found = polled.Type == (uint)EventType.Quit;
            }

            found.ShouldBeTrue();
            polled.Quit.Timestamp.ShouldBeGreaterThan(0UL);
        }
        finally
        {
            Sdl.Quit();
        }
    }

    [Fact]
    public void ClearError_leaves_an_empty_message()
    {
        Assert.SkipUnless(Staged, NotStaged);

        Sdl.OutOfMemory().ShouldBe((byte)0);
        Utf8(Sdl.GetError()).ShouldNotBeEmpty();
        Sdl.ClearError().ShouldBe((byte)1);

        Utf8(Sdl.GetError()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(4000)]
    public void Utf8_overloads_terminate_strings_of_any_length(int length)
    {
        Assert.SkipUnless(Staged, NotStaged);
        var name = "SDL.app.metadata.name"u8;
        var value = Encoding.UTF8.GetBytes("é" + new string('j', length - 2));

        Sdl.SetAppMetadataProperty(name, value).ShouldBe((byte)1, Utf8(Sdl.GetError()));

        new ReadOnlySpan<byte>(Sdl.GetAppMetadataProperty(name), length + 1).ToArray().ShouldBe([.. value, 0]);
    }

    [Fact]
    public void A_hidden_window_round_trips_its_title_and_size()
    {
        Assert.SkipUnless(Staged, NotStaged);
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "SDL video must run on the main thread on macOS, and xunit runs tests on worker threads.");
        if (Sdl.Init(InitFlags.Video) == 0)
        {
            Assert.Skip($"SDL video is unavailable here (headless?): {Utf8(Sdl.GetError())}");
        }

        try
        {
            var window = Sdl.CreateWindow("Jade"u8, 320, 200, WindowFlags.Hidden);
            window.IsNull.ShouldBeFalse(Utf8(Sdl.GetError()));
            try
            {
                window.GetFlags().HasFlag(WindowFlags.Hidden).ShouldBeTrue();
                window.SetTitle("Jade bindings"u8).ShouldBe((byte)1, Utf8(Sdl.GetError()));
                Utf8(window.GetTitle()).ShouldBe("Jade bindings");
                window.GetSize(out var width, out var height).ShouldBe((byte)1, Utf8(Sdl.GetError()));
                (width, height).ShouldBe((320, 200));
                Sdl.GetWindowFromID(window.GetID()).ShouldBe(window);
            }
            finally
            {
                window.Destroy();
            }
        }
        finally
        {
            Sdl.Quit();
        }
    }

    private static string Utf8(byte* value) => value is null ? string.Empty : Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value));

    // The versions.json staged next to the library by scripts/build-native.cs, copied by the test project.
    private static string StagedVersion(string upstream)
    {
        using var versions = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "jade_native.versions.json")));
        return versions.RootElement.GetProperty("upstreams").EnumerateArray()
            .Single(u => u.GetProperty("name").GetString() == upstream)
            .GetProperty("version").GetString()!;
    }
}
