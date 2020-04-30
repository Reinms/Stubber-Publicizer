using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Mono.Cecil;
using AssemblyStripper.Code;

namespace AssemblyStripper
{
    public static class Stubber
    {
        public static Boolean StubAssembly( in StubbingOptions options )
        {
            String targetPath = options.targetAssemblyPath;
            String outputPath = options.outputPath;
            String assemblyName = options.assemblyName;

            if( String.IsNullOrEmpty( targetPath ) || String.IsNullOrEmpty( outputPath ) || String.IsNullOrEmpty(assemblyName) )
            {
                // TODO: Log an error here
                return false;
            }

            var targetAssembly = AssemblyDefinition.ReadAssembly( targetPath );
            if( targetAssembly == null )
            {
                // TODO: Log an error here
                return false;
            }

            Console.WriteLine( targetPath );
            Console.WriteLine( outputPath );

            String outDir = Directory.CreateDirectory( outputPath ).FullName;

            String editorPath = String.Format( "{0}\\{1}.editor.dll", outDir, assemblyName );
            String runtimePath = String.Format( "{0}\\{1}.runtime.dll", outDir, assemblyName );

            Console.WriteLine( editorPath );
            Console.WriteLine( runtimePath );

            var editorAsmName = new AssemblyNameDefinition( assemblyName, new Version(1,0,0,0));
            var runtimeAsmName = new AssemblyNameDefinition( assemblyName, new Version(1,0,0,0));

            var editorAsm = AssemblyDefinition.CreateAssembly( editorAsmName, assemblyName, ModuleKind.Dll );
            var runtimeAsm = AssemblyDefinition.CreateAssembly( runtimeAsmName, assemblyName, ModuleKind.Dll );

            var writer = new AssemblyWriter( editorAsm, runtimeAsm );

            AssemblyScanner.ScanAndWriteValidTypes( targetAssembly, writer, ShouldWriteType );
            // TODO: Setup References

            foreach( var s in allAttributeNames )
            {
                Console.WriteLine( s );
            }

            return true;
        }

        public static Boolean ShouldWriteType( in TypeDefinition type )
        {
            //Console.WriteLine( type.FullName );
            var attributes = type.CustomAttributes;

            for( Int32 i = 0; i < attributes.Count; ++i )
            {
                var atrib = attributes[i];
                allAttributeNames.Add( atrib.AttributeType.FullName );
                //Console.WriteLine( atrib.AttributeType.FullName );
            }
            return true;
        }

        private static HashSet<String> badAttributeNames = new HashSet<String>
        {
            "CompilerGeneratedAttribute",
        };

        private static HashSet<String> allAttributeNames = new HashSet<String>();
    }
}
