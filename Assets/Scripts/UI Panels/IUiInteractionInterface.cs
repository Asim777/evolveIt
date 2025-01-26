namespace UI_Panels
{
    public interface IUiInteractionInterface
    {
        void OnResumePauseButtonClicked();
        void OnStopButtonClicked();
        void OnSpeedDownButtonClicked();
        void OnSpeedUpButtonClicked();
        void OnEspAddToWatchlistButtonClicked();
        void OnEntitiesWatchlistPanelClicked();
        void OnGeneticInformationPanelClicked();
        void OnEntitiesMultipleSelectionButtonClicked();
        void OnEwpAddToWathclistButtonClicked();
        void OnWatchlistMultipleSelectionButtonClicked();
        void OnDeleteFromWatchlistButtonClicked();
    }
}