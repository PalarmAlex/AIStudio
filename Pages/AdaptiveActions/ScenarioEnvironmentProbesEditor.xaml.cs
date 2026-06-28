using AIStudio.Common;
using AIStudio.Common.SymbiontEnv;
using ISIDA.Actions;
using ISIDA.Scenarios;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class ScenarioEnvironmentProbesEditor : Window
  {
    public List<ScenarioEnvironmentProbeEntry> SelectedProbes { get; private set; }
    private readonly List<ScenarioEnvironmentProbeEntry> _initialProbes;
    private List<EnvironmentProbeActionItem> _items;

    public ScenarioEnvironmentProbesEditor(
        string title,
        IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> environmentActions,
        IList<ScenarioEnvironmentProbeEntry> currentProbes)
    {
      InitializeComponent();
      Title = title;
      if (HeaderTitle != null)
        HeaderTitle.Text = title;

      var cur = currentProbes ?? new List<ScenarioEnvironmentProbeEntry>();
      _initialProbes = cur.Select(e => e.Clone()).ToList();
      SelectedProbes = cur.Select(e => e.Clone()).ToList();

      var curById = cur.ToDictionary(e => e.ActionId, e => e.IsPressure);
      _items = environmentActions
          .OrderBy(a => a.Id)
          .Select(a =>
          {
            bool isPressure = false;
            bool isRelease = false;
            if (curById.TryGetValue(a.Id, out bool pressure))
            {
              if (pressure)
                isPressure = true;
              else
                isRelease = true;
            }
            return new EnvironmentProbeActionItem
            {
              Id = a.Id,
              Name = $"{a.Id}: {a.Name ?? ""}",
              Description = a.Description,
              IsPressure = isPressure,
              IsRelease = isRelease
            };
          })
          .ToList();

      ProbesList.ItemsSource = _items;
    }

    private void ProbeReset_Click(object sender, RoutedEventArgs e) =>
        EnvironmentProbeSelectionUi.ResetRowClick(sender, e);

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      SelectedProbes = _items
          .Where(x => x.IsPressure || x.IsRelease)
          .Select(x => new ScenarioEnvironmentProbeEntry
          {
            ActionId = x.Id,
            IsPressure = x.IsPressure
          })
          .ToList();
      DialogResult = true;
      Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close();
        e.Handled = true;
      }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      if (DialogResult == true)
        return;
      if (!HasSelectionChanged())
        return;
      var r = MessageBox.Show(
          "Закрыть без сохранения выбранных воздействий среды?",
          Title,
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);
      if (r != MessageBoxResult.Yes)
        e.Cancel = true;
    }

    private bool HasSelectionChanged()
    {
      var cur = BuildCurrentSelection()
          .OrderBy(x => x.ActionId)
          .ThenBy(x => x.IsPressure)
          .ToList();
      var init = _initialProbes
          .OrderBy(x => x.ActionId)
          .ThenBy(x => x.IsPressure)
          .ToList();
      if (cur.Count != init.Count)
        return true;
      for (int i = 0; i < cur.Count; i++)
      {
        if (cur[i].ActionId != init[i].ActionId || cur[i].IsPressure != init[i].IsPressure)
          return true;
      }
      return false;
    }

    private List<ScenarioEnvironmentProbeEntry> BuildCurrentSelection() =>
        _items
            .Where(x => x.IsPressure || x.IsRelease)
            .Select(x => new ScenarioEnvironmentProbeEntry
            {
              ActionId = x.Id,
              IsPressure = x.IsPressure
            })
            .ToList();
  }
}
