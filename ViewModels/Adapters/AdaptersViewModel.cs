using AIStudio.Common.Adapters;
using AIStudio.ViewModels;
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
  /// UI раздела «Адаптеры среды» (фаза 1 MVP).
  /// </summary>
  public sealed class AdaptersViewModel
  {
    private string _reportText = "Выберите адаптер и нажмите «Проверить», либо установите новый пакет.";

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
      OpenGuideCommand = new RelayCommand(_ => OpenAuthorGuide());
      OpenAdaptersFolderCommand = new RelayCommand(_ => OpenAdaptersFolder());
      Reload();
    }

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
    public ICommand OpenGuideCommand { get; }
    public ICommand OpenAdaptersFolderCommand { get; }

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
        ReportText = "Нет установленных адаптеров. Каталог: " + AdapterPaths.AdaptersRootPath;
    }

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
        MessageBox.Show(loadError, "Установить адаптер", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      string targetDir = AdapterPaths.GetAdapterDirectory(manifest.Id);
      bool replace = false;
      if (Directory.Exists(targetDir))
      {
        MessageBoxResult confirm = MessageBox.Show(
            "Адаптер «" + manifest.Id + "» уже установлен в:\n" + targetDir +
            "\n\nЗаменить каталог полностью?",
            "Установить адаптер",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
          return;
        replace = true;
      }

      if (!AdapterInstaller.TryInstallFromDirectory(sourceDirectory, replace, out string installed, out string error))
      {
        MessageBox.Show(error ?? "Установка не выполнена.", "Установить адаптер", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      Reload();
      ReportText = "Установлено: " + installed;
      MessageBox.Show(
          "Адаптер «" + manifest.DisplayName + "» (" + manifest.Id + ") установлен.\n" + installed,
          "Установить адаптер",
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
              "Установить адаптер",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);
          if (confirm == MessageBoxResult.Yes &&
              AdapterInstaller.TryInstallFromZip(zipPath, replaceExisting: true, out installed, out error))
          {
            Reload();
            ReportText = "Установлено из ZIP: " + installed;
            MessageBox.Show("Пакет установлен:\n" + installed, "Установить адаптер", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
          }
        }

        MessageBox.Show(error ?? "Установка не выполнена.", "Установить адаптер", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      Reload();
      ReportText = "Установлено из ZIP: " + installed;
      MessageBox.Show("Пакет установлен:\n" + installed, "Установить адаптер", MessageBoxButton.OK, MessageBoxImage.Information);
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
      sb.AppendLine("Адаптер: " + adapter.DisplayName + " (" + adapter.Id + ")");
      sb.AppendLine("Каталог: " + adapter.PackageRootPath);
      sb.AppendLine();

      foreach (AdapterValidationMessage msg in messages)
        sb.AppendLine("[" + msg.Severity + "] " + msg.Text);

      return sb.ToString();
    }

    private static void OpenAuthorGuide()
    {
      string path = AdapterAuthorGuideLocator.TryFindGuidePath();
      if (string.IsNullOrEmpty(path))
      {
        MessageBox.Show(
            "Файл AdapterAuthorGuide.md не найден рядом с AIStudio.\nОжидается docs\\AdapterAuthorGuide.md в каталоге приложения или репозитория.",
            "Руководство автора",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      try
      {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось открыть файл: " + ex.Message, "Руководство автора", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
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
        MessageBox.Show(ex.Message, "Каталог адаптеров", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }
  }
}
