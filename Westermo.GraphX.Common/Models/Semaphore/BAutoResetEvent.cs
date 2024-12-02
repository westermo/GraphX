using System;
using System.Threading;

namespace Westermo.GraphX
{
    public class BAutoResetEvent(bool initialState) : BWaitHandle, IDisposable
    {
        private AutoResetEvent _are = new(initialState);

        // Summary:
        //     Sets the state of the event to non-signaled, which causes threads to block.
        //
        // Returns:
        //     true if the operation succeeds; otherwise, false.
        public bool Reset()
        {
            return _are.Reset();
        }

        //
        // Summary:
        //     Sets the state of the event to signaled, which allows one or more waiting
        //     threads to proceed.
        //
        // Returns:
        //     true if the operation succeeds; otherwise, false.
        public bool Set()
        {
            return _are.Set();
        }

        protected override void OnSuccessfullWait()
        {
            // nothing special needed
        }

        public override bool WaitOne()
        {
            throw new NotImplementedException();
        }

        public override bool WaitOne(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override bool WaitOne(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        internal override WaitHandle WaitHandle => _are;

        public void Dispose()
        {
            if (_are == null) return;
            _are.Dispose();
            _are = null;
        }
    }
}