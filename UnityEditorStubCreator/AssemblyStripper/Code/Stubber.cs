namespace AssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public static class Stubber
    {
        public static Boolean StubAssembly(StubbingOptions options)
        {
            try
            {
                String targetPath = options.targetAssemblyPath;
                String outputPath = options.outputPath;
                String editorName = options.editorRenameTo;

                if(String.IsNullOrEmpty(targetPath) || String.IsNullOrEmpty(outputPath) || String.IsNullOrEmpty(editorName))
                {
                    Console.WriteLine("Invalid path");
                    return false;
                }

                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(new FileInfo(targetPath).DirectoryName);
                var readerParams = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                };

                var targetAssembly = AssemblyDefinition.ReadAssembly(targetPath, readerParams);
                if(targetAssembly == null)
                {
                    Console.WriteLine("No Assembly found");
                    return false;
                }

                String origName = new FileInfo(targetPath).Name;

                String outDir = Directory.CreateDirectory(outputPath).FullName;

                if(options.outputReferenceStub)
                {
                    String path = $@"{outDir}\{origName}.refstub.dll";

                    foreach(ModuleDefinition module in targetAssembly.Modules)
                    {
                        var typesToRemove = new HashSet<TypeDefinition>();
                        foreach(TypeDefinition type in module.Types) if(ReferenceStubType(type, options.makeReferenceStubPublic)) _ = typesToRemove.Add(type);
                        foreach(TypeDefinition t in typesToRemove) _ = module.Types.Remove(t);
                    }
                    
                    targetAssembly.Write(path);
                    targetAssembly = AssemblyDefinition.ReadAssembly(targetPath, readerParams);
                }
                if(options.outputEditorStub)
                {
                    String path = $@"{outDir}\{editorName}.editor.dll";
                    AssemblyNameDefinition asmNameDef = targetAssembly.Name;
                    asmNameDef.Name = editorName;

                    foreach(ModuleDefinition module in targetAssembly.Modules)
                    {
                        var typesToRemove = new HashSet<TypeDefinition>();
                        foreach(TypeDefinition type in module.Types) if(EditorStubType(type, options.makeEditorStubPublic)) _ = typesToRemove.Add(type);
                        foreach(TypeDefinition t in typesToRemove) _ = module.Types.Remove(t);
                    }

                    targetAssembly.Write(path);
                    targetAssembly = AssemblyDefinition.ReadAssembly(targetPath, readerParams);
                }
                if(options.outputForwardAssembly)
                {
                    String path = $@"{outDir}\{editorName}.forward.dll";
                    AssemblyNameDefinition asmNameDef = targetAssembly.Name;
                    asmNameDef.Name = editorName;
                    var sourceAssembly = AssemblyDefinition.ReadAssembly(targetPath, readerParams);

                    foreach(ModuleDefinition module in targetAssembly.Modules)
                    {
                        module.Types.Clear();
                    }
                    foreach(ModuleDefinition module in sourceAssembly.Modules)
                    {
                        foreach(TypeDefinition type in module.Types)
                        {
                            ForwardType(type, targetAssembly, sourceAssembly);
                        }
                    }

                    targetAssembly.Write(path);
                }
                return true;
            } catch(Exception e)
            {
                Console.WriteLine(e);
                _ = Console.ReadKey();
                return false;
            }
        }
        private static readonly ConstructorInfo nonSerializedAttributeConstructor = typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo typeForwardedToConstructor = typeof(TypeForwardedToAttribute).GetConstructor(new[] { typeof(Type), });

        private static void CreateSimpleMethodBody(MethodDefinition method)
        {
            if(method is null) return;
            if(method.Body is null) return;
            ILProcessor proc = method.Body.GetILProcessor();
            TypeReference returnType = method.ReturnType;
            switch(returnType)
            {
                case var ret when ret?.IsPrimitive ?? false:
                    switch(ret.FullName)
                    {
                        case "System.Boolean":
                            proc.Emit(OpCodes.Ldc_I4_0);
                            break;
                        case "System.Single":
                            proc.Emit(OpCodes.Ldc_R4, 0.0f);
                            break;
                        case "System.Double":
                            proc.Emit(OpCodes.Ldc_R8, 0.0);
                            break;
                        case "System.SByte":
                        case "System.Byte":
                        case "System.Int16":
                        case "System.UInt16":
                        case "System.Int32":
                        case "System.UInt32":
                        case "System.Int64":
                        case "System.UInt64":
                        case "System.IntPtr":
                        case "System.UIntPtr":
                            proc.Emit(OpCodes.Ldc_I4_0);
                            break;
                        case var v:
                            Console.WriteLine($"Unknown primative type: {v}");
                            break;
                    }
                    break;
                case var ret when ret?.IsValueType ?? false:
                    TypeReference typeref = method.Module.ImportReference(ret);
                    var vardef = new VariableDefinition(typeref);
                    method.Body.Variables.Add(vardef);
                    proc.Emit(OpCodes.Ldloca_S, (Byte)0);
                    proc.Emit(OpCodes.Initobj, ret);
                    proc.Emit(OpCodes.Ldloc_0);
                    break;
                case var ret when ret?.FullName == "System.Void":
                    break;
                case var ret when ret != null:
                    proc.Emit(OpCodes.Ldnull);
                    break;
            }
            proc.Emit(OpCodes.Ret);
        }

        private static Boolean ReferenceStubType(TypeDefinition type, Boolean makePub )
        {
            if(type is null) return false;
            if(type.IsNested) foreach(CustomAttribute atr in type.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;

            if(makePub)
            {  
                if(type.IsNested)
                {
                    type.IsNestedPublic = true;
                } else
                {
                    type.IsPublic = true;
                }
            }

            var methodsToRemove = new HashSet<MethodDefinition>();
            foreach(MethodDefinition method in type.Methods) if(ReferenceStubMethod(method, makePub)) _ = methodsToRemove.Add(method);
            foreach(MethodDefinition m in methodsToRemove) _ = type.Methods.Remove(m);

            var propertiesToRemove = new HashSet<PropertyDefinition>();
            foreach(PropertyDefinition property in type.Properties) if(ReferenceStubProperty(property, makePub)) _ = propertiesToRemove.Add(property);
            foreach(PropertyDefinition p in propertiesToRemove) _ = type.Properties.Remove(p);


            var fieldsToRemove = new HashSet<FieldDefinition>();
            foreach(FieldDefinition field in type.Fields) if(ReferenceStubField(field, makePub)) _ = fieldsToRemove.Add(field);
            foreach(FieldDefinition f in fieldsToRemove) _ = type.Fields.Remove(f);

            var nestedTypesToRemove = new HashSet<TypeDefinition>();
            foreach(TypeDefinition sub in type.NestedTypes) if(ReferenceStubType(sub, makePub)) _ = nestedTypesToRemove.Add(sub);
            foreach(TypeDefinition t in nestedTypesToRemove) _ = type.NestedTypes.Remove(t);

            return false;
        }

        private static Boolean ReferenceStubProperty(PropertyDefinition property, Boolean makePub)
        {
            foreach(CustomAttribute atr in property.CustomAttributes) if (atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            _ = ReferenceStubMethod(property.GetMethod, makePub);
            _ = ReferenceStubMethod(property.SetMethod, makePub);
            return false;
        }

        private static Boolean ReferenceStubMethod(MethodDefinition method, Boolean makePub)
        {
            if(method is null) return false;
            foreach(CustomAttribute atr in method.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            if(makePub) method.IsPublic = true;
            method?.Body?.Variables?.Clear();
            method?.Body?.Instructions?.Clear();
            CreateSimpleMethodBody(method);

            return false;
        }
        private static Boolean ReferenceStubField(FieldDefinition field, Boolean makePub)
        {
            if(field is null) return false;
            foreach(CustomAttribute atr in field.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            if(makePub) field.IsPublic = true;

            return false;
        }





        private static Boolean EditorStubType(TypeDefinition type, Boolean makePub)
        {
            if(type is null) return false;
            if(type.IsNested) foreach(CustomAttribute atr in type.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;

            if(makePub)
            {
                if(type.IsNested)
                {
                    type.IsNestedPublic = true;
                } else
                {
                    type.IsPublic = true;
                }
            }

            var methodsToRemove = new HashSet<MethodDefinition>();
            foreach(MethodDefinition method in type.Methods) if(EditorStubMethod(method, makePub)) _ = methodsToRemove.Add(method);
            foreach(MethodDefinition m in methodsToRemove) _ = type.Methods.Remove(m);

            var propertiesToRemove = new HashSet<PropertyDefinition>();
            foreach(PropertyDefinition property in type.Properties) if(EditorStubProperty(property, makePub)) _ = propertiesToRemove.Add(property);
            foreach(PropertyDefinition p in propertiesToRemove) _ = type.Properties.Remove(p);


            var fieldsToRemove = new HashSet<FieldDefinition>();
            foreach(FieldDefinition field in type.Fields) if(EditorStubField(field, makePub)) _ = fieldsToRemove.Add(field);
            foreach(FieldDefinition f in fieldsToRemove) _ = type.Fields.Remove(f);

            var nestedTypesToRemove = new HashSet<TypeDefinition>();
            foreach(TypeDefinition sub in type.NestedTypes) if(EditorStubType(sub, makePub)) _ = nestedTypesToRemove.Add(sub);
            foreach(TypeDefinition t in nestedTypesToRemove) _ = type.NestedTypes.Remove(t);

            return false;
        }

        private static Boolean EditorStubProperty(PropertyDefinition property, Boolean makePub)
        {
            foreach(CustomAttribute atr in property.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            _ = EditorStubMethod(property.GetMethod, makePub);
            _ = EditorStubMethod(property.SetMethod, makePub);
            return false;
        }

        private static Boolean EditorStubMethod(MethodDefinition method, Boolean makePub)
        {
            if(method is null) return false;
            foreach(CustomAttribute atr in method.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            if(makePub) method.IsPublic = true;
            method?.Body?.Variables?.Clear();
            method?.Body?.Instructions?.Clear();
            CreateSimpleMethodBody(method);

            return false;
        }
        private static Boolean EditorStubField(FieldDefinition field, Boolean makePub)
        {
            if(field is null) return false;
            Boolean serialized = false;
            foreach(CustomAttribute atr in field.CustomAttributes)
            {
                switch(atr.AttributeType.FullName)
                {
                    case "System.Runtime.CompilerServices.CompilerGeneratedAttribute":
                        return true;
                    case "UnityEngine.SerializeField":
                        serialized = true;
                        continue;
                    default:
                        continue;
                }
            }
            if(makePub)
            {
                if(!field.IsPublic && !serialized)
                {
                    //Console.WriteLine($"Should add NonSerialized to field: {field.FullName}");
                    field.CustomAttributes.Add(new CustomAttribute(field.Module.ImportReference(nonSerializedAttributeConstructor)));
                }
                field.IsPublic = true;
            }

            return false;
        }


        private static void ForwardType(TypeDefinition type, AssemblyDefinition from, AssemblyDefinition to)
        {
            var atr = new CustomAttribute(from.MainModule.ImportReference(typeForwardedToConstructor));
            atr.ConstructorArguments.Add(new CustomAttributeArgument(from.MainModule.ImportReference(typeof(Type)), from.MainModule.ImportReference(type)));
            from.CustomAttributes.Add(atr);
        }








        //private static Boolean CheckType( in TypeDefinition type )
        //{
        //    Boolean result = type.IsSerializable;
        //    foreach( var attrib in type.CustomAttributes )
        //    {
        //        if( badAttributeNames.Contains( attrib.AttributeType.FullName ) )
        //        {
        //            result &= false;
        //            break;
        //        }
        //    }
        //    result |= type.InheritsFrom( "UnityEngine.Object" );
        //    if( type.HasNestedTypes )
        //    {
        //        foreach( var nestedType in type.NestedTypes )
        //        {
        //            result |= CheckType( nestedType );
        //        }
        //    }
        //    //Console.WriteLine( String.Format( "{0}:   {1}", type.FullName, result ) );
        //    return result;
        //}
        //private static Boolean CheckField( in FieldDefinition field )
        //{
        //    var result = field.IsPublic;
        //    if( !result )
        //    {
        //        foreach( var attrib in field.CustomAttributes )
        //        {
        //            if( attrib.AttributeType.FullName == "UnityEngine.SerializeField" )
        //            {
        //                result = true;
        //                break;
        //            }
        //        }
        //    }
        //    //Console.WriteLine( String.Format( "{0}:   {1}", field.Name, result ) );
        //    return result;
        //}
        //private static Boolean CheckProperty( in PropertyDefinition property )
        //{
        //    return false;
        //}
        //private static Boolean CheckMethod( in MethodDefinition method )
        //{
        //    return false;
        //}


        //private static Boolean InheritsFrom( this TypeReference type, String name )
        //{
        //    return type == null
        //        ? false
        //        : type.Resolve().BaseType == null
        //        ? false
        //        : type.FullName == "System.Object" ? false : type.FullName == name ? true : InheritsFrom( type.Resolve().BaseType.Resolve(), name );
        //}


        //private static HashSet<String> badAttributeNames = new HashSet<String>
        //{
        //    "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        //};

        //private static HashSet<String> allAttributeNames = new HashSet<String>();
    }
}