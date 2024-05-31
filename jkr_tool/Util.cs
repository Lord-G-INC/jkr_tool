using System.Runtime.InteropServices;

namespace jkr_tool;

public static class Util {
    public static Span<T> MakeSpan<T>(this ref T item) where T : unmanaged {
        return MemoryMarshal.CreateSpan(ref item, 1);
    }
    public static Span<TTo> Cast<TFrom, TTo>(this Span<TFrom> span) where TFrom : struct where TTo : struct {
        return MemoryMarshal.Cast<TFrom, TTo>(span);
    }
}