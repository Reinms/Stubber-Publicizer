namespace AssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    public static class Stubber
    {
        public static Boolean StubAssembly(StubbingOptions options)
        {
            try
            {
                String targetPath = options.targetAssemblyPath;
                String outputPath = options.outputPath;
                String editorName = options.editorRenameTo;

                if(String.IsNullOrEmpty(targetPath) || String.IsNullOrEmpty(outputPath) || ((options.outputEditorStub || options.outputForwardAssembly) && String.IsNullOrEmpty(editorName)))
                {
                    Console.WriteLine("Invalid path");
                    return false;
                }

                resolver = new DefaultAssemblyResolver();
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

                GetReferences(targetAssembly);

                var file = new FileInfo(targetPath);
                String origName = file.Name.Substring(0,file.Name.Length-file.Extension.Length);

                String outDir = Directory.CreateDirectory(outputPath).FullName;

                if(options.outputReferenceStub)
                {
                    String path = $@"{outDir}\{origName}.refstub.dll";

                    foreach(ModuleDefinition module in targetAssembly.Modules)
                    {
                        var typesToRemove = new HashSet<TypeDefinition>();
                        foreach(TypeDefinition type in module.Types) if(ReferenceStubType(type, options.makeReferenceStubPublic, options.removeReadonly)) _ = typesToRemove.Add(type);
                        foreach(TypeDefinition t in typesToRemove) _ = module.Types.Remove(t);
                    }
                    
                    targetAssembly.Write(path);
                    targetAssembly = AssemblyDefinition.ReadAssembly(targetPath, readerParams);
                    GetReferences(targetAssembly);

                }
                if(options.outputEditorStub)
                {
                    String path = $@"{outDir}\{editorName}.editor.dll";
                    AssemblyNameDefinition asmNameDef = targetAssembly.Name;
                    asmNameDef.Name = editorName;

                    foreach(ModuleDefinition module in targetAssembly.Modules)
                    {
                        var typesToRemove = new HashSet<TypeDefinition>();
                        foreach(TypeDefinition type in module.Types) if(EditorStubType(type, options.makeEditorStubPublic, options.removeReadonly)) _ = typesToRemove.Add(type);
                        foreach(TypeDefinition t in typesToRemove) _ = module.Types.Remove(t);
                    }

                    targetAssembly.Write(path);
                    targetAssembly = AssemblyDefinition.ReadAssembly(targetPath, readerParams);
                    GetReferences(targetAssembly);
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
                            ForwardType(type, targetAssembly);
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
        private static DefaultAssemblyResolver resolver;
        private static MethodReference nonSerializedAttributeConstructor;
        private static MethodReference typeForwardedToConstructor;
        private static TypeDefinition system_type;
        
        private static void GetReferences(AssemblyDefinition assembly)
        {
            TypeDefinition sys_NonSerializedAttribute, sys_TypeForwardedToAttribute, sys_Type;
            sys_NonSerializedAttribute = sys_TypeForwardedToAttribute = sys_Type = null;
            foreach(ModuleDefinition module in assembly.Modules)
            {
                foreach(AssemblyNameReference reffedAssembly in module.AssemblyReferences)
                {
                    AssemblyDefinition asm = resolver.Resolve(reffedAssembly);
                    if(asm is null) continue;
                    foreach(ModuleDefinition reffedModule in asm.Modules)
                    {
                        if(reffedModule.GetType(typeof(TypeForwardedToAttribute).FullName) is TypeDefinition def1) sys_TypeForwardedToAttribute = def1;
                        if(reffedModule.GetType(typeof(NonSerializedAttribute).FullName) is TypeDefinition def2) sys_NonSerializedAttribute = def2;
                        if(reffedModule.GetType("System", "Type") is TypeDefinition def3) sys_Type = def3;
                    }
                }
            }

            if(sys_NonSerializedAttribute is null) Console.WriteLine($"Unable to resolve type {nameof(NonSerializedAttribute)}, make sure that you are running on an assembly that is in its normal location");
            if(sys_TypeForwardedToAttribute is null) Console.WriteLine($"Unable to resolve type {nameof(TypeForwardedToAttribute)}, make sure that you are running on an assembly that is in its normal location");
            if(sys_Type is null) Console.WriteLine($"Unable to resolve type {nameof(Type)}, make sure that you are running on an assembly that is in its normal location");

            system_type = sys_Type;

            static Boolean HasNoParameters(MethodDefinition method) => method.HasParameters == false;
            MethodDefinition nonSerializedConstructor = sys_NonSerializedAttribute.GetConstructors().First(HasNoParameters);
            if(nonSerializedConstructor is null) Console.WriteLine($"No constructor found for {nameof(NonSerializedAttribute)}");
            nonSerializedAttributeConstructor = nonSerializedConstructor;

            static Boolean HasSingleArgumentOfTypeType(MethodDefinition method) => method.HasParameters && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == typeof(System.Type).FullName;
            MethodDefinition typeForwardedConstr = sys_TypeForwardedToAttribute.GetConstructors().First(HasSingleArgumentOfTypeType);
            if(typeForwardedConstr is null) Console.WriteLine($"No constructor found for {nameof(TypeForwardedToAttribute)}");
            typeForwardedToConstructor = typeForwardedConstr;
        }


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
                        case "System.Char":
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

        private static Boolean ReferenceStubType(TypeDefinition type, Boolean makePub, Boolean removeReadonly )
        {
            if(type is null) return false;

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

            var methodBlacklist = new HashSet<MethodDefinition>();

            var propertiesToRemove = new HashSet<PropertyDefinition>();
            foreach(PropertyDefinition property in type.Properties) if(ReferenceStubProperty(property, makePub, methodBlacklist)) _ = propertiesToRemove.Add(property);
            foreach(PropertyDefinition p in propertiesToRemove) _ = type.Properties.Remove(p);

            var eventsToRemove = new HashSet<EventDefinition>();
            foreach(EventDefinition eventDef in type.Events) if(ReferenceStubEvent(eventDef, makePub, methodBlacklist)) _ = eventsToRemove.Add(eventDef);
            foreach(EventDefinition e in eventsToRemove) _ = type.Events.Remove(e);

            var fieldsToRemove = new HashSet<FieldDefinition>();
            foreach(FieldDefinition field in type.Fields) if(ReferenceStubField(field, makePub, removeReadonly)) _ = fieldsToRemove.Add(field);
            foreach(FieldDefinition f in fieldsToRemove) _ = type.Fields.Remove(f);

            var methodsToRemove = new HashSet<MethodDefinition>();
            foreach(MethodDefinition method in type.Methods) if(ReferenceStubMethod(method, makePub, methodBlacklist)) _ = methodsToRemove.Add(method);
            foreach(MethodDefinition m in methodsToRemove) _ = type.Methods.Remove(m);

            var nestedTypesToRemove = new HashSet<TypeDefinition>();
            foreach(TypeDefinition sub in type.NestedTypes) if(ReferenceStubType(sub, makePub, removeReadonly)) _ = nestedTypesToRemove.Add(sub);
            foreach(TypeDefinition t in nestedTypesToRemove) _ = type.NestedTypes.Remove(t);

            if(type.IsNested) foreach(CustomAttribute atr in type.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }

        private static Boolean ReferenceStubProperty(PropertyDefinition property, Boolean makePub, HashSet<MethodDefinition> blacklist)
        {
            if(property is null) return false;
            _ = property.Module.ImportReference(property.PropertyType);
            _ = ReferenceStubMethod(property.GetMethod, makePub, blacklist);
            _ = ReferenceStubMethod(property.SetMethod, makePub, blacklist);
            foreach(CustomAttribute atr in property.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }

        private static Boolean ReferenceStubEvent(EventDefinition eventDef, Boolean makePub, HashSet<MethodDefinition> blacklist)
        {
            if(eventDef is null) return false;
            _ = eventDef.Module.ImportReference(eventDef.EventType);
            _ = ReferenceStubMethod(eventDef.AddMethod, makePub, blacklist);
            _ = ReferenceStubMethod(eventDef.RemoveMethod, makePub, blacklist);
            _ = ReferenceStubMethod(eventDef.InvokeMethod, makePub, blacklist);
            foreach(MethodDefinition m in eventDef.OtherMethods) _ = ReferenceStubMethod(m, makePub, blacklist);
            foreach(CustomAttribute atr in eventDef.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }

        private static Boolean ReferenceStubMethod(MethodDefinition method, Boolean makePub, HashSet<MethodDefinition> blacklist)
        {
            if(method is null) return false;
            _ = method.Module.ImportReference(method.ReturnType);
            foreach(ParameterDefinition parameter in method.Parameters) _ = method.Module.ImportReference(parameter.ParameterType);
            if(!blacklist.Add(method)) return false;
            if(makePub) method.IsPublic = true;
            method?.Body?.Variables?.Clear();
            method?.Body?.Instructions?.Clear();
            CreateSimpleMethodBody(method);
            foreach(CustomAttribute atr in method.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }
        private static Boolean ReferenceStubField(FieldDefinition field, Boolean makePub, Boolean removeReadonly)
        {
            if(field is null) return false;
            _ = field.Module.ImportReference(field.FieldType);
            Boolean ret = false;
            Boolean serialized = false;
            foreach(CustomAttribute atr in field.CustomAttributes)
            {
                switch(atr.AttributeType.FullName)
                {
                    case "System.Runtime.CompilerServices.CompilerGeneratedAttribute":
                        ret = true;
                        continue;
                    case "UnityEngine.SerializeField":
                        serialized = true;
                        continue;
                    default:
                        continue;
                }
            }
            if(makePub)
            {
                if(!field.IsPublic && !serialized) field.CustomAttributes.Add(new CustomAttribute(field.Module.ImportReference(nonSerializedAttributeConstructor)));
                field.IsPublic = true;
            }
            if( removeReadonly )
            {
                field.IsInitOnly = false;
            }
            return ret;
        }





        private static Boolean EditorStubType(TypeDefinition type, Boolean makePub, Boolean removeReadonly)
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

            var methodBlacklist = new HashSet<MethodDefinition>();



            var propertiesToRemove = new HashSet<PropertyDefinition>();
            foreach(PropertyDefinition property in type.Properties) if(EditorStubProperty(property, makePub, methodBlacklist)) _ = propertiesToRemove.Add(property);
            foreach(PropertyDefinition p in propertiesToRemove) _ = type.Properties.Remove(p);

            var eventsToRemove = new HashSet<EventDefinition>();
            foreach(EventDefinition eventDef in type.Events) if(EditorStubEvent(eventDef, makePub, methodBlacklist)) _ = eventsToRemove.Add(eventDef);
            foreach(EventDefinition e in eventsToRemove) _ = type.Events.Remove(e);

            var fieldsToRemove = new HashSet<FieldDefinition>();
            foreach(FieldDefinition field in type.Fields) if(EditorStubField(field, makePub, removeReadonly)) _ = fieldsToRemove.Add(field);
            foreach(FieldDefinition f in fieldsToRemove) _ = type.Fields.Remove(f);


            var methodsToRemove = new HashSet<MethodDefinition>();
            foreach(MethodDefinition method in type.Methods) if(EditorStubMethod(method, makePub, methodBlacklist)) _ = methodsToRemove.Add(method);
            foreach(MethodDefinition m in methodsToRemove) _ = type.Methods.Remove(m);

            var nestedTypesToRemove = new HashSet<TypeDefinition>();
            foreach(TypeDefinition sub in type.NestedTypes) if(EditorStubType(sub, makePub, removeReadonly)) _ = nestedTypesToRemove.Add(sub);
            foreach(TypeDefinition t in nestedTypesToRemove) _ = type.NestedTypes.Remove(t);

            return false;
        }

        private static Boolean EditorStubProperty(PropertyDefinition property, Boolean makePub, HashSet<MethodDefinition> blacklist)
        {
            if(property is null) return false;
            _ = property.Module.ImportReference(property.PropertyType);
            _ = EditorStubMethod(property.GetMethod, makePub, blacklist);
            _ = EditorStubMethod(property.SetMethod, makePub, blacklist);
            foreach(CustomAttribute atr in property.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }

        private static Boolean EditorStubEvent(EventDefinition eventDef, Boolean makePub, HashSet<MethodDefinition> blacklist)
        {
            if(eventDef is null) return false;
            _ = eventDef.Module.ImportReference(eventDef.EventType);
            _ = EditorStubMethod(eventDef.AddMethod, makePub, blacklist);
            _ = EditorStubMethod(eventDef.RemoveMethod, makePub, blacklist);
            _ = EditorStubMethod(eventDef.InvokeMethod, makePub, blacklist);
            foreach(MethodDefinition m in eventDef.OtherMethods) _ = EditorStubMethod(m, makePub, blacklist);
            foreach(CustomAttribute atr in eventDef.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }

        private static Boolean EditorStubMethod(MethodDefinition method, Boolean makePub, HashSet<MethodDefinition> blacklist)
        {
            if(method is null) return false;
            _ = method.Module.ImportReference(method.ReturnType);
            foreach(ParameterDefinition parameter in method.Parameters) _ = method.Module.ImportReference(parameter.ParameterType);
            if(!blacklist.Add(method)) return false;
            if(makePub) method.IsPublic = true;
            method?.Body?.Variables?.Clear();
            method?.Body?.Instructions?.Clear();
            CreateSimpleMethodBody(method);
            foreach(CustomAttribute atr in method.CustomAttributes) if(atr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") return true;
            return false;
        }
        private static Boolean EditorStubField(FieldDefinition field, Boolean makePub, Boolean removeReadonly)
        {
            if(field is null) return false;
            _ = field.Module.ImportReference(field.FieldType);
            Boolean serialized = false;
            Boolean ret = false;
            foreach(CustomAttribute atr in field.CustomAttributes)
            {
                switch(atr.AttributeType.FullName)
                {
                    case "System.Runtime.CompilerServices.CompilerGeneratedAttribute":
                        ret = true;
                        continue;
                    case "UnityEngine.SerializeField":
                        serialized = true;
                        continue;
                    default:
                        continue;
                }
            }
            if(makePub)
            {
                if(!field.IsPublic && !serialized) field.CustomAttributes.Add(new CustomAttribute(field.Module.ImportReference(nonSerializedAttributeConstructor)));
                field.IsPublic = true;
            }
            if(removeReadonly)
            {
                field.IsInitOnly = false;
            }

            return ret;
        }


        private static void ForwardType(TypeDefinition type, AssemblyDefinition from)
        {
            var atr = new CustomAttribute(from.MainModule.ImportReference(typeForwardedToConstructor));
            atr.ConstructorArguments.Add(new CustomAttributeArgument(from.MainModule.ImportReference(system_type), from.MainModule.ImportReference(type)));
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