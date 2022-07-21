using Windows.Win32.Foundation;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace Mesh;

static class OSAL
{
    internal static string GetSteamPath() => (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null)!;

    internal static string GetCloudPath()
    {
        unsafe
        {
            PInvoke.SHGetKnownFolderPath(PInvoke.FOLDERID_SkyDrive, 0, null, out var Buffer);
            var _ = new string(Buffer);
            PInvoke.CoTaskMemFree(Buffer);
            return _;
        }
    }

    unsafe static internal string? TryGetSetting(string File, string Section, string Key)
    {
        var MAX_PATH = 260U;
        fixed (char* Buffer = new char[MAX_PATH])
            return 0 < PInvoke.GetPrivateProfileString(Section, Key, null, Buffer, MAX_PATH, File) ? new string(Buffer) : null;
    }
}