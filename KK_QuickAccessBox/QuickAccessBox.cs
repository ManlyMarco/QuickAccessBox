using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using KKAPI.Studio;
using Studio;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_QuickAccessBox
{
	[BepInPlugin(GUID, GUID, Version)]
	[BepInDependency(DynamicTranslationLoader.DynamicTranslator.GUID)]
	public class QuickAccessBox : BaseUnityPlugin
	{
		public const string GUID = "KK_QuickAccessBox";
		internal const string Version = "0.1";
		private bool _forceFocusTextbox;

		private IEnumerable<ItemInfo> _itemList;
		private Vector2 _scrollPosition = Vector2.zero;

		private string _searchStr = string.Empty;
		private bool _showBox;

		/// <summary>
		/// List of all studio items that can be added into the game
		/// </summary>
		public IEnumerable<ItemInfo> ItemList => _itemList ?? (_itemList = GetItemList());

		[DisplayName("Search developer information")]
		[Description("The search box will search asset filenames, group/category/item ID numbers, manifests and other things from list files.")]
		public static ConfigWrapper<bool> SearchDeveloperInfo { get; private set; }

		[DisplayName("Show results if search string is empty")]
		[Description("If enabled will show everything, if disabled will show nothing.")]
		public static ConfigWrapper<bool> ShowEmptyResults { get; private set; }
		
		[Advanced(true)]
		public static SavedKeyboardShortcut GenerateThumbsKey { get; private set; }

		[DisplayName("Show quick access box")]
		public static SavedKeyboardShortcut ShowBoxKey { get; private set; }

		public bool ShowBox
		{
			get => _showBox;
			set
			{
				if (value == _showBox)
					return;

				if (value && !_showBox)
					_forceFocusTextbox = true;

				_showBox = value;
				_searchStr = string.Empty;
			}
		}

		internal static Info Info { get; private set; }

		private static List<ItemInfo> GetItemList()
		{
			var results = new List<ItemInfo>();

			foreach (var group in Info.dicItemLoadInfo)
			{
				foreach (var category in group.Value)
				{
					foreach (var item in category.Value)
					{
						try
						{
							results.Add(new ItemInfo(group.Key, category.Key, item.Key, item.Value));
						}
						catch (Exception e)
						{
							Logger.Log(LogLevel.Warning, $"Failed to load information about item {item.Value.name} group={group.Key} category={category.Key} itemNo={item.Key} - {e.Message}");
						}
					}
				}
			}

			results.Sort((x, y) => string.Compare(x.FullName, y.FullName, StringComparison.Ordinal));
			return results;
		}

		private bool ItemMatchesSearch(ItemInfo x)
		{
			if (string.IsNullOrEmpty(_searchStr)) return ShowEmptyResults.Value;

			var splitSearchStr = _searchStr.ToLowerInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			return splitSearchStr.All(s => x.SearchStr.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private void OnGUI()
		{
			if (!ShowBox) return;

			if (ItemList == null)
				_itemList = GetItemList();

			GUILayout.Window(GetHashCode(), new Rect(100, 100, 500, 500), QuickAccessWindow, "Quick access box");
		}

		private void QuickAccessWindow(int id)
		{
			if (Event.current.keyCode == KeyCode.Escape)
				ShowBox = false;

			GUILayout.BeginVertical();
			{
				GUILayout.BeginHorizontal();
				{
					const string searchBoxName = "searchBox";
					GUI.SetNextControlName(searchBoxName);
					_searchStr = GUILayout.TextField(_searchStr);
					if (_forceFocusTextbox)
					{
						_forceFocusTextbox = false;
						GUI.FocusControl(searchBoxName);
					}
				}
				GUILayout.EndHorizontal();

				_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
				{
					GUILayout.BeginVertical();
					{
						var filteredItems = ItemList.Where(ItemMatchesSearch).Take(40).ToList();

						if (filteredItems.Count == 0)
						{
							GUILayout.Box("Nothing found");
						}
						else
						{
							foreach (var itemInfo in filteredItems)
							{
								if (GUILayout.Button(itemInfo.FullName, GUI.skin.box, GUILayout.ExpandWidth(true)))
									itemInfo.AddItem();
							}

							if (filteredItems.Count == 40)
								GUILayout.Box("and more...");
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();
		}

		private void Start()
		{
			if (!StudioAPI.InsideStudio)
			{
				enabled = false;
				return;
			}

			Info = Singleton<Info>.Instance;

			ShowBoxKey = new SavedKeyboardShortcut(nameof(ShowBoxKey), this, new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl));
			SearchDeveloperInfo = new ConfigWrapper<bool>(nameof(SearchDeveloperInfo), this, false);
			ShowEmptyResults = new ConfigWrapper<bool>(nameof(ShowEmptyResults), this, false);

			// todo disable by default
			GenerateThumbsKey = new SavedKeyboardShortcut(nameof(GenerateThumbsKey), this, new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl, KeyCode.LeftShift));
		}

		private void Update()
		{
			if (ShowBoxKey.IsDown())
				ShowBox = !ShowBox;

			if (ShowBox)
			{
				if (Input.GetKeyDown(KeyCode.Escape))
					ShowBox = false;
			}

			if (GenerateThumbsKey.IsDown())
			{
				StartCoroutine(ThumbnailGenerator.MakeThumbnail(ItemList, @"D:\thumb_background.png", "D:\\"));
			}
		}
	}
}
