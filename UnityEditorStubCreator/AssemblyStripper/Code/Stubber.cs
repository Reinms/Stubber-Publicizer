using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Mono.Cecil;
using AssemblyStripper.Code;
using System.Linq;

namespace AssemblyStripper
{
    public static class Stubber
    {
        public static Boolean StubAssembly( in StubbingOptions options )
        {
            try
            {
                String targetPath = options.targetAssemblyPath;
                String outputPath = options.outputPath;
                String assemblyName = options.assemblyName;

                if( String.IsNullOrEmpty( targetPath ) || String.IsNullOrEmpty( outputPath ) || String.IsNullOrEmpty( assemblyName ) )
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

                String outDir = Directory.CreateDirectory( outputPath ).FullName;

                String editorPath = String.Format( "{0}\\{1}.editor.dll", outDir, assemblyName );
                String runtimePath = String.Format( "{0}\\{1}.runtime.dll", outDir, assemblyName );

                var editorAsmName = new AssemblyNameDefinition( assemblyName, new Version(1,0,0,0));
                var runtimeAsmName = new AssemblyNameDefinition( assemblyName, new Version(1,0,0,0));

                var editorAsm = AssemblyDefinition.CreateAssembly( editorAsmName, assemblyName, ModuleKind.Dll );
                var runtimeAsm = AssemblyDefinition.CreateAssembly( runtimeAsmName, assemblyName, ModuleKind.Dll );

                Console.WriteLine( "Generating Engine assembly map" );

                var engineMap = EngineMapper.MapEngine(options.pathToEditor);

                Console.WriteLine( "Finished generating engine assembly map" );
                Console.WriteLine( "Attempting to resolve dependencies" );

                var map = ReferenceMapper.MapReferences( targetAssembly, editorAsm, engineMap );

                Console.WriteLine( "Finished resolving dependencies" );


                var writer = new AssemblyWriter( editorAsm, runtimeAsm, map );
                var conditions = new WriteConditions( CheckType, CheckMethod, CheckProperty, CheckField );

                // TODO: Setup References

                AssemblyScanner.ScanAndWriteValidTypes( targetAssembly, writer, conditions );


                writer.SaveToDisk(editorPath,runtimePath);

                foreach( var s in allAttributeNames )
                {
                    Console.WriteLine( s );
                }

                return true;
            } catch( Exception e )
            {
                Console.WriteLine( e );
                Console.ReadKey();
                return false;
            }
        }

        private static Boolean CheckType( in TypeDefinition type )
        {
            Boolean result = type.IsSerializable;
            foreach( var attrib in type.CustomAttributes )
            {
                if( badAttributeNames.Contains( attrib.AttributeType.FullName ) )
                {
                    result &= false;
                    break;
                }
            }
            result |= type.InheritsFrom( "UnityEngine.Object" );
            if( type.HasNestedTypes )
            {
                foreach( var nestedType in type.NestedTypes )
                {
                    result |= CheckType( nestedType );
                }
            }
            //Console.WriteLine( String.Format( "{0}:   {1}", type.FullName, result ) );
            return result;
        }
        private static Boolean CheckField( in FieldDefinition field )
        {
            var result = field.IsPublic;
            if( !result )
            {
                foreach( var attrib in field.CustomAttributes )
                {
                    if( attrib.AttributeType.FullName == "UnityEngine.SerializeField" )
                    {
                        result = true;
                        break;
                    }
                }
            }
            //Console.WriteLine( String.Format( "{0}:   {1}", field.Name, result ) );
            return result;
        }
        private static Boolean CheckProperty( in PropertyDefinition property )
        {
            return false;
        }
        private static Boolean CheckMethod( in MethodDefinition method )
        {
            return false;
        }


        private static Boolean InheritsFrom( this TypeReference type, String name )
        {
            return type == null
                ? false
                : type.Resolve().BaseType == null
                ? false
                : type.FullName == "System.Object" ? false : type.FullName == name ? true : InheritsFrom( type.Resolve().BaseType.Resolve(), name );
        }


        private static HashSet<String> badAttributeNames = new HashSet<String>
        {
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        };

        private static HashSet<String> allAttributeNames = new HashSet<String>();
    }
}
