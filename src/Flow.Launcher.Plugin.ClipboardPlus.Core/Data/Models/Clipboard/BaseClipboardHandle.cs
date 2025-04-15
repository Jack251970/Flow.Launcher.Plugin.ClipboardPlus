// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.Diagnostics;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// BaseClipboardHandle is a base class that handles the clipboard
/// </summary>
[SupportedOSPlatform("windows5.0")]
internal class BaseClipboardHandle
{
    #region Fields

    protected static readonly HRESULT CLIPBOARD_E_CANT_OPEN = unchecked((HRESULT)0x800401D0);
    protected static readonly HRESULT RPC_SERVER_UNAVAILABLE = unchecked((HRESULT)0x800706BA);

    protected HWND _executableHandle = HWND.Null;
    protected string _executableName = string.Empty;
    protected string _executablePath = string.Empty;
    protected string _executableTitle = string.Empty;

    #endregion

    #region Methods

    #region Souce App Management

    protected unsafe bool GetApplicationInfo()
    {
        _executableHandle = HWND.Null;
        _executableName = string.Empty;
        _executableTitle = string.Empty;
        _executablePath = string.Empty;

        try
        {
            _executableHandle = PInvoke.GetForegroundWindow();

            uint processId = 0;
            _ = PInvoke.GetWindowThreadProcessId(_executableHandle, &processId);
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName;
            if (process.MainModule is ProcessModule processModule)
            {
                _executablePath = processModule.FileName;
                _executableName = _executablePath[(_executablePath.LastIndexOf(@"\", StringComparison.Ordinal) + 1)..];
            }

            // Edited from: https://github.com/taooceros
            const int capacity = 256;
            Span<char> buffer = capacity < 1024 ? stackalloc char[capacity] : new char[capacity];
            fixed (char* pBuffer = buffer)
            {
                // If the window has no title bar or text, if the title bar is empty,
                // or if the window or control handle is invalid, the return value is zero.
                var length = PInvoke.GetWindowText(_executableHandle, (PWSTR)pBuffer, capacity);
                _executableTitle = buffer[..length].ToString();
            }

            return true;
        }
        catch (Exception)
        {
            // ignored
            return true;
        }
    }

    #endregion

    #endregion
}
