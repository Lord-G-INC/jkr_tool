global using Binary_Stream;
global using static jkr_lib.Globals;
global using System.Text;
using System.Numerics;

namespace jkr_lib;

public static class Globals {
    public static T Align32<T>(T num) where T : struct, INumber<T>, IBitwiseOperators<T, T, T> {
        T n = T.CreateChecked(31);
        return (num + n) & ~n;
    }
    public static u16 CalcHash(string str) {
        u16 ret = 0;
        foreach (var b in Shift_JIS.GetBytes(str)) {
            ret = (u16)(b + (ret * 3u));
        }
        return ret;
    }

    public static Encoding Shift_JIS => Encoding.GetEncoding("Shift-JIS");

    static Globals() {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}