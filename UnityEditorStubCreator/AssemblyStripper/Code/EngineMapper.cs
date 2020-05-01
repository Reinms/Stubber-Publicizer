using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace AssemblyStripper.Code
{
    internal static class EngineMapper
    {
        internal static Dictionary<String, AssemblyDefinition> MapEngine( String editorPath )
        {
            var map = new Dictionary<String,AssemblyDefinition>();
            var asmList = new List<AssemblyDefinition>();

            var dataPath = Path.Combine( editorPath, "Data" );

            var managed = new DirectoryInfo( Path.Combine( dataPath, "Managed", "UnityEngine" ) );
            foreach( var file in managed.EnumerateFiles("*.dll") )
            {
                asmList.Add( AssemblyDefinition.ReadAssembly( file.FullName ) );
            }

            var extensions = new DirectoryInfo( Path.Combine( dataPath, "UnityExtensions", "Unity" ) );
            foreach( var dir in extensions.EnumerateDirectories() )
            {
                foreach( var file in dir.EnumerateFiles( "*.dll" ) )
                {
                    asmList.Add( AssemblyDefinition.ReadAssembly( file.FullName ) );
                }
            }

            foreach( var def in asmList )
            {
                var key = def.Name.Name;
                if( map.ContainsKey( key ) )
                {
                    Console.WriteLine( String.Format( "Duplicate name: {0}", key ) );
                    continue;
                }
                map[key] = def;
            }

            return map;
        }
    }
}
