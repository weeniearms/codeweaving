using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace BasicWeaving
{
    public class Weaver
    {
        public void Execute(string targetPath)
        {
            var targetAssembly = AssemblyDefinition.ReadAssembly(targetPath);
            
            var writeLineMethod = targetAssembly.MainModule.Import(typeof (Console).GetMethod("WriteLine", new[] {typeof (string)}));

            foreach (var method in targetAssembly.Modules.SelectMany(m => m.Types).SelectMany(t => t.Methods).Where(m => m.Name != ".ctor"))
            {
                method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, method.Name));
                method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, writeLineMethod));
            }

            targetAssembly.Write(targetPath);
        }
    }
}
