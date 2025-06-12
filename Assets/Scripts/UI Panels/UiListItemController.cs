using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Panels
{
    public class UiListItemController<T> : MonoBehaviour
    {
        private readonly Color32 _lightListItemColor = new(255, 255, 255, 171);
        private readonly Color32 _darkListItemColor = new(0, 0, 0, 171);
        private readonly Color32 _lightListItemTextColor = new(255, 255, 255, 255);
        private readonly Color32 _darkListItemTextColor = new(0, 0, 0, 255);

        private bool _isSelected;

        public bool IsSelected
        {
            set
            {
                _isSelected = value;
                
                var backgroundColor = _isSelected ? _lightListItemColor : _darkListItemColor;
                var textColor = _isSelected ? _darkListItemTextColor : _lightListItemTextColor;

                var button = gameObject.transform.Find("ListItem_Button");
                var buttonImage = button.GetComponent<Image>();
                var text = button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>();
                buttonImage.color = backgroundColor;
                text.color = textColor;
                text.fontStyle = _isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private T _itemObject;

        public T ItemObject
        {
            get => _itemObject;
            set => _itemObject = value;
        }

        private bool _isMultipleSelectActive;

        private UiListType _listType;
        private string _itemName;

        public void Init(UiListType listType, string itemName)
        {
            _listType = listType;
            _itemName = itemName;
        }

        void Start()
        {
            var button = gameObject.transform.Find("ListItem_Button");
            button.Find("ListItem_Text").GetComponent<TextMeshProUGUI>().text = _itemName;

            AddListItemClickListener(button, _listType);
        }

        public void ToggleMultipleSelection()
        {
            var button = gameObject.transform.Find("ListItem_Button");
            var toggleTransform = button.transform.Find("ListItem_Toggle");
            var toggleCanvasGroup = toggleTransform.GetComponent<CanvasGroup>();

            // Change the toggle visibility
            if (_isMultipleSelectActive)
            {
                toggleTransform.GetComponent<Toggle>().isOn = false;
            }

            toggleCanvasGroup.alpha = _isMultipleSelectActive ? 0 : 1;
            _isMultipleSelectActive = !_isMultipleSelectActive;
        }

        private void AddListItemClickListener(Transform button, UiListType listType)
        {
            var listItemButton = button.GetComponent<Button>();
            listItemButton.onClick.AddListener(() => OnListItemClicked(listType));
        }

        private void OnListItemClicked(UiListType listType)
        {
            // Handle the click event
            if (_isMultipleSelectActive)
            {
                var toggle = gameObject.transform.Find("ListItem_Button").transform.Find("ListItem_Toggle")
                    .GetComponent<Toggle>();
                OnEntityListToggleClicked(toggle, listType);
            }
            else
            {
                if (_itemObject == null) return;
                switch (listType)
                {
                    case UiListType.EntityList:
                    case UiListType.Watchlist:
                        if (_itemObject is GameObject entity == false) return;
                        HandleEntityListItemClick(entity);
                        break;
                    case UiListType.GeneList: 
                    case UiListType.NeuronList:
                        HandleGeneticListItemClick(_itemObject, listType);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
                }
            }

            UiController.Instance.SetActiveList(listType);
        }
        
        private void HandleEntityListItemClick(GameObject entity)
        {
            var entityController = entity.GetComponent<EntityController>();
            if (entityController == null) return;

            Debug.Log("Clicked on entity from a list: " + entity.gameObject.name);

            if (SimulationController.Instance.GetSelectedEntity() == entity) return;

            entity.GetComponent<EntityController>().SelectEntity(true);
        }
        
        private void HandleGeneticListItemClick<T>(T itemObject, UiListType listType)
        {
            
            Debug.Log("Clicked on item from a gentic list: " + itemObject);
        }

        private void OnEntityListToggleClicked(Toggle toggle, UiListType listType)
        {
            if (!toggle.isOn)
            {
                if (_itemObject != null && _itemObject is GameObject entity)
                {
                    EntitiesWatchlistPanelController.Instance.AddToSelectedEntityList(entity, listType);
                }

                toggle.isOn = true;
            }
            else
            {
                EntitiesWatchlistPanelController.Instance.RemoveFromSelectedEntityList(listType);
                toggle.isOn = false;
            }
        }
    }
}