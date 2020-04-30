using System;
using AssemblyStripper;
using System.IO;

namespace ConsoleStubber
{
    class Program
    {
        static void Main( string[] args )
        {
            if( args.Length != 1 )
            {
                Console.WriteLine( "Bad args, press any key to close" );
                _ = Console.ReadKey();
                return;
            }

            String path = args[0];
            DirectoryInfo outPath = Directory.GetParent( path );
            outPath = outPath.CreateSubdirectory( "StubOutput" );
            if( !outPath.Exists )
            {
                outPath.Create();
            }

            var options = new StubbingOptions
            {
                targetAssemblyPath = args[0],
                outputPath = outPath.FullName,
                assemblyName = "Test",
            };

            if( Stubber.StubAssembly( options ) )
            {
                Console.WriteLine( "Success" );
            } else
            {
                Console.WriteLine( "Failed" );
            }



            Console.WriteLine( "Press any key to close" );
            _ = Console.ReadKey();
        }
    }
}
