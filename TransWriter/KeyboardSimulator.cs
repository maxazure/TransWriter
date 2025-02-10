using System;
using System.Runtime.InteropServices;

public class KeyboardSimulator
{
    // 定义输入结构
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // 引入 Windows API
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // 常量定义
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// 发送组合键（如 Ctrl + V）。
    /// </summary>
    /// <param name="modifiers">修饰键（如 Ctrl、Alt、Shift），可以为 null。</param>
    /// <param name="key">主要按键（如 V 键）。</param>
    public static void SendKeyCombo(ushort[] modifiers, ushort key)
    {
        int inputCount = (modifiers?.Length ?? 0) * 2 + 2; // 每个修饰键按下+释放，加上主键按下+释放
        INPUT[] inputs = new INPUT[inputCount];
        int index = 0;

        // 1. 按下修饰键（如 Ctrl）
        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                inputs[index++] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT { wVk = mod, dwFlags = 0 }
                    }
                };
            }
        }

        // 2. 按下主键（如 V）
        inputs[index++] = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT { wVk = key, dwFlags = 0 }
            }
        };

        // 3. 释放主键
        inputs[index++] = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT { wVk = key, dwFlags = KEYEVENTF_KEYUP }
            }
        };

        // 4. 释放修饰键
        if (modifiers != null)
        {
            for (int i = modifiers.Length - 1; i >= 0; i--)
            {
                inputs[index++] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT { wVk = modifiers[i], dwFlags = KEYEVENTF_KEYUP }
                    }
                };
            }
        }

        // 发送输入
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}
