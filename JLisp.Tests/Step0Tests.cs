using System;
using System.Diagnostics;
using JLisp.Parsing.Types;
using JLisp.Tests.Repls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JLisp.Tests
{
    [TestClass] 
    public class Step0Tests
    {
        [DataTestMethod]
        [DataRow(typeof(Repl0))]
        [DataRow(typeof(Repl1))]
        public void TestReplX(Type type)
        {
            var p = (IProcess)Activator.CreateInstance(type);
            foreach (var line in CsvReader.ReadLines($"{type.Name}.csv"))
            {
                Trace.Write($"\nTestValue: {line.TestValue}, ExpectedValue {line.ExpectedValue}");
                switch (line.TestType)
                {
                    case TestType.Normal:
                        var accualValue = p.Process(line.TestValue);
                        Assert.AreEqual(line.ExpectedValue, accualValue, $"\nTestValue: {line.TestValue}");
                        break;
                    case TestType.ParserError:
                        Assert.ThrowsException<ParseError>(() => p.Process(line.TestValue), line.ExpectedValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }

        }
    }
}
