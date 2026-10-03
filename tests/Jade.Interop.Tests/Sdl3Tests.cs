using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Jade.Interop.Sdl3;
using Shouldly;
using Xunit;

namespace Jade.Interop.Tests;

// SDL keeps global state (init ref-counts, the event queue, hints), so every SDL test lives in this one class:
// xunit runs the tests of a class one after the other.
public sealed unsafe class Sdl3Tests
{
    private const string NotStaged = "jade_native is not staged for this RID; run `dotnet scripts/build-native.cs` and rebuild the tests.";

    private static readonly bool s_staged = NativeLibrary.TryLoad("jade_native", typeof(Sdl).Assembly, null, out _);

    [Fact]
    public void GetVersion_returns_the_staged_SDL_version()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        var expected = StagedVersion("sdl3").Split('.').Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();

        var version = Sdl.GetVersion();

        version.ShouldBe(Sdl.VersionNum(expected[0], expected[1], expected[2]));
        (Sdl.VersionNumMajor(version), Sdl.VersionNumMinor(version), Sdl.VersionNumMicro(version)).ShouldBe((expected[0], expected[1], expected[2]));
        Utf8(Sdl.GetRevision()).ShouldContain($"release-{StagedVersion("sdl3")}");
    }

    [Fact]
    public void Version_constants_match_the_library()
    {
        Assert.SkipUnless(s_staged, NotStaged);

        Sdl.Version.ShouldBe(Sdl.GetVersion());
        Sdl.Version.ShouldBe(Sdl.VersionNum(Sdl.MajorVersion, Sdl.MinorVersion, Sdl.MicroVersion));
    }

    [Fact]
    public void Macro_helpers_follow_their_C_definitions()
    {
        Sdl.ButtonMask(Sdl.ButtonLeft).ShouldBe(MouseButtonFlags.Left);
        Sdl.ButtonMask(Sdl.ButtonX2).ShouldBe(MouseButtonFlags.X2);
        Sdl.WindowPosCenteredDisplay(0).ShouldBe((int)Sdl.WindowPosCentered);
        Sdl.WindowPosUndefinedDisplay(0).ShouldBe((int)Sdl.WindowPosUndefined);
        Sdl.WindowPosIsCentered(Sdl.WindowPosCenteredDisplay((DisplayID)2)).ShouldBeTrue();
        Sdl.WindowPosIsUndefined(Sdl.WindowPosCenteredDisplay((DisplayID)2)).ShouldBeFalse();
        Sdl.WindowPosIsUndefined(100).ShouldBeFalse();
    }

    [Fact]
    public void Init_without_subsystems_succeeds_and_Quit_releases_everything()
    {
        Assert.SkipUnless(s_staged, NotStaged);

        Sdl.Init(0).ShouldBeTrue(Utf8(Sdl.GetError()));
        Sdl.Quit();

        Sdl.WasInit(0).ShouldBe((InitFlags)0);
    }

    [Fact]
    public void A_pushed_quit_event_is_polled_back()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        Sdl.Init(InitFlags.Events).ShouldBeTrue(Utf8(Sdl.GetError()));
        try
        {
            var pushed = new Event { Quit = new QuitEvent { Type = EventType.Quit } };
            Sdl.PushEvent(ref pushed).ShouldBeTrue(Utf8(Sdl.GetError()));

            var polled = default(Event);
            var found = false;
            while (!found && Sdl.PollEvent(out polled))
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
    public void A_user_event_keeps_its_payload_through_the_queue()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        Sdl.Init(InitFlags.Events).ShouldBeTrue(Utf8(Sdl.GetError()));
        try
        {
            var type = Sdl.RegisterEvents(1);
            type.ShouldNotBe(0u, Utf8(Sdl.GetError()));
            var pushed = new Event { User = new UserEvent { Type = type, Code = 42, Data1 = (void*)0x1234, WindowID = (WindowID)7 } };
            Sdl.PushEvent(ref pushed).ShouldBeTrue(Utf8(Sdl.GetError()));

            var polled = default(Event);
            while (Sdl.PollEvent(out polled) && polled.Type != type)
            {
            }

            polled.Type.ShouldBe(type);
            (polled.User.Code, (nint)polled.User.Data1, polled.User.WindowID).ShouldBe((42, (nint)0x1234, (WindowID)7));
        }
        finally
        {
            Sdl.Quit();
        }
    }

    [Fact]
    public void ClearError_leaves_an_empty_message()
    {
        Assert.SkipUnless(s_staged, NotStaged);

        Sdl.OutOfMemory().ShouldBeFalse();
        Utf8(Sdl.GetError()).ShouldNotBeEmpty();
        Sdl.ClearError().ShouldBeTrue();

        Utf8(Sdl.GetError()).ShouldBeEmpty();
    }

    [Fact]
    public void A_hint_set_through_its_constant_is_read_back()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        try
        {
            Sdl.SetHint(Sdl.HintAppName, "Jade tests"u8).ShouldBeTrue(Utf8(Sdl.GetError()));

            Utf8(Sdl.GetHint(Sdl.HintAppName)).ShouldBe("Jade tests");
            Encoding.UTF8.GetString(Sdl.HintAppName).ShouldBe("SDL_APP_NAME");
        }
        finally
        {
            Sdl.ResetHint(Sdl.HintAppName);
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(4000)]
    public void Utf8_overloads_terminate_strings_of_any_length(int length)
    {
        Assert.SkipUnless(s_staged, NotStaged);
        var name = Sdl.PropAppMetadataNameString;
        var value = Encoding.UTF8.GetBytes("é" + new string('j', length - 2));

        Sdl.SetAppMetadataProperty(name, value).ShouldBeTrue(Utf8(Sdl.GetError()));

        new ReadOnlySpan<byte>(Sdl.GetAppMetadataProperty(name), length + 1).ToArray().ShouldBe([.. value, 0]);
    }

    [Fact]
    public void Properties_round_trip_every_type()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        var props = Sdl.CreateProperties();
        props.ShouldNotBe((PropertiesID)0, Utf8(Sdl.GetError()));
        try
        {
            var pointer = (void*)0xBEEF;
            Sdl.SetStringProperty(props, "jade.string"u8, "héllo"u8).ShouldBeTrue(Utf8(Sdl.GetError()));
            Sdl.SetNumberProperty(props, "jade.number"u8, long.MinValue).ShouldBeTrue(Utf8(Sdl.GetError()));
            Sdl.SetFloatProperty(props, "jade.float"u8, 1.5f).ShouldBeTrue(Utf8(Sdl.GetError()));
            Sdl.SetBooleanProperty(props, "jade.boolean"u8, true).ShouldBeTrue(Utf8(Sdl.GetError()));
            Sdl.SetPointerProperty(props, "jade.pointer"u8, pointer).ShouldBeTrue(Utf8(Sdl.GetError()));

            Utf8(Sdl.GetStringProperty(props, "jade.string"u8, null)).ShouldBe("héllo");
            Sdl.GetNumberProperty(props, "jade.number"u8, 0).ShouldBe(long.MinValue);
            Sdl.GetFloatProperty(props, "jade.float"u8, 0).ShouldBe(1.5f);
            Sdl.GetBooleanProperty(props, "jade.boolean"u8, false).ShouldBeTrue();
            ((nint)Sdl.GetPointerProperty(props, "jade.pointer"u8, null)).ShouldBe((nint)pointer);
            Sdl.GetPropertyType(props, "jade.float"u8).ShouldBe(PropertyType.Float);
            Sdl.GetPropertyType(props, "jade.missing"u8).ShouldBe(PropertyType.Invalid);
            Sdl.GetBooleanProperty(props, "jade.missing"u8, true).ShouldBeTrue();

            var count = 0;
            Sdl.EnumerateProperties(props, &CountProperty, &count).ShouldBeTrue(Utf8(Sdl.GetError()));
            count.ShouldBe(5);

            Sdl.ClearProperty(props, "jade.string"u8).ShouldBeTrue(Utf8(Sdl.GetError()));
            Sdl.HasProperty(props, "jade.string"u8).ShouldBeFalse();
        }
        finally
        {
            Sdl.DestroyProperties(props);
        }
    }

    [Fact]
    public void An_IOStream_reads_and_writes_memory()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        byte[] source = [0x78, 0x56, 0x34, 0x12, 0xAA, 0xBB];
        var target = new byte[8];
        fixed (byte* sourcePtr = source)
        fixed (byte* targetPtr = target)
        {
            var reader = Sdl.IOFromConstMem(sourcePtr, (nuint)source.Length);
            reader.IsNull.ShouldBeFalse(Utf8(Sdl.GetError()));
            try
            {
                reader.GetSize().ShouldBe(source.Length);
                reader.ReadU32LE(out var value).ShouldBeTrue(Utf8(Sdl.GetError()));
                value.ShouldBe(0x12345678u);
                reader.Tell().ShouldBe(4);

                byte rest = 0;
                reader.Read(&rest, 1).ShouldBe(1u);
                rest.ShouldBe((byte)0xAA);
                reader.Seek(-1, IOWhence.End).ShouldBe(source.Length - 1);
                reader.ReadU8(out rest).ShouldBeTrue(Utf8(Sdl.GetError()));
                rest.ShouldBe((byte)0xBB);
                reader.ReadU8(out _).ShouldBeFalse();
                reader.GetStatus().ShouldBe(IOStatus.Eof);
            }
            finally
            {
                reader.Close().ShouldBeTrue(Utf8(Sdl.GetError()));
            }

            var writer = Sdl.IOFromMem(targetPtr, (nuint)target.Length);
            writer.IsNull.ShouldBeFalse(Utf8(Sdl.GetError()));
            try
            {
                writer.WriteU32BE(0x01020304).ShouldBeTrue(Utf8(Sdl.GetError()));
                writer.Write(sourcePtr, 4).ShouldBe(4u);
                writer.WriteU8(0).ShouldBeFalse();
            }
            finally
            {
                writer.Close().ShouldBeTrue(Utf8(Sdl.GetError()));
            }
        }

        target.ShouldBe([0x01, 0x02, 0x03, 0x04, 0x78, 0x56, 0x34, 0x12]);
    }

    [Fact]
    public void A_hidden_window_round_trips_its_title_and_size()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "SDL video must run on the main thread on macOS, and xunit runs tests on worker threads.");
        if (!Sdl.Init(InitFlags.Video))
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
                window.SetTitle("Jade bindings"u8).ShouldBeTrue(Utf8(Sdl.GetError()));
                Utf8(window.GetTitle()).ShouldBe("Jade bindings");
                window.GetSize(out var width, out var height).ShouldBeTrue(Utf8(Sdl.GetError()));
                (width, height).ShouldBe((320, 200));
                Sdl.GetWindowFromID(window.GetID()).ShouldBe(window);
                Sdl.GetWindowProperties(window).ShouldNotBe((PropertiesID)0, Utf8(Sdl.GetError()));
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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CountProperty(void* userdata, PropertiesID props, byte* name) => (*(int*)userdata)++;

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
