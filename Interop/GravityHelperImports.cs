using System;
using MonoMod.ModInterop;

namespace Celeste.Mods.EmHelper.Interop
{
    [ModImportName("GravityHelper")]
    public sealed class GravityHelperImports
    {
        public Func<bool> IsPlayerInverted;
    }
}
