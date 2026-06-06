using AIStudio.Common.Adapters;
using AIStudio.ViewModels;
using AIStudio.Windows;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.ViewModels.Adapters
{
  /// <summary>
  /// UI раздела «Зарегистрированные пакеты среды» (фаза 1 MVP).
  /// </summary>
  public sealed class AdaptersViewModel
  {
    private const string RegisterPackageTitle = "Зарегистрировать пакет";
    private string _reportText = "Выберите пакет и нажмите «Проверить».";
    /// <summary>
    /// Создаёт модель страницы адаптеров.
    /// </summary>
    public AdaptersViewModel()
    {
      Items = new ObservableCollection<AdapterListItem>();
      RefreshCommand = new RelayCommand(_ => Reload());
      InstallFromFolderCommand = new RelayCommand(_ => InstallFromFolder());
      InstallFromZipCommand = new RelayCommand(_ => InstallFromZip());
      VerifyCommand = new RelayCommand(_ => VerifySelected(), _ => Selected != null);
      CreateFromDemoCommand = new RelayCommand(_ => CreateFromDemo(GetOwnerWindow()));
      OpenAdaptersFolderCommand = new RelayCommand(_ => OpenAdaptersFolder());
      Reload();
    }

    /// <summary>Подсказка с полным путём к каталогу зарегистрированных адаптеров.</summary>
    public string AdaptersCatalogHint =>
        "Каталог: " + AdapterPaths.AdaptersRootPath;
    /// <summary>Установленные адаптеры.</summary>
    public ObservableCollection<AdapterListItem> Items { get; }
    /// <summary>Выбранный адаптер.</summary>
    public AdapterListItem Selected { get; set; }
    /// <summary>Отчёт «Проверить».</summary>
    public string ReportText
    {
      get => _reportText;
      private set => _reportText = value ?? string.Empty;
    }

    public ICommand RefreshCommand { get; }
    public ICommand InstallFromFolderCommand { get; }
    public ICommand InstallFromZipCommand { get; }
    public ICommand VerifyCommand { get; }
    public ICommand CreateFromDemoCommand { get; }
    public ICommand OpenAdaptersFolderCommand { get; }
    /// <summary>Открывает мастер создания пакета (demo + runtime).</summary>
    public void CreateFromDemo(Window owner)
    {
      var dialog = new AdapterManifestEditorWindow(isCreate: true, initial: CreateDefaultManifest())
      {
        Owner = owner ?? GetOwnerWindow()
      };
      if (dialog.ShowDialog() != true || dialog.EditedManifest == null)
        return;
      var runtimeDialog = new VistaFolderBrowserDialog
      {
        Description = "Опционально: каталог bin\\Debug host с DLL адаптера. Отмена — только стартовый SDK.",
        UseDescriptionForTitle = true
      };
      string hostBin = null;
      if (runtimeDialog.ShowDialog(owner ?? GetOwnerWindow()) == true)
        hostBin = runtimeDialog.SelectedPath;
      if (!AdapterPackageBuilder.TryCreateAndRegisterFromDemo(
              dialog.EditedManifest,
              hostBin,
              out string installedPath,
              out string error))
      {
        MessageBox.Show(error, "Создать пакет", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      Reload();
      SelectById(dialog.EditedManifest.Id);
      ReportText = "Зарегистрирован пакет:\n" + installedPath;
      MessageBox.Show(
          "Пакет «" + dialog.EditedManifest.DisplayName + "» зарегистрирован:\n" + installedPath,
          "Создать пакет",
          MessageBoxButton.OK,
          MessageBoxImage.Information);
    }

    /// <summary>Открывает редактор manifest выбранного пакета.</summary>
    public void EditSelected(Window owner)
    {
      if (Selected == null)
        return;
      if (!AdapterManifest.TryLoad(Selected.PackageRootPath, out AdapterManifest manifest, out string loadError))
      {
        MessageBox.Show(loadError, "Свойства пакета", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      var dialog = new AdapterManifestEditorWindow(isCreate: false, manifest, Selected.Id)
      {
        Owner = owner ?? GetOwnerWindow()
      };
      if (dialog.ShowDialog() != true || dialog.EditedManifest == null)
        return;
      if (!AdapterPackageManager.TryUpdatePackage(
              dialog.EditedManifest,
              Selected.PackageRootPath,
              Selected.Id,
              out string updatedPath,
              out string error))
      {
        MessageBox.Show(error, "Свойства пакета", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      string savedId = dialog.EditedManifest.Id;
      Reload();
      SelectById(savedId);
      ReportText = "Сохранён manifest.json:\n" + updatedPath;
    }

    /// <summary>Удаляет выбранный пакет после подтверждения.</summary>
    public void DeleteSelected(Window owner)
    {
      if (Selected == null || string.IsNullOrWhiteSpace(Selected.Id))
        return;
      MessageBoxResult confirm = MessageBox.Show(
          owner ?? GetOwnerWindow(),
          "Удалить пакет «" + Selected.DisplayName + "» (" + Selected.Id + ")?\n\n"
          + "Будет удалён каталог:\n" + Selected.PackageRootPath,
          "Удалить пакет",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning,
          MessageBoxResult.No);
      if (confirm != MessageBoxResult.Yes)
        return;
      string adapterId = Selected.Id;
      if (!AdapterPackageManager.TryDeletePackage(adapterId, out string error))
      {
        MessageBox.Show(owner ?? GetOwnerWindow(), error, "Удалить пакет", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      Reload();
      ReportText = "Пакет «" + adapterId + "» удалён.";
    }

    private void Reload()
    {
      Items.Clear();
      foreach (AdapterManifest manifest in AdapterRegistry.GetInstalledAdapters())
      {
        Items.Add(new AdapterListItem
        {
          Id = manifest.Id,
          DisplayName = manifest.DisplayName,
          Version = manifest.Version,
          ContractVersion = manifest.ContractVersion,
          PackageRootPath = manifest.PackageRootPath
        });
      }
      if (Items.Count == 0)
        ReportText = "Нет зарегистрированных пакетов. Каталог: " + AdapterPaths.AdaptersRootPath;
    }

    private void SelectById(string id)
    {
      if (string.IsNullOrWhiteSpace(id))
        return;
      Selected = Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static AdapterManifest CreateDefaultManifest()
    {
      return new AdapterManifest
      {
        Version = "0.1.0",
        ContractVersion = AdapterManifest.SupportedContractVersion,
        BootDataRelativePath = "BootData",
        SchemaVersion = "1.0",
        Description = "Новый пакет адаптера среды."
      };
    }

    private static Window GetOwnerWindow() => Application.Current?.MainWindow;
    private void InstallFromFolder()
    {
      var dialog = new VistaFolderBrowserDialog
      {
        Description = "Выберите корень пакета адаптера (с manifest.json)",
        UseDescriptionForTitle = true
      };
      if (dialog.ShowDialog() != true)
        return;
      TryInstallPackage(dialog.SelectedPath);
    }

    private void InstallFromZip()
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите ZIP пакета адаптера",
        Filter = "ZIP архив (*.zip)|*.zip|Все файлы|*.*"
      };
      if (dialog.ShowDialog() != true)
        return;
      TryInstallPackageFromZip(dialog.FileName);
    }

    private void TryInstallPackage(string sourceDirectory)
    {
      if (!AdapterManifest.TryLoad(sourceDirectory, out AdapterManifest manifest, out string loadError))
      {
        MessageBox.Show(loadError, RegisterPackageTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      string targetDir = AdapterPaths.GetAdapterDirectory(manifest.Id);
      bool replace = false;
      if (Directory.Exists(targetDir))
      {
        MessageBoxResult confirm = MessageBox.Show(
            "Пакет «" + manifest.Id + "» уже зарегистрирован в:\n" + targetDir +
            "\n\nЗаменить каталог полностью?",
            RegisterPackageTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
          return;
        replace = true;
      }
      if (!AdapterInstaller.TryInstallFromDirectory(sourceDirectory, replace, out string installed, out string error))
      {
        MessageBox.Show(error ?? "Регистрация не выполнена.", RegisterPackageTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      Reload();
      SelectById(manifest.Id);
      ReportText = "Зарегистрировано: " + installed;
      MessageBox.Show(
          "Пакет «" + manifest.DisplayName + "» (" + manifest.Id + ") зарегистрирован.\n" + installed,
          RegisterPackageTitle,
          MessageBoxButton.OK,
          MessageBoxImage.Information);
    }

    private void TryInstallPackageFromZip(string zipPath)
    {
      if (!AdapterInstaller.TryInstallFromZip(zipPath, replaceExisting: false, out string installed, out string error))
      {
        if (!string.IsNullOrEmpty(error) &&
            error.IndexOf("Подтвердите замену", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          MessageBoxResult confirm = MessageBox.Show(
              error + "\n\nЗаменить существующую копию?",
              RegisterPackageTitle,
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);
          if (confirm == MessageBoxResult.Yes &&
              AdapterInstaller.TryInstallFromZip(zipPath, replaceExisting: true, out installed, out error))
          {
            Reload();
            ReportText = "Зарегистрировано из ZIP: " + installed;
            MessageBox.Show("Пакет зарегистрирован:\n" + installed, RegisterPackageTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
          }
        }
        MessageBox.Show(error ?? "Регистрация не выполнена.", RegisterPackageTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      Reload();
      ReportText = "Зарегистрировано из ZIP: " + installed;
      MessageBox.Show("Пакет зарегистрирован:\n" + installed, RegisterPackageTitle, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void VerifySelected()
    {
      if (Selected == null || string.IsNullOrWhiteSpace(Selected.PackageRootPath))
        return;
      IReadOnlyList<AdapterValidationMessage> messages = AdapterValidator.Validate(Selected.PackageRootPath);
      ReportText = FormatReport(Selected, messages);
      MessageBoxImage icon = AdapterValidator.HasErrors(messages)
          ? MessageBoxImage.Error
          : messages.Any(m => m.Severity == AdapterValidationSeverity.Warning)
              ? MessageBoxImage.Warning
              : MessageBoxImage.Information;
      MessageBox.Show(ReportText, "Проверить — " + Selected.Id, MessageBoxButton.OK, icon);
    }

    private static string FormatReport(AdapterListItem adapter, IReadOnlyList<AdapterValidationMessage> messages)
    {
      var sb = new StringBuilder();
      sb.AppendLine("Пакет: " + adapter.DisplayName + " (" + adapter.Id + ")");
      sb.AppendLine("Каталог: " + adapter.PackageRootPath);
      sb.AppendLine();
      foreach (AdapterValidationMessage msg in messages)
        sb.AppendLine("[" + msg.Severity + "] " + msg.Text);
      return sb.ToString();
    }

    private static void OpenAdaptersFolder()
    {
      AdapterPaths.EnsureAdaptersRoot();
      try
      {
        Process.Start(new ProcessStartInfo(AdapterPaths.AdaptersRootPath) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Каталог пакетов", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }
  }
}
