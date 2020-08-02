namespace AssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public struct StubbingOptions
    {
        public Boolean outputEditorStub;
        public Boolean makeEditorStubPublic;
        public Boolean outputReferenceStub;
        public Boolean makeReferenceStubPublic;
        public Boolean outputForwardAssembly;
        public Boolean removeNonSerializedTypesForEditor;
        public String targetAssemblyPath;
        public String outputPath;
        public String editorRenameTo;
    }
}
