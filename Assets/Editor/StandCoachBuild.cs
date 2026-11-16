using System.IO;
using ActiveMotion3D.EndlessRunner.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal sealed class StandCoachBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 10;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows64) return;

        const string folder = "StandCoachRuntime";
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
        if (!StandCoachRuntime.HasRuntimeFiles(source))
        {
            Debug.LogWarning("This build is missing Stand Coach. Run Tools/Setup-StandCoach.ps1, then build again to include offline feedback.");
            return;
        }

        string executable = report.summary.outputPath;
        string playerData = Path.Combine(Path.GetDirectoryName(executable), Path.GetFileNameWithoutExtension(executable) + "_Data");
        string destination = Path.Combine(playerData, "StreamingAssets", folder);
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(file, target, true);
        }
    }
}
