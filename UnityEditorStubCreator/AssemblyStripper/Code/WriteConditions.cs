using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace AssemblyStripper
{
    internal delegate Boolean CheckDelegate<TObject>( in TObject arg );
    internal class WriteConditions
    {
        internal WriteConditions( CheckDelegate<TypeDefinition> type, CheckDelegate<MethodDefinition> method, CheckDelegate<PropertyDefinition> property, CheckDelegate<FieldDefinition> field )
        {
            this.CheckType = type;
            this.CheckMethod = method;
            this.CheckProperty = property;
            this.CheckField = field;
        }


#pragma warning disable IDE1006 // Naming Styles
        internal CheckDelegate<TypeDefinition> CheckType { get; private set; }
        internal CheckDelegate<MethodDefinition> CheckMethod { get; private set; }
        internal CheckDelegate<PropertyDefinition> CheckProperty { get; private set; }
        internal CheckDelegate<FieldDefinition> CheckField { get; private set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
