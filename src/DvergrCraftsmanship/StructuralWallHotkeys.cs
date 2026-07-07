using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace DvergrCraftsmanship;

internal static class StructuralWallHotkeys
{
    private static readonly HashSet<KeyCode> ModifierKeys = new()
    {
        KeyCode.LeftControl,
        KeyCode.RightControl,
        KeyCode.LeftShift,
        KeyCode.RightShift,
        KeyCode.LeftAlt,
        KeyCode.RightAlt,
    };

    internal static bool WasPressed(KeyboardShortcut shortcut)
    {
        Evaluate(NormalizeForGameplay(shortcut), out bool pressed, includeTrace: false, out _);
        return pressed;
    }

    internal static KeyboardShortcut NormalizeForGameplay(KeyboardShortcut shortcut)
    {
        KeyCode mainKey = shortcut.MainKey;
        if (mainKey == KeyCode.None || !IsModifierKey(mainKey))
        {
            return shortcut;
        }

        foreach (KeyCode key in shortcut.Modifiers)
        {
            if (!IsModifierKey(key))
            {
                List<KeyCode> modifiers = shortcut.Modifiers.Where(IsModifierKey).ToList();
                modifiers.Add(mainKey);
                return new KeyboardShortcut(key, modifiers.Distinct().OrderBy(static code => (int)code).ToArray());
            }
        }

        return shortcut;
    }

    internal static bool Evaluate(KeyboardShortcut shortcut, out bool pressed, bool includeTrace, out string trace)
    {
        shortcut = NormalizeForGameplay(shortcut);
        StringBuilder builder = includeTrace ? new StringBuilder() : null;
        KeyCode mainKey = shortcut.MainKey;
        bool mainDown = mainKey != KeyCode.None && ZInput.GetKeyDown(mainKey, logWarning: false);
        bool mainHeld = mainKey != KeyCode.None && ZInput.GetKey(mainKey, logWarning: false);
        bool modifiersOk = true;

        if (includeTrace)
        {
            builder.Append("main=").Append(mainKey)
                .Append(" down=").Append(mainDown)
                .Append(" held=").Append(mainHeld);
        }

        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            bool modifierHeld = ZInput.GetKey(modifier, logWarning: false);
            modifiersOk &= modifierHeld;
            if (includeTrace)
            {
                builder.Append(" | mod ").Append(modifier).Append('=').Append(modifierHeld);
            }
        }

        pressed = mainKey != KeyCode.None && mainDown && modifiersOk;
        if (includeTrace)
        {
            builder.Append(" => ").Append(pressed);
            trace = builder.ToString();
        }
        else
        {
            trace = string.Empty;
        }

        return pressed;
    }

    internal static bool ProbeAnyActivity(KeyCode mainKey, IEnumerable<KeyCode> modifiers)
    {
        if (mainKey != KeyCode.None &&
            (ZInput.GetKeyDown(mainKey, logWarning: false) || ZInput.GetKey(mainKey, logWarning: false)))
        {
            return true;
        }

        foreach (KeyCode modifier in modifiers)
        {
            if (ZInput.GetKeyDown(modifier, logWarning: false) || ZInput.GetKey(modifier, logWarning: false))
            {
                return true;
            }
        }

        return false;
    }

    internal static string FormatHint(KeyboardShortcut shortcut)
    {
        return NormalizeForGameplay(shortcut).ToString();
    }

    private static bool IsModifierKey(KeyCode keyCode)
    {
        return ModifierKeys.Contains(keyCode);
    }
}
