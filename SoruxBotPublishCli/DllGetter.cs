using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

namespace SoruxBotPublishCli;

public static class DllGetter
{
    public static List<string> GetDllList(string csprojPath)
    {
        // 注册 MSBuild
        MSBuildLocator.RegisterDefaults();
        
        // 获取 NuGet 依赖项的 DLL 路径
        return GetNuGetDllPaths(csprojPath);
        
    }

    private static List<string> GetNuGetDllPaths(string csprojPath)
    {
        var dllPaths = new List<string>();
        var project = new Project(csprojPath);

        // 从项目对象中获取所有 PackageReference 项
        var packageReferences = project.GetItems("PackageReference")
            .Where(sp =>
                !sp.EvaluatedInclude.Contains("soruxbot.sdk", StringComparison.CurrentCultureIgnoreCase));

        // 遍历所有 PackageReference 项
        foreach (var packageReference in packageReferences)
        {
            var packageName = packageReference.EvaluatedInclude.ToLower();
            var packageVersion = packageReference.GetMetadataValue("Version");

            // 构造 NuGet 包目录路径
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nugetPackagePath = Path.Combine(
                userProfile, ".nuget", "packages", packageName, packageVersion);

            if (!Directory.Exists(nugetPackagePath)) continue;
            
            // 如果目录存在，查找其中符合条件的 DLL 文件
            var dllFiles = 
                Directory.GetFiles(nugetPackagePath, 
                    "*.dll", SearchOption.AllDirectories)
                    .Where(t => t.Contains("net6.0") || t.Contains("net8.0")||t.Contains("netstandard"))
                    .Where(t=>t.Contains("lib"))
                    .ToList();
            
            // Console.WriteLine(dllFiles.Count);
            // foreach (var dllFile in dllFiles)
            // {
            //     Console.WriteLine(dllFile);
            //
            // }
            
            dllPaths.Sort();
            if (dllFiles.Count == 0) continue;
            dllPaths.Add(dllFiles[0]);
        }

        return dllPaths;
    }
}

