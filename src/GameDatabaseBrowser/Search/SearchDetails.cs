using System;

namespace GameDatabaseBrowser.Search
{
    public class SearchDetails
    {
        public readonly SearchType SearchType;
        public readonly string TextValue;
        public readonly bool NeedExpand;
        public readonly bool ExactMatch;
        public readonly bool NeedReferences;
        public readonly bool IncludeAbstractClass;
        public readonly bool IncludeNotApprovedClass;

        public SearchDetails(MainWindow mainWindow)
        {
            SearchType = (SearchType)Enum.ToObject(typeof(SearchType), mainWindow.SearchTypeComboBox.SelectedIndex);
            NeedExpand = mainWindow.expandResultToggle.IsChecked == true;
            ExactMatch = mainWindow.exactMatchToggle.IsChecked == true;
            NeedReferences = mainWindow.referencesToggle.IsChecked == true;
            IncludeAbstractClass = mainWindow.abstractClassToggle.IsChecked == true;
            IncludeNotApprovedClass = mainWindow.notApprovedClassToggle.IsChecked == true;

            switch (SearchType)
            {
                case SearchType.ByText:
                    TextValue = mainWindow.txtSearch.Text;
                    break;
                case SearchType.ByPrototypeClass:
                    TextValue = mainWindow.classAutoCompletionText.Text;
                    break;
                case SearchType.ByPrototypeBlueprint:
                    TextValue = mainWindow.blueprintAutoCompletionText.Text;
                    break;
                case SearchType.SelectedPrototype:
                    TextValue = mainWindow.selectedPrototypeSearchText.Text;
                    break;
            }
        }
    }
}
