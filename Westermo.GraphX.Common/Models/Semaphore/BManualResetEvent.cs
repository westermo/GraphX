using System;
using System.Threading;

namespace Westermo.GraphX.Common.Models.Semaphore
{
    public class BManualResetEvent(bool initialState) : BWaitHandle, IDisposable
    {
        private ManualResetEvent _mre = new(initialState);

        // Summary:
        //     Sets the state of the event to non-signaled, which causes threads to block.
        //
        // Returns:
        //     true if the operation succeeds; otherwise, false.
        public bool Reset()
        {
            return _mre.Reset();
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
            return _mre.Set();
        }

        protected override void OnSuccessfullWait()
        {
            // nothing special needed
        }

        public override bool WaitOne()
        {
            return _mre.WaitOne();
        }

        public override bool WaitOne(TimeSpan timeout)
        {
            return _mre.WaitOne(timeout);
        }

        public override bool WaitOne(int millisecondsTimeout)
        {
            return _mre.WaitOne(millisecondsTimeout);
        }

        internal override WaitHandle WaitHandle => _mre;

        public void Dispose()
        {
            if (_mre != null)
            {
                _mre.Dispose();
                _mre = null;
            }
        }
    }
}