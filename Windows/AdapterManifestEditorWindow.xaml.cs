using AIStudio.Common.Adapters;
using System.Windows;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Windows
{
  /// <summary>
  /// Редактор полей <c>manifest.json</c> при создании пакета из demo или изменении зарегистрированного адаптера.
  /// </summary>
  public partial class AdapterManifestEditorWindow : Window
  {
    private readonly bool _isCreate;
    private readonly string _originalId;
    private readonly AdapterManifest _initial;

    /// <summary>
    /// Создаёт окно редактора manifest.
    /// </summary>
    /// <param name="isCreate">Создание нового пакета из demo.</param>
    /// <param name="initial">Начальные значения полей.</param>
    /// <param name="originalId">Исходный id при редактировании (для проверки уникальности).</param>
    public AdapterManifestEditorWindow(bool isCreate, AdapterManifest initial, string originalId = null)
    {
      InitializeComponent();
      _isCreate = isCreate;
      _originalId = originalId;
      _initial = initial;

      Title = isCreate ? "Создать пакет из demo" : "Свойства пакета адаптера";
      HintTextBlock.Text = isCreate
          ? "Будет создан каталог в " + AdapterPaths.AdaptersRootPath
            + " на основе каркаса demo. Заполните поля и нажмите «Применить»."
          : "Изменения сохраняются в manifest.json зарегистрированного пакета.";

      AdapterManifest source = initial ?? new AdapterManifest();
      IdTextBox.Text = isCreate ? string.Empty : (source.Id ?? string.Empty);
      DisplayNameTextBox.Text = source.DisplayName ?? string.Empty;
      VersionTextBox.Text = string.IsNullOrWhiteSpace(source.Version) ? "0.1.0" : source.Version;
      ContractVersionTextBox.Text = string.IsNullOrWhiteSpace(source.ContractVersion)
          ? AdapterManifest.SupportedContractVersion
          : source.ContractVersion;
      AuthorTextBox.Text = source.Author ?? string.Empty;
      DescriptionTextBox.Text = source.Description ?? string.Empty;
    }

    /// <summary>Результат после подтверждения.</summary>
    public AdapterManifest EditedManifest { get; private set; }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
      var manifest = new AdapterManifest
      {
        Id = IdTextBox.Text,
        DisplayName = DisplayNameTextBox.Text,
        Version = VersionTextBox.Text,
        ContractVersion = ContractVersionTextBox.Text,
        Author = AuthorTextBox.Text,
        Description = DescriptionTextBox.Text,
        BootDataRelativePath = _isCreate ? "BootData" : (_initial?.BootDataRelativePath ?? "BootData"),
        SchemaVersion = _isCreate ? "1.0" : _initial?.SchemaVersion,
        InstallerTemplateRelativePath = _isCreate ? null : _initial?.InstallerTemplateRelativePath,
        AdapterSettingsRelativePath = _isCreate ? null : _initial?.AdapterSettingsRelativePath,
        PackageRootPath = _isCreate ? null : _initial?.PackageRootPath
      };

      if (!AdapterPackageManager.TryValidateManifest(
              manifest,
              _isCreate,
              _originalId,
              out string error))
      {
        ShowValidationError(error);
        return;
      }

      EditedManifest = manifest;
      DialogResult = true;
    }

    private void ShowValidationError(string message)
    {
      ValidationTextBlock.Text = message;
      ValidationTextBlock.Visibility = Visibility.Visible;
    }
  }
}
