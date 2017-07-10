using System;
using System.Collections.Generic;
using System.IO;

namespace JLisp.Tests
{
    internal static class CsvReader
    {
        public static IEnumerable<Line> ReadLines(string fname)
        {
            var dir = Directory.GetCurrentDirectory();
            Console.WriteLine(dir);
            using (TextReader r = new StreamReader(File.OpenRead("../../../CSV/" + fname)))
            {
                string lstr;
                while ((lstr = r.ReadLine()) != null)
                {
                    var split = lstr.Split('£');
                    yield return new Line((TestType)int.Parse(split[0]), split[1], split[2].Trim());
                }

            }
        }
    }

    struct Line
    {
        public TestType TestType { get; }
        public string TestValue { get; }
        public string ExpectedValue { get; }

        public Line(TestType testType, string testValue, string expectedValue)
        {
            TestType = testType;
            TestValue = testValue;
            ExpectedValue = expectedValue;
        }
    }

    enum TestType { Normal = 1, ParserError = 2}
}
