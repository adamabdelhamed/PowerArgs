using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgsTests
{
    public class AfterInvokeInterceptor : ArgHook
    {
        Action<HookContext> hook;
        public AfterInvokeInterceptor(Action<HookContext> hook)
        {
            this.hook = hook;
        }

        public override void AfterInvoke(HookContext context)
        {
            hook(context);
        }
    }
}
