using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class EmptyCart : ICart
    {
        public IReadOnlyList<CartItem> Items()
        {
            return Array.Empty<CartItem>();
        }
    }
}