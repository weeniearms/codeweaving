using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;

namespace PostSharpWeaving
{
    public class GeneratePluginBaseClassesTask : Task
    {
        public override bool Execute()
        {
            var enumerator = this.Project.GetService<IAnnotationRepositoryService>().GetAnnotationsOfType(typeof(PluggableClass), false);

            var processedtypes = new List<TypeDefDeclaration>();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                    continue;

                var pluggableType = ((MethodDefDeclaration)enumerator.Current.TargetElement).DeclaringType;

                if (processedtypes.Contains(pluggableType))
                    continue;

                this.GeneratePluginBaseClasses(pluggableType);

                processedtypes.Add(pluggableType);
            }

            return base.Execute();
        }

        private void GeneratePluginBaseClasses(TypeDefDeclaration pluggableType)
        {
            var pluginBaseType = new TypeDefDeclaration
            {
                Name = pluggableType.Name + "PluginBase",
                Attributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract
            };

            this.Project.Module.Types.Add(pluginBaseType);

            this.GenerateConstructor(pluginBaseType);
            this.GeneratePluginMethods(pluggableType, pluginBaseType);
        }

        private void GenerateConstructor(TypeDefDeclaration extensionType)
        {
            var constructor = new MethodDefDeclaration
            {
                Attributes =
                    MethodAttributes.SpecialName | MethodAttributes.Family | MethodAttributes.HideBySig |
                    MethodAttributes.RTSpecialName,
                Name = ".ctor"
            };
            extensionType.Methods.Add(constructor);
            constructor.MethodBody.RootInstructionBlock = constructor.MethodBody.CreateInstructionBlock();
            constructor.MethodBody.MaxStack = 8;
            var callInstructionSequence = constructor.MethodBody.CreateInstructionSequence();
            var retInstructionSequence = constructor.MethodBody.CreateInstructionSequence();
            constructor.MethodBody.RootInstructionBlock.AddInstructionSequence(callInstructionSequence, NodePosition.After, null);
            constructor.MethodBody.RootInstructionBlock.AddInstructionSequence(retInstructionSequence, NodePosition.After, null);
            constructor.MethodBody.MaxStack = 8;

            using (var writer = new InstructionWriter())
            {
                writer.AttachInstructionSequence(callInstructionSequence);
                writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                writer.EmitInstructionMethod(OpCodeNumber.Call, this.Project.Module.FindMethod(this.Project.Module.FindType(typeof(MarshalByRefObject), BindingOptions.Default).GetTypeDefinition().BaseType.GetNakedType(TypeNakingOptions.None), ".ctor"));
                writer.DetachInstructionSequence();

                writer.AttachInstructionSequence(retInstructionSequence);
                writer.EmitInstruction(OpCodeNumber.Ret);
                writer.DetachInstructionSequence();
            }
        }

        private void GeneratePluginMethods(ITypeSignature pluggableType, TypeDefDeclaration pluginBaseType)
        {
            foreach (var method in pluggableType.GetTypeDefinition().Methods.Cast<MethodDefDeclaration>().Where(m => m.Name != ".ctor"))
            {
                GeneratePluginMethod(pluginBaseType, method, typeof(bool), PluggableClass.PluginMethodType.Before);
                GeneratePluginMethod(pluginBaseType, method, typeof(void), PluggableClass.PluginMethodType.After);
            }
        }

        private void GeneratePluginMethod(TypeDefDeclaration pluginBaseType, MethodDefDeclaration pluggableMethod, Type returnType, PluggableClass.PluginMethodType pluginMethodType)
        {
            var pluginMethod = new MethodDefDeclaration
            {
                Name = pluginMethodType + pluggableMethod.Name,
                Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot
            };

            pluginBaseType.Methods.Add(pluginMethod);
            pluginMethod.Parameters.AddRange(pluggableMethod.Parameters.Select(p => new ParameterDeclaration(p.Ordinal, p.Name, p.ParameterType)).ToList());

            foreach (var parameter in pluginMethod.Parameters)
                parameter.Attributes &= ~ParameterAttributes.Out;
            if (!pluggableMethod.ReturnParameter.ParameterType.Equals(this.Project.Module.FindType(typeof(void), BindingOptions.Default)))
            {
                var resultParameterName = pluggableMethod.Parameters.Any(p => p.Name == "result")
                                              ? pluggableMethod.Name.Trim('~').Substring(0, 1).ToLowerInvariant() +
                                                pluggableMethod.Name.Trim('~').Substring(1) + "Result"
                                              : "result";
                pluginMethod.Parameters.Add(new ParameterDeclaration(pluginMethod.Parameters.Count, resultParameterName, new PointerTypeSignature(pluggableMethod.ReturnParameter.ParameterType, true)));
            }

            pluginMethod.ReturnParameter = ParameterDeclaration.CreateReturnParameter(this.Project.Module.FindType(returnType, BindingOptions.Default));

            pluginMethod.MethodBody.RootInstructionBlock = pluginMethod.MethodBody.CreateInstructionBlock();
            var instructionSequence = pluginMethod.MethodBody.CreateInstructionSequence();
            pluginMethod.MethodBody.RootInstructionBlock.AddInstructionSequence(instructionSequence, NodePosition.After, null);

            using (var writer = new InstructionWriter())
            {
                writer.AttachInstructionSequence(instructionSequence);
                if (returnType == typeof(bool))
                    writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                writer.EmitInstruction(OpCodeNumber.Ret);
                writer.DetachInstructionSequence();
            }
        }
    }
}