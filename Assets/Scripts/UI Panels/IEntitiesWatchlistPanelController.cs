namespace UI_Panels
{
    public interface IEntitiesWatchlistPanelController
    {
        void OnListMultipleSelectionButtonClicked(EntityListType listType);
        void OnEwpAddToWathclistButtonClicked();
        void OnDeleteFromWatchlistButtonClicked();
    }
}