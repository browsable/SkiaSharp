using System.Runtime.InteropServices;

var RunNuGetRestore = new Action<FilePath> ((solution) =>
{
    NuGetRestore (solution, new NuGetRestoreSettings { 
        ToolPath = NugetToolPath,
        Source = NuGetSources,
        Verbosity = NuGetVerbosity.Detailed
    });
});

var RunDotNetCoreRestore = new Action<string> ((solution) =>
{
    DotNetCoreRestore (solution, new DotNetCoreRestoreSettings { 
        Sources = NuGetSources,
        Verbosity = DotNetCoreRestoreVerbosity.Verbose
    });
});

var PackageNuGet = new Action<FilePath, DirectoryPath> ((nuspecPath, outputPath) =>
{
    if (!DirectoryExists (outputPath)) {
        CreateDirectory (outputPath);
    }

    NuGetPack (nuspecPath, new NuGetPackSettings { 
        Verbosity = NuGetVerbosity.Detailed,
        OutputDirectory = outputPath,        
        BasePath = "./",
        ToolPath = NugetToolPath
    });                
});

var RunProcess = new Action<FilePath, ProcessSettings> ((process, settings) =>
{
    var result = StartProcess (process, settings);
    if (result != 0) {
        throw new Exception ("Process '" + process + "' failed with error: " + result);
    }
});

var RunTests = new Action<FilePath, bool> ((testAssembly, is64) =>
{
    var dir = testAssembly.GetDirectory ();
    RunProcess (is64 ? TestConsoleToolPath_x64 : TestConsoleToolPath_x86, new ProcessSettings {
        Arguments = string.Format ("\"{0}\"", testAssembly),
    });
});

var RunMdocUpdate = new Action<FilePath[], DirectoryPath, DirectoryPath[]> ((assemblies, docsRoot, refs) =>
{
    var refArgs = string.Empty;
    if (refs != null) {
        refArgs = string.Join (" ", refs.Select (r => string.Format ("--lib=\"{0}\"", r)));
    }
    var assemblyArgs = string.Join (" ", assemblies.Select (a => string.Format ("\"{0}\"", a)));
    RunProcess (MDocPath, new ProcessSettings {
        Arguments = string.Format ("update --preserve --out=\"{0}\" {1} {2}", docsRoot, refArgs, assemblyArgs),
    });
});

var RunMdocMSXml = new Action<DirectoryPath, DirectoryPath> ((docsRoot, outputDir) =>
{
    RunProcess (MDocPath, new ProcessSettings {
        Arguments = string.Format ("export-msxdoc \"{0}\"", MakeAbsolute (docsRoot)),
        WorkingDirectory = MakeAbsolute (outputDir).ToString ()
    });
});

var RunMdocAssemble = new Action<DirectoryPath, FilePath> ((docsRoot, output) =>
{
    RunProcess (MDocPath, new ProcessSettings {
        Arguments = string.Format ("assemble --out=\"{0}\" \"{1}\"", output, docsRoot),
    });
});

var ClearSkiaSharpNuGetCache = new Action (() => {
    // first we need to add our new nuget to the cache so we can restore
    // we first need to delete the old stuff
    DirectoryPath home = EnvironmentVariable ("USERPROFILE") ?? EnvironmentVariable ("HOME");
    var installedNuGet = home.Combine (".nuget").Combine ("packages").Combine ("SkiaSharp");
    if (DirectoryExists (installedNuGet)) {
        Warning ("SkiaSharp nugets were installed at '{0}', removing...", installedNuGet);
        CleanDirectory (installedNuGet);
    }
    installedNuGet = home.Combine (".nuget").Combine ("packages").Combine ("SkiaSharp.Views");
    if (DirectoryExists (installedNuGet)) {
        Warning ("SkiaSharp nugets were installed at '{0}', removing...", installedNuGet);
        CleanDirectory (installedNuGet);
    }
    installedNuGet = home.Combine (".nuget").Combine ("packages").Combine ("SkiaSharp.Views.Forms");
    if (DirectoryExists (installedNuGet)) {
        Warning ("SkiaSharp nugets were installed at '{0}', removing...", installedNuGet);
        CleanDirectory (installedNuGet);
    }
});

internal static class MacPlatformDetector
{
    internal static readonly Lazy<bool> IsMac = new Lazy<bool> (IsRunningOnMac);

    [DllImport ("libc")]
    static extern int uname (IntPtr buf);

    static bool IsRunningOnMac ()
    {
        IntPtr buf = IntPtr.Zero;
        try {
            buf = Marshal.AllocHGlobal (8192);
            // This is a hacktastic way of getting sysname from uname ()
            if (uname (buf) == 0) {
                string os = Marshal.PtrToStringAnsi (buf);
                if (os == "Darwin")
                    return true;
            }
        } catch {
        } finally {
            if (buf != IntPtr.Zero)
                Marshal.FreeHGlobal (buf);
        }
        return false;
    }
}

var IsRunningOnMac = new Func<bool> (() => {
    return System.Environment.OSVersion.Platform == PlatformID.MacOSX || MacPlatformDetector.IsMac.Value;
});

var IsRunningOnLinux = new Func<bool> (() => {
    return IsRunningOnUnix () && !IsRunningOnMac ();
});
