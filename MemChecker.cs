using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MemCheck
{
    internal class MemChecker
    {
        private readonly Queue<int[]> myArrays = new ();

        public MemChecker()
        {
        }

        public void Run(int count = 100)
        {
            int nProcessors = Environment.ProcessorCount;
            long pageSize = Environment.SystemPageSize;

            Console.WriteLine($"Got {nProcessors} logical processors");
            Console.WriteLine($"Page size is {pageSize} Bytes");

            while (count-- > 0)
            {
                const int gbToAlloc = 4;

                int[] m1;

                try
                {
                    m1 = AllocAndWait(gbToAlloc, TimeSpan.FromSeconds(1));
                }
                catch (OutOfMemoryException ex)
                {
                    Console.WriteLine($"{ex.Message} - Returning all bytes");

                    myArrays.Clear();
                    continue;
                }

                myArrays.Enqueue(m1);

                Console.WriteLine($"{gbToAlloc} GB memory chunk enqueued - {myArrays.Count * gbToAlloc} GB total");

                if (myArrays.Count > 1)
                {
                    Console.WriteLine("Start copying memory chunks…");
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    var source = myArrays.Peek();
                    int pCount = 0;
                    Task[] bTask = new[] { Task.CompletedTask, Task.CompletedTask, Task.CompletedTask };

                    foreach (var dest in myArrays.Skip(1))
                    {
                        int threadNumber = pCount % (nProcessors - 1);
                        bTask[threadNumber] = Task.Run(() =>
                        {
                            Console.WriteLine($"THREAD {threadNumber + 1} START copying {gbToAlloc} GB chunk…");
                            var timer = System.Diagnostics.Stopwatch.StartNew();
                            Array.Copy(source, dest, dest.Length);
                            var elapsed = timer.Elapsed.TotalSeconds;
                            Console.WriteLine($"THREAD {threadNumber + 1} DONE in {elapsed:0.0} s ({gbToAlloc * 1024 / elapsed:0} MB/s)");
                        });

                        if (threadNumber == (nProcessors - 2))
                        {
                            Console.WriteLine("Waiting all threads done…");
                            Task.WaitAll(bTask);
                        }

                        pCount++;
                    }

                    Console.WriteLine("Waiting all threads done…");
                    Task.WaitAll(bTask);
                    Console.WriteLine("Took {0:0.0} s total", stopwatch.Elapsed.TotalSeconds);
                }

                Console.WriteLine("Waiting 3 s until repeat…");
                Thread.Sleep((int)TimeSpan.FromSeconds(3).TotalMilliseconds);
            }
        }

        private static int[] AllocAndWait(int allocGb, TimeSpan waitTime)
        {
            long intToAlloc = allocGb * 1024L * 1024L * 1024L / sizeof(int);
            int[] myMem = new int[intToAlloc];

            myMem[0] = 0x55;
            myMem[intToAlloc - 1] = 0xaa;

            Console.WriteLine($"{allocGb} GB memory chunk allocated! Waiting…");
            Thread.Sleep((int)waitTime.TotalMilliseconds);

            return myMem;
        }
   }
}