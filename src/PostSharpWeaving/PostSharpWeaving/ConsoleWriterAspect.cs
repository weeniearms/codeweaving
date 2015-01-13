using System;
using PostSharp.Aspects;

namespace PostSharpWeaving
{
    [Serializable]
    public class ConsoleWriterAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            base.OnEntry(args);

            Console.WriteLine(args.Method.Name);
        }
    }
}
