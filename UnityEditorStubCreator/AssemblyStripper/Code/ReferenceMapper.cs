using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace AssemblyStripper.Code
{
    internal static class ReferenceMapper
    {
        internal static Dictionary<String, AssemblyDefinition> MapReferences( AssemblyDefinition sourceAssembly, AssemblyDefinition editorAssembly, Dictionary<String,AssemblyDefinition> engineMap )
        {
            var map = new Dictionary<String,AssemblyDefinition>();
            foreach( var module in sourceAssembly.Modules )
            {
                foreach( var assemblyNameRef in module.AssemblyReferences )
                {
                    var name = assemblyNameRef.Name;
                    if( systemAssemblies.Contains( name ) )
                    {
                        map[assemblyNameRef.Name] = null;
                    } else if( engineMap.TryGetValue( name, out var value ) )
                    {
                        map[assemblyNameRef.Name] = value;
                    } else
                    {
                        Console.WriteLine( name );
                        Console.WriteLine( "Will be stubbed" );
                        map[assemblyNameRef.Name] = editorAssembly;
                    }
                }
            }

            return map;
        }


        private static HashSet<String> systemAssemblies = new HashSet<String>
        {
            "netstandard",
            "mscorlib",
        };
    }
}
