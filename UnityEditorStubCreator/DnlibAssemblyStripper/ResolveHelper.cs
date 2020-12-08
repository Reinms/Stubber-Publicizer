namespace DnlibAssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using dnlib.DotNet;

    internal class ResolveHelper
    {
        private readonly List<AssemblyDef> assemblies = new();
        private readonly List<ModuleDef> modules = new();

        internal void AddFile(FileInfo file)
        {
            try
            {
                var asm = AssemblyDef.Load(File.ReadAllBytes(file.FullName));
                if(asm is not null)
                {
                    this.assemblies.Add(asm);
                    foreach(var m in asm.Modules) this.modules.Add(m);
                }
            } catch { }
        }
    }
}
