using FlaUI.Core.WindowsAPI;

namespace FlauiCli.Core;

/// <summary>Maps friendly key names (Enter, Tab, ArrowDown, F5, …) to UIA virtual key codes.</summary>
public static class Keys
{
    private static readonly Dictionary<string, VirtualKeyShort> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enter"] = VirtualKeyShort.RETURN,
        ["return"] = VirtualKeyShort.RETURN,
        ["tab"] = VirtualKeyShort.TAB,
        ["escape"] = VirtualKeyShort.ESCAPE,
        ["esc"] = VirtualKeyShort.ESCAPE,
        ["space"] = VirtualKeyShort.SPACE,
        ["backspace"] = VirtualKeyShort.BACK,
        ["delete"] = VirtualKeyShort.DELETE,
        ["del"] = VirtualKeyShort.DELETE,
        ["up"] = VirtualKeyShort.UP,
        ["arrowup"] = VirtualKeyShort.UP,
        ["down"] = VirtualKeyShort.DOWN,
        ["arrowdown"] = VirtualKeyShort.DOWN,
        ["left"] = VirtualKeyShort.LEFT,
        ["arrowleft"] = VirtualKeyShort.LEFT,
        ["right"] = VirtualKeyShort.RIGHT,
        ["arrowright"] = VirtualKeyShort.RIGHT,
        ["home"] = VirtualKeyShort.HOME,
        ["end"] = VirtualKeyShort.END,
        ["pageup"] = VirtualKeyShort.PRIOR,
        ["pagedown"] = VirtualKeyShort.NEXT,
        ["insert"] = VirtualKeyShort.INSERT,
        ["f1"] = VirtualKeyShort.F1,
        ["f2"] = VirtualKeyShort.F2,
        ["f3"] = VirtualKeyShort.F3,
        ["f4"] = VirtualKeyShort.F4,
        ["f5"] = VirtualKeyShort.F5,
        ["f6"] = VirtualKeyShort.F6,
        ["f7"] = VirtualKeyShort.F7,
        ["f8"] = VirtualKeyShort.F8,
        ["f9"] = VirtualKeyShort.F9,
        ["f10"] = VirtualKeyShort.F10,
        ["f11"] = VirtualKeyShort.F11,
        ["f12"] = VirtualKeyShort.F12,
    };

    public static bool TryResolve(string name, out VirtualKeyShort key) => Map.TryGetValue(name.Trim(), out key);

    public static string KnownNames => string.Join(", ", Map.Keys);
}
