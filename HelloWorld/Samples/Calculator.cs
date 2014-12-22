using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelloWorld.Samples
{

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling), TabCompletion(typeof(ItemNameCompletion), REPL = true, HistoryToSave = 10)]
    public class CalculatorProgram
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [StickyArg]
        public string TheSticky { get; set; }

        [ArgActionMethod, ArgDescription("Adds the two operands")]
        public void Add(TwoOperandArgs args)
        {
            Console.WriteLine(args.Value1 + args.Value2);
        }

        [ArgActionMethod, ArgDescription("Subtracts the two operands")]
        public void Subtract(TwoOperandArgs args)
        {
            Console.WriteLine(args.Value1 - args.Value2);
        }

        [ArgActionMethod, ArgDescription("Multiplies the two operands")]
        public void Multiply(TwoOperandArgs args)
        {
            Console.WriteLine(args.Value1 * args.Value2);
        }

        [ArgActionMethod, ArgDescription("Divides the two operands")]
        public void Divide(TwoOperandArgs args)
        {
            Console.WriteLine(args.Value1 / args.Value2);
        }
    }

    public class TwoOperandArgs
    {
        [ArgRequired, ArgDescription("The first operand to process"), ArgPosition(1)]
        public double Value1 { get; set; }
        [ArgRequired, ArgDescription("The second operand to process"), ArgPosition(2)]
        public double Value2 { get; set; }
    }

    public class CalculatorProgramSample
    {
        public static void _Main(string[] args)
        {
            Args.InvokeAction<CalculatorProgram>(args);
        }
    }
}
