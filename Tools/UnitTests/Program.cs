using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace UnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = Config.ReadFile("sample.yml");
            TestResult("Config reader",
                () => config.Find("ship-to").Find("state").Value == "KS",
                () => config.Find("items").Children[1].Find("quantity").Value == "1",
                () => config.FindValueOrDefault("notPresent", "default") == "default"
                );

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        delegate bool testFunc();

        static void TestResult(string name, params testFunc[] tests)
        {
            Console.WriteLine(name);
            int failures = 0;

            for (int i = 0; i < tests.Length; i++)
            {
                Console.Write("  ");
                Console.Write(i + 1);
                Console.Write("/");
                Console.Write(tests.Length);
                Console.Write(" ... ");

                if (tests[i]())
                    Console.WriteLine("passed");
                else
                {
                    Console.WriteLine("FAILED");
                    failures++;
                }
            }

            if (failures == 0)
                Console.WriteLine(name + " passed all tests");
            else
            {
                Console.Error.Write(name);
                Console.Error.Write(" failed ");
                Console.Error.Write(failures);
                Console.Error.Write("/");
                Console.Error.Write(tests.Length);
                Console.Error.WriteLine(" tests");
            }
            Console.WriteLine();
        }
    }
}
