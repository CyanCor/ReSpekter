using System.IO;
using System.Reflection;

namespace CyanCor.ReSpekter
{
    class Program
    {
        static void Main(string[] args)
        {
            var c = new Context();
            c.Run(Assembly.LoadFile(Path.GetFullPath(args[0])), new object[] { args });
        }
    }
}
