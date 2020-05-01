using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using System.Linq;

namespace AssemblyStripper.Code
{
    internal class AssemblyWriter
    {
        internal AssemblyWriter( in AssemblyDefinition editorAssembly, in AssemblyDefinition runtimeAssembly, in Dictionary<String,AssemblyDefinition> referenceMap )
        {
            this.editorAssembly = editorAssembly;
            this.runtimeAssembly = runtimeAssembly;
            this.referenceMap = referenceMap;

            this.editorModule = this.editorAssembly.MainModule;
            this.runtimeModule = this.runtimeAssembly.MainModule;
        }

        private readonly Dictionary<String, AssemblyDefinition> referenceMap;

        private readonly AssemblyDefinition editorAssembly;
        private readonly AssemblyDefinition runtimeAssembly;

        private readonly ModuleDefinition editorModule;
        private readonly ModuleDefinition runtimeModule;

        private ModuleDefinition[] unityEngineModules;

        private readonly Dictionary<String, TypeDefinition> writtenTypes = new Dictionary<String, TypeDefinition>();

        private WriteConditions conditions;

        private List<MethodDefinition> methodsToWrite = new List<MethodDefinition>();
        private List<FieldDefinition> fieldsToWrite = new List<FieldDefinition>();
        private List<PropertyDefinition> propertiesToWrite = new List<PropertyDefinition>();
        private List<CustomAttribute> attributesToApply = new List<CustomAttribute>();

        internal void WriteType( in TypeDefinition origType, in WriteConditions conditions )
        {
            this.conditions = conditions;
            var parentType = origType.DeclaringType;


            if( parentType == null )
            {
                var def = new TypeDefinition( origType.Namespace, origType.Name, origType.Attributes, this.RemapType(origType.BaseType) );
                foreach( var attrib in origType.CustomAttributes )
                {
                    this.attributesToApply.Add( attrib );
                }
                editorModule.Types.Add( def );
                writtenTypes[origType.FullName] = def;
            } else
            {
                var parent = this.writtenTypes[parentType.FullName];
                var def = new TypeDefinition( origType.Namespace, origType.Name, origType.Attributes, this.RemapType(origType.BaseType) );
                foreach( var attrib in origType.CustomAttributes )
                {
                    def.CustomAttributes.Add( attrib );
                }
                parent.NestedTypes.Add( def );
                writtenTypes[origType.FullName] = def;
            }

            foreach( var nestedType in origType.NestedTypes )
            {
                if( conditions.CheckType( nestedType ) )
                {
                    this.WriteType( nestedType, conditions );
                }
            }

            foreach( var field in origType.Fields )
            {
                if( conditions.CheckField( field ) )
                {
                    fieldsToWrite.Add( field );
                    //var newType = this.RemapType( field.FieldType );
                    //if( newType != null )
                    //{
                    //    try
                    //    {
                    //        newType = this.editorModule.ImportReference( newType );
                    //        var def = new FieldDefinition( field.FullName, field.Attributes, newType );
                    //        foreach( var attrib in field.CustomAttributes )
                    //        {
                    //            def.CustomAttributes.Add( attrib );
                    //        }
                    //        var type = writtenTypes[origType.FullName];
                    //        type.Fields.Add( def );
                    //    } catch( Exception e )
                    //    {
                    //        Console.WriteLine( newType.FullName );
                    //        Console.WriteLine( e );
                    //        Console.ReadKey();
                    //    }
                    //}
                }
            }

            foreach( var property in origType.Properties )
            {
                if( conditions.CheckProperty( property ) )
                {
                    propertiesToWrite.Add( property );
                    // TODO: Write the property
                }
            }

            foreach( var method in origType.Methods )
            {
                if( conditions.CheckMethod( method ) )
                {
                    methodsToWrite.Add( method );
                    // TODO: Write the method
                }
            }


        }

        internal void WriteRefs(in AssemblyDefinition runtimeAsm, params AssemblyDefinition[] editorAssemblies )
        {
            foreach( var kv in this.referenceMap )
            {
            }

            //foreach( var asm in unityEditorAssemblies )
            //{
            //    this.editorModule.AssemblyReferences.Add( asm.Name );
            //    this.editorModule.ModuleReferences.Add( asm.MainModule );
            //}
        }

        internal void FinishWrites()
        {

        }

        internal void SaveToDisk(in String editorName, in String runtimeName )
        {
            this.editorAssembly.Write( editorName );
        }


        private TypeReference RemapType( TypeReference type )
        {
            if( type == null )
            {
                Console.WriteLine( "Null type remapped" );
                return null;
            }

            TypeReference result = this.editorModule.GetType( type.FullName );
            if( result != null ) return result;


            if( type.IsArray )
            {
                return new ArrayType( this.RemapType( type.GetElementType() ) );
            }
            if( type.IsByReference )
            {
                return new ByReferenceType( this.RemapType( type.GetElementType() ) );
            }
            if( type.IsPointer )
            {
                return new PointerType( this.RemapType( type.GetElementType() ) );
            }
            if( type.IsFunctionPointer )
            {
                Console.WriteLine( "Fuck no..." );
                return null;
            }
            if( type.IsSentinel )
            {
                return new SentinelType( this.RemapType( type.GetElementType() ) );
            }
            if( type.IsGenericParameter )
            {
                Console.WriteLine( "No" );
                return null;
            }
            if( type.IsGenericInstance )
            {
                return new GenericInstanceType( this.RemapType( type.GetElementType() ) );
            }
            if( type.IsRequiredModifier )
            {
                Console.WriteLine( "Very no" );
                return null;
            }
            if( type.IsOptionalModifier )
            {
                Console.WriteLine( "Ugh" );
                return null;
            }
            if( type.IsPinned )
            {
                return new PinnedType( this.RemapType( type.GetElementType() ) );
            }

            var name = type?.Resolve()?.Module?.Assembly?.Name as AssemblyNameReference;
            if( name == null )
            {
                Console.WriteLine( "Module or assembly was null for the type" );
                return null;
            }
            if( this.referenceMap.TryGetValue( name.Name, out var asm ) )
            {
                if( asm == null ) return type;
                var t = asm.MainModule.GetType( type.FullName );
                if( t == null )
                {
                    Console.WriteLine( String.Format( "Could not find type {0}", type.FullName ) );
                    return null;
                }
                return this.editorModule.ImportReference(t);
            } else
            {
                var asmToStub = type.Resolve().Module.Assembly;
                Console.WriteLine( String.Format("Stubbing {0}", asmToStub.FullName ) );
                Console.WriteLine( type.FullName );
                return null;
                //AssemblyScanner.ScanAndWriteValidTypes( asmToStub, this, this.conditions );
                //return editorModule.GetType(type.FullName);
            }
        }


        private TypeReference FindEngineType( in String fullName )
        {
            foreach( var module in this.unityEngineModules )
            {
                var t = module.GetType(fullName);
                if( t != null ) return t;
            }
            Console.WriteLine( String.Format( "Type: {0} not found", fullName ) );
            return null;
        }
    }
}
