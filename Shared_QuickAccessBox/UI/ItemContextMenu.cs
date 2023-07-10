using UILib;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace KK_QuickAccessBox.UI
{
    /// <summary>
    /// A lot of code borrowed from KK_Plugins/ItemBlacklist
    /// </summary>
    internal class ItemContextMenu
    {
        private readonly GameObject _canvasRoot;

        private Canvas ContextMenu;
        private CanvasGroup ContextMenuCanvasGroup;
        private Image ContextMenuPanel;
        private Button FavoriteButton;
        private Button FavoriteModButton;
        private Button BlacklistButton;
        private Button BlacklistModButton;
        private Button InfoButton;
        private readonly float UIWidth = 0.22f;
        private readonly float UIHeight = 0.245f;

        internal const float MarginSize = 4f;
        internal const float PanelHeight = 35f;
        internal const float ScrollOffsetX = -15f;
        internal readonly Color RowColor = new Color(0, 0, 0, 0.01f);
        internal RectOffset Padding = new RectOffset(3, 3, 0, 1);

        public ItemContextMenu(GameObject canvasRoot)
        {
            _canvasRoot = canvasRoot;
        }

        protected void InitUI()
        {
            if (ContextMenu != null) return;

            ContextMenu = UIUtility.CreateNewUISystem("ContextMenu");
            ContextMenu.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            ContextMenu.transform.SetParent(_canvasRoot.transform);
            ContextMenu.sortingOrder = 900;
            ContextMenuCanvasGroup = ContextMenu.GetOrAddComponent<CanvasGroup>();
            SetMenuVisibility(false);

            ContextMenuPanel = UIUtility.CreatePanel("Panel", ContextMenu.transform);
            ContextMenuPanel.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            ContextMenuPanel.transform.SetRect(0.05f, 0.05f, UIWidth, UIHeight);

            UIUtility.AddOutlineToObject(ContextMenuPanel.transform, Color.black);

            var scrollRect = UIUtility.CreateScrollView("ContextMenuWindow", ContextMenuPanel.transform);
            scrollRect.transform.SetRect(0f, 0f, 1f, 1f, MarginSize, MarginSize, 0.5f - MarginSize, -MarginSize);
            scrollRect.gameObject.AddComponent<Mask>();
            scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(ScrollOffsetX, 0f);
            scrollRect.viewport.offsetMax = new Vector2(ScrollOffsetX, 0f);
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.GetComponent<Image>().color = RowColor;

            var fontSize = 21;
            {
                var contentItem = UIUtility.CreatePanel("FavoriteContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("FavoritePanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                FavoriteButton = UIUtility.CreateButton("FavoriteButton", itemPanel.transform, "Favorite this item");
                FavoriteButton.gameObject.AddComponent<LayoutElement>();

                var text = FavoriteButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = fontSize;
            }
            {
                var contentItem = UIUtility.CreatePanel("FavoriteModContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("FavoriteModPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                FavoriteModButton = UIUtility.CreateButton("FavoriteModButton", itemPanel.transform, "Favorite all items from this mod");
                FavoriteModButton.gameObject.AddComponent<LayoutElement>();

                var text = FavoriteModButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = fontSize;
            }
            {
                var contentItem = UIUtility.CreatePanel("BlacklistContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("BlacklistPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                BlacklistButton = UIUtility.CreateButton("BlacklistButton", itemPanel.transform, "Hide this item");
                BlacklistButton.gameObject.AddComponent<LayoutElement>();

                var text = BlacklistButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = fontSize;
            }
            {
                var contentItem = UIUtility.CreatePanel("BlacklistModContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("BlacklistModPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                BlacklistModButton = UIUtility.CreateButton("BlacklistModButton", itemPanel.transform, "Hide all items from this mod");
                BlacklistModButton.gameObject.AddComponent<LayoutElement>();

                var text = BlacklistModButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = fontSize;
            }
            {
                var contentItem = UIUtility.CreatePanel("InfoContent", scrollRect.content.transform);
                contentItem.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
                contentItem.gameObject.AddComponent<Mask>();
                contentItem.color = RowColor;

                var itemPanel = UIUtility.CreatePanel("InfoPanel", contentItem.transform);
                itemPanel.color = RowColor;
                itemPanel.gameObject.AddComponent<CanvasGroup>();
                itemPanel.gameObject.AddComponent<HorizontalLayoutGroup>().padding = Padding;

                InfoButton = UIUtility.CreateButton("InfoButton", itemPanel.transform, "Print item info");
                InfoButton.gameObject.AddComponent<LayoutElement>();

                var text = InfoButton.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = false;
                text.fontSize = fontSize;
            }

            ContextMenu.UpdateAsObservable().Subscribe(_ =>
            {
                if (ContextMenuCanvasGroup && ContextMenuCanvasGroup.blocksRaycasts)
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        SetMenuVisibility(false);
                    }
                }
            });
        }

        public void ShowMenu(ItemInfo item)
        {
            //todo show dropdown when clicking on empty area too to allow switching filtering mode with no items visible
            // also add options to clear faves / blacklist / recents?

            InitUI();

            if (item == null)
            {
                SetMenuVisibility(false);
                return;
            }

            var xPosition = Input.mousePosition.x / Screen.width + 0.01f;
            var yPosition = Input.mousePosition.y / Screen.height - UIHeight - 0.01f;

            ContextMenuPanel.transform.SetRect(xPosition, yPosition, UIWidth + xPosition, UIHeight + yPosition);
            SetMenuVisibility(true);

            var guid = item.GUID;
            var itemId = item.NewCacheId;

            FavoriteButton.onClick.RemoveAllListeners();
            FavoriteModButton.onClick.RemoveAllListeners();
            BlacklistButton.onClick.RemoveAllListeners();
            BlacklistModButton.onClick.RemoveAllListeners();
            InfoButton.onClick.RemoveAllListeners();

            FavoriteButton.enabled = true;
            FavoriteModButton.enabled = true;
            BlacklistButton.enabled = true;
            BlacklistModButton.enabled = true;

            var favorited = QuickAccessBox.Instance.Favorited;
            if (favorited.Check(guid, itemId))
            {
                FavoriteButton.GetComponentInChildren<Text>().text = "Unfavorite this item";
                FavoriteButton.onClick.AddListener(() => favorited.RemoveItem(guid, itemId));
                FavoriteModButton.GetComponentInChildren<Text>().text = "Unfavorite all items from this mod";
                FavoriteModButton.onClick.AddListener(() => favorited.RemoveMod(guid));
            }
            else
            {
                FavoriteButton.GetComponentInChildren<Text>().text = "Favorite this item";
                FavoriteButton.onClick.AddListener(() => favorited.AddItem(guid, itemId));
                FavoriteModButton.GetComponentInChildren<Text>().text = "Favorite all items from this mod";
                FavoriteModButton.onClick.AddListener(() => favorited.AddMod(guid));
            }

            var blacklisted = QuickAccessBox.Instance.Blacklisted;
            if (blacklisted.Check(guid, itemId))
            {
                BlacklistButton.GetComponentInChildren<Text>().text = "Unhide this item";
                BlacklistButton.onClick.AddListener(() => blacklisted.RemoveItem(guid, itemId));
                BlacklistModButton.GetComponentInChildren<Text>().text = "Unhide all items from this mod";
                BlacklistModButton.onClick.AddListener(() => blacklisted.RemoveMod(guid));
            }
            else
            {
                BlacklistButton.GetComponentInChildren<Text>().text = "Hide this item";
                BlacklistButton.onClick.AddListener(() => blacklisted.AddItem(guid, itemId));
                BlacklistModButton.GetComponentInChildren<Text>().text = "Hide all items from this mod";
                BlacklistModButton.onClick.AddListener(() => blacklisted.AddMod(guid));
            }

            InfoButton.onClick.AddListener(() => QuickAccessBox.Logger.LogMessage(item.ToDescriptionString()));
        }

        public void SetMenuVisibility(bool visible)
        {
            if (ContextMenuCanvasGroup == null) return;
            ContextMenuCanvasGroup.alpha = visible ? 1 : 0;
            ContextMenuCanvasGroup.blocksRaycasts = visible;
        }
    }
}
