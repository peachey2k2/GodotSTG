using System;
using Godot;

namespace GodotSTG;
internal static class Debug{
    internal static void Assert(bool cond, string msg){
#if DEBUG
        // until we have actual asserts, this is the best that i have.
        if (cond) return;
        GD.PrintErr(msg);
        throw new ApplicationException($"Assert Failed: {msg}");
#endif
    }
}