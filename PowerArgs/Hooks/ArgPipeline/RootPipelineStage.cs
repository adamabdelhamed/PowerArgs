using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace PowerArgs.Preview
{
    internal class RootPipelineStage : PipelineStage
    {
        public RootPipelineStage(string[] args) : base(args)
        {
            PipelineStage.Current = this;
        }

        public override void Accept(object o)
        {
            throw new NotSupportedException("The root pipeline stage cannot accept pipeline inputs");
        }

        public override bool IsDrained
        {
            get
            {
                return true;
            }
            protected set
            {
                throw new InvalidOperationException("The root pipeline stage is always drained");
            }
        }

        public override void Drain()
        {
            FireDrained();
            PipelineStage.Current = null;
        }

        public override string ToString()
        {
            return "Root stage";
        }
    }
}
