global using binary_stream;
global using jkr_tool;
global using static jkr_tool.Globals;
global using System.Text;
using System.Numerics;

namespace jkr_tool;

public static class Globals {
    public static T Align32<T>(T num) where T : struct, INumber<T>, IBitwiseOperators<T, T, T> {
        T n = T.Parse("31", null);
        return (num + n) & ~n;
    }
    public static u16 CalcHash(string str) {
        u16 ret = 0;
        foreach (var b in Encoding.ASCII.GetBytes(str)) {
            ret *= 3;
            ret += b;
        }
        return ret;
    }
}