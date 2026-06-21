using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  public partial class EnvironmentRecipeEditorView : UserControl
  {
    private bool _isInitializing = true;

    public EnvironmentRecipeEditorView()
    {
      InitializeComponent();
      DataContextChanged += OnDataContextChanged;
      Loaded += OnLoaded;
      ShowTab(RecipeEditorTab.General);
    }

    private EnvironmentRecipeEditorViewModel Vm => DataContext as EnvironmentRecipeEditorViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e) => _isInitializing = false;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue is EnvironmentRecipeEditorViewModel oldVm)
        oldVm.PropertyChanged -= Vm_PropertyChanged;
      if (e.NewValue is EnvironmentRecipeEditorViewModel newVm)
      {
        newVm.PropertyChanged += Vm_PropertyChanged;
        ShowTab(newVm.SelectedTab);
      }
    }

    private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(EnvironmentRecipeEditorViewModel.SelectedTab) && Vm != null)
        ShowTab(Vm.SelectedTab);
    }

    private void PickRecipeId_Click(object sender, RoutedEventArgs e) =>
        Vm?.PickRecipeId(Window.GetWindow(this));

    private void PickAdaptiveAction_Click(object sender, RoutedEventArgs e) =>
        Vm?.PickAdaptiveAction(Window.GetWindow(this));

    private void TabGeneral_Click(object sender, RoutedEventArgs e) => ShowTab(RecipeEditorTab.General);

    private void TabSteps_Click(object sender, RoutedEventArgs e) => ShowTab(RecipeEditorTab.Steps);

    private void ShowTab(RecipeEditorTab tab)
    {
      if (Vm != null)
        Vm.SelectedTab = tab;
      GeneralPanel.Visibility = tab == RecipeEditorTab.General ? Visibility.Visible : Visibility.Collapsed;
      StepsPanel.Visibility = tab == RecipeEditorTab.Steps ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StepsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
      e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    private void ModelField_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!_isInitializing)
        Vm?.OnModelFieldChanged();
    }

    private void ModelField_Changed(object sender, RoutedEventArgs e)
    {
      if (!_isInitializing)
        Vm?.OnModelFieldChanged();
    }
  }
}
