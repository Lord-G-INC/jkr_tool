global using binary_stream;
global using static jkr_lib.Globals;
global using System.Text;
using System.Numerics;

namespace jkr_lib;

public static class Globals {
    public static T Align32<T>(T num) where T : struct, INumber<T>, IBitwiseOperators<T, T, T> {
        T n = T.Parse("31", null);
        return (num + n) & ~n;
    }
    public static u16 CalcHash(string str) {
        u16 ret = 0;
        foreach (var b in Encoding.ASCII.GetBytes(str)) {
            ret = (u16)(b + (ret * 0x1fu));
        }
        return ret;
    }
}