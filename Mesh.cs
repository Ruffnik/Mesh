using Microsoft.Win32.TaskScheduler;
using System.Reflection;

namespace Mesh;//Apart from the below-mentioned 2do-points, there's also creating a nicer conflict UI, TaskDialog with WindowsAPICodePack should do the trick, alternatively move everything to NativeMethods.txt & do shit by hand; porting this INI mess to .NET XML would be badly needed either way

static class Mesh
{
    readonly static string Caption = Assembly.GetEntryAssembly()!.GetName().Name!;
    readonly static string CWD = Path.Combine(OSAL.GetCloudPath(), Caption);

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            foreach (var Game in Directory.GetFiles(CWD, "*.ini"))
                ProcessGame(Game);
        }
        catch (Exception Ex)
        {
            MessageBox.Show($"{Ex}", Caption);
        }
#if RELEASE
        TaskService Service = new();
        var Definition = Service.NewTask();
        Definition.Triggers.Add(new DailyTrigger());
        Definition.Actions.Add(Path.ChangeExtension(Environment.GetCommandLineArgs()[0], "exe"));
        Definition.Settings.StartWhenAvailable = true;
        Service.RootFolder.RegisterTaskDefinition(Caption, Definition);
#endif
    }

    static void ProcessGame(string Game)//Port this to a single call over an Array of Strings, Delegates & stuff
    {
        for (var i = 0; ; i++)
        {
            var Folder = OSAL.TryGetSetting(Game, "Folders", i.ToString())!;
            if (Folder is null || !TryProcessFolder(CWD, Folder, Path.GetFileNameWithoutExtension(Game)))
                break;
        }
        for (var i = 0; ; i++)
        {
            var File = OSAL.TryGetSetting(Game, "Files", i.ToString())!;
            if (File is null || !ProcessFile(CWD, File, Path.GetFileNameWithoutExtension(Game)))
                break;
        }
        for (var i = 0; ; i++)
        {
            var Key = OSAL.TryGetSetting(Game, "Keys", i.ToString())!;
            if (Key is null || !ProcessKey(CWD, Key, Path.GetFileNameWithoutExtension(Game)))
                break;
        }
    }

    private static bool ProcessKey(string CWD, string Key, string Game)
    {
        //There's no reparse points for keys->things get more complicated
        //-dump the key into a file, be it in LocalAppData or some .NET "put your stuff there"-stuff
        //-keep a last-known-checksum of key & file
        //-use checksums to see what changed since last time
        //-one changed->unisync else bisync
        //-remember to update last-known-checksum
        //-need filesystem/registry watchers to trigger sync
        //-reparse point for dump
        throw new NotImplementedException();
    }

    private static bool ProcessFile(string CWD, string File, string Game)
    {
        //The same plan as with folders: reparse points
        //Check if a hard or a symbolic link does the trick
        throw new NotImplementedException();
    }

    static bool TryProcessFolder(string CWD, string Folder, string Game)
    {
        var Local = new FileInfo(Path.GetFullPath(Environment.ExpandEnvironmentVariables(Resolve(Folder))));
        var Cloud = new FileInfo(Path.GetFullPath(Path.Combine(CWD, Game, Local.Name)));
        if ((Local.Attributes.HasFlag(FileAttributes.ReparsePoint) && Cloud.Attributes.HasFlag(FileAttributes.ReparsePoint)) || (!Directory.Exists(Local.FullName) && !Directory.Exists(Cloud.FullName)))
            return false;
        else
        {
            if (!Directory.Exists(Cloud.DirectoryName))
                Directory.CreateDirectory(Cloud.DirectoryName!);
            return TryUniSyncFolder(Local, Cloud) || TryUniSyncFolder(Cloud, Local) || TryBiSyncFolder(Local, Cloud, Game);
        }
    }

    private static string Resolve(string RawPath)
    {
        const char Separator = '|';
        if (RawPath.Contains(Separator))
        {
            var Parts = RawPath.Split(Separator);
            RawPath = Path.Combine(Parts[1] switch
            {
                "SteamApps" => Path.Combine(OSAL.GetSteamPath(), "SteamApps", "Common"),
                _ => throw new ArgumentException(Parts[1]),
            }, Parts[2]);
        }
        return RawPath;
    }

    static bool TryBiSyncFolder(FileInfo Local, FileInfo Cloud, string Game)
    {
        switch (TaskDialog.ShowDialog(new TaskDialogPage()
        {
            AllowMinimize = true,
            Caption = Caption,
            Icon = TaskDialogIcon.Warning,
            Heading = $"Select the state to keep for {Game}:",
            Text = $@"{nameof(Local)} from {Local.LastWriteTime}
{nameof(Cloud)} {Cloud.LastWriteTime}",
            Buttons = new TaskDialogButtonCollection { nameof(Local), nameof(Cloud) }
        }).Text)
        {
            case nameof(Local):
                Directory.Delete(Cloud.FullName, true);
                return TryUniSyncFolder(Local, Cloud);
            case nameof(Cloud):
                Directory.Delete(Local.FullName, true);
                return TryUniSyncFolder(Cloud, Local);
            default:
                return false;
        }
    }

    static bool TryUniSyncFolder(FileInfo From, FileInfo To)
    {
        if (!Directory.Exists(From.FullName))
            return false;
        else if (!Directory.Exists(To.FullName))
        {
            Directory.CreateSymbolicLink(To.FullName, From.FullName);
            return true;
        }
        else
            return To.Attributes.HasFlag(FileAttributes.ReparsePoint) && From.FullName.Equals(To.LinkTarget, StringComparison.InvariantCultureIgnoreCase);
    }
}