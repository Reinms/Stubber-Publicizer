namespace ConsoleStubber
{
    using System;
    using AssemblyStripper;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    public static class Program
    {
        public static void Main(String[] args)
        {
            if(args is null || args.Length != 1)
            {
                Console.WriteLine("Bad args, press any key to close");
                _ = Console.ReadKey();
                return;
            }

            String path = args[0];

            StubbingOptions options = GetOptionsFromConsole(path);

            Console.WriteLine(Stubber.StubAssembly(options) ? "Success" : "Failed");
            Console.WriteLine("Press any key to close");
            _ = Console.ReadKey();
        }

        private static readonly HashSet<String> positiveAnswers = new HashSet<String>
        { "yes", "true", "y", "t" };
        private static readonly HashSet<String> negativeAnswers = new HashSet<String>
        { "no", "false", "n", "f" };

        private static StubbingOptions GetOptionsFromConsole(String path)
        {
            static void OutputAndPublic(String name, Boolean askPublic, out Boolean output, out Boolean publ)
            {
                Console.WriteLine($"Output {name}?");
                while(!PositiveOrNegativeInput(out output)) { }
                if(output && askPublic)
                {
                    Console.WriteLine($"Make {name} public?");
                    while(!PositiveOrNegativeInput(out publ)) { }
                } else
                {
                    publ = false;
                }
            }

            StubbingOptions options;
            do
            {
                Boolean stripNonSerialized = false;
                String editorName = "";

                OutputAndPublic("Reference Assembly", true, out Boolean outputRefAsm, out Boolean pubRefAsm);
#if EDITORFWD
                OutputAndPublic("Editor Assembly", true, out Boolean outputEditorAsm, out Boolean pubEditorAsm);
                OutputAndPublic("Forward Assembly", false, out Boolean outputForwardAsm, out Boolean _);
                if( outputEditorAsm || outputForwardAsm)
                {
                    Console.WriteLine("Editor/Forward assembly name:");
                    editorName = GetStringInput(false, Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()));
                    if(outputEditorAsm)
                    {
                        Console.WriteLine("Strip non-serialized types from editor assembly? (recommended true)");
                        while(!PositiveOrNegativeInput(out stripNonSerialized)) { }
                    }
                }
#else
                Boolean outputEditorAsm = false;
                Boolean pubEditorAsm = false;
                Boolean outputForwardAsm = false;
#endif
                Console.WriteLine("Remove readonly?");
                Boolean removeReadonly = false;
                while(!PositiveOrNegativeInput(out removeReadonly)) { }

                Console.WriteLine("Output subfolder name:");
                String outputSubfolderName = GetStringInput(true, Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()));
                String outputPath = new FileInfo(path).DirectoryName;
                if(outputSubfolderName != "")
                {
                    DirectoryInfo outPath = Directory.GetParent(path);
                    outPath = outPath.CreateSubdirectory(outputSubfolderName);
                    if(!outPath.Exists) outPath.Create();
                    outputPath = outPath.FullName;
                }

                options = new StubbingOptions
                {
                    outputReferenceStub = outputRefAsm,
                    makeReferenceStubPublic = pubRefAsm,
                    outputEditorStub = outputEditorAsm,
                    makeEditorStubPublic = pubEditorAsm,
                    outputForwardAssembly = outputForwardAsm,
                    outputPath = outputPath,
                    targetAssemblyPath = path,
                    editorRenameTo = editorName,
                    removeNonSerializedTypesForEditor = stripNonSerialized,
                    removeReadonly = removeReadonly
                };
            } while(!ConfirmSettings(options));
            return options;
        }

        private static Boolean ConfirmSettings(StubbingOptions options)
        {
            String nl = Environment.NewLine;
            Console.WriteLine(
$"Settings:{nl}{nameof(options.outputReferenceStub)}: {options.outputReferenceStub}{nl}{nameof(options.makeReferenceStubPublic)}: {options.makeReferenceStubPublic}{nl}{nameof(options.outputEditorStub)}: {options.outputEditorStub}{nl}{nameof(options.makeEditorStubPublic)}: {options.makeEditorStubPublic}{nl}{nameof(options.outputForwardAssembly)}{nl}{options.outputForwardAssembly}{nl}{nameof(options.editorRenameTo)}: {options.editorRenameTo}{nl}{nameof(options.removeReadonly)}: {options.removeReadonly}{nl}{nameof(options.removeNonSerializedTypesForEditor)}: {options.removeNonSerializedTypesForEditor}{nl}{nameof(options.outputPath)}: {options.outputPath}{nl}{nameof(options.targetAssemblyPath)}: {options.targetAssemblyPath}{nl}{nl}Confirm?");
            Boolean res;
            while (!PositiveOrNegativeInput(out res)) { }
            return res;
        }

        private static Boolean PositiveOrNegativeInput(out Boolean result)
        {
            String input = Console.ReadLine();
            switch((positiveAnswers.Contains(input), negativeAnswers.Contains(input)))
            {
                case var (a, b) when a && !b:
                    return result = true;
                case var (a, b) when !a && b:
                    result = false;
                    return true;
                case var (a, b) when a && b:
                    Console.WriteLine($"{input} is both positive and negative... good job...");
                    goto default;         
                default:
                    Console.WriteLine($"Invalid input: {input}\r\nPositive inputs are: {String.Join(", ", positiveAnswers)}\r\nNegative inputs are: {String.Join(", ", negativeAnswers)}");
                    return result = false;
            }
        }


        private delegate Boolean StringCheckPredicate(String input, out String errorMessage);
        private static String GetStringInput<TCollection>( Boolean emptyAllowed, TCollection invalidChars, StringCheckPredicate extraCheck = null )
            where TCollection : IEnumerable<Char>
        {
            static Boolean IsValid(String input, Boolean emptyAllowed, TCollection invalidChars, StringCheckPredicate extraCheck)
            {
                foreach(Char c in invalidChars) if(input.Contains(c))
                {
                    Console.WriteLine($"Invalid character {c} in input: {input}");
                    return false;
                }
                if(emptyAllowed || input != "")
                {
                    if (extraCheck is null) return true;
                    if (extraCheck(input, out String err) ) return true;
                    Console.WriteLine($"Invalid input {input}\r\n{err}");
                }
                Console.WriteLine("Empty strings not allowed");
                return false;
            }
            String input;
            do
            {
                input = Console.ReadLine();
            } while(!IsValid(input, emptyAllowed, invalidChars, extraCheck));
            return input;
        }
    }
}
