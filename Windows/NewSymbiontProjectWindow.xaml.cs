using AIStudio.Common.Adapters;
using System.Collections.Generic;
using System.Windows;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Windows
{
  /// <summary>
  /// Опциональный выбор типа среды перед указанием каталога нового проекта симбионта.
  /// </summary>
  public partial class NewSymbiontProjectWindow : Window
  {
    /// <summary>
    /// Создаёт окно выбора типа среды.
    /// </summary>
    public NewSymbiontProjectWindow(IReadOnlyList<AdapterManifest> adapters)
    {
      InitializeComponent();
      var items = new List<EnvironmentPackageSelectionItem> { EnvironmentPackageSelectionItem.None };
      if (adapters != null)
      {
        for (int i = 0; i < adapters.Count; i++)
          items.Add(EnvironmentPackageSelectionItem.FromManifest(adapters[i]));
      }
      AdapterCombo.ItemsSource = items;
      AdapterCombo.DisplayMemberPath = nameof(EnvironmentPackageSelectionItem.DisplayName);
      AdapterCombo.SelectedItem = EnvironmentPackageSelectionItem.None;
      IncludeEnvironmentCheckBox.IsChecked = false;
      UpdateEnvironmentUiState();
    }

    /// <summary>Выбранный пакет после подтверждения; null — проект без среды.</summary>
    public AdapterManifest SelectedAdapter { get; private set; }
    /// <summary>Идентификатор типа среды или null.</summary>
    public string SelectedAdapterId => SelectedAdapter?.Id;
    private void OnIncludeEnvironmentChanged(object sender, RoutedEventArgs e)
    {
      UpdateEnvironmentUiState();
    }

    private void UpdateEnvironmentUiState()
    {
      bool include = IncludeEnvironmentCheckBox.IsChecked == true;
      AdapterCombo.IsEnabled = include;
      if (!include)
        AdapterCombo.SelectedItem = EnvironmentPackageSelectionItem.None;
      HintText.Text = include
          ? "BootData проекта будет дополнен образцами из %ProgramData%\\ISIDA\\Adapters\\{id}\\. Runtime DLL в студию не загружается."
          : "Без типа среды доступны гомеостаз, пульт оператора и виртуальные тесты. Меню «Среда» — после регистрации пакета и выбора типа среды в свойствах симбионта.";
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
      if (IncludeEnvironmentCheckBox.IsChecked != true)
      {
        SelectedAdapter = null;
        DialogResult = true;
        return;
      }
      if (!(AdapterCombo.SelectedItem is EnvironmentPackageSelectionItem selected) || selected.Manifest == null)
      {
        MessageBox.Show(
            "Выберите тип среды из списка или снимите флажок «Указать тип среды…».\n\n" +
            "Если список пуст — зарегистрируйте пакет: Проект → Зарегистрированные пакеты…",
            Title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }
      SelectedAdapter = selected.Manifest;
      DialogResult = true;
    }
  }
}
