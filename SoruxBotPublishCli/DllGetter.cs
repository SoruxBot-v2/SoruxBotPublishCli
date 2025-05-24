using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using System.Reflection;
using SoruxBot.SDK.Plugins.Basic;

namespace SoruxBotPublishCli;

public static class DllGetter
{
    private static readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    
    static DllGetter()
    {
        // 注册程序集解析事件，用于处理依赖项加载
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

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
                !sp.EvaluatedInclude.EndsWith("soruxbot.sdk", StringComparison.CurrentCultureIgnoreCase));

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

            // 如果目录存在,查找其中符合条件的 DLL 文件,并排除插件类库
            var dllFiles = Directory.GetFiles(nugetPackagePath,
                    "*.dll", SearchOption.AllDirectories)
                .Where(t => t.Contains("net6.0") || t.Contains("net8.0") || t.Contains("netstandard"))
                .Where(t => t.Contains("lib"))
                .Where(t => !IsPluginLibrary(t))
                .ToList();

            dllFiles.Sort();
            if (dllFiles.Count == 0) continue;
            dllPaths.Add(dllFiles[0]);
        }

        return dllPaths;
    }

    /// <summary>
    /// 检查指定的 DLL 是否是插件类库
    /// </summary>
    /// <param name="dllPath">DLL 文件路径</param>
    /// <returns>如果是插件类库返回 true，否则返回 false</returns>
    private static bool IsPluginLibrary(string dllPath)
    {
        try
        {
            // 尝试使用 LoadFrom 加载程序集，它会尝试解析依赖项
            Assembly assembly;
            
            // 检查是否已经加载过这个程序集
            var assemblyName = AssemblyName.GetAssemblyName(dllPath);
            var fullName = assemblyName.FullName;
            
            if (_loadedAssemblies.ContainsKey(fullName))
            {
                assembly = _loadedAssemblies[fullName];
            }
            else
            {
                assembly = Assembly.LoadFrom(dllPath);
                _loadedAssemblies[fullName] = assembly;
            }

            // 获取所有导出的类型并检查是否有继承自 SoruxBotLib 的类型
            var exportedTypes = assembly.GetExportedTypes();
            return exportedTypes.Any(p => p.BaseType?.FullName == typeof(SoruxBotLib).FullName);
        }
        catch (Exception ex)
        {
            // 如果加载失败，记录错误但不影响程序继续执行
            Console.WriteLine($"警告：无法加载程序集 {dllPath}: {ex.Message}");
            
            // 如果是依赖项缺失等问题，假设它不是插件库
            return false;
        }
    }

    /// <summary>
    /// 程序集解析事件处理器，用于处理依赖项加载
    /// </summary>
    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            // 尝试从已加载的程序集中查找
            if (_loadedAssemblies.ContainsKey(args.Name))
            {
                return _loadedAssemblies[args.Name];
            }

            // 尝试从 NuGet 包目录中查找依赖项
            var assemblyName = new AssemblyName(args.Name);
            var packageName = assemblyName.Name?.ToLower();
            
            if (string.IsNullOrEmpty(packageName))
                return null;

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nugetPackagesPath = Path.Combine(userProfile, ".nuget", "packages");
            
            if (!Directory.Exists(nugetPackagesPath))
                return null;

            // 在 NuGet 包目录中搜索匹配的程序集
            var packageDirs = Directory.GetDirectories(nugetPackagesPath, packageName + "*", SearchOption.TopDirectoryOnly);
            
            foreach (var packageDir in packageDirs)
            {
                var dllFiles = Directory.GetFiles(packageDir, $"{packageName}.dll", SearchOption.AllDirectories)
                    .Where(f => f.Contains("net6.0") || f.Contains("net8.0") || f.Contains("netstandard"))
                    .Where(f => f.Contains("lib"))
                    .ToList();

                if (dllFiles.Any())
                {
                    var assembly = Assembly.LoadFrom(dllFiles.First());
                    _loadedAssemblies[args.Name] = assembly;
                    return assembly;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}