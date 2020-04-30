using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace AssemblyStripper.Code
{
    internal static class AssemblyScanner
    {
        internal delegate Boolean TypeCheckDelegate( in TypeDefinition type );

        internal static void ScanAndWriteValidTypes( in AssemblyDefinition source, in AssemblyWriter writer, in TypeCheckDelegate condition )
        {
            var module = source.MainModule;

            var types = module.GetTypes();

            foreach( var type in module.GetTypes() )
            {
                if( condition( type ) )
                {
                    writer.WriteType( type );
                }
            }
        }
    }
}
