using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace AssemblyStripper.Code
{
    internal static class AssemblyScanner
    {
        internal static void ScanAndWriteValidTypes( in AssemblyDefinition source, in AssemblyWriter writer, in WriteConditions conditions )
        {
            var module = source.MainModule;

            foreach( var type in module.Types )
            {
                if( type == null ) continue;
                if( conditions.CheckType( type ) )
                {
                    writer.WriteType( type, conditions );
                }
            }
        }
    }
}
