namespace DnlibAssemblyStripper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public readonly struct StubResult
    {
        private readonly Exception _error;

        public Boolean success => this._error is null;
        public Exception error => this._error;

        internal StubResult(Exception e)
        {
            this._error = e;
        }
    }
}
