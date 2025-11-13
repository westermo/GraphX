using System;
using System.Threading;

namespace Westermo.GraphX.Common.Models.Semaphore;

public abstract class BWaitHandle
{
    protected abstract void OnSuccessfullWait();
    public abstract bool WaitOne();
    public abstract bool WaitOne(TimeSpan timeout);
    public abstract bool WaitOne(int millisecondsTimeout);

    internal abstract WaitHandle WaitHandle { get; }

    private static WaitHandle[] ToWaitHandle(BWaitHandle[] waitHandles)
    {
        var n = waitHandles.Length;
        var wh = new WaitHandle[n];

        for (var i = 0; i < n; ++i)
            wh[i] = waitHandles[i].WaitHandle;

        return wh;
    }

    public static int WaitAny(BWaitHandle[] waitHandles)
    {
        var wh = ToWaitHandle(waitHandles);
        var res = WaitHandle.WaitAny(wh);
        if (res >= 0)
            waitHandles[res].OnSuccessfullWait();
        return res;
    }

    public static int WaitAny(BWaitHandle[] waitHandles, int millisecondsTimeout)
    {
        var wh = ToWaitHandle(waitHandles);
        var res = WaitHandle.WaitAny(wh, millisecondsTimeout);
        if (res >= 0)
            waitHandles[res].OnSuccessfullWait();
        return res;
    }

    public static int WaitAny(BWaitHandle[] waitHandles, TimeSpan timeout)
    {
        var wh = ToWaitHandle(waitHandles);
        var res = WaitHandle.WaitAny(wh, timeout);
        if (res >= 0)
            waitHandles[res].OnSuccessfullWait();
        return res;
    }

    public static int WaitAll(BWaitHandle[] waitHandles)
    {
        throw new NotImplementedException();
    }

    public static int WaitAll(BWaitHandle[] waitHandles, int millisecondsTimeout)
    {
        throw new NotImplementedException();
    }

    public static int WaitAll(BWaitHandle[] waitHandles, TimeSpan timeout)
    {
        throw new NotImplementedException();
    }
}