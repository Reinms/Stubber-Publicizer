using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace AssemblyStripper.Code
{
    internal class AssemblyWriter
    {
        internal AssemblyWriter( in AssemblyDefinition editorAssembly, in AssemblyDefinition runtimeAssembly )
        {
            this.editorAssembly = editorAssembly;
            this.runtimeAssembly = runtimeAssembly;

            this.editorModule = this.editorAssembly.MainModule;
            this.runtimeModule = this.runtimeAssembly.MainModule;

        }

        private readonly AssemblyDefinition editorAssembly;
        private readonly AssemblyDefinition runtimeAssembly;

        private readonly ModuleDefinition editorModule;
        private readonly ModuleDefinition runtimeModule;

        internal void WriteType( in TypeDefinition origType )
        {

            // TODO: Implement
        }

        internal void WriteRefs()
        {
            // TODO: Implement
        }
    }
}
