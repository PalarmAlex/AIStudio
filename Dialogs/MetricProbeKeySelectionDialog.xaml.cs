using AIStudio.Common.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class MetricProbeKeySelectionDialog : Window
  {
    public string SelectedProbeKey { get; private set; }
    private readonly List<MetricProbeItem> _allItems = new List<MetricProbeItem>();

    public MetricProbeKeySelectionDialog(string initiallySelected, IEnumerable<AdapterSchemaMetricProbe> probes)
    {
      InitializeComponent();
      SelectedProbeKey = initiallySelected ?? string.Empty;
      LoadProbes(probes);
      ApplyFilter();
    }

    private void LoadProbes(IEnumerable<AdapterSchemaMetricProbe> probes)
    {
      _allItems.Clear();
      if (probes == null)
        return;

      var knownKeys = new HashSet<string>(StringComparer.Ordinal);
      foreach (AdapterSchemaMetricProbe probe in probes.OrderBy(p => p?.Key ?? string.Empty, StringComparer.Ordinal))
      {
        if (probe == null)
          continue;
        string key = (probe.Key ?? string.Empty).Trim();
        if (!knownKeys.Add(key))
          continue;
        _allItems.Add(new MetricProbeItem
        {
          Key = key,
          Label = probe.Label ?? string.Empty,
          Description = probe.Description ?? string.Empty,
          DisplayText = probe.DisplayText,
          IsSelected = string.Equals(key, SelectedProbeKey, StringComparison.Ordinal)
        });
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<MetricProbeItem> items = _allItems;
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = _allItems.Where(item => MatchesFilter(item, lowered));
      }

      var filtered = items.ToList();
      ProbesList.ItemsSource = filtered;

      MetricProbeItem selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        ProbesList.SelectedItem = selectedItem;
        ProbesList.ScrollIntoView(selectedItem);
      }
    }

    private static bool MatchesFilter(MetricProbeItem item, string loweredFilter)
    {
      return (item.Key ?? string.Empty).ToLowerInvariant().Contains(loweredFilter)
          || (item.Label ?? string.Empty).ToLowerInvariant().Contains(loweredFilter)
          || (item.Description ?? string.Empty).ToLowerInvariant().Contains(loweredFilter)
          || (item.DisplayText ?? string.Empty).ToLowerInvariant().Contains(loweredFilter);
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is MetricProbeItem item))
        return;

      foreach (MetricProbeItem probeItem in _allItems)
        probeItem.IsSelected = string.Equals(probeItem.Key, item.Key, StringComparison.Ordinal);

      SelectedProbeKey = item.Key;
      ProbesList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      MetricProbeItem selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
        SelectedProbeKey = selectedItem.Key;

      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
        e.Handled = true;
      }
      else if (e.Key == Key.Enter)
      {
        OkButton_Click(sender, e);
        e.Handled = true;
      }
    }

    private void ProbesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(ProbesList.SelectedItem is MetricProbeItem item))
        return;

      foreach (MetricProbeItem probeItem in _allItems)
        probeItem.IsSelected = string.Equals(probeItem.Key, item.Key, StringComparison.Ordinal);

      SelectedProbeKey = item.Key;
      DialogResult = true;
      Close();
    }
  }

  public sealed class MetricProbeItem
  {
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
  }
}
