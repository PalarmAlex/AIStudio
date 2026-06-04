using AIStudio.Common.Adapters;
using System.Collections.Generic;
using System.Windows;

using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Windows
{
  /// <summary>
  /// Выбор адаптера перед указанием каталога нового проекта.
  /// </summary>
  public partial class NewSymbiontProjectWindow : Window
  {
    /// <summary>
    /// Создаёт окно выбора адаптера.
    /// </summary>
    public NewSymbiontProjectWindow(IReadOnlyList<AdapterManifest> adapters)
    {
      InitializeComponent();
      AdapterCombo.ItemsSource = adapters;
      if (adapters != null && adapters.Count > 0)
        AdapterCombo.SelectedIndex = 0;
    }

    /// <summary>Выбранный адаптер после подтверждения.</summary>
    public AdapterManifest SelectedAdapter { get; private set; }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
      SelectedAdapter = AdapterCombo.SelectedItem as AdapterManifest;
      if (SelectedAdapter == null)
      {
        MessageBox.Show(
            "Выберите адаптер из списка. Если список пуст — установите пакет в разделе «Адаптеры среды».",
            Title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      DialogResult = true;
    }
  }
}
