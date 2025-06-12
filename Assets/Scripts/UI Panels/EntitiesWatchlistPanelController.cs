using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Panels
{
    public class EntitiesWatchlistPanelController : MonoBehaviour, IEntitiesWatchlistPanelController
    {
        public static EntitiesWatchlistPanelController Instance { get; private set; }

        private bool _entityListMultipleSelectionActive;
        private bool _watchlistListMultipleSelectionActive;

        private Transform _entityListScrollView;
        private Transform _watchlistScrollView;

        private Transform _entitiesScrollViewContent;
        private Transform _watchlistScrollViewContent;
        private SelectedListItem _currentlySelectedEntityListItem;
        private SelectedListItem _currentlySelectedWatchlistListItem;
        private Transform _entityListAddToWatchlistButton;
        private Transform _watchlistDeleteButton;

        private GameObject _listItemPrefab;

        // First value in dictionary is list item, second value is entity object
        private readonly Dictionary<GameObject, GameObject> _selectedEntityListItems = new();
        private readonly Dictionary<GameObject, GameObject> _selectedWatchlistItems = new();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            _entityListScrollView = GameObject.Find("EWP_EntityListScrollView").transform;
            _watchlistScrollView = GameObject.Find("EWP_WatchlistScrollView").transform;
            _entitiesScrollViewContent = GameObject.Find("EWP_EntityListScrollView/Viewport/Content").transform;
            _watchlistScrollViewContent = GameObject.Find("EWP_WatchlistScrollView/Viewport/Content").transform;

            _entityListAddToWatchlistButton = GameObject.Find("EWP_AddToWatchlistButton").transform;
            _entityListAddToWatchlistButton.gameObject.SetActive(false);
            _watchlistDeleteButton = GameObject.Find("EWP_WatchlistDeleteButton").transform;
            _watchlistDeleteButton.gameObject.SetActive(false);

            _listItemPrefab = Resources.Load<GameObject>("ListItemPrefab");

            SimulationController.Instance.Entities.CollectionChanged +=
                (_, e) => UpdateEntitiesList(e, UiListType.EntityList);
            SimulationController.Instance.Watchlist.CollectionChanged +=
                (_, e) => UpdateEntitiesList(e, UiListType.Watchlist);
        }

        public void InitiateEntitiesList()
        {
            var entities = SimulationController.Instance.Entities;

            // Populate the list with new items
            foreach (var entity in entities)
            {
                AddEntityToList(entity, _entitiesScrollViewContent, UiListType.EntityList);
            }
        }

        public void OnListMultipleSelectionButtonClicked(UiListType listType)
        {
            Transform scrollView;
            bool isMultipleSelectActive;
            List<Transform> actionButtons;

            switch (listType)
            {
                case UiListType.EntityList:
                    scrollView = _entitiesScrollViewContent;
                    isMultipleSelectActive = _entityListMultipleSelectionActive;
                    actionButtons = new List<Transform> { _entityListAddToWatchlistButton };
                    break;
                case UiListType.Watchlist:
                    scrollView = _watchlistScrollViewContent;
                    isMultipleSelectActive = _watchlistListMultipleSelectionActive;
                    actionButtons = new List<Transform> { _watchlistDeleteButton };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }

            // Show multiple selection toggles
            foreach (Transform listItem in scrollView)
            {
                var button = listItem.transform.Find("ListItem_Button");
                var toggleTransform = button.transform.Find("ListItem_Toggle");
                var toggleCanvasGroup = toggleTransform.GetComponent<CanvasGroup>();

                // Change the toggle visibility
                if (isMultipleSelectActive)
                {
                    toggleTransform.GetComponent<Toggle>().isOn = false;
                }

                toggleCanvasGroup.alpha = isMultipleSelectActive ? 0 : 1;
            }

            // Clear the list of selected items when going out of Multi-select mode
            if (isMultipleSelectActive)
            {
                switch (listType)
                {
                    case UiListType.EntityList:
                        _selectedEntityListItems.Clear();
                        break;
                    case UiListType.Watchlist:
                        _selectedWatchlistItems.Clear();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
                }
            }

            // All list items should be unselected when going to and from Multi-select mode
            var selectedEntity = SimulationController.Instance.GetSelectedEntity();
            if (selectedEntity != null)
            {
                SimulationController.Instance.DeselectSelectedEntity();
            }

            foreach (var actionButton in actionButtons)
            {
                actionButton.gameObject.SetActive(!isMultipleSelectActive);
            }

            switch (listType)
            {
                case UiListType.EntityList:
                    _entityListMultipleSelectionActive = !isMultipleSelectActive;
                    break;
                case UiListType.Watchlist:
                    _watchlistListMultipleSelectionActive = !isMultipleSelectActive;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }
        }

        public void OnEwpAddToWathclistButtonClicked()
        {
            foreach (var selectedEntityListItem in _selectedEntityListItems)
            {
                if (!SimulationController.Instance.Watchlist.Contains(selectedEntityListItem.Value))
                {
                    SimulationController.Instance.Watchlist.Add(selectedEntityListItem.Value);
                }
            }

            _selectedEntityListItems.Clear();
            OnListMultipleSelectionButtonClicked(UiListType.EntityList);
        }

        public void OnEspAddToWatchlistButtonClicked(GameObject selectedEntity)
        {
            // Select entity added to the Watchlist
            SelectListItem(selectedEntity.GetComponent<EntityController>(), UiListType.Watchlist, true);
        }

        public void OnDeleteFromWatchlistButtonClicked()
        {
            SimulationController.Instance.Watchlist.RemoveRange(_selectedWatchlistItems.Values);
            _selectedWatchlistItems.Clear();
            OnListMultipleSelectionButtonClicked(UiListType.Watchlist);
        }

        public void OnEntityDeselected()
        {
            UnselectCurrentlySelectedListItem(UiListType.EntityList);
            UnselectCurrentlySelectedListItem(UiListType.Watchlist);
        }

        public void OnEntitySelected(EntityController entity, bool isSelectedFromUi)
        {
            // Select the entity in the entities list
            SelectListItem(entity, UiListType.EntityList, !isSelectedFromUi);

            // Select the entity in the watchlist list
            SelectListItem(entity, UiListType.Watchlist, !isSelectedFromUi);
        }

        public void OnSimulationStopped()
        {
            foreach (Transform child in _entitiesScrollViewContent)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in _watchlistScrollViewContent)
            {
                Destroy(child.gameObject);
            }
        }

        public void SelectNextListItem(UiListType listType)
        {
            Transform[] listItems;
            Transform nextItem = null;

            switch (listType)
            {
                case UiListType.EntityList:
                    if (_currentlySelectedEntityListItem != null)
                    {
                        listItems = _entitiesScrollViewContent.transform.GetComponentsInChildren<Transform>(false);
                        nextItem = listItems[_currentlySelectedEntityListItem.Index + 1];
                    }

                    break;
                case UiListType.Watchlist:
                    if (_currentlySelectedWatchlistListItem != null)
                    {
                        listItems = _watchlistScrollViewContent.transform.GetComponentsInChildren<Transform>(false);
                        nextItem = listItems[_currentlySelectedWatchlistListItem.Index + 1];
                    }

                    break;
                default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }

            if (nextItem == null) return;
            var nextEntity = SimulationController.Instance.Entities.First(entity => entity.name == nextItem.name);
            if (nextEntity != null) SimulationController.Instance.RegisterSelectedEntity(nextEntity);
        }

        private void OnEntityListToggleClicked(Toggle toggle, GameObject entity, Transform listItem,
            UiListType listType)
        {
            if (!toggle.isOn)
            {
                switch (listType)
                {
                    case UiListType.EntityList:
                        _selectedEntityListItems.Add(listItem.gameObject, entity);
                        break;
                    case UiListType.Watchlist:
                        _selectedWatchlistItems.Add(listItem.gameObject, entity);
                        break;
                    case UiListType.GeneList:
                    case UiListType.NeuronList:
                    case UiListType.None:
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
                }

                toggle.isOn = true;
            }
            else
            {
                switch (listType)
                {
                    case UiListType.EntityList:
                        _selectedEntityListItems.Remove(listItem.gameObject);
                        break;
                    case UiListType.Watchlist:
                        _selectedWatchlistItems.Remove(listItem.gameObject);
                        break;
                    case UiListType.GeneList:
                    case UiListType.NeuronList:
                    case UiListType.None:
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
                }

                toggle.isOn = false;
            }
        }

        private void UpdateEntitiesList(NotifyCollectionChangedEventArgs e, UiListType listType)
        {
            var scrollViewContent = listType switch
            {
                UiListType.EntityList => _entitiesScrollViewContent,
                UiListType.Watchlist => _watchlistScrollViewContent,
                _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
            };

            if (e.NewItems != null)
            {
                foreach (GameObject entity in e.NewItems)
                {
                    AddEntityToList(entity, scrollViewContent, listType);
                }
            }

            if (e.OldItems != null)
            {
                foreach (GameObject entity in e.OldItems)
                {
                    // Remove the entity from the entities list
                    RemoveListItem(entity, scrollViewContent);
                }
            }
        }

        private void AddEntityToList(GameObject entity, Transform scrollViewContent, UiListType listType)
        {
            var listItem = Instantiate(_listItemPrefab, scrollViewContent);
            _listItemPrefab.GetComponent<UiListItemController<GameObject>>().Init(listType, entity.gameObject.name);

            var button = listItem.transform.Find("ListItem_Button");

            button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;
        }

        private void RemoveListItem(GameObject entity, Transform scrollView)
        {
            foreach (Transform child in scrollView)
            {
                if (child.name == entity.name)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void SelectListItem(EntityController entity, UiListType listType, bool shouldScrollToItem)
        {
            Transform scrollView;
            Transform scrollViewContent;

            switch (listType)
            {
                case UiListType.EntityList:
                    scrollView = _entityListScrollView;
                    scrollViewContent = _entitiesScrollViewContent;
                    break;
                case UiListType.Watchlist:
                    scrollView = _watchlistScrollView;
                    scrollViewContent = _watchlistScrollViewContent;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }

            UnselectCurrentlySelectedListItem(listType);

            // Find the item in the list that matches the entity game object
            for (var i = 0; i < scrollViewContent.childCount; i++)
            {
                var listItem = scrollViewContent.GetChild(i);
                if (listItem.name == entity.name)
                {
                    var selectedListItem = new SelectedListItem(listItem, i);
                    // Update the UI of the selected list item
                    listItem.GetComponent<UiListItemController<EntityController>>().IsSelected = true;

                    // Scroll to the selected item
                    if (shouldScrollToItem) ScrollToSelectedItem(scrollView, listItem);

                    switch (listType)
                    {
                        case UiListType.EntityList:
                            _currentlySelectedEntityListItem = selectedListItem;
                            break;
                        case UiListType.Watchlist:
                            _currentlySelectedWatchlistListItem = selectedListItem;
                            break;
                        default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
                    }

                    break;
                }
            }

            // If selected item doesn't exist in watchlist, unselect previously selected watchlist item
            UnselectSelectedWatchlistItem(entity, listType);
        }

        private void UnselectCurrentlySelectedListItem(UiListType listType)
        {
            var currentlySelectedItem = listType switch
            {
                UiListType.EntityList => _currentlySelectedEntityListItem,
                UiListType.Watchlist => _currentlySelectedWatchlistListItem,
                _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
            };

            if (currentlySelectedItem != null)
            {
                currentlySelectedItem.ListItem.GetComponent<UiListItemController<EntityController>>().IsSelected =
                    false;
            }

            switch (listType)
            {
                case UiListType.EntityList:
                    _currentlySelectedEntityListItem = null;
                    break;
                case UiListType.Watchlist:
                    _currentlySelectedWatchlistListItem = null;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }
        }

        private void UnselectSelectedWatchlistItem(EntityController entity, UiListType listType)
        {
            var watchlist = SimulationController.Instance.Watchlist;
            if (listType == UiListType.EntityList && _currentlySelectedWatchlistListItem != null)
            {
                var watchListContainsSelectedEntity = watchlist.Contains(entity.gameObject);
                if (!watchListContainsSelectedEntity)
                {
                    _currentlySelectedWatchlistListItem.ListItem.GetComponent<UiListItemController<EntityController>>()
                        .IsSelected = false;
                    _currentlySelectedWatchlistListItem = null;
                }
            }
        }

        private static void ScrollToSelectedItem(Transform scrollView, Transform listItem)
        {
            Canvas.ForceUpdateCanvases();
            var itemRect = listItem.GetComponent<RectTransform>();
            var scrollRect = scrollView.GetComponent<ScrollRect>();
            var scrollValue = 1 + itemRect.anchoredPosition.y / scrollRect.content.rect.height;
            scrollRect.verticalNormalizedPosition = scrollValue;
        }

        public void AddToSelectedEntityList(GameObject entity, UiListType listType)
        {
            switch (listType)
            {
                case UiListType.EntityList:
                    _selectedEntityListItems.Add(gameObject, entity);
                    break;
                case UiListType.Watchlist:
                    _selectedWatchlistItems.Add(gameObject, entity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }
        }

        public void RemoveFromSelectedEntityList(UiListType listType)
        {
            switch (listType)
            {
                case UiListType.EntityList:
                    _selectedEntityListItems.Remove(gameObject);
                    break;
                case UiListType.Watchlist:
                    _selectedWatchlistItems.Remove(gameObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
            }
        }
    }

    internal class SelectedListItem
    {
        public Transform ListItem;
        public int Index;

        public SelectedListItem(Transform listItem, int index)
        {
            ListItem = listItem;
            Index = index;
        }
    }
}