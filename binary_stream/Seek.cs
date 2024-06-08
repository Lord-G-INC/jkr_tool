namespace binary_stream;

public class Seek<T>: IDisposable where T: Stream {
    protected T Stream;
    protected long Position;

    public Seek(T stream, long toseek, SeekOrigin way = SeekOrigin.Begin) {
        Stream = stream;
        Position = Stream.Position;
        Stream.Seek(toseek, way);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        Stream.Seek(Position, SeekOrigin.Begin);
    }

    public void ExecTask(Action<T> func) {
        func(Stream);
    }

    public Res ExecTask<Res>(Func<T, Res> func) {
        return func(Stream);
    }
}

public static class SeekExt {
    public static void SeekTask<T>(this T stream, long toseek, Action<T> func) where T : Stream {
        using var seek = new Seek<T>(stream, toseek);
        seek.ExecTask(func);
    }
    public static void SeekTask<T>(this T stream, long toseek, SeekOrigin way, Action<T> func) where T : Stream {
        using var seek = new Seek<T>(stream, toseek, way);
        seek.ExecTask(func);
    }
    public static Ret SeekTask<T, Ret>(this T stream, long toseek, Func<T, Ret> func) where T : Stream {
        using var seek = new Seek<T>(stream, toseek);
        return seek.ExecTask(func);
    }
    public static Ret SeekTask<T, Ret>(this T stream, long toseek, SeekOrigin way, Func<T, Ret> func) where T : Stream {
        using var seek = new Seek<T>(stream, toseek, way);
        return seek.ExecTask(func);
    }
}