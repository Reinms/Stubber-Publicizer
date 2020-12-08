namespace DnlibAssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using AssemblyStripper;

    using dnlib.DotNet;

    internal class ModuleStubContext
    {
        private readonly StubContext parent;
        private readonly ModuleDef target;

        private readonly TypeRef nonSerializedAttribute;

        internal ModuleStubContext(StubbingOptions options, ModuleDef target, StubContext parent)
        {
            this.target = target;

            if(options.preserveSerialization)
            {
                this.nonSerializedAttribute = target.CorLibTypes.GetTypeRef(typeof(NonSerializedAttribute).Namespace, typeof(NonSerializedAttribute).Name);
            }
        }

        internal void Execute()
        {

        }
    }
}
