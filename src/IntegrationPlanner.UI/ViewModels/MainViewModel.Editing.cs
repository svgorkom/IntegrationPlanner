using System.Collections.ObjectModel;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Jira;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.ViewModels.Properties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IntegrationPlanner.UI.ViewModels;

// Editing: selection, property panel, toolbar actions and canvas drag. Every action is one undoable command.
public sealed partial class MainViewModel
{
    private EditorSelection? _selection;
    private bool _isSynchronizing;
    private bool _isSelecting;
    private ScenePoint? _reshapeAnchor;

    [ObservableProperty]
    private BranchItem? _selectedBranch;

    [ObservableProperty]
    private PropertiesViewModel? _selectedProperties;

    [ObservableProperty]
    private IReadOnlyCollection<string> _selectedKeys = [];

    [ObservableProperty]
    private IReadOnlyDictionary<string, SceneDragTarget> _dragTargets = new Dictionary<string, SceneDragTarget>();

    public ObservableCollection<BranchItem> Branches { get; } = [];

    public double HorizontalSnap => _layoutOptions.StepWidth;

    public double VerticalSnap => _layoutOptions.LaneSpacing;

    private Scenario? CurrentScenario =>
        _editor is not null && SelectedScenario is not null ? _editor.Current.Scenarios.Find(scenario => scenario.Id == SelectedScenario.Id) : null;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => _editor!.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => _editor!.Redo();

    [RelayCommand(CanExecute = nameof(HasPlan))]
    private void AddScenario()
    {
        Scenario scenario = PlanItemFactory.NewScenario(_editor!.Current, CurrentScenario);
        _editor.Execute(new AddScenarioCommand(scenario));
        SelectedScenario = Scenarios.FirstOrDefault(item => item.Id == scenario.Id);
    }

    [RelayCommand(CanExecute = nameof(HasScenario))]
    private void DuplicateScenario()
    {
        Scenario scenario = PlanItemFactory.Duplicate(CurrentScenario!);
        _editor!.Execute(new AddScenarioCommand(scenario));
        SelectedScenario = Scenarios.FirstOrDefault(item => item.Id == scenario.Id);
    }

    [RelayCommand(CanExecute = nameof(HasScenario))]
    private void DeleteScenario() => _editor!.Execute(new DeleteScenarioCommand(SelectedScenario!.Id));

    [RelayCommand(CanExecute = nameof(HasPlan))]
    private void AddBranch()
    {
        BranchId branchId = BranchId.New();
        _editor!.Execute(PlanItemFactory.AddBranch(_editor.Current, CurrentScenario?.Id, branchId));
        SetSelection(new BranchSelection(branchId));
    }

    public bool AddBranchFromTicket(JiraIssue issue, string branchName, string mainlineBranchName)
    {
        ArgumentNullException.ThrowIfNull(issue);
        if (_editor is null)
        {
            return false;
        }

        TicketBranchResult result = TicketBranchFactory.Create(_editor.Current, CurrentScenario, issue, branchName, mainlineBranchName);
        switch (result.Error)
        {
            case TicketBranchError.DuplicateTicket:
                _messages.ShowError(UiText.Format(UiText.JiraDuplicateTicketFormat, issue.Key));
                return false;
            case TicketBranchError.DuplicateName:
                _messages.ShowError(UiText.Format(UiText.JiraDuplicateNameFormat, branchName.Trim()));
                return false;
        }

        _editor.Execute(result.Command!);
        SetSelection(new BranchSelection(result.BranchId));
        return true;
    }

    [RelayCommand(CanExecute = nameof(HasLanes))]
    private void AddPull() => AddConnector(ConnectorKind.Pull);

    [RelayCommand(CanExecute = nameof(HasLanes))]
    private void AddMerge() => AddConnector(ConnectorKind.Merge);

