// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using DotNetEnv;

namespace SoruxBotPublishCli
{
    public static class Program
    {
        private static string? _dotnetPath;
        private static string? _outputPath;
        private static string? _toolPath;
        private static string? _cwd;

        private static void Init()
        {
            _cwd = Directory.GetCurrentDirectory();
            Console.WriteLine(_cwd);
            // 加载 .env 文件
            Env.Load();

            // 读取配置
            _dotnetPath = Env.GetString("SORUX_DOTNET_PATH");
            _outputPath = Env.GetString("SORUX_OUTPUT_PATH");
            _toolPath = Env.GetString("SORUX_TOOL_PATH");

            if (_dotnetPath == null)
            {
                Console.WriteLine("DOTNET_PATH is not set in .env file, using default value.");
                _dotnetPath = "dotnet";
            }

            if (_outputPath == null)
            {
                Console.WriteLine("OUTPUT_PATH is not set in .env file, using default value.");
                _outputPath = "./out.dll";
            }
            
            if (_toolPath == null)
            {
                Console.WriteLine("TOOL_PATH is not set in .env file, using default value.");
                _toolPath = "./resources/ILRepack.exe";
            }


            Console.WriteLine("SORUX_DOTNET_PATH: " + _dotnetPath);
            Console.WriteLine("SORUX_OUTPUT_PATH: " + _outputPath);
            Console.WriteLine("SORUX_TOOL_PATH: " + _toolPath);


            // Console.WriteLine(Environment.GetEnvironmentVariable("DOTNET_PATH"));
        }

        public static void Main(string[] args)
        {
            Init();
            var csprojArr = Directory.GetFiles(_cwd!,
                "*.csproj", SearchOption.TopDirectoryOnly).ToList();

            if (csprojArr.Count != 1)
            {
                Console.WriteLine("There should be only one csproj file in the directory.");
                return;
            }

            var csprojPath = csprojArr[0];
            Console.WriteLine("find proj => " + csprojPath);

            var list = DllGetter.GetDllList(csprojPath);

            Console.WriteLine("your proj dependency dll => ");

            foreach (var dll in list)
            {
                Console.WriteLine(" => " + dll);
            }
            
            var mainDll =
                Path.GetFileName(csprojPath)
                    .Replace(".csproj", ".dll");

            Console.WriteLine("mainDll => " + mainDll);
            // return;
            var paths = Directory.GetFiles(_cwd!,
                    mainDll, SearchOption.AllDirectories)
                .Where(t => t.Contains("/bin", StringComparison.CurrentCultureIgnoreCase))
                .Where(t => t.Contains("/release", StringComparison.CurrentCultureIgnoreCase))
                .ToList();
            Console.WriteLine(paths.Count);

            if (paths.Count != 1)
            {
                Console.WriteLine("There should be only one main dll file in the directory.");
                return;
            }

            list.Add(paths[0]);
            Exec(list);
        }

        private static void Exec(List<string> list)
        {
            // 通过命令行运行程序il-repack
            // 创建一个新的进程
            var process = new Process();

            var argumentsStr = _toolPath + " /out:" + _outputPath;
            argumentsStr = list.Aggregate(argumentsStr, (current, dll) => current + (" " + dll));

            // 设置进程启动信息
            process.StartInfo.FileName = _dotnetPath;
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
                Console.WriteLine("ERROR: " + args.Data);
            };

            try
            {
                // 启动进程
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 等待进程退出
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }
}