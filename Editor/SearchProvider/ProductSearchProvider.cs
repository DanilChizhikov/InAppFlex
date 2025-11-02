using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DTech.InAppFlex.Editor
{
    internal sealed class ProductSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private readonly List<string> _products = new();

        private Action<string> _onSelectedCallback;
        
        public void Setup(string[] productIds, Action<string> callback)
        {
            _products.Clear();
            _products.AddRange(productIds);
            _onSelectedCallback = callback;
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Products"), 0)
            };
            
            foreach (var productId in _products)
            {
                entries.Add(new SearchTreeEntry(new GUIContent(productId))
                {
                    level = 1,
                    userData = productId
                });
            }
            
            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is string productId)
            {
                _onSelectedCallback?.Invoke(productId);
                return true;
            }
            
            return false;
        }
    }
}