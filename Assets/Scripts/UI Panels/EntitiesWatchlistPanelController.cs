using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        private Transform _currentlySelectedEntityListItem;
        private Transform _currentlySelectedWatchlistListItem;
        private Transform _entityListAddToWatchlistButton;
        private Transform _watchlistDeleteButton;

        private GameObject _entityListItemPrefab;

        private List<GameObject> _selectedEntityListItems = new();
        private List<GameObject> _selectedWatchlistItems = new();

        private readonly Color32 _lightListItemColor = new(255, 255, 255, 171);
        private readonly Color32 _darkListItemColor = new(0, 0, 0, 171);
        private readonly Color32 _lightListItemTextColor = new(255, 255, 255, 255);
        private readonly Color32 _darkListItemTextColor = new(0, 0, 0, 255);

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

            _entityListItemPrefab = Resources.Load<GameObject>("ListItemPrefab");

            SimulationController.Instance.Entities.CollectionChanged +=
                (_, e) => UpdateEntitiesList(e, EntityListType.EntityList);
            SimulationController.Instance.Watchlist.CollectionChanged +=
                (_, e) => UpdateEntitiesList(e, EntityListType.Watchlist);
        }

        public void InitiateEntitiesList()
        {
            var entities = SimulationController.Instance.Entities;
            Debug.Log("UpdateEntitiesWatchlistPanel called. Number of entities: " + entities.Count);

            // Populate the list with new items
            foreach (var entity in entities)
            {
                AddEntityToList(entity, _entitiesScrollViewContent, EntityListType.EntityList);
            }
        }

        public void OnEntitiesMultipleSelectionButtonClicked()
        {
            // Show multiple selection toggles
            foreach (Transform listItem in _entitiesScrollViewContent)
            {
                var button = listItem.transform.Find("ListItem_Button");
                var toggleTransform = button.transform.Find("ListItem_Toggle");
                var toggleCanvasGroup = toggleTransform.GetComponent<CanvasGroup>();

                // Change the toggle visibility
                if (_entityListMultipleSelectionActive)
                {
                    toggleTransform.GetComponent<Toggle>().isOn = false;
                }
                toggleCanvasGroup.alpha = _entityListMultipleSelectionActive ? 0 : 1;
            }

            // Clear the list of selected items when going out of Multi-select mode
            if (_entityListMultipleSelectionActive)
            {
                _selectedEntityListItems.Clear();
            }

            // All list items should be unselected when going to and from Multi-select mode
            var selectedEntity = SimulationController.Instance.GetSelectedEntity();
            if (selectedEntity != null)
            {
                SimulationController.Instance.DeselectSelectedEntity();
            }
            
            _entityListAddToWatchlistButton.gameObject.SetActive(_entityListMultipleSelectionActive);

            _entityListMultipleSelectionActive = !_entityListMultipleSelectionActive;
        }

        public void OnEwpAddToWathclistButtonClicked()
        {
            foreach (var selectedEntityListItem in _selectedEntityListItems)
            {
                if (!SimulationController.Instance.Watchlist.Contains(selectedEntityListItem))
                {
                    SimulationController.Instance.Watchlist.Add(selectedEntityListItem);
                }
            }

            _selectedEntityListItems.Clear();

            OnEntitiesMultipleSelectionButtonClicked();
        }

        public void OnEspAddToWatchlistButtonClicked(GameObject selectedEntity)
        {
            // Select entity added to the Watchlist
            SelectListItem(selectedEntity.GetComponent<EntityController>(), EntityListType.Watchlist, true);
        }

        public void OnWatchlistMultipleSelectionButtonClicked()
        {
            throw new NotImplementedException();
        }

        public void OnDeleteFromWatchlistButtonClicked()
        {
            throw new NotImplementedException();
        }

        public void OnEntityDeselected()
        {
            UnselectCurrentlySelectedListItem(EntityListType.EntityList);
            UnselectCurrentlySelectedListItem(EntityListType.Watchlist);
        }

        public void OnEntitySelected(EntityController entity, bool isSelectedFromUi)
        {
            // Select the entity in the entities list
            SelectListItem(entity, EntityListType.EntityList, !isSelectedFromUi);

            // Select the entity in the watchlist list
            SelectListItem(entity, EntityListType.Watchlist, !isSelectedFromUi);
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

        private void OnEntityListToggleClicked(Toggle toggle, Transform listItem)
        {
            if (!toggle.isOn)
            {
                _selectedEntityListItems.Add(listItem.gameObject);
                toggle.isOn = true;
                if (!_entityListAddToWatchlistButton.gameObject.activeSelf)
                {
                    _entityListAddToWatchlistButton.gameObject.SetActive(true);
                }
            }
            else
            {
                _selectedEntityListItems.Remove(listItem.gameObject);
                toggle.isOn = false;
                if (_selectedEntityListItems.Count == 0)
                {
                    _entityListAddToWatchlistButton.gameObject.SetActive(false);
                }
            }
        }

        private void OnListItemClicked(GameObject entity, Transform listItem, EntityListType listType)
        {
            // Handle the click event
            var multipleSelectionState = listType == EntityListType.EntityList
                ? _entityListMultipleSelectionActive
                : _watchlistListMultipleSelectionActive;

            if (multipleSelectionState)
            {
                var toggle = listItem.Find("ListItem_Button").transform.Find("ListItem_Toggle").GetComponent<Toggle>();
                OnEntityListToggleClicked(toggle, listItem);
            }
            else
            {
                Debug.Log("Clicked on entity: " + entity.gameObject.name);
                entity.GetComponent<EntityController>().SelectEntity(true);
            }
        }

        private void UpdateEntitiesList(NotifyCollectionChangedEventArgs e, EntityListType listType)
        {
            var scrollViewContent = listType == EntityListType.EntityList
                ? _entitiesScrollViewContent
                : _watchlistScrollViewContent;

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

        private void AddEntityToList(GameObject entity, Transform scrollViewContent, EntityListType listType)
        {
            var listItem = Instantiate(_entityListItemPrefab, scrollViewContent);
            listItem.name = entity.gameObject.name;
            var button = listItem.transform.Find("ListItem_Button");

            button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = entity.gameObject.name;

            AddListItemClickListener(button, listItem.transform, entity, listType);
        }

        private void AddListItemClickListener(Transform button, Transform listItem, GameObject entity,
            EntityListType listType)
        {
            var listItemButton = button.GetComponent<Button>();
            listItemButton.onClick.AddListener(() => OnListItemClicked(entity, listItem, listType));
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

        private void SelectListItem(EntityController entity, EntityListType listType, bool shouldScrollToItem)
        {
            var scrollViewContent = listType == EntityListType.EntityList
                ? _entitiesScrollViewContent
                : _watchlistScrollViewContent;
            var scrollView = listType == EntityListType.EntityList ? _entityListScrollView : _watchlistScrollView;
            UnselectCurrentlySelectedListItem(listType);

            // Find the item in the list that matches the entity game object
            foreach (Transform listItem in scrollViewContent)
            {
                if (listItem.name == entity.name)
                {
                    // Set the color of the item to the selected color
                    ChangeListItemUi(listItem, true);

                    // Scroll to the selected item
                    if (shouldScrollToItem) ScrollToSelectedItem(scrollView, listItem);

                    if (listType == EntityListType.EntityList)
                    {
                        _currentlySelectedEntityListItem = listItem;
                    }
                    else
                    {
                        _currentlySelectedWatchlistListItem = listItem;
                    }

                    break;
                }
            }

            // If selected item doesn't exist in watchlist, unselect previously selected watchlist item
            UnselectSelectedWatchlistItem(entity, listType);
        }

        private void UnselectCurrentlySelectedListItem(EntityListType listType)
        {
            var currentlySelectedItem = listType == EntityListType.EntityList
                ? _currentlySelectedEntityListItem
                : _currentlySelectedWatchlistListItem;

            if (currentlySelectedItem != null)
            {
                ChangeListItemUi(currentlySelectedItem, false);
            }
        }

        private void UnselectSelectedWatchlistItem(EntityController entity, EntityListType listType)
        {
            var watchlist = SimulationController.Instance.Watchlist;
            if (listType == EntityListType.EntityList && _currentlySelectedWatchlistListItem != null)
            {
                var watchListContainsSelectedEntity = watchlist.Contains(entity.gameObject);
                if (!watchListContainsSelectedEntity)
                {
                    ChangeListItemUi(_currentlySelectedWatchlistListItem, false);
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

        private void ChangeListItemUi(Transform listItem, bool isSelected)
        {
            // Sometimes list item is deleted by the time we arrive here. We want to prevent NullReferenceException
            if (listItem == null) return;

            var backgroundColor = isSelected ? _lightListItemColor : _darkListItemColor;
            var textColor = isSelected ? _darkListItemTextColor : _lightListItemTextColor;

            var button = listItem.transform.Find("ListItem_Button");
            var buttonImage = button.GetComponent<Image>();
            var text = button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>();
            buttonImage.color = backgroundColor;
            text.color = textColor;
            text.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    internal enum EntityListType
    {
        EntityList,
        Watchlist
    }
}