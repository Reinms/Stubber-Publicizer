namespace DnlibAssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using AssemblyStripper;

    using dnlib.DotNet;

    public static class Stubber
    {
        public static async Task<StubResult> TryStubAsync(StubbingOptions options) => await Task.Run(() => TryStub(options));

        public static StubResult TryStub(StubbingOptions options)
        {
            AssemblyDef targetAsm;
            try
            {
                targetAsm = AssemblyDef.Load(File.ReadAllBytes(options.inputAssembly.FullName));
            } catch(Exception e)
            {
                return new(e);
            }

            StubContext context;
            try
            {
                context = new(options, targetAsm);
            } catch(Exception e)
            {
                return new(e);
            }

            try
            {
                context.Execute();
            } catch(Exception e)
            {
                return new(e);
            }

            try
            {
                targetAsm.Write(File.Create(options.outputPath));
            } catch(Exception e)
            {
                return new(e);
            }
            
            return new();
        }
    }
}