    [RelayCommand(CanExecute = nameof(HasLanes))]
    private void AddMarker()
    {
        Scenario scenario = CurrentScenario!;
        Marker? marker = PlanItemFactory.NewMarker(scenario, _selection);
        if (marker is null)
        {
            return;
        }

        _editor!.Execute(new AddMarkerCommand(scenario.Id, marker));
        SetSelection(new MarkerSelection(marker.Id));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelection()
    {
        IPlanCommand? command = (_selection, CurrentScenario) switch
        {
            (BranchSelection branch, _) => new DeleteBranchCommand(branch.BranchId),
            (ConnectorSelection connector, Scenario scenario) => new DeleteConnectorCommand(scenario.Id, connector.ConnectorId),
            (MarkerSelection marker, Scenario scenario) => new DeleteMarkerCommand(scenario.Id, marker.MarkerId),
            _ => null,
        };
        if (command is not null)
        {
            _editor!.Execute(command);
        }
    }

    [RelayCommand]
    private void SelectElement(string? key) => SetSelection(SceneSelection.FromKey(key));

    [RelayCommand]
    private void MoveElement(SceneMove? move)
    {
        if (move is null || CurrentScenario is not Scenario scenario)
        {
            return;
        }

        IPlanCommand? command = DragPlanner.CommandFor(scenario, move, _layoutOptions);
        if (command is not null)
        {
            _editor!.Execute(command);
        }
    }

    // Previews stand in for the scene until the drag ends; only the committed result becomes an undoable edit.
    [RelayCommand]
    private void ReshapeMarker(SceneReshape? reshape)
    {
        if (reshape is null || _editor is null || CurrentScenario is not Scenario scenario
            || SceneSelection.FromKey(reshape.Key) is not MarkerSelection selected
            || scenario.Markers.Find(item => item.Id == selected.MarkerId) is not Marker marker)
        {
            return;
        }

        if (reshape.Phase == ReshapePhase.Cancel)
        {
            _reshapeAnchor = null;
            Relayout();
            return;
        }

        _reshapeAnchor ??= Scene is null ? null : MarkerReshaper.AnchorOf(Scene, reshape.Key, _layoutOptions.MarkerGap);
        Marker? reshaped = _reshapeAnchor is ScenePoint anchor
            ? MarkerReshaper.Reshape(marker, anchor, new ScenePoint(reshape.X, reshape.Y), _layoutOptions.MarkerGap)
            : null;
        if (reshape.Phase == ReshapePhase.Commit)
        {
            _reshapeAnchor = null;
            if (reshaped is null || reshaped == marker)
            {
                Relayout();
                return;
            }

            _editor.Execute(new UpdateMarkerCommand(scenario.Id, reshaped));
            return;
        }

        if (reshaped is not null)
        {
            Scene = _layoutEngine.Layout(new UpdateMarkerCommand(scenario.Id, reshaped).Apply(_editor.Current), scenario.Id);
        }
    }

    // Merge by default, pull when requested; the step follows the horizontal drop position.
    [RelayCommand]
    private void ConnectElements(SceneConnect? connect)
    {
        if (connect is null || CurrentScenario is not Scenario scenario
            || SceneSelection.FromKey(connect.FromKey) is not BranchSelection from
            || SceneSelection.FromKey(connect.ToKey) is not BranchSelection to
            || from.BranchId == to.BranchId)
        {
            return;
        }

        int step = Math.Clamp((int)Math.Round(connect.X / _layoutOptions.StepWidth), 0, Math.Max(0, scenario.StepCount - 1));
        ConnectorKind kind = connect.IsPull ? ConnectorKind.Pull : ConnectorKind.Merge;
        Connector connector = new(ConnectorId.New(), kind, from.BranchId, to.BranchId, step, null, ProgressState.Planned);
        _editor!.Execute(new AddConnectorCommand(scenario.Id, connector));
        SetSelection(new ConnectorSelection(connector.Id));
    }

    public string? RenameText(string key)
    {
        if (_editor is null || CurrentScenario is not Scenario scenario)
        {
            return null;
        }

        return SceneSelection.FromKey(key) switch
        {
            BranchSelection branch => _editor.Current.Branches.Find(item => item.Id == branch.BranchId)?.Name,
            ConnectorSelection connector => scenario.Connectors.Find(item => item.Id == connector.ConnectorId)?.Label ?? string.Empty,
            MarkerSelection marker => scenario.Markers.Find(item => item.Id == marker.MarkerId)?.Label,
            _ => null,
        };
    }

    [RelayCommand]
    private void RenameElement(SceneRename? rename)
    {
        if (rename is null || _editor is null || CurrentScenario is not Scenario scenario)
        {
            return;
        }

        IPlanCommand? command = SceneSelection.FromKey(rename.Key) switch
        {
            BranchSelection branch when _editor.Current.Branches.Find(item => item.Id == branch.BranchId) is Branch found
                => new UpdateBranchCommand(found with { Name = rename.Text }),
            ConnectorSelection connector when scenario.Connectors.Find(item => item.Id == connector.ConnectorId) is Connector found
                => new UpdateConnectorCommand(scenario.Id, found with { Label = string.IsNullOrWhiteSpace(rename.Text) ? null : rename.Text }),
            MarkerSelection marker when scenario.Markers.Find(item => item.Id == marker.MarkerId) is Marker found
                => new UpdateMarkerCommand(scenario.Id, found with { Label = rename.Text }),
            _ => null,
        };
        if (command is not null)
        {
            _editor.Execute(command);
        }
    }

    private bool CanUndo() => _editor?.CanUndo == true;

    private bool CanRedo() => _editor?.CanRedo == true;

    private bool HasScenario() => CurrentScenario is not null;

    private bool HasLanes() => CurrentScenario?.Lanes.IsEmpty == false;

    private bool HasSelection() => _selection is not null;

    private void AddConnector(ConnectorKind kind)
    {
        Scenario scenario = CurrentScenario!;
        BranchId? selectedBranchId = _selection is BranchSelection branch ? branch.BranchId : null;
        Connector? connector = PlanItemFactory.NewConnector(_editor!.Current, scenario, kind, selectedBranchId);
        if (connector is null)
        {
            return;
        }

        _editor.Execute(new AddConnectorCommand(scenario.Id, connector));
        SetSelection(new ConnectorSelection(connector.Id));
    }

    partial void OnSelectedBranchChanged(BranchItem? value)
    {
        if (_isSynchronizing || _isSelecting || value is null)
        {
            return;
        }

        SetSelection(new BranchSelection(value.Id));
    }

    private void OnEditorChanged(object? sender, EventArgs e) => Synchronize();

    // Brings lists, scene, selection and command states in line with the editor's current plan.
    private void Synchronize()
    {
        Plan plan = _editor!.Current;
        ScenarioId? previousScenarioId = SelectedScenario?.Id;
        _isSynchronizing = true;
        try
        {
            SynchronizeItems(Scenarios, plan.Scenarios, scenario => scenario.Id, item => item.Id, scenario => new ScenarioItem(scenario.Id, scenario.Name), (item, scenario) => item.Name = scenario.Name);
            SynchronizeItems(Branches, plan.Branches, branch => branch.Id, item => item.Id, branch => new BranchItem(branch.Id, Choices.BranchText(branch)), (item, branch) => item.Name = Choices.BranchText(branch));
            if (SelectedScenario is null || !Scenarios.Contains(SelectedScenario))
            {
                SelectedScenario = Scenarios.FirstOrDefault();
            }
        }
        finally
        {
            _isSynchronizing = false;
        }

        Relayout();
        if (SelectedScenario?.Id != previousScenarioId || SelectedProperties?.Refresh() != true)
        {
            SetSelection(_selection);
        }
        else
        {
            SynchronizeSelectedBranch();
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(WindowTitle));
        UpdateCommandStates();
    }

    private void Relayout()
    {
        Scenario? scenario = CurrentScenario;
        DragTargets = scenario is null ? new Dictionary<string, SceneDragTarget>() : DragPlanner.TargetsFor(scenario);
        Scene = scenario is null ? null : _layoutEngine.Layout(_editor!.Current, scenario.Id);
    }

    private void SetSelection(EditorSelection? selection)
    {
        _selection = selection is not null && Exists(selection) ? selection : null;
        SynchronizeSelectedBranch();
        SelectedKeys = SceneSelection.KeysOf(_selection);
        SelectedProperties = CreateProperties(_selection);
        DeleteSelectionCommand.NotifyCanExecuteChanged();
    }

    private void SynchronizeSelectedBranch()
    {
        _isSelecting = true;
        try
        {
            SelectedBranch = _selection is BranchSelection branch ? Branches.FirstOrDefault(item => item.Id == branch.BranchId) : null;
        }
        finally
        {
            _isSelecting = false;
        }
    }

    private bool Exists(EditorSelection selection) => selection switch
    {
        BranchSelection branch => _editor?.Current.Branches.Exists(item => item.Id == branch.BranchId) == true,
        ConnectorSelection connector => CurrentScenario?.Connectors.Exists(item => item.Id == connector.ConnectorId) == true,
        MarkerSelection marker => CurrentScenario?.Markers.Exists(item => item.Id == marker.MarkerId) == true,
        _ => false,
    };

    private PropertiesViewModel? CreateProperties(EditorSelection? selection)
    {
        if (_editor is null)
        {
            return null;
        }

        ScenarioId? scenarioId = CurrentScenario?.Id;
        return (selection, scenarioId) switch
        {
            (BranchSelection branch, _) => new BranchPropertiesViewModel(_editor, scenarioId, branch.BranchId, TicketOpener),
            (ConnectorSelection connector, ScenarioId id) => new ConnectorPropertiesViewModel(_editor, id, connector.ConnectorId),
            (MarkerSelection marker, ScenarioId id) => new MarkerPropertiesViewModel(_editor, id, marker.MarkerId),
            _ => new PlanPropertiesViewModel(_editor, scenarioId),
        };
    }

    private void UpdateCommandStates()
    {
        foreach (IRelayCommand command in new IRelayCommand[]
        {
            SaveCommand, SaveAsCommand, UndoCommand, RedoCommand, AddScenarioCommand, DuplicateScenarioCommand, DeleteScenarioCommand,
            AddBranchCommand, AddPullCommand, AddMergeCommand, AddMarkerCommand, DeleteSelectionCommand,
        })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    // Updates the list in place so that bound selections survive renames and reorders.
    private static void SynchronizeItems<TModel, TItem, TId>(
        ObservableCollection<TItem> items,
        IReadOnlyList<TModel> models,
        Func<TModel, TId> modelId,
        Func<TItem, TId> itemId,
        Func<TModel, TItem> create,
        Action<TItem, TModel> update)
    {
        for (int index = 0; index < models.Count; index++)
        {
            TModel model = models[index];
            TId id = modelId(model);
            int existing = -1;
            for (int candidate = index; candidate < items.Count && existing < 0; candidate++)
            {
                existing = EqualityComparer<TId>.Default.Equals(itemId(items[candidate]), id) ? candidate : -1;
            }

            if (existing < 0)
            {
                items.Insert(index, create(model));
                continue;
            }

            if (existing != index)
            {
                items.Move(existing, index);
            }

            update(items[index], model);
        }

        while (items.Count > models.Count)
        {
            items.RemoveAt(items.Count - 1);
        }
    }
}
