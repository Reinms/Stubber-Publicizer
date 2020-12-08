namespace DnlibAssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class AssemblyMapper
    {
        private readonly ResolveHelper inputResolve;
        private readonly ResolveHelper outputResolve;

        private readonly HashSet<String> unchangedAssemblies = new();


        internal AssemblyMapper(ResolveHelper input, ResolveHelper output)
        {

        }
    }
}
