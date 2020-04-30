using System;
using System.Runtime.CompilerServices;

namespace AssemblyStripper
{
    public static class ILMethods
    {
        [MethodImpl( MethodImplOptions.ForwardRef )]
        public static extern int Square( int number );
    }
}
