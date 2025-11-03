using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DTech.InAppFlex.Editor
{
	[CustomPropertyDrawer(typeof(ProductIdAttribute))]
	internal sealed class ProductIdDrawer : PropertyDrawer
	{
		private static readonly List<string> _products = new();
		
		private readonly ProductSearchProvider _provider = new();
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.HelpBox(position, $"{nameof(ProductIdAttribute)} supported only string field or properties!", MessageType.Error);
				return;
			}

			IProductCollection collection = GetProductCollection();
			if (collection == null)
			{
				EditorGUI.HelpBox(position, $"{nameof(ProductCollection)} not found!\nPlease create it in the project.", MessageType.Error);
				return;
			}
			
			_products.Clear();
			for (int i = 0; i < collection.Count; i++)
			{
				IProductInfo info = collection[i];
				_products.Add(info.Id);
			}

			bool isEmptyValue = string.IsNullOrEmpty(property.stringValue);
			string caption = isEmptyValue ? "Select Product" : property.stringValue;

			GUIStyle style = new GUIStyle(EditorStyles.popup);
			if (!isEmptyValue && !_products.Contains(caption))
			{
				style.normal.textColor = Color.red;
			}

			if (GUI.Button(position, caption, style))
			{
				_provider.Setup(_products.ToArray(), (productId) =>
				{
					property.stringValue = productId;
					property.serializedObject.ApplyModifiedProperties();
				});
				
				var context = new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
				SearchWindow.Open(context, _provider);
			}
		}

		private static IProductCollection GetProductCollection()
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(ProductCollection)}");
			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ProductCollection productCollection = AssetDatabase.LoadAssetAtPath<ProductCollection>(assetPath);
				if (productCollection != null)
				{
					return productCollection;
				}
			}
			
			return null;
		}
	}
}