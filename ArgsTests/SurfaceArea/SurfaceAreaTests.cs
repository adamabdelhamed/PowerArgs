using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using PowerArgs;
using ArgsTests.SurfaceArea;
using System.IO;

namespace ArgsTests
{
    public class TestGenericType { }

    [TestClass]
    public class SurfaceAreaTests
    {
       
        // TODO - Turn this back on after implementing a better diff
        //[TestMethod]
        public void TestSurfaceArea()
        {
            Assembly testAssembly = typeof(Args).Assembly;
            AssemblyDef def = new AssemblyDef(testAssembly);
            var defString = JSON.Json.Stringify(def);
            AssemblyDef apiSurface = JSON.Json.Parse<AssemblyDef>(Resource1.APISurface);

            var diff = def.PocoDiff(apiSurface);

            if (diff.Count > 0)
            {
                foreach (var delta in diff)
                {
                    Console.WriteLine(delta);
                }
                Assert.Fail("API Surface Violation: " + diff[0]);
            }
        }
    }
}
