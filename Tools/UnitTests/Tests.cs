using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using RakNet;

namespace UnitTests
{
    class Tests
    {
        static void Main(string[] args)
        {
            Config config = Config.ReadFile("sample.yml");
            TestResult("Config reader",
                () => config.Find("ship-to").Find("state").Value == "KS",
                () => config.Find("items").Children[1].Find("quantity").Value == "1",
                () => config.FindValueOrDefault("notPresent", "default") == "default"
                );

            TestResult("Message read/write",
                () => ReadWriteUShort(),
                () => ReadWriteUInt(),
                () => ReadWriteULong()
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

        private static bool ReadWriteUShort()
        {
            ushort val1 = 27;

            Message m = new Message(0, RakNet.PacketPriority.MEDIUM_PRIORITY, RakNet.PacketReliability.RELIABLE, 0);
            m.Write(val1);

            m.ResetRead();
            ushort val2 = m.ReadUShort();

            return val2 == val1;
        }

        private static bool ReadWriteUInt()
        {
            uint val1 = 27;

            Message m = new Message(0, RakNet.PacketPriority.MEDIUM_PRIORITY, RakNet.PacketReliability.RELIABLE, 0);
            m.Write(val1);

            m.ResetRead();
            uint val2 = m.ReadUInt();

            return val2 == val1;
        }

        private static bool ReadWriteULong()
        {
            ulong val1 = 27;

            Message m = new Message(0, RakNet.PacketPriority.MEDIUM_PRIORITY, RakNet.PacketReliability.RELIABLE, 0);
            m.Write(val1);

            m.ResetRead();
            ulong val2 = m.ReadULong();

            return val2 == val1;
        }
    }
}
