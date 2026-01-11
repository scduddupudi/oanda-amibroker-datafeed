// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GQEContext.cs" company="Clartix LLC">
//   Copyright © 2026 Sanjay DUDDUPUDI, Clartix LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AmiBroker.Plugin.Models
{
    using System.Runtime.InteropServices;

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct GQEContext
    {
        public int StructSize;
    }
}
