namespace UI_Panels
{
    public interface IEntitiesWatchlistPanelController
    {
        void OnListMultipleSelectionButtonClicked(UiListType listType);
        void OnEwpAddToWathclistButtonClicked();
        void OnDeleteFromWatchlistButtonClicked();
    }
}