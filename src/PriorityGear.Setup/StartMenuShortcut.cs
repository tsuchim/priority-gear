using System.Runtime.InteropServices;
using System.Text;

namespace PriorityGear.Setup;

public static class StartMenuShortcut
{
    public static ShortcutSpec CreateSpec(SetupInstallPlan plan)
    {
        return new ShortcutSpec(
            plan.StartMenuShortcutPath,
            plan.AppExePath,
            plan.VersionInstallDirectory,
            "PriorityGear");
    }

    public static void Create(SetupInstallPlan plan)
    {
        ShortcutSpec spec = CreateSpec(plan);
        Directory.CreateDirectory(Path.GetDirectoryName(spec.ShortcutPath)!);

        object? shellLinkObject = null;
        try
        {
            shellLinkObject = new ShellLink();
            IShellLinkW shellLink = (IShellLinkW)shellLinkObject;
            shellLink.SetPath(spec.TargetPath);
            shellLink.SetWorkingDirectory(spec.WorkingDirectory);
            shellLink.SetDescription(spec.Description);
            ((IPersistFile)shellLink).Save(spec.ShortcutPath, remember: true);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                $"Failed to create Start Menu shortcut: {spec.ShortcutPath}; target: {spec.TargetPath}",
                ex);
        }
        finally
        {
            if (shellLinkObject is not null && Marshal.IsComObject(shellLinkObject))
            {
                Marshal.FinalReleaseComObject(shellLinkObject);
            }
        }

        if (!File.Exists(spec.ShortcutPath))
        {
            throw new FileNotFoundException($"Start Menu shortcut was not created: {spec.ShortcutPath}", spec.ShortcutPath);
        }
    }

    public static void Delete(SetupInstallPlan plan)
    {
        if (File.Exists(plan.StartMenuShortcutPath))
        {
            File.Delete(plan.StartMenuShortcutPath);
        }

        if (Directory.Exists(plan.StartMenuDirectory) &&
            !Directory.EnumerateFileSystemEntries(plan.StartMenuDirectory).Any())
        {
            Directory.Delete(plan.StartMenuDirectory);
        }
    }
}

public sealed record ShortcutSpec(
    string ShortcutPath,
    string TargetPath,
    string WorkingDirectory,
    string Description);

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
internal sealed class ShellLink;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal interface IShellLinkW
{
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);

    void GetIDList(out IntPtr ppidl);

    void SetIDList(IntPtr pidl);

    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    void GetHotkey(out short pwHotkey);

    void SetHotkey(short wHotkey);

    void GetShowCmd(out int piShowCmd);

    void SetShowCmd(int iShowCmd);

    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

    void Resolve(IntPtr hwnd, uint fFlags);

    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("0000010b-0000-0000-C000-000000000046")]
internal interface IPersistFile
{
    void GetClassID(out Guid pClassID);

    void IsDirty();

    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

    void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool remember);

    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

    void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}
