using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    internal class ProgressOperationControl : ConsoleControl
    {
        public ProgressOperation Operation { get; private set; }

        public ProgressOperationControl(ProgressOperation operation)
        {
            this.Operation = operation;
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
                
        }
    }
}
