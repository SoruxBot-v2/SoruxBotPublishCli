using System.Diagnostics;
using System.Diagnostics;
using DotNetEnv;

namespace SoruxBotPublishCli;

public class ExecMerge
{
    private static readonly string? DotnetPath;
    private static readonly string? OutputPath;
    private static readonly string? ToolPath;
    private static readonly string? Cwd;
    private static readonly string? CsprojPath;
    private static readonly List<string> DepDllList;
    private static bool errorFlag = false;

    static ExecMerge()
    {
        Cwd = Directory.GetCurrentDirectory();
        SimpleLogger.Info("current work dir -> " + Cwd);
        
        // 获取工具所在路径
        ToolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "ILRepack.exe");
        
        // 加载 .env 文件
        Env.Load();

        // 读取配置
        DotnetPath = Env.GetString("SORUX_DOTNET_PATH");
        OutputPath = Env.GetString("SORUX_OUTPUT_PATH");
        


        if (DotnetPath == null)
        {
            SimpleLogger.Warning("SORUX_DOTNET_PATH is not set, using default value.");
            DotnetPath = "dotnet";
        }

        if (OutputPath == null)
        {
            SimpleLogger.Warning("SORUX_OUTPUT_PATH is not set, using default value.");
            OutputPath = "./out.dll";
        }


        SimpleLogger.Info("SORUX_DOTNET_PATH: " + DotnetPath);
        SimpleLogger.Info("SORUX_OUTPUT_PATH: " + OutputPath);
        SimpleLogger.Info("SORUX_TOOL_PATH: " + ToolPath);


        // Console.WriteLine(Environment.GetEnvironmentVariable("DOTNET_PATH"));

        var csprojArr = Directory.GetFiles(Cwd,
            "*.csproj", SearchOption.TopDirectoryOnly).ToList();

        if (csprojArr.Count != 1)
        {
            SimpleLogger.Error("There should be only one csproj file in current working directory.");
            Environment.Exit(1);
        }

        CsprojPath = csprojArr[0];
        SimpleLogger.Info("find proj: " + CsprojPath);

        DepDllList = DllGetter.GetDllList(CsprojPath);

        SimpleLogger.Info("your proj dependency dll: ");

        foreach (var dll in DepDllList)
        {
            Console.WriteLine(" => " + dll);
        }

        var mainDll =
            Path.GetFileName(CsprojPath)
                .Replace(".csproj", ".dll");

        var paths = Directory.GetFiles(Cwd,
                mainDll, SearchOption.AllDirectories)
            .Where(t =>
                t.Contains("bin", StringComparison.CurrentCulture)
                && t.Contains("Release", StringComparison.CurrentCulture)
                && t.Contains("publish", StringComparison.CurrentCulture)
            ).ToList();
        foreach (var path in paths)
        {
            Console.WriteLine(path);
        }

        Console.WriteLine(paths.Count);

        if (paths.Count != 1)
        {
            Console.WriteLine("There should be only one main dll file in the directory.");
            Environment.Exit(1);
        }

        SimpleLogger.Info("your mainDll path: ");
        Console.WriteLine(" => " + paths[0]);

        DepDllList.Add(paths[0]);
    }

    public static void Exec()
    {
        // 通过命令行运行程序il-repack
        // 创建一个新的进程
        var process = new Process();
        SimpleLogger.Info("starting il-repack process...");

        var argumentsStr = ToolPath + " /out:" + OutputPath;
        argumentsStr = DepDllList.Aggregate(argumentsStr,
            (current, dll) => current + " " + dll);

        // 设置进程启动信息
        process.StartInfo.FileName = DotnetPath;
        process.StartInfo.Arguments = argumentsStr;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        // 绑定输出和错误数据接收事件
        process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data == null) return;
            SimpleLogger.Error(args.Data);
            errorFlag = true;
        };

        try
        {
            // 启动进程
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 等待进程退出
            process.WaitForExit();

            if (!errorFlag)
            {
                SimpleLogger.Info("built successfully!");
            }
                
            
            
        }
        catch (Exception ex)
        {
            SimpleLogger.Error("Exception: " + ex.Message);
        }
    }
}