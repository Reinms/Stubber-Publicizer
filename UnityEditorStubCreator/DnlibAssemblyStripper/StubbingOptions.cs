namespace AssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    using dnlib.DotNet;

    public struct StubbingOptions
    {
        public Boolean makeAllPublic;
        public Boolean removeReadonly;
        public Boolean removeMethodBodies;
        public Boolean preserveSerialization;
        public Boolean targetIsUnityAssembly;
        public Boolean preserveEditorMethods;
        public Boolean stripAllNonSerialized;
        public Boolean remapToOutputContext;        //

        public FileInfo inputAssembly;
        public FileOrDirectoryInfo[] inputContext;
        public DirectoryInfo outputDirectory;
        public FileOrDirectoryInfo[] outputContext;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct FileOrDirectoryInfo
    {
        [FieldOffset(0)]
        private readonly Byte _mode;
        [FieldOffset(1)]
        private readonly FileInfo _file;
        [FieldOffset(1)]
        private readonly DirectoryInfo _directory;

        public static implicit operator FileOrDirectoryInfo(FileInfo file) => new(file);
        public static implicit operator FileOrDirectoryInfo(DirectoryInfo directory) => new(directory);

        public FileOrDirectoryInfo(FileInfo file)
        {
            this._mode = 1;
            this._directory = null;
            this._file = file;
        }
        public FileOrDirectoryInfo(DirectoryInfo directory)
        {
            this._mode = 2;
            this._file = null;
            this._directory = directory;
        }

        internal Byte mode => this.mode;
        internal FileInfo file => this.mode == 1 ? this._file : null;
        internal DirectoryInfo directory => this.mode == 2 ? this._directory : null;

        internal IEnumerator<FileInfo> GetEnumerator() => this.mode switch
        {
            1 => Enumerable.Repeat(this.file, 1).GetEnumerator(),
            2 => this.directory.EnumerateFiles().GetEnumerator(),
            _ => Enumerable.Empty<FileInfo>().GetEnumerator(),
        };
    }

    internal static class FileOrDirectoryXtn
    {
        internal static IEnumerator<FileInfo> GetEnumerator(this FileOrDirectoryInfo self) => self.mode switch
        {
            1 => Enumerable.Repeat(self.file, 1).GetEnumerator(),
            2 => self.directory.EnumerateFiles().GetEnumerator(),
            _ => Enumerable.Empty<FileInfo>().GetEnumerator(),
        };
    }

    public struct Filter
    {

    }

    public interface IFilter
    {

    }
}
