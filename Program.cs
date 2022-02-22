using System;

namespace MemCheck
{
    public static class Program
    {
        private static void Main()
        {
            Console.WriteLine("Hello MemCheck!");

            new MemChecker().Run();
        }
    }
}
