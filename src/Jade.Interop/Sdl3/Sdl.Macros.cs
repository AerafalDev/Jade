namespace Jade.Interop.Sdl3;

// SDL's function-like macros that C# code needs, written by hand: the generator only binds exported functions and
// object-like macros. Each one follows its C definition in the staged SDL headers.
public static partial class Sdl
{
    /// <summary>Returns the <see cref="MouseButtonFlags"/> bit of a mouse button, like <c>SDL_BUTTON_MASK</c>.</summary>
    /// <param name="button">A button index such as <see cref="ButtonLeft"/>, from 1.</param>
    /// <returns>The button's bit in the mask that <see cref="GetMouseState(float*, float*)"/> returns.</returns>
    public static MouseButtonFlags ButtonMask(int button) => (MouseButtonFlags)(1u << (button - 1));

    /// <summary>Packs a version into one number, like <c>SDL_VERSIONNUM</c>: 3.4.16 is 3004016.</summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The patch version.</param>
    /// <returns>The number, comparable with <see cref="GetVersion"/> and <see cref="Version"/>.</returns>
    public static int VersionNum(int major, int minor, int patch) => (major * 1000000) + (minor * 1000) + patch;

    /// <summary>Extracts the major version of a packed version, like <c>SDL_VERSIONNUM_MAJOR</c>.</summary>
    /// <param name="version">A packed version, for example from <see cref="GetVersion"/>.</param>
    /// <returns>The major version.</returns>
    public static int VersionNumMajor(int version) => version / 1000000;

    /// <summary>Extracts the minor version of a packed version, like <c>SDL_VERSIONNUM_MINOR</c>.</summary>
    /// <param name="version">A packed version, for example from <see cref="GetVersion"/>.</param>
    /// <returns>The minor version.</returns>
    public static int VersionNumMinor(int version) => version / 1000 % 1000;

    /// <summary>Extracts the micro version of a packed version, like <c>SDL_VERSIONNUM_MICRO</c>.</summary>
    /// <param name="version">A packed version, for example from <see cref="GetVersion"/>.</param>
    /// <returns>The micro version.</returns>
    public static int VersionNumMicro(int version) => version % 1000;

    /// <summary>Returns a window position that lets SDL place the window on a display, like <c>SDL_WINDOWPOS_UNDEFINED_DISPLAY</c>.</summary>
    /// <param name="display">The display, or 0 for the primary one.</param>
    /// <returns>An x or y coordinate for <see cref="SetWindowPosition"/> or the window creation properties.</returns>
    public static int WindowPosUndefinedDisplay(DisplayID display) => (int)(WindowPosUndefinedMask | (uint)display);

    /// <summary>Returns a window position that centers the window on a display, like <c>SDL_WINDOWPOS_CENTERED_DISPLAY</c>.</summary>
    /// <param name="display">The display, or 0 for the primary one.</param>
    /// <returns>An x or y coordinate for <see cref="SetWindowPosition"/> or the window creation properties.</returns>
    public static int WindowPosCenteredDisplay(DisplayID display) => (int)(WindowPosCenteredMask | (uint)display);

    /// <summary>Returns whether a window coordinate is an undefined position, like <c>SDL_WINDOWPOS_ISUNDEFINED</c>.</summary>
    /// <param name="position">An x or y coordinate.</param>
    /// <returns><see langword="true"/> for <see cref="WindowPosUndefinedDisplay"/> values.</returns>
    public static bool WindowPosIsUndefined(int position) => ((uint)position & 0xFFFF0000) == WindowPosUndefinedMask;

    /// <summary>Returns whether a window coordinate is a centered position, like <c>SDL_WINDOWPOS_ISCENTERED</c>.</summary>
    /// <param name="position">An x or y coordinate.</param>
    /// <returns><see langword="true"/> for <see cref="WindowPosCenteredDisplay"/> values.</returns>
    public static bool WindowPosIsCentered(int position) => ((uint)position & 0xFFFF0000) == WindowPosCenteredMask;
}
