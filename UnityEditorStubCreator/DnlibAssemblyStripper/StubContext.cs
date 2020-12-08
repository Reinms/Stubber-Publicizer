namespace DnlibAssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    using AssemblyStripper;

    using dnlib.DotNet;

    internal class StubContext
    {
        private readonly AssemblyDef target;
        private readonly List<ModuleStubContext> moduleContexts = new();

        private readonly AssemblyMapper mapper;


        internal StubContext(StubbingOptions options, AssemblyDef target)
        {
            this.target = target;


            if(options.remapToOutputContext)
            {
                var srcRes = new ResolveHelper();
                var dstRes = new ResolveHelper();

                foreach(var fd in options.inputContext)
                {
                    foreach(var f in fd)
                    {
                        srcRes.AddFile(f);
                    }
                }

                foreach(var fd in options.outputContext)
                {
                    foreach(var f in fd)
                    {
                        dstRes.AddFile(f);
                    }
                }

                this.mapper = new(srcRes, dstRes);
            }

            foreach(var m in target.Modules)
            {
                this.moduleContexts.Add(new(options, m, this));
            }
        }

        internal void Execute()
        {
            foreach(var m in this.moduleContexts)
            {
                m.Execute();
            }
        }
    }
}
