using AIStudio.Converters;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Memory.Episodic;
using ISIDA.Psychic.Understanding;
using ISIDA.Sensors;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace AIStudio.ViewModels.Episodic
{
  /// <summary>
  /// Тексты узлов дерева эпизодической памяти и подсказок (общий код для дерева, ленты кадров истории и др.).
  /// </summary>
  public sealed class EpisodicMemoryNodePresentation
  {
    /// <summary>Подписанный исход для отображения: прямое — Effect, учитель — оценка (StimulsEffect).</summary>
    public static int GetSignedOutcome(EpisodicParams p)
    {
      if (p == null) return 0;
      return p.IsTeacher ? p.StimulsEffect : p.Effect;
    }

    private readonly GomeostasSystem _gomeostas;
    private readonly EmotionsImageSystem _emotionsImage;
    private readonly InfluenceActionSystem _influenceAction;
    private readonly AdaptiveActionsSystem _adaptiveActions;
    private readonly ProblemTreeSystem _problemTree;
    private readonly InfluenceActionsImagesSystem _influenceActionsImages;
    private readonly ActionsImagesSystem _actionsImages;
    private readonly SensorySystem _sensorySystem;
    private readonly AutomatizmTreeSystem _automatizmTree;
    private readonly VerbalBrocaImagesSystem _verbalBroca;

    public EpisodicMemoryNodePresentation(
      GomeostasSystem gomeostas,
      EmotionsImageSystem emotionsImage,
      InfluenceActionSystem influenceAction,
      AdaptiveActionsSystem adaptiveActions,
      ProblemTreeSystem problemTree,
      InfluenceActionsImagesSystem influenceActionsImages,
      ActionsImagesSystem actionsImages,
      SensorySystem sensorySystem,
      AutomatizmTreeSystem automatizmTree = null,
      VerbalBrocaImagesSystem verbalBroca = null)
    {
      _gomeostas = gomeostas;
      _emotionsImage = emotionsImage;
      _influenceAction = influenceAction;
      _adaptiveActions = adaptiveActions;
      _problemTree = problemTree;
      _influenceActionsImages = influenceActionsImages;
      _actionsImages = actionsImages;
      _sensorySystem = sensorySystem;
      _automatizmTree = automatizmTree;
      _verbalBroca = verbalBroca;
    }

    public string BuildFullFrameTooltip(EpisodicMemoryNode node)
    {
      if (node == null) return "—";
      var lines = new List<string>();
      for (int depth = 0; depth <= EpisodicMemoryTree.LeafLevelIndex; depth++)
      {
        var (text, _, _) = GetNodeDisplayAndTooltip(node, depth);
        if (depth == 0)
          lines.Add("Состояние: " + text);
        else
          lines.Add(text);
      }
      return string.Join("\n", lines);
    }

    public (string text, string tooltip, Brush effectBrush) GetNodeDisplayAndTooltip(EpisodicMemoryNode node, int depth)
    {
      if (node == null) return ("—", "—", null);

      string text;
      string tooltip;
      Brush effectBrush = null;

      switch (depth)
      {
        case 0:
          text = GetBaseIdText(node.BaseID);
          tooltip = $"BaseID: {node.BaseID}";
          break;
        case 1:
          string emotionText = GetEmotionText(node.EmotionID);
          if (emotionText == "—" && node.Children != null && node.Children.Count > 0)
          {
            var firstWithEmotion = node.Children.FirstOrDefault(c => c.EmotionID > 0);
            if (firstWithEmotion != null)
              emotionText = GetEmotionText(firstWithEmotion.EmotionID);
          }
          text = "Эмоция: " + emotionText;
          tooltip = emotionText + $"\nEmotionID: {node.EmotionID}";
          break;
        case 2:
          text = $"Understanding: {node.UnderstandingNodeId}";
          tooltip = $"Узел дерева понимания (Understanding), ID: {node.UnderstandingNodeId}";
          break;
        case 3:
          text = $"NodePID: {node.NodePID}";
          tooltip = GetNodePidTooltip(node.NodePID);
          break;
        case 4:
          text = $"Тригггер: {node.TriggerId}";
          tooltip = GetTriggerTooltip(node.TriggerId);
          break;
        case 5:
          if (node.Params != null)
          {
            text = node.Params.IsTeacher
              ? $"Акция: {node.ActionId}, учитель: {FormatEffect(node.Params.StimulsEffect)}"
              : $"Акция: {node.ActionId}, Эффект: {FormatEffect(node.Params.Effect)}";
          }
          else
            text = $"Акция: {node.ActionId}";
          tooltip = GetActionTooltip(node.ActionId);
          if (node.Params != null)
          {
            if (node.Params.IsTeacher)
              tooltip += $"\nУчительское правило, оценка: {node.Params.StimulsEffect}, Count: {node.Params.Count}";
            else
              tooltip += $"\nЭффект: {node.Params.Effect}, Count: {node.Params.Count}";
          }
          if (node.Params != null)
          {
            int v = GetSignedOutcome(node.Params);
            effectBrush = v > 0 ? Brushes.DarkGreen : (v < 0 ? Brushes.DarkRed : Brushes.DarkGoldenrod);
          }
          break;
        default:
          text = BuildCompositeLabel(node);
          tooltip = $"ID: {node.ID}";
          break;
      }

      return (text ?? $"ID:{node.ID}", tooltip ?? text, effectBrush);
    }

    public string GetTriggerPhraseText(int triggerId)
    {
      var img = _actionsImages?.GetActionsImage(triggerId);
      if (img?.PhraseIdList == null || img.PhraseIdList.Count == 0) return null;
      return GetPhraseTextFromImage(img);
    }

    public string GetActionPhraseText(int actionId)
    {
      var img = _actionsImages?.GetActionsImage(actionId);
      if (img?.PhraseIdList == null || img.PhraseIdList.Count == 0) return null;
      return GetPhraseTextFromImage(img);
    }

    private string GetPhraseTextFromImage(ActionsImagesSystem.ActionsImage img)
    {
      if (_sensorySystem?.VerbalChannel == null) return null;
      var vc = _sensorySystem.VerbalChannel;
      var list = img.PhraseIdList.Select(pid => vc.GetPhraseFromPhraseId(pid)).Where(s => !string.IsNullOrEmpty(s)).ToList();
      return list.Any() ? string.Join(" ", list) : null;
    }

    private static string FormatEffect(int effect)
    {
      if (effect > 0) return "+" + effect;
      return effect.ToString();
    }

    private string GetBaseIdText(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"BaseID:{baseId}";
      }
    }

    private string BuildCompositeLabel(EpisodicMemoryNode node)
    {
      if (node.NodePID != 0) return $"NodePID: {node.NodePID}";
      if (node.EmotionID != 0) return "Эмоция: " + GetEmotionText(node.EmotionID);
      if (node.TriggerId != 0) return $"Тригггер: {node.TriggerId}";
      if (node.ActionId != 0) return $"Акция: {node.ActionId}";
      return GetBaseIdText(node.BaseID);
    }

    private string GetEmotionText(int emotionId)
    {
      if (emotionId <= 0) return "—";
      try
      {
        var img = _emotionsImage?.GetEmotionsImage(emotionId);
        if (img?.BaseStylesList == null || !img.BaseStylesList.Any()) return emotionId.ToString();
        var styles = _gomeostas?.GetAllBehaviorStyles();
        if (styles == null) return emotionId.ToString();
        var names = img.BaseStylesList.Where(id => styles.ContainsKey(id)).Select(id => styles[id].Name).ToList();
        return names.Any() ? string.Join(", ", names) : emotionId.ToString();
      }
      catch { return emotionId.ToString(); }
    }

    /// <summary>
    /// NodePID — ID узла дерева проблем; по AutTreeID строится та же подсказка условий, что для «ID дерева условий» в таблице автоматизмов.
    /// </summary>
    public string GetNodePidConditionsTooltip(int nodePid) => BuildNodePidTooltip(nodePid);

    private string GetNodePidTooltip(int nodePid) => BuildNodePidTooltip(nodePid);

    private string BuildNodePidTooltip(int nodePid)
    {
      if (nodePid <= 0)
        return $"NodePID: {nodePid}";

      var pn = FindProblemNodeById(_problemTree?.Tree, nodePid);
      if (pn == null)
        return $"NodePID: {nodePid}\nУзел дерева проблем не найден.";

      int branchId = pn.AutTreeID;
      if (branchId > 0 && branchId < 1_000_000)
      {
        if (_automatizmTree == null)
        {
          return $"NodePID: {nodePid}\nAutTreeID: {branchId}\nДерево автоматизмов недоступно для расшифровки условий.";
        }

        var treeNode = _automatizmTree.GetNodeById(branchId);
        if (treeNode != null)
        {
          var item = BuildAutomatizmDisplayItemForAutTreeNode(treeNode);
          return TreeNodeConditionsToTooltipConverter.FormatConditionsTooltip(item);
        }

        return $"NodePID: {nodePid}\nAutTreeID: {branchId}\nУзел дерева условий не найден.";
      }

      var sb = new StringBuilder();
      sb.AppendLine($"NodePID: {nodePid} — узел дерева проблем.");
      if (branchId <= 0)
        sb.AppendLine("AutTreeID не задан — нет привязки к узлу дерева условий.");
      else
        sb.AppendLine($"AutTreeID: {branchId} вне диапазона узлов дерева (< 1 000 000).");
      sb.AppendLine($"SituationTreeID: {pn.SituationTreeID}, ThemeID: {pn.ThemeID}, PurposeID: {pn.PurposeID}");
      return sb.ToString().TrimEnd();
    }

    private AutomatizmsViewModel.AutomatizmDisplayItem BuildAutomatizmDisplayItemForAutTreeNode(AutomatizmNode treeNode)
    {
      var emotionIdList = new List<int>();
      if (treeNode.EmotionID > 0 && _emotionsImage != null)
      {
        var emotionImage = _emotionsImage.GetEmotionsImage(treeNode.EmotionID);
        if (emotionImage?.BaseStylesList != null)
          emotionIdList = emotionImage.BaseStylesList.ToList();
      }

      var influenceActionIds = new List<int>();
      if (treeNode.ActivityID > 0 && _influenceActionsImages != null)
      {
        var ids = _influenceActionsImages.GetInfluenceActionIds(treeNode.ActivityID);
        if (ids != null)
          influenceActionIds = ids.ToList();
      }

      string toneMoodText = string.Empty;
      if (treeNode.ToneMoodID > 0)
      {
        try { toneMoodText = PsychicSystem.GetToneMoodString(treeNode.ToneMoodID); }
        catch { /* ignore */ }
      }

      string verbalText = string.Empty;
      if (treeNode.VerbID > 0 && _verbalBroca != null && _sensorySystem?.VerbalChannel != null)
      {
        try
        {
          var verbalImage = _verbalBroca.GetVerbalBrocaImage(treeNode.VerbID);
          if (verbalImage?.PhraseIdList != null && verbalImage.PhraseIdList.Any())
          {
            var phraseTexts = verbalImage.PhraseIdList
              .Select(pid => _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(pid))
              .Where(s => !string.IsNullOrEmpty(s))
              .Select(s => "\"" + s + "\"")
              .ToList();
            if (phraseTexts.Any())
              verbalText = string.Join(" ", phraseTexts);
          }
        }
        catch { /* ignore */ }
      }

      return new AutomatizmsViewModel.AutomatizmDisplayItem
      {
        ToneMoodID = treeNode.ToneMoodID,
        BaseConditionText = FormatBaseConditionForTooltip(treeNode.BaseID),
        EmotionText = FormatEmotionStylesForTooltip(emotionIdList),
        InfluenceActionsText = FormatInfluenceActionsForTooltip(influenceActionIds),
        ToneMoodText = toneMoodText,
        VerbalText = verbalText,
        SimbolID = treeNode.SimbolID,
        VisualID = treeNode.VisualID
      };
    }

    private static string FormatBaseConditionForTooltip(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"Неизвестное ({baseId})";
      }
    }

    private string FormatEmotionStylesForTooltip(List<int> emotionIds)
    {
      if (emotionIds == null || !emotionIds.Any())
        return "Нет эмоций";
      try
      {
        var behaviorStyles = _gomeostas?.GetAllBehaviorStyles();
        if (behaviorStyles == null)
          return $"Стили: {string.Join(", ", emotionIds)}";
        var names = emotionIds
          .Where(id => behaviorStyles.ContainsKey(id))
          .Select(id => behaviorStyles[id].Name)
          .ToList();
        return names.Any() ? string.Join(", ", names) : $"Стили: {string.Join(", ", emotionIds)}";
      }
      catch
      {
        return $"Стили: {string.Join(", ", emotionIds)}";
      }
    }

    private string FormatInfluenceActionsForTooltip(List<int> actionIds)
    {
      if (actionIds == null || !actionIds.Any())
        return "Нет воздействий";
      try
      {
        var allActions = _influenceAction?.GetAllInfluenceActions();
        if (allActions == null)
          return $"Действия: {string.Join(", ", actionIds)}";
        var names = actionIds
          .Where(id => allActions.Any(a => a.Id == id))
          .Select(id => allActions.First(a => a.Id == id).Name)
          .ToList();
        return names.Any() ? string.Join(", ", names) : $"Действия: {string.Join(", ", actionIds)}";
      }
      catch
      {
        return $"Действия: {string.Join(", ", actionIds)}";
      }
    }

    private static ProblemTreeNode FindProblemNodeById(ProblemTreeNode root, int id)
    {
      if (root == null) return null;
      if (root.ID == id) return root;
      foreach (var c in root.Children ?? Enumerable.Empty<ProblemTreeNode>())
      {
        var found = FindProblemNodeById(c, id);
        if (found != null) return found;
      }
      return null;
    }

    /// <summary>Подсказка для образа триггера (действия, фраза, тон/настроение).</summary>
    public string GetTriggerTooltip(int triggerId)
    {
      if (triggerId <= 0) return "—";
      var actImg = _actionsImages?.GetActionsImage(triggerId);
      if (actImg != null)
        return BuildImageTooltip(actImg);
      var infImg = _influenceActionsImages?.GetInfluenceActionsImage(triggerId);
      if (infImg?.ActIdList != null && infImg.ActIdList.Count > 0 && _influenceAction != null)
      {
        var names = infImg.ActIdList
          .Where(id => _influenceAction.GetAllInfluenceActions().Any(a => a.Id == id))
          .Select(id => _influenceAction.GetAllInfluenceActions().First(a => a.Id == id).Name)
          .ToList();
        return $"Действие: {(names.Any() ? string.Join(", ", names) : "Нет")}\nФраза: Нет\nТон: —\nНастроение: —";
      }
      return $"Триггер ID: {triggerId}";
    }

    /// <summary>Подсказка для образа акции (действия, фраза, тон/настроение).</summary>
    public string GetActionTooltip(int actionId)
    {
      if (actionId <= 0) return "—";
      var actImg = _actionsImages?.GetActionsImage(actionId);
      return actImg != null ? BuildImageTooltip(actImg) : $"Акция ID: {actionId}";
    }

    private string BuildImageTooltip(ActionsImagesSystem.ActionsImage actImg)
    {
      var sb = new System.Text.StringBuilder();
      string actionText = "Нет";
      if (actImg.ActIdList != null && actImg.ActIdList.Count > 0 && _adaptiveActions != null)
      {
        var names = actImg.ActIdList
          .Where(id => _adaptiveActions.GetAllAdaptiveActions().Any(a => a.Id == id))
          .Select(id => _adaptiveActions.GetAllAdaptiveActions().First(a => a.Id == id).Name)
          .ToList();
        actionText = names.Any() ? string.Join(", ", names) : "Нет";
      }
      else if (actImg.ActIdList != null && actImg.ActIdList.Count > 0 && _influenceAction != null)
      {
        var names = actImg.ActIdList
          .Where(id => _influenceAction.GetAllInfluenceActions().Any(a => a.Id == id))
          .Select(id => _influenceAction.GetAllInfluenceActions().First(a => a.Id == id).Name)
          .ToList();
        actionText = names.Any() ? string.Join(", ", names) : "Нет";
      }
      sb.AppendLine($"Действие: {actionText}");

      string phraseText = "Нет";
      if (actImg.PhraseIdList != null && actImg.PhraseIdList.Count > 0 && _sensorySystem?.VerbalChannel != null)
      {
        var phrases = actImg.PhraseIdList.Select(pid => _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(pid)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        phraseText = phrases.Any() ? string.Join(" ", phrases) : "Нет";
      }
      sb.AppendLine($"Фраза: {phraseText}");

      string tone = ActionsImagesSystem.GetToneText(actImg.ToneId);
      string mood = ActionsImagesSystem.GetMoodText(actImg.MoodId);
      sb.AppendLine(string.IsNullOrEmpty(tone) ? "Тон: —" : $"Тон: {tone}");
      sb.AppendLine(string.IsNullOrEmpty(mood) ? "Настроение: —" : $"Настроение: {mood}");
      return sb.ToString().TrimEnd();
    }
  }
}
