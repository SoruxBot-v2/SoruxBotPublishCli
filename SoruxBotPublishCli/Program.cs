// See https://aka.ms/new-console-template for more information


using System.Text;

namespace SoruxBotPublishCli
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            ExecMerge.Exec();
        }
    }
}