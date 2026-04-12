using System.Collections.ObjectModel;
using System.IO;
using Ai.McuUiStudio.App.Services.Documentation;
using Ai.McuUiStudio.App.Services.Localization;
using Ai.McuUiStudio.App.Services.Project;
using Avalonia.Threading;
using Ai.McuUiStudio.App.Services.Preview;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;
using Ai.McuUiStudio.Core.Services;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ContextEngine _contextEngine;
    private readonly DocumentationServerService? _documentationServer;
    private readonly LvglMetaModelRegistry _lvglMetaRegistry;
    private readonly LocalizationCatalog _localizationCatalog;
    private readonly MetaModelRegistry _registry;
    private readonly DocumentValidator _validator;
    private readonly JsonDocumentParser _jsonDocumentParser;
    private readonly JsonDocumentSerializer _jsonDocumentSerializer;
    private readonly LvglCGenerator _lvglCGenerator;
    private readonly McuDisplayCodeGenerator _mcuDisplayCodeGenerator;
    private readonly McuEventCodeGenerator _mcuEventCodeGenerator;
    private readonly McuUpdateCodeGenerator _mcuUpdateCodeGenerator;
    private readonly RtosMessagesCodeGenerator _rtosMessagesCodeGenerator;
    private readonly UiDocument _document;
    private readonly ProjectSettingsViewModel _projectSettings;
    private readonly Dictionary<string, bool> _propertyGroupState = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _toolboxGroupState = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _autoRenderDebounceCts;
    private IPreviewService _previewService;
    private string _selectedPreviewBackend = PreviewBackendCatalog.NativeLvglPreviewLabel;
    private int _selectedDiagnosticsTabIndex;
    private LanguageOption? _selectedLanguage;
    private NodeViewModel? _selectedNode;
    private string? _selectedNodeHighlightId;
    private int _selectedPropertyTabIndex;
    private ToolboxItemViewModel? _selectedToolboxItem;
    private string? _selectedTool;
    private string _propertySortMode = "Kategorisiert";
    private bool _projectLoaded;
    private int _previewZoomPercent = 100;
    private bool _documentationPanelVisible = false;
    private bool _strictValidation;
    private string _toolboxSearchText = string.Empty;
    private string _toolboxSortMode = "Gruppiert";
    private string _validationSummary = string.Empty;
    private string _jsonPreview = string.Empty;
    private string? _currentDocumentPath;
    private string? _currentProjectFilePath;

    public MainWindowViewModel(DocumentationServerService? documentationServer = null)
    {
        _documentationServer = documentationServer;
        _previewService = PreviewBackendCatalog.CreateService(_selectedPreviewBackend);
        _previewService.LogReceived += HandlePreviewLogReceived;
        _localizationCatalog = LocalizationCatalog.LoadEmbedded(LvglMetaModelLoader.DefaultVersion);
        _lvglMetaRegistry = LvglMetaModelRegistry.CreateDefault();
        _registry = MetaModelRegistry.CreateDefault();
        _contextEngine = new ContextEngine(_registry);
        _validator = new DocumentValidator(_lvglMetaRegistry);
        _jsonDocumentParser = new JsonDocumentParser();
        _jsonDocumentSerializer = new JsonDocumentSerializer();
        _lvglCGenerator = new LvglCGenerator(_lvglMetaRegistry);
        _mcuDisplayCodeGenerator = new McuDisplayCodeGenerator(_lvglMetaRegistry);
        _mcuEventCodeGenerator = new McuEventCodeGenerator(_lvglMetaRegistry);
        _mcuUpdateCodeGenerator = new McuUpdateCodeGenerator(_lvglMetaRegistry);
        _rtosMessagesCodeGenerator = new RtosMessagesCodeGenerator(_lvglMetaRegistry);

        _document = new UiDocument(CreateInitialDocument());
        _projectSettings = new ProjectSettingsViewModel();
        _projectSettings.StrictValidation = _strictValidation;

        RootNodes = new ObservableCollection<NodeViewModel>([CreateTree(_document.Root)]);
        AvailableLanguages = new ObservableCollection<LanguageOption>(_localizationCatalog.AvailableLanguages);
        ToolboxItems = new ObservableCollection<ToolboxItemViewModel>();
        ToolboxSortModes = new ObservableCollection<string>(["Gruppiert", "Alphabetisch"]);
        PreviewBackendOptions = new ObservableCollection<string>(PreviewBackendCatalog.All);
        PropertyGridItems = new ObservableCollection<PropertyGridItemViewModel>();
        PropertySortModes = new ObservableCollection<string>(["Kategorisiert", "Alphabetisch"]);
        Properties = new ObservableCollection<AttributeEditorViewModel>();
        EditorState = new EditorStateViewModel();
        EditorState.PropertyChanged += HandleEditorStatePropertyChanged;
        ApplyEditorStateLocalization();
        EditorState.PreviewBackend = _previewService.BackendName;
        EditorState.IsSimulatorConnected = _previewService.IsConnected;
        _selectedLanguage = AvailableLanguages.FirstOrDefault(x =>
            string.Equals(x.Code, _localizationCatalog.CurrentLanguageCode, StringComparison.OrdinalIgnoreCase))
            ?? AvailableLanguages.FirstOrDefault();

        SelectedNode = RootNodes[0];
    }

    public ObservableCollection<NodeViewModel> RootNodes { get; }

    public ObservableCollection<LanguageOption> AvailableLanguages { get; }

    public ObservableCollection<ToolboxItemViewModel> ToolboxItems { get; }

    public ObservableCollection<string> ToolboxSortModes { get; }

    public ObservableCollection<string> PreviewBackendOptions { get; }

    public ObservableCollection<PropertyGridItemViewModel> PropertyGridItems { get; }

    public ObservableCollection<string> PropertySortModes { get; }

    public ObservableCollection<AttributeEditorViewModel> Properties { get; }

    public EditorStateViewModel EditorState { get; }

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is null || !SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            _localizationCatalog.SetLanguage(value.Code);
            ApplyEditorStateLocalization();
            RaiseLocalizedTextPropertiesChanged();
            RebuildTree(SelectedNode?.Node.Id);
            _ = RefreshDocumentStateAsync();
        }
    }

    public NodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (ReferenceEquals(_selectedNode, value))
            {
                return;
            }

            CommitPendingPropertyEdits();

            if (SetProperty(ref _selectedNode, value))
            {
                _selectedNodeHighlightId = value?.Node.Attributes.TryGetValue("id", out var id) == true ? id : null;
                RefreshSelectionState();
                _ = _previewService.HighlightAsync(_selectedNodeHighlightId);
            }
        }
    }

    public bool IsProjectLoaded
    {
        get => _projectLoaded;
        private set => SetProperty(ref _projectLoaded, value);
    }

    public string? SelectedTool
    {
        get => _selectedTool;
        set => SetProperty(ref _selectedTool, value);
    }

    public ToolboxItemViewModel? SelectedToolboxItem
    {
        get => _selectedToolboxItem;
        set
        {
            if (value is not null && !value.IsSelectable)
            {
                SetProperty(ref _selectedToolboxItem, null);
                SelectedTool = null;
                return;
            }

            if (SetProperty(ref _selectedToolboxItem, value) && value?.IsSelectable == true)
            {
                SelectedTool = value.ToolName;
            }
        }
    }

    public string ToolboxSortMode
    {
        get => _toolboxSortMode;
        set
        {
            if (SetProperty(ref _toolboxSortMode, value))
            {
                RaisePropertyChanged(nameof(IsGroupedToolboxSort));
                RaisePropertyChanged(nameof(ToolboxSortGlyph));
                RaisePropertyChanged(nameof(ToolboxSortHint));
                RefreshSelectionState();
            }
        }
    }

    public string ToolboxSearchText
    {
        get => _toolboxSearchText;
        set
        {
            if (SetProperty(ref _toolboxSearchText, value))
            {
                SelectFirstMatchingToolboxItem(_toolboxSearchText);
            }
        }
    }

    public string SelectedPreviewBackend
    {
        get => _selectedPreviewBackend;
        set
        {
            if (SetProperty(ref _selectedPreviewBackend, value))
            {
                _ = SwitchPreviewBackendAsync(value);
            }
        }
    }

    public int PreviewZoomPercent
    {
        get => _previewZoomPercent;
        private set
        {
            if (SetProperty(ref _previewZoomPercent, value))
            {
                RaisePropertyChanged(nameof(PreviewZoomText));
            }
        }
    }

    public string PropertySortMode
    {
        get => _propertySortMode;
        set
        {
            if (SetProperty(ref _propertySortMode, value))
            {
                RaisePropertyChanged(nameof(IsCategorizedPropertySort));
                RaisePropertyChanged(nameof(PropertySortGlyph));
                RaisePropertyChanged(nameof(PropertySortHint));
                RefreshSelectionState();
            }
        }
    }

    public int SelectedPropertyTabIndex
    {
        get => _selectedPropertyTabIndex;
        set
        {
            if (SetProperty(ref _selectedPropertyTabIndex, value))
            {
                RaisePropertyChanged(nameof(IsPropertiesModeSelected));
                RaisePropertyChanged(nameof(IsEventsModeSelected));
                RaisePropertyChanged(nameof(IsCategorizedPropertySort));
                RaisePropertyChanged(nameof(PropertySortGlyph));
                RaisePropertyChanged(nameof(PropertySortHint));
                RefreshSelectionState();
            }
        }
    }

    public bool IsPropertiesModeSelected => SelectedPropertyTabIndex == 0;

    public bool IsEventsModeSelected => SelectedPropertyTabIndex == 1;

    public int SelectedDiagnosticsTabIndex
    {
        get => _selectedDiagnosticsTabIndex;
        set
        {
            if (SetProperty(ref _selectedDiagnosticsTabIndex, value))
            {
                RaisePropertyChanged(nameof(IsValidationTabSelected));
                RaisePropertyChanged(nameof(IsTechnicalLogTabSelected));
                RaisePropertyChanged(nameof(IsEventCallbacksTabSelected));
                RaisePropertyChanged(nameof(IsJsonPreviewTabSelected));
            }
        }
    }

    public bool IsValidationTabSelected => SelectedDiagnosticsTabIndex == 0;

    public bool IsTechnicalLogTabSelected => SelectedDiagnosticsTabIndex == 1;

    public bool IsEventCallbacksTabSelected => SelectedDiagnosticsTabIndex == 2;

    public bool IsJsonPreviewTabSelected => SelectedDiagnosticsTabIndex == 3;

    public bool IsCategorizedPropertySort => string.Equals(PropertySortMode, "Kategorisiert", StringComparison.OrdinalIgnoreCase);

    public string PropertySortGlyph => IsCategorizedPropertySort ? "AZ" : "▤";

    private string Ui(string key) => _localizationCatalog.GetUiString(key);

    public string PropertySortHint => IsCategorizedPropertySort
        ? Ui("sort.to_alphabetical")
        : Ui("sort.to_categorized");

    public bool IsGroupedToolboxSort => string.Equals(ToolboxSortMode, "Gruppiert", StringComparison.OrdinalIgnoreCase);

    public string ToolboxSortGlyph => IsGroupedToolboxSort ? "AZ" : "▤";

    public string ToolboxSortHint => IsGroupedToolboxSort
        ? Ui("sort.to_alphabetical")
        : Ui("sort.to_grouped");

    public string RuntimeDocumentMode => "LVGL";

    public string ToolbarTitle => Ui("toolbar.title");

    public string WindowTitle => Ui("window.title");

    public string ToolbarDraftBadge => Ui("toolbar.badge.draft");

    public string ToolbarOpenLabel => Ui("toolbar.open");

    public string ToolbarSaveLabel => Ui("toolbar.save");

    public string ToolbarProjectLabel => Ui("toolbar.project");

    public string ToolbarProjectTooltip => Ui("toolbar.project_tooltip");

    public string ToolbarExitTooltip => Ui("toolbar.exit_tooltip");

    public string ToolbarThemeLabel => Ui("toolbar.theme");

    public string ToolbarThemeTooltip => Ui("toolbar.theme_tooltip");

    public string ToolbarLvConfLabel => Ui("toolbar.lv_conf");

    public string ToolbarLvConfTooltip => Ui("toolbar.lv_conf_tooltip");

    public string ToolbarGenerateCodeTooltip => Ui("toolbar.generate_code_tooltip");

    public string ToolbarLvglLabel => Ui("toolbar.lvgl");

    public string ToolbarLvglActiveLabel => Ui("toolbar.lvgl_active");

    public string ToolbarModeLabel => Ui("toolbar.mode");

    public string ToolbarLanguageLabel => Ui("toolbar.language");

    public string ToolbarPreviewResetSizeTooltip => Ui("toolbar.preview_reset_size_tooltip");

    public string ToolbarPreviewZoomLabel => Ui("toolbar.preview_zoom");

    public string ToolbarBackendLabel => Ui("toolbar.backend");

    public string ToolbarAutoRenderLabel => Ui("toolbar.auto_render");

    public string ToolbarRenderLabel => Ui("toolbar.render");

    public string ToolbarOpenTooltip => Ui("toolbar.open_tooltip");

    public string ToolbarNewTooltip => Ui("toolbar.new_tooltip");

    public string ToolbarSaveTooltip => Ui("toolbar.save_tooltip");

    public string ToolbarAddElementTooltip => Ui("toolbar.add_element_tooltip");

    public string ToolbarDuplicateTooltip => Ui("toolbar.duplicate_tooltip");

    public string ToolbarDeleteTooltip => Ui("toolbar.delete_tooltip");

    public string ToolbarMoveUpTooltip => Ui("toolbar.move_up_tooltip");

    public string ToolbarMoveDownTooltip => Ui("toolbar.move_down_tooltip");

    public string ToolbarRenderTooltip => Ui("toolbar.render_tooltip");

    public string ToolbarPreviewZoomOutTooltip => Ui("toolbar.preview_zoom_out_tooltip");

    public string ToolbarPreviewZoomInTooltip => Ui("toolbar.preview_zoom_in_tooltip");

    public string PreviewZoomText => $"{PreviewZoomPercent}%";

    public string ToolboxTitle => Ui("panel.toolbox");

    public string ToolboxContextHint => Ui("toolbox.context_hint");

    public string ToolboxUnsupportedLabel => Ui("toolbox.not_renderable");

    public string ToolboxAddElementLabel => Ui("toolbox.add_element");

    public string ToolboxDuplicateLabel => Ui("toolbox.duplicate");

    public string ToolboxDeleteLabel => Ui("toolbox.delete");

    public string ToolboxMoveUpLabel => Ui("toolbox.move_up");

    public string ToolboxMoveDownLabel => Ui("toolbox.move_down");

    public string ToolboxStrictLabel => Ui("toolbox.strict");

    public string StructureTitle => Ui("panel.structure");

    public string PropertiesTitle => Ui("panel.properties");

    public string DocumentationTitle => Ui("panel.documentation");

    public string DocumentationUrl => _documentationServer?.GetDocumentationUrl(
        SelectedLanguage?.Code ?? _localizationCatalog.CurrentLanguageCode) ?? string.Empty;

    public string DocumentationPlaceholderTitle => Ui("panel.documentation.placeholder_title");

    public string DocumentationPlaceholderText => Ui("panel.documentation.placeholder_text");

    public string DiagnosticsTitle => Ui("panel.diagnostics");

    public string PropertiesTabLabel => Ui("properties.tab.properties");

    public string EventsTabLabel => Ui("properties.tab.events");

    public string PropertyBoolActiveLabel => Ui("properties.bool.active");

    public string ValidationTabLabel => Ui("diagnostics.tab.validation");

    public string TechnicalLogTabLabel => Ui("diagnostics.tab.technical_log");

    public string EventCallbacksTabLabel => Ui("diagnostics.tab.event_callbacks");

    public string JsonPreviewTabLabel => Ui("diagnostics.tab.json_preview");

    public string DiagnosticsClearLogLabel => Ui("diagnostics.clear_log");

    public bool IsDocumentationPanelVisible
    {
        get => _documentationPanelVisible;
        set
        {
            if (SetProperty(ref _documentationPanelVisible, value))
            {
                RaisePropertyChanged(nameof(DocumentationPanelToggleGlyph));
                RaisePropertyChanged(nameof(DocumentationPanelToggleTooltip));
            }
        }
    }

    public string DocumentationPanelToggleGlyph => IsDocumentationPanelVisible ? ">" : "<";

    public string DocumentationPanelToggleTooltip => IsDocumentationPanelVisible
        ? Ui("panel.documentation.hide")
        : Ui("panel.documentation.show");

    public bool AreAllTreeNodesExpanded => RootNodes.Count > 0 && RootNodes.All(AreAllExpanded);

    public string TreeExpandCollapseGlyph => AreAllTreeNodesExpanded ? "▼" : "▶";

    public string TreeExpandCollapseHint => AreAllTreeNodesExpanded
        ? Ui("tree.collapse_all")
        : Ui("tree.expand_all");

    public string StatusSimulatorText => $"{Ui("status.simulator")}: {(EditorState.IsSimulatorConnected ? Ui("status.yes") : Ui("status.no"))}";

    public string StatusChangesText => $"{Ui("status.changes")}: {(EditorState.IsDirty ? Ui("status.yes") : Ui("status.no"))}";

    public string StatusBackendText => $"{Ui("status.backend")}: {EditorState.PreviewBackend}";

    public string StatusLvglText => $"{Ui("status.lvgl")}: {_projectSettings.LvglVersion}";

    public string StatusModeText => $"{Ui("status.mode")}: {_projectSettings.Mode}";

    public string StatusStrictText => $"{ToolboxStrictLabel}: {(StrictValidation ? Ui("status.yes") : Ui("status.no"))}";

    public string ProjectDialogTitle => Ui("dialog.project_title");

    public string ThemeDialogTitle => Ui("dialog.theme_title");

    public string RequirementRequiredLabel => Ui("properties.requirement.required");

    public string RequirementOptionalLabel => Ui("properties.requirement.optional");

    public string ThemeDialogFileLabel => Ui("dialog.theme.file");

    public string ThemeDialogPropertyHeader => Ui("dialog.theme.property");

    public string ThemeDialogValueHeader => Ui("dialog.theme.value");

    public string ThemeDialogDescriptionHeader => Ui("dialog.theme.description");

    public string ThemeDialogSaveLabel => Ui("dialog.save");

    public string LvConfDialogTitle => Ui("dialog.lv_conf_title");

    public string LvConfDialogFileLabel => Ui("dialog.lv_conf.file");

    public string LvConfDialogPropertyHeader => Ui("dialog.lv_conf.property");

    public string LvConfDialogValueHeader => Ui("dialog.lv_conf.value");

    public string LvConfDialogDescriptionHeader => Ui("dialog.lv_conf.description");

    public string LvConfDialogSaveLabel => Ui("dialog.save");

    public string DialogCloseLabel => Ui("dialog.close");

    public string ThemeDialogPlaceholderText => Ui("dialog.theme.placeholder");

    public string ThemeDialogSaveSuccessFormat => Ui("message.theme_saved");

    public string ThemeDialogSaveFailedFormat => Ui("error.theme_save_failed");

    public string ThemeDialogLoadFailedFormat => Ui("error.theme_load_failed");

    public string LvConfDialogPlaceholderText => Ui("dialog.lv_conf.placeholder");

    public string LvConfDialogSaveSuccessFormat => Ui("message.lv_conf_saved");

    public string LvConfDialogSaveFailedFormat => Ui("error.lv_conf_save_failed");

    public string LvConfDialogLoadFailedFormat => Ui("error.lv_conf_load_failed");

    public bool StrictValidation
    {
        get => _strictValidation;
        set
        {
            if (SetProperty(ref _strictValidation, value))
            {
                _projectSettings.StrictValidation = value;
                RaisePropertyChanged(nameof(StatusStrictText));
                _ = RefreshDocumentStateAsync();
            }
        }
    }

    public string ValidationSummary
    {
        get => _validationSummary;
        private set => SetProperty(ref _validationSummary, value);
    }

    public string JsonPreview
    {
        get => _jsonPreview;
        private set => SetProperty(ref _jsonPreview, value);
    }

    public string? CurrentDocumentPath
    {
        get => _currentDocumentPath;
        private set
        {
            if (SetProperty(ref _currentDocumentPath, value))
            {
                RaisePropertyChanged(nameof(HasCurrentDocumentPath));
            }
        }
    }

    public string? CurrentProjectFilePath
    {
        get => _currentProjectFilePath;
        private set => SetProperty(ref _currentProjectFilePath, value);
    }

    public ProjectSettingsViewModel SnapshotProjectSettings() => _projectSettings.Clone();

    public LocalizationCatalog GetLocalizationCatalog() => _localizationCatalog;

    public void SetProjectLoaded(bool isLoaded)
    {
        if (IsProjectLoaded == isLoaded)
        {
            return;
        }

        IsProjectLoaded = isLoaded;
        RaisePropertyChanged(nameof(IsEditorReady));

        if (isLoaded)
        {
            RefreshSelectionState();
        }
        else
        {
            ToolboxItems.Clear();
            PropertyGridItems.Clear();
            Properties.Clear();
            ValidationSummary = string.Empty;
            JsonPreview = string.Empty;
            EditorState.IsDirty = false;
            EditorState.IsPreviewOutOfDate = true;
            EditorState.IsSimulatorConnected = false;
            EditorState.SetPreviewStatus(Ui("state.preview_not_rendered"));
        }
    }

    public bool IsEditorReady => IsProjectLoaded;

    public void ApplyProjectSettings(ProjectSettingsViewModel settings, string? projectFilePath = null)
    {
        _projectSettings.CopyFrom(settings);
        if (!string.IsNullOrWhiteSpace(projectFilePath))
        {
            CurrentProjectFilePath = projectFilePath;
        }
        StrictValidation = settings.StrictValidation;
        RaisePropertyChanged(nameof(StatusLvglText));
        RaisePropertyChanged(nameof(StatusModeText));
    }

    public bool TryLoadProjectMainDocument(out string? errorMessage)
    {
        errorMessage = null;

        var projectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(_projectSettings.ProjectDirectory);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return false;
        }

        var mainDocumentPath = Path.Combine(projectDirectory, "screens", "ui_start.json");
        if (!File.Exists(mainDocumentPath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(mainDocumentPath);
            return TryLoadDocumentJson(json, mainDocumentPath, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public bool HasCurrentDocumentPath => !string.IsNullOrWhiteSpace(CurrentDocumentPath);

    public string OpenDialogTitle => Ui("dialog.open_screen_title");

    public string NewScreenDialogTitle => Ui("dialog.new_screen_title");

    public string SaveDialogTitle => Ui("dialog.save_screen_title");

    public string ScreenJsonFileTypeLabel => Ui("dialog.filetype.screen_json");

    public string ErrorFileLoadFailedFormat => Ui("error.file_load_failed");

    public string ErrorSaveTargetUnavailableText => Ui("error.save_target_unavailable");

    public string NewScreenCreatedFormat => Ui("message.new_screen_created");

    public void AddSelectedToolAsChild()
    {
        if (SelectedNode is null || string.IsNullOrWhiteSpace(SelectedTool))
        {
            return;
        }

        AddToolAsChild(SelectedTool, SelectedNode);
    }

    public void AddToolAsChild(string toolName, NodeViewModel? targetNode)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        var parentNode = targetNode ?? RootNodes.FirstOrDefault();
        if (parentNode is null)
        {
            return;
        }

        var newNode = new UiNode(toolName);
        parentNode.Node.Children.Add(newNode);
        var childViewModel = CreateTree(newNode);
        parentNode.Children.Add(childViewModel);
        parentNode.IsExpanded = true;
        SelectedNode = childViewModel;
        MarkDocumentChanged();
    }

    public bool CanAddToolAsChild(string toolName, NodeViewModel? targetNode)
    {
        if (string.IsNullOrWhiteSpace(toolName) || targetNode is null)
        {
            return false;
        }

        return CanParentAcceptTool(targetNode.Node.ElementName, toolName);
    }

    public bool CanAddToolAsSibling(string toolName, NodeViewModel? targetNode)
    {
        if (string.IsNullOrWhiteSpace(toolName) || targetNode is null)
        {
            return false;
        }

        var parentNode = FindParent(_document.Root, targetNode.Node);
        if (parentNode is null)
        {
            return false;
        }

        return CanParentAcceptTool(parentNode.ElementName, toolName);
    }

    public bool CanAddToolBefore(string toolName, NodeViewModel? targetNode) => CanAddToolAsSibling(toolName, targetNode);

    public bool CanAddToolAfter(string toolName, NodeViewModel? targetNode) => CanAddToolAsSibling(toolName, targetNode);

    public NodeViewModel? FindNodeById(Guid nodeId)
    {
        if (RootNodes.Count == 0)
        {
            return null;
        }

        return FindNodeViewModel(RootNodes[0], nodeId);
    }

    public bool CanMoveNodeAsChild(NodeViewModel? draggedNode, NodeViewModel? targetNode)
    {
        if (draggedNode is null || targetNode is null || draggedNode.Node == _document.Root)
        {
            return false;
        }

        if (draggedNode.Node.Id == targetNode.Node.Id || IsDescendantOf(targetNode.Node, draggedNode.Node))
        {
            return false;
        }

        return true;
    }

    public bool CanMoveNodeAsSibling(NodeViewModel? draggedNode, NodeViewModel? targetNode)
    {
        if (draggedNode is null || targetNode is null || draggedNode.Node == _document.Root)
        {
            return false;
        }

        if (draggedNode.Node.Id == targetNode.Node.Id)
        {
            return false;
        }

        var parentNode = FindParent(_document.Root, targetNode.Node);
        if (parentNode is null)
        {
            return false;
        }

        if (IsDescendantOf(parentNode, draggedNode.Node))
        {
            return false;
        }

        return true;
    }

    public void AddToolAsSibling(string toolName, NodeViewModel? targetNode)
    {
        AddToolRelativeToSibling(toolName, targetNode, insertAfter: true);
    }

    public void AddToolBefore(string toolName, NodeViewModel? targetNode)
    {
        AddToolRelativeToSibling(toolName, targetNode, insertAfter: false);
    }

    public void AddToolAfter(string toolName, NodeViewModel? targetNode)
    {
        AddToolRelativeToSibling(toolName, targetNode, insertAfter: true);
    }

    private void AddToolRelativeToSibling(string toolName, NodeViewModel? targetNode, bool insertAfter)
    {
        if (string.IsNullOrWhiteSpace(toolName) || targetNode is null)
        {
            return;
        }

        var parentNode = FindParent(_document.Root, targetNode.Node);
        var parentViewModel = FindParentViewModel(RootNodes[0], targetNode);
        if (parentNode is null || parentViewModel is null)
        {
            return;
        }

        var insertIndex = parentNode.Children.IndexOf(targetNode.Node);
        if (insertIndex < 0)
        {
            insertIndex = insertAfter ? parentNode.Children.Count : 0;
        }
        else if (insertAfter)
        {
            insertIndex++;
        }

        var newNode = new UiNode(toolName);
        parentNode.Children.Insert(insertIndex, newNode);
        var newNodeViewModel = CreateTree(newNode);
        parentViewModel.Children.Insert(insertIndex, newNodeViewModel);
        parentViewModel.IsExpanded = true;
        SelectedNode = newNodeViewModel;
        MarkDocumentChanged();
    }

    public void MoveNodeAsChild(NodeViewModel? draggedNode, NodeViewModel? targetNode)
    {
        if (!CanMoveNodeAsChild(draggedNode, targetNode) || draggedNode is null || targetNode is null)
        {
            return;
        }

        var sourceParent = FindParent(_document.Root, draggedNode.Node);
        var sourceParentViewModel = FindParentViewModel(RootNodes[0], draggedNode);
        if (sourceParent is null || sourceParentViewModel is null)
        {
            return;
        }

        sourceParent.Children.Remove(draggedNode.Node);
        sourceParentViewModel.Children.Remove(draggedNode);

        targetNode.Node.Children.Add(draggedNode.Node);
        targetNode.Children.Add(draggedNode);
        targetNode.IsExpanded = true;
        SelectedNode = draggedNode;
        MarkDocumentChanged();
    }

    public void MoveNodeAsSibling(NodeViewModel? draggedNode, NodeViewModel? targetNode)
    {
        MoveNodeRelativeToSibling(draggedNode, targetNode, insertAfter: true);
    }

    public void MoveNodeBefore(NodeViewModel? draggedNode, NodeViewModel? targetNode)
    {
        MoveNodeRelativeToSibling(draggedNode, targetNode, insertAfter: false);
    }

    public void MoveNodeAfter(NodeViewModel? draggedNode, NodeViewModel? targetNode)
    {
        MoveNodeRelativeToSibling(draggedNode, targetNode, insertAfter: true);
    }

    private void MoveNodeRelativeToSibling(NodeViewModel? draggedNode, NodeViewModel? targetNode, bool insertAfter)
    {
        if (!CanMoveNodeAsSibling(draggedNode, targetNode) || draggedNode is null || targetNode is null)
        {
            return;
        }

        var sourceParent = FindParent(_document.Root, draggedNode.Node);
        var sourceParentViewModel = FindParentViewModel(RootNodes[0], draggedNode);
        var targetParent = FindParent(_document.Root, targetNode.Node);
        var targetParentViewModel = FindParentViewModel(RootNodes[0], targetNode);
        if (sourceParent is null || sourceParentViewModel is null || targetParent is null || targetParentViewModel is null)
        {
            return;
        }

        sourceParent.Children.Remove(draggedNode.Node);
        sourceParentViewModel.Children.Remove(draggedNode);

        var insertIndex = targetParent.Children.FindIndex(x => x.Id == targetNode.Node.Id);
        if (insertIndex < 0)
        {
            insertIndex = targetParent.Children.Count;
        }
        else if (insertAfter)
        {
            insertIndex++;
        }

        targetParent.Children.Insert(insertIndex, draggedNode.Node);
        targetParentViewModel.Children.Insert(insertIndex, draggedNode);
        targetParentViewModel.IsExpanded = true;
        SelectedNode = draggedNode;
        MarkDocumentChanged();
    }

    public void DeleteSelectedNode()
    {
        if (SelectedNode is null || SelectedNode.Node == _document.Root)
        {
            return;
        }

        var parent = FindParent(_document.Root, SelectedNode.Node);
        if (parent is null)
        {
            return;
        }

        parent.Children.Remove(SelectedNode.Node);
        var parentViewModel = FindParentViewModel(RootNodes[0], SelectedNode);
        if (parentViewModel is null)
        {
            return;
        }

        parentViewModel.Children.Remove(SelectedNode);
        SelectedNode = parentViewModel;
        MarkDocumentChanged();
    }

    public void DuplicateSelectedNode()
    {
        if (SelectedNode is null)
        {
            return;
        }

        var parent = FindParent(_document.Root, SelectedNode.Node);
        if (parent is null)
        {
            return;
        }

        var clone = CloneNode(SelectedNode.Node);
        parent.Children.Add(clone);
        var parentViewModel = FindParentViewModel(RootNodes[0], SelectedNode);
        if (parentViewModel is null)
        {
            return;
        }

        var cloneViewModel = CreateTree(clone);
        parentViewModel.Children.Add(cloneViewModel);
        SelectedNode = cloneViewModel;
        MarkDocumentChanged();
    }

    public void MoveSelectedNodeUp()
    {
        MoveSelectedNode(-1);
    }

    public void MoveSelectedNodeDown()
    {
        MoveSelectedNode(1);
    }

    public void DecreasePreviewZoom()
    {
        SetPreviewZoomPercent(PreviewZoomPercent - 25);
    }

    public void IncreasePreviewZoom()
    {
        SetPreviewZoomPercent(PreviewZoomPercent + 25);
    }

    public void RenderPreviewNow()
    {
        CancelPendingAutoRender();
        _ = RenderPreviewNowAsync(forceFullReload: true);
    }

    public string ExportDocumentJson() => _jsonDocumentSerializer.Serialize(_document);

    public LvglCGenerationResult ExportLvglC(string unitName = "ui_main") => _lvglCGenerator.Generate(_document, unitName);

    public McuDisplayCodeGenerationResult ExportMcuDisplayCode(string unitName = "ui_start") => _mcuDisplayCodeGenerator.Generate(_document, unitName);

    public string ExportPreviewCode()
    {
        var generated = ExportLvglC("ui_preview");
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            $"// {generated.HeaderFileName}",
            generated.HeaderCode.TrimEnd(),
            $"// {generated.SourceFileName}",
            generated.SourceCode.TrimEnd());
    }

    public bool TryLoadDocumentJson(string json, string? sourcePath, out string? errorMessage)
    {
        try
        {
            var parsedDocument = _jsonDocumentParser.Parse(json);
            _document.Root = parsedDocument.Root;
            CurrentDocumentPath = sourcePath;
            EditorState.IsDirty = false;
            EditorState.IsPreviewOutOfDate = true;
            EditorState.SetPreviewStatus(sourcePath is null
                ? Ui("message.document_loaded")
                : string.Format(
                    Ui("message.document_loaded_named"),
                    Path.GetFileName(sourcePath)));
            RebuildTree(_document.Root.Id);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void MarkDocumentSaved(string? targetPath)
    {
        CurrentDocumentPath = targetPath;
        EditorState.IsDirty = false;
        EditorState.SetPreviewStatus(targetPath is null
            ? Ui("message.document_saved")
            : string.Format(
                Ui("message.document_saved_named"),
                Path.GetFileName(targetPath)));
    }

    public bool TryGenerateMcuDisplaySources(out string? statusMessage)
    {
        statusMessage = null;

        var projectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(_projectSettings.ProjectDirectory);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            statusMessage = Ui("error.project_directory_required");
            return false;
        }

        var screensDirectory = Path.Combine(projectDirectory, "screens");
        if (!Directory.Exists(screensDirectory))
        {
            statusMessage = string.Format(
                Ui("error.screens_directory_missing"),
                screensDirectory);
            return false;
        }

        var outputDirectory = _projectSettings.OutputDirectory;
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Path.Combine(projectDirectory, "build");
        }

        try
        {
            Directory.CreateDirectory(outputDirectory);

            var screenFiles = Directory
                .EnumerateFiles(screensDirectory, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (screenFiles.Count == 0)
            {
                statusMessage = string.Format(
                    Ui("message.no_screen_files_found"),
                    screensDirectory);
                return false;
            }

            foreach (var screenFile in screenFiles)
            {
                var json = File.ReadAllText(screenFile);
                var document = _jsonDocumentParser.Parse(json);
                var unitName = CreateMcuUnitName(screensDirectory, screenFile);
                if (string.Equals(_projectSettings.ProjectTemplate, "RTOS-Messages", StringComparison.OrdinalIgnoreCase))
                {
                    var generatedTemplate = _rtosMessagesCodeGenerator.Generate(document, unitName);
                    var generatedDisplay = _mcuDisplayCodeGenerator.Generate(document, unitName, generatedTemplate.ExportedObjectIds);
                    var sourceCode = InjectDisplayBindings(generatedDisplay.SourceCode, generatedTemplate.ContractHeaderFileName, generatedTemplate.BindingCallCode);

                    File.WriteAllText(Path.Combine(outputDirectory, generatedDisplay.HeaderFileName), generatedDisplay.HeaderCode);
                    File.WriteAllText(Path.Combine(outputDirectory, generatedDisplay.SourceFileName), sourceCode);
                    File.WriteAllText(Path.Combine(outputDirectory, generatedTemplate.ContractHeaderFileName), generatedTemplate.ContractHeaderCode);
                    File.WriteAllText(Path.Combine(outputDirectory, generatedTemplate.EventSourceFileName), generatedTemplate.EventSourceCode);
                    File.WriteAllText(Path.Combine(outputDirectory, generatedTemplate.UpdateSourceFileName), generatedTemplate.UpdateSourceCode);
                }
                else
                {
                    var generated = _mcuDisplayCodeGenerator.Generate(document, unitName);
                    var generatedEvents = _mcuEventCodeGenerator.Generate(document, unitName);
                    var generatedUpdates = _mcuUpdateCodeGenerator.Generate(document, unitName);
                    var sourceCode = generatedEvents.HasBindings
                        ? InjectDisplayBindings(generated.SourceCode, generatedEvents.HeaderFileName, generatedEvents.BindingCallCode)
                        : generated.SourceCode;

                    File.WriteAllText(Path.Combine(outputDirectory, generated.HeaderFileName), generated.HeaderCode);
                    File.WriteAllText(Path.Combine(outputDirectory, generated.SourceFileName), sourceCode);
                    if (generatedEvents.HasBindings)
                    {
                        File.WriteAllText(Path.Combine(outputDirectory, generatedEvents.HeaderFileName), generatedEvents.HeaderCode);
                        File.WriteAllText(Path.Combine(outputDirectory, generatedEvents.SourceFileName), generatedEvents.SourceCode);
                    }
                    else
                    {
                        TryDeleteGeneratedFile(outputDirectory, generatedEvents.HeaderFileName);
                        TryDeleteGeneratedFile(outputDirectory, generatedEvents.SourceFileName);
                    }

                    if (generatedUpdates.HasTargets)
                    {
                        File.WriteAllText(Path.Combine(outputDirectory, generatedUpdates.HeaderFileName), generatedUpdates.HeaderCode);
                        File.WriteAllText(Path.Combine(outputDirectory, generatedUpdates.SourceFileName), generatedUpdates.SourceCode);
                    }
                    else
                    {
                        TryDeleteGeneratedFile(outputDirectory, generatedUpdates.HeaderFileName);
                        TryDeleteGeneratedFile(outputDirectory, generatedUpdates.SourceFileName);
                    }
                }
            }

            statusMessage = string.Format(
                Ui("message.mcu_code_generated"),
                screenFiles.Count,
                outputDirectory);
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = string.Format(
                Ui("error.mcu_code_generation_failed"),
                ex.Message);
            return false;
        }
    }

    private static string InjectDisplayBindings(string displaySourceCode, string includeFileName, string bindingCallCode)
    {
        var eventInclude = $"#include \"{includeFileName}\"";
        var result = displaySourceCode.Contains(eventInclude, StringComparison.Ordinal)
            ? displaySourceCode
            : displaySourceCode.Replace("#include \"lvgl.h\"", eventInclude + Environment.NewLine + "#include \"lvgl.h\"", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(bindingCallCode))
        {
            return result;
        }

        return result.Replace(
            "    lv_screen_load(screen);",
            bindingCallCode + Environment.NewLine + Environment.NewLine + "    lv_screen_load(screen);",
            StringComparison.Ordinal);
    }

    private static void TryDeleteGeneratedFile(string outputDirectory, string fileName)
    {
        var fullPath = Path.Combine(outputDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            return;
        }

        File.Delete(fullPath);
    }

    private static string CreateMcuUnitName(string screensDirectory, string screenFile)
    {
        var relativePath = Path.GetRelativePath(screensDirectory, screenFile);
        var withoutExtension = Path.ChangeExtension(relativePath, null) ?? Path.GetFileNameWithoutExtension(screenFile);
        var normalized = withoutExtension
            .Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_')
            .Replace(' ', '_');

        return normalized;
    }

    public bool TryCreateAndLoadNewScreen(string targetPath, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            var screenName = Path.GetFileNameWithoutExtension(targetPath);
            var json = ProjectScaffoldService.CreateDefaultScreenJson(_projectSettings, screenName);
            File.WriteAllText(targetPath, json);

            RegisterScreenFileInProject(targetPath);

            if (!TryLoadDocumentJson(json, targetPath, out errorMessage))
            {
                return false;
            }

            EditorState.SetPreviewStatus(string.Format(NewScreenCreatedFormat, Path.GetFileName(targetPath)));
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private void RegisterScreenFileInProject(string screenPath)
    {
        var projectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(_projectSettings.ProjectDirectory);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return;
        }

        string registeredPath;
        try
        {
            var fullProjectDirectory = Path.GetFullPath(projectDirectory);
            var fullScreenPath = Path.GetFullPath(screenPath);
            registeredPath = Path.GetRelativePath(fullProjectDirectory, fullScreenPath);
        }
        catch
        {
            registeredPath = screenPath;
        }

        var entries = _projectSettings.ScreenFiles
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (!entries.Any(x => string.Equals(x, registeredPath, StringComparison.OrdinalIgnoreCase)))
        {
            entries.Add(registeredPath);
            _projectSettings.ScreenFiles = string.Join(Environment.NewLine, entries);
            SaveProjectSettingsIfPossible();
        }
    }

    private void SaveProjectSettingsIfPossible()
    {
        if (string.IsNullOrWhiteSpace(CurrentProjectFilePath))
        {
            return;
        }

        var json = ProjectFileSerializer.Serialize(_projectSettings);
        File.WriteAllText(CurrentProjectFilePath, json);
    }

    public void ShowPropertiesPanel()
    {
        SelectedPropertyTabIndex = 0;
    }

    public void ShowEventsPanel()
    {
        SelectedPropertyTabIndex = 1;
    }

    public void TogglePropertySortMode()
    {
        PropertySortMode = IsCategorizedPropertySort ? "Alphabetisch" : "Kategorisiert";
    }

    public void ToggleToolboxSortMode()
    {
        ToolboxSortMode = IsGroupedToolboxSort ? "Alphabetisch" : "Gruppiert";
    }

    public void ToggleDocumentationPanel()
    {
        IsDocumentationPanelVisible = !IsDocumentationPanelVisible;
    }

    public void ToggleAllTreeNodes()
    {
        var expand = !AreAllTreeNodesExpanded;

        foreach (var root in RootNodes)
        {
            root.SetExpandedRecursive(expand);
        }

        RaisePropertyChanged(nameof(AreAllTreeNodesExpanded));
        RaisePropertyChanged(nameof(TreeExpandCollapseGlyph));
        RaisePropertyChanged(nameof(TreeExpandCollapseHint));
    }

    public void SelectValidationTab() => SelectedDiagnosticsTabIndex = 0;

    public void SelectTechnicalLogTab() => SelectedDiagnosticsTabIndex = 1;

    public void SelectEventCallbacksTab() => SelectedDiagnosticsTabIndex = 2;

    public void SelectJsonPreviewTab() => SelectedDiagnosticsTabIndex = 3;

    public async Task ShutdownPreviewAsync()
    {
        CancelPendingAutoRender();
        try
        {
            await _previewService.DisconnectAsync();
            EditorState.IsSimulatorConnected = false;
            EditorState.SetPreviewStatus(Ui("message.preview_process_ended"));
        }
        catch (Exception ex)
        {
            EditorState.AppendPreviewLog(string.Format(
                Ui("message.preview_process_shutdown_failed"),
                ex.Message));
        }
    }

    public async Task RenderPreviewNowAsync(bool forceFullReload, bool resetWindowToTargetSize = false)
    {
        JsonPreview = ExportDocumentJson();
        var previewCode = ExportPreviewCode();

        var request = new PreviewRenderRequest(
            previewCode,
            _document.Root.Attributes.TryGetValue("name", out var documentName) && !string.IsNullOrWhiteSpace(documentName)
                ? documentName
                : "unnamed",
            forceFullReload,
            TryGetAbsoluteScreenDimension(_document.Root, "width"),
            TryGetAbsoluteScreenDimension(_document.Root, "height"),
            null,
            resetWindowToTargetSize);

        var result = await _previewService.RenderAsync(request);
        EditorState.IsSimulatorConnected = result.IsConnected;
        EditorState.PreviewBackend = _previewService.BackendName;
        EditorState.IsPreviewOutOfDate = !result.Success;
        EditorState.SetPreviewStatus(result.StatusMessage);

        if (!string.IsNullOrWhiteSpace(_selectedNodeHighlightId))
        {
            _ = _previewService.HighlightAsync(_selectedNodeHighlightId);
        }
    }

    private void HandlePreviewLogReceived(object? sender, string line)
    {
        Dispatcher.UIThread.Post(() => EditorState.AppendPreviewLog(line));
    }

    private void HandleEditorStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(EditorStateViewModel.IsSimulatorConnected), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(EditorStateViewModel.IsDirty), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(EditorStateViewModel.PreviewBackend), StringComparison.Ordinal))
        {
            RaisePropertyChanged(nameof(StatusSimulatorText));
            RaisePropertyChanged(nameof(StatusChangesText));
            RaisePropertyChanged(nameof(StatusBackendText));
        }
    }

    private void ApplyEditorStateLocalization()
    {
        EditorState.ConfigureLocalizedTexts(
            Ui("state.empty_log"),
            Ui("state.no_simulator"),
            Ui("state.preview_not_rendered"));
    }

    private void RaiseLocalizedTextPropertiesChanged()
    {
        RaisePropertyChanged(nameof(WindowTitle));
        RaisePropertyChanged(nameof(ToolbarTitle));
        RaisePropertyChanged(nameof(ToolbarDraftBadge));
        RaisePropertyChanged(nameof(ToolbarOpenLabel));
        RaisePropertyChanged(nameof(ToolbarSaveLabel));
        RaisePropertyChanged(nameof(ToolbarProjectLabel));
        RaisePropertyChanged(nameof(ToolbarProjectTooltip));
        RaisePropertyChanged(nameof(ToolbarExitTooltip));
        RaisePropertyChanged(nameof(ToolbarThemeLabel));
        RaisePropertyChanged(nameof(ToolbarThemeTooltip));
        RaisePropertyChanged(nameof(ToolbarLvConfLabel));
        RaisePropertyChanged(nameof(ToolbarLvConfTooltip));
        RaisePropertyChanged(nameof(ToolbarGenerateCodeTooltip));
        RaisePropertyChanged(nameof(ToolbarLvglLabel));
        RaisePropertyChanged(nameof(ToolbarLvglActiveLabel));
        RaisePropertyChanged(nameof(ToolbarModeLabel));
        RaisePropertyChanged(nameof(ToolbarLanguageLabel));
        RaisePropertyChanged(nameof(ToolbarPreviewResetSizeTooltip));
        RaisePropertyChanged(nameof(ToolbarPreviewZoomLabel));
        RaisePropertyChanged(nameof(ToolbarBackendLabel));
        RaisePropertyChanged(nameof(ToolbarAutoRenderLabel));
        RaisePropertyChanged(nameof(ToolbarRenderLabel));
        RaisePropertyChanged(nameof(ToolbarOpenTooltip));
        RaisePropertyChanged(nameof(ToolbarNewTooltip));
        RaisePropertyChanged(nameof(ToolbarSaveTooltip));
        RaisePropertyChanged(nameof(ToolbarAddElementTooltip));
        RaisePropertyChanged(nameof(ToolbarDuplicateTooltip));
        RaisePropertyChanged(nameof(ToolbarDeleteTooltip));
        RaisePropertyChanged(nameof(ToolbarMoveUpTooltip));
        RaisePropertyChanged(nameof(ToolbarMoveDownTooltip));
        RaisePropertyChanged(nameof(ToolbarRenderTooltip));
        RaisePropertyChanged(nameof(ToolbarPreviewZoomOutTooltip));
        RaisePropertyChanged(nameof(ToolbarPreviewZoomInTooltip));
        RaisePropertyChanged(nameof(ToolboxTitle));
        RaisePropertyChanged(nameof(ToolboxContextHint));
        RaisePropertyChanged(nameof(ToolboxUnsupportedLabel));
        RaisePropertyChanged(nameof(ToolboxAddElementLabel));
        RaisePropertyChanged(nameof(ToolboxDuplicateLabel));
        RaisePropertyChanged(nameof(ToolboxDeleteLabel));
        RaisePropertyChanged(nameof(ToolboxMoveUpLabel));
        RaisePropertyChanged(nameof(ToolboxMoveDownLabel));
        RaisePropertyChanged(nameof(ToolboxStrictLabel));
        RaisePropertyChanged(nameof(StructureTitle));
        RaisePropertyChanged(nameof(PropertiesTitle));
        RaisePropertyChanged(nameof(DocumentationTitle));
        RaisePropertyChanged(nameof(DocumentationUrl));
        RaisePropertyChanged(nameof(DocumentationPlaceholderTitle));
        RaisePropertyChanged(nameof(DocumentationPlaceholderText));
        RaisePropertyChanged(nameof(DocumentationPanelToggleGlyph));
        RaisePropertyChanged(nameof(DocumentationPanelToggleTooltip));
        RaisePropertyChanged(nameof(DiagnosticsTitle));
        RaisePropertyChanged(nameof(PropertiesTabLabel));
        RaisePropertyChanged(nameof(EventsTabLabel));
        RaisePropertyChanged(nameof(PropertyBoolActiveLabel));
        RaisePropertyChanged(nameof(ValidationTabLabel));
        RaisePropertyChanged(nameof(TechnicalLogTabLabel));
        RaisePropertyChanged(nameof(EventCallbacksTabLabel));
        RaisePropertyChanged(nameof(JsonPreviewTabLabel));
        RaisePropertyChanged(nameof(DiagnosticsClearLogLabel));
        RaisePropertyChanged(nameof(PropertySortHint));
        RaisePropertyChanged(nameof(ToolboxSortHint));
        RaisePropertyChanged(nameof(TreeExpandCollapseHint));
        RaisePropertyChanged(nameof(StatusSimulatorText));
        RaisePropertyChanged(nameof(StatusChangesText));
        RaisePropertyChanged(nameof(StatusBackendText));
        RaisePropertyChanged(nameof(StatusLvglText));
        RaisePropertyChanged(nameof(StatusModeText));
        RaisePropertyChanged(nameof(StatusStrictText));
        RaisePropertyChanged(nameof(OpenDialogTitle));
        RaisePropertyChanged(nameof(NewScreenDialogTitle));
        RaisePropertyChanged(nameof(SaveDialogTitle));
        RaisePropertyChanged(nameof(ScreenJsonFileTypeLabel));
        RaisePropertyChanged(nameof(ErrorFileLoadFailedFormat));
        RaisePropertyChanged(nameof(ErrorSaveTargetUnavailableText));
        RaisePropertyChanged(nameof(ProjectDialogTitle));
        RaisePropertyChanged(nameof(ThemeDialogTitle));
        RaisePropertyChanged(nameof(ThemeDialogFileLabel));
        RaisePropertyChanged(nameof(ThemeDialogPropertyHeader));
        RaisePropertyChanged(nameof(ThemeDialogValueHeader));
        RaisePropertyChanged(nameof(ThemeDialogDescriptionHeader));
        RaisePropertyChanged(nameof(ThemeDialogSaveLabel));
        RaisePropertyChanged(nameof(LvConfDialogTitle));
        RaisePropertyChanged(nameof(LvConfDialogFileLabel));
        RaisePropertyChanged(nameof(LvConfDialogPropertyHeader));
        RaisePropertyChanged(nameof(LvConfDialogValueHeader));
        RaisePropertyChanged(nameof(LvConfDialogDescriptionHeader));
        RaisePropertyChanged(nameof(LvConfDialogSaveLabel));
        RaisePropertyChanged(nameof(DialogCloseLabel));
        RaisePropertyChanged(nameof(ThemeDialogPlaceholderText));
        RaisePropertyChanged(nameof(ThemeDialogSaveSuccessFormat));
        RaisePropertyChanged(nameof(ThemeDialogSaveFailedFormat));
        RaisePropertyChanged(nameof(ThemeDialogLoadFailedFormat));
        RaisePropertyChanged(nameof(LvConfDialogPlaceholderText));
        RaisePropertyChanged(nameof(LvConfDialogSaveSuccessFormat));
        RaisePropertyChanged(nameof(LvConfDialogSaveFailedFormat));
        RaisePropertyChanged(nameof(LvConfDialogLoadFailedFormat));
    }

    private async Task SwitchPreviewBackendAsync(string backendLabel)
    {
        CancelPendingAutoRender();
        var previousService = _previewService;
        previousService.LogReceived -= HandlePreviewLogReceived;

        try
        {
            await previousService.DisconnectAsync();
        }
        catch (Exception ex)
        {
            EditorState.AppendPreviewLog(string.Format(
                Ui("message.preview_backend_disconnect_failed"),
                ex.Message));
        }

        _previewService = PreviewBackendCatalog.CreateService(backendLabel);
        _previewService.LogReceived += HandlePreviewLogReceived;

        EditorState.IsSimulatorConnected = false;
        EditorState.IsPreviewOutOfDate = true;
        EditorState.PreviewBackend = _previewService.BackendName;
        EditorState.SetPreviewStatus(string.Format(
            Ui("message.preview_backend_switched"),
            _previewService.BackendName));
        EditorState.AppendPreviewLog(string.Format(
            Ui("message.preview_backend_switched_log"),
            _previewService.BackendName));

        await RefreshDocumentStateAsync();
    }

    public void ToggleToolboxGroup(string? groupKey, bool currentlyExpanded)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            return;
        }

        _toolboxGroupState[groupKey] = !currentlyExpanded;
        RefreshSelectionState();
    }

    public void TogglePropertyGroup(string? groupKey, bool currentlyExpanded)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            return;
        }

        _propertyGroupState[groupKey] = !currentlyExpanded;
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        var previouslySelectedToolName = SelectedToolboxItem?.ToolName;
        ToolboxItems.Clear();
        PropertyGridItems.Clear();
        Properties.Clear();
        RaisePropertyChanged(nameof(AreAllTreeNodesExpanded));
        RaisePropertyChanged(nameof(TreeExpandCollapseGlyph));
        RaisePropertyChanged(nameof(TreeExpandCollapseHint));

        if (!IsProjectLoaded || SelectedNode is null)
        {
            return;
        }

        BuildToolboxItems();

        var lvglDefinition = ResolveLvglElement(SelectedNode.Node.ElementName);
        if (lvglDefinition is not null)
        {
            foreach (var attribute in lvglDefinition.Attributes)
            {
                if (ShouldSkipAttributeEditor(attribute))
                {
                    continue;
                }

                SelectedNode.Node.Attributes.TryGetValue(attribute.LvglName, out var value);
                _lvglMetaRegistry.TryGetAttributeType(attribute.TypeName, out var typeDefinition);
                var allowedValues = attribute.AllowedValues?.Count > 0
                    ? attribute.AllowedValues
                    : typeDefinition?.AllowedValues;

                Properties.Add(
                    new AttributeEditorViewModel(
                        attribute.Name,
                        attribute.LvglName,
                        attribute.DisplayName,
                        value,
                        attribute.TypeName,
                        GetPropertyCategory(attribute.Name, attribute.LvglName, attribute.TypeName),
                        BuildAttributeHint(attribute.Name, attribute.LvglName, attribute.TypeName, attribute.IsRequired),
                        attribute.IsRequired,
                        allowedValues,
                        attribute.Supported,
                        Ui("editor.empty"),
                        RequirementRequiredLabel,
                        RequirementOptionalLabel,
                        BuildAllowedValueLabels(attribute.TypeName),
                        IsMcuRelatedAttribute(attribute.Name, attribute.LvglName),
                        UpdateAttribute));
            }
        }
        else if (_registry.TryGet(SelectedNode.Node.ElementName, out var definition) && definition is not null)
        {
            foreach (var attribute in definition.Attributes)
            {
                SelectedNode.Node.Attributes.TryGetValue(attribute.Name, out var value);
                _registry.TryGetAttributeType(attribute.TypeName, out var typeDefinition);
                var allowedValues = attribute.AllowedValues?.Count > 0
                    ? attribute.AllowedValues
                    : typeDefinition?.AllowedValues;

                Properties.Add(
                    new AttributeEditorViewModel(
                        attribute.Name,
                        attribute.Name,
                        _localizationCatalog.GetAttributeLabel(attribute.Name) ??
                        AttributeEditorViewModel.CreateFallbackDisplayName(attribute.Name),
                        value,
                        attribute.TypeName,
                        GetPropertyCategory(attribute.Name, attribute.Name, attribute.TypeName),
                        BuildAttributeHint(attribute.Name, attribute.Name, attribute.TypeName, attribute.IsRequired),
                        attribute.IsRequired,
                        allowedValues,
                        true,
                        Ui("editor.empty"),
                        RequirementRequiredLabel,
                        RequirementOptionalLabel,
                        BuildAllowedValueLabels(attribute.TypeName),
                        IsMcuRelatedAttribute(attribute.Name, attribute.Name),
                        UpdateAttribute));
            }
        }

        AddEventEditors(lvglDefinition);
        AddRtosMessagesEventEditors();

        BuildPropertyGridItems();
        if (!SelectFirstMatchingToolboxItem(_toolboxSearchText))
        {
            var matchingToolboxItem = !string.IsNullOrWhiteSpace(previouslySelectedToolName)
                ? ToolboxItems.FirstOrDefault(x => x.IsSelectable &&
                                                   string.Equals(x.ToolName, previouslySelectedToolName, StringComparison.Ordinal))
                : null;

            SelectedToolboxItem = matchingToolboxItem;
            if (SelectedToolboxItem is not { IsSelectable: true })
            {
                SelectedToolboxItem = ToolboxItems.FirstOrDefault(x => x.IsSelectable);
            }
        }

        SelectedTool = SelectedToolboxItem?.ToolName;
        _ = RefreshDocumentStateAsync();
    }

    private bool SelectFirstMatchingToolboxItem(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return false;
        }

        var term = searchText.Trim();
        var selectableItems = ToolboxItems.Where(x => x.IsSelectable).ToArray();

        static string NormalizeLabel(ToolboxItemViewModel item)
        {
            var label = item.Label.TrimStart();
            var parenIndex = label.IndexOf(" (", StringComparison.Ordinal);
            return parenIndex > 0 ? label[..parenIndex] : label;
        }

        var match =
            selectableItems.FirstOrDefault(x => NormalizeLabel(x).StartsWith(term, StringComparison.OrdinalIgnoreCase)) ??
            selectableItems.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToolName) &&
                                                x.ToolName.StartsWith(term, StringComparison.OrdinalIgnoreCase)) ??
            selectableItems.FirstOrDefault(x => NormalizeLabel(x).Contains(term, StringComparison.OrdinalIgnoreCase)) ??
            selectableItems.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToolName) &&
                                                x.ToolName.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return false;
        }

        SelectedToolboxItem = match;
        return true;
    }

    private void UpdateAttribute(string attributeName, string? value)
    {
        if (SelectedNode is null)
        {
            return;
        }

        if (TryUpdateEventBinding(attributeName, value))
        {
            SynchronizeMirroredEditors(attributeName, value);
            SelectedNode.Refresh();
            MarkDocumentChanged();
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            SelectedNode.Node.Attributes.Remove(attributeName);
        }
        else
        {
            SelectedNode.Node.Attributes[attributeName] = value;
        }

        if (string.Equals(attributeName, "border_color", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(value))
        {
            if (!SelectedNode.Node.Attributes.TryGetValue("border_width", out var borderWidthValue) ||
                string.IsNullOrWhiteSpace(borderWidthValue))
            {
                SelectedNode.Node.Attributes["border_width"] = "1";
            }
        }

        SynchronizeMirroredEditors(attributeName, value);
        SelectedNode.Refresh();
        MarkDocumentChanged();
    }

    private void AddEventEditors(LvglElementDefinition? lvglDefinition)
    {
        if (SelectedNode is null || lvglDefinition is null || lvglDefinition.Events.Count == 0)
        {
            return;
        }

        foreach (var eventDefinition in lvglDefinition.Events)
        {
            var binding = SelectedNode.Node.Events.FirstOrDefault(x => string.Equals(x.Name, eventDefinition.Name, StringComparison.OrdinalIgnoreCase));

            foreach (var attribute in eventDefinition.Attributes)
            {
                if (ShouldSkipEventAttributeEditor(attribute))
                {
                    continue;
                }

                var value = binding is not null && binding.Attributes.TryGetValue(attribute.LvglName, out var eventValue)
                    ? eventValue
                    : null;
                _lvglMetaRegistry.TryGetAttributeType(attribute.TypeName, out var typeDefinition);
                var allowedValues = attribute.AllowedValues?.Count > 0
                    ? attribute.AllowedValues
                    : typeDefinition?.AllowedValues;

                var displayName = string.Equals(attribute.Name, "callback", StringComparison.OrdinalIgnoreCase)
                    ? eventDefinition.DisplayName
                    : AttributeEditorViewModel.CreateFallbackDisplayName(attribute.Name);

                Properties.Add(
                    new AttributeEditorViewModel(
                        $"__event__{eventDefinition.Name}__{attribute.Name}",
                        $"__event__{eventDefinition.Name}__{attribute.LvglName}",
                        displayName,
                        value,
                        attribute.TypeName,
                        eventDefinition.DisplayName,
                        BuildAttributeHint(attribute.Name, attribute.LvglName, attribute.TypeName, attribute.IsRequired),
                        attribute.IsRequired,
                        allowedValues,
                        attribute.Supported,
                        Ui("editor.empty"),
                        RequirementRequiredLabel,
                        RequirementOptionalLabel,
                        BuildAllowedValueLabels(attribute.TypeName),
                        IsMcuRelatedAttribute(attribute.Name, attribute.LvglName),
                        UpdateAttribute));
            }
        }
    }

    private void AddRtosMessagesEventEditors()
    {
        if (!IsRtosMessagesTemplateActive() || !SelectedNodeSupportsEvents() || SelectedNode is null)
        {
            return;
        }

        SelectedNode.Node.Attributes.TryGetValue("id", out var idValue);
        var firstEventIndex = Properties
            .Select((editor, index) => new { editor, index })
            .FirstOrDefault(x => x.editor.Name.StartsWith("__event__", StringComparison.OrdinalIgnoreCase))
            ?.index ?? Properties.Count;

        Properties.Insert(
            firstEventIndex,
            new AttributeEditorViewModel(
                "__rtos_event_id",
                "id",
                _localizationCatalog.GetAttributeLabel("id") ?? "Id",
                idValue,
                "string",
                "Data",
                BuildAttributeHint("id", "id", "string", false),
                false,
                null,
                true,
                Ui("editor.empty"),
                RequirementRequiredLabel,
                RequirementOptionalLabel,
                BuildAllowedValueLabels("string"),
                true,
                UpdateAttribute));
    }

    private void SynchronizeMirroredEditors(string attributeName, string? value)
    {
        foreach (var editor in Properties.Where(x => string.Equals(x.StorageName, attributeName, StringComparison.OrdinalIgnoreCase)))
        {
            editor.SetValueFromModel(value);
        }
    }

    private static bool IsMcuRelatedAttribute(string? name, string? storageName)
    {
        var candidate = !string.IsNullOrWhiteSpace(storageName) ? storageName : name;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return string.Equals(candidate, "id", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "useUpdate", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "callback", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "action", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "parameter", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "eventGroup", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "eventType", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidate, "useMessages", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryUpdateEventBinding(string storageName, string? value)
    {
        if (SelectedNode is null || !storageName.StartsWith("__event__", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = storageName.Split(new[] { "__" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !string.Equals(parts[0], "event", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var eventName = parts[1];
        var attributeName = parts[2];
        var binding = SelectedNode.Node.Events.FirstOrDefault(x => string.Equals(x.Name, eventName, StringComparison.OrdinalIgnoreCase));
        if (binding is null)
        {
            binding = new UiEventBinding(eventName);
            SelectedNode.Node.Events.Add(binding);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            binding.Attributes.Remove(attributeName);
            if (binding.Attributes.Count == 0)
            {
                SelectedNode.Node.Events.Remove(binding);
            }
        }
        else
        {
            binding.Attributes[attributeName] = value;
        }

        return true;
    }

    public void CommitPendingPropertyEdits()
    {
        foreach (var property in Properties)
        {
            property.CommitEditValue();
        }
    }

    private async Task RefreshDocumentStateAsync()
    {
        if (!IsProjectLoaded)
        {
            return;
        }

        var result = _validator.Validate(_document, StrictValidation, _projectSettings.ProjectTemplate);
        ValidationSummary = result.IsValid
            ? Ui("message.validation_ok")
            : string.Join(Environment.NewLine, result.Errors);

        if (EditorState.AutoRenderEnabled)
        {
            JsonPreview = ExportDocumentJson();
            ScheduleAutoRender();
        }
        else
        {
            EditorState.IsPreviewOutOfDate = true;
            EditorState.SetPreviewStatus(Ui("message.preview_stale"));
            EditorState.IsSimulatorConnected = _previewService.IsConnected;
            EditorState.PreviewBackend = _previewService.BackendName;
            JsonPreview = ExportDocumentJson();
        }
    }

    private void ScheduleAutoRender()
    {
        CancelPendingAutoRender();

        var cts = new CancellationTokenSource();
        _autoRenderDebounceCts = cts;
        _ = DebouncedAutoRenderAsync(cts.Token);
    }

    private async Task DebouncedAutoRenderAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await RenderPreviewNowAsync(forceFullReload: false);
        }
        catch (OperationCanceledException)
        {
            // A newer edit superseded this auto-render request.
        }
    }

    private void CancelPendingAutoRender()
    {
        _autoRenderDebounceCts?.Cancel();
        _autoRenderDebounceCts?.Dispose();
        _autoRenderDebounceCts = null;
    }

    private void SetPreviewZoomPercent(int value)
    {
        var normalized = value switch
        {
            <= 50 => 50,
            <= 75 => 75,
            <= 100 => 100,
            <= 125 => 125,
            _ => 150
        };

        PreviewZoomPercent = normalized;
    }

    public void ResetPreviewZoomToOriginalSize()
    {
        PreviewZoomPercent = 100;
        _ = RenderPreviewNowAsync(forceFullReload: false, resetWindowToTargetSize: true);
    }

    private static UiNode CreateInitialDocument()
    {
        var screen = new UiNode("screen");
        screen.Attributes["name"] = "main_screen";
        screen.Attributes["width"] = "1280";
        screen.Attributes["height"] = "720";
        return screen;
    }

    private void MarkDocumentChanged(bool rebuildTree = false, Guid? selectedNodeId = null)
    {
        EditorState.IsDirty = true;
        EditorState.IsPreviewOutOfDate = true;
        EditorState.SetPreviewStatus(EditorState.AutoRenderEnabled
            ? Ui("message.preview_current")
            : Ui("message.preview_stale"));

        if (rebuildTree)
        {
            RebuildTree(selectedNodeId ?? SelectedNode?.Node.Id);
        }

        _ = RefreshDocumentStateAsync();
    }

    private static int? TryGetAbsoluteScreenDimension(UiNode node, string attributeName)
    {
        if (!node.Attributes.TryGetValue(attributeName, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return int.TryParse(rawValue, out var parsedValue) && parsedValue > 0
            ? parsedValue
            : null;
    }

    private void MoveSelectedNode(int delta)
    {
        if (SelectedNode is null || SelectedNode.Node == _document.Root)
        {
            return;
        }

        var parent = FindParent(_document.Root, SelectedNode.Node);
        if (parent is null)
        {
            return;
        }

        var index = parent.Children.FindIndex(x => x.Id == SelectedNode.Node.Id);
        if (index < 0)
        {
            return;
        }

        var newIndex = index + delta;
        if (newIndex < 0 || newIndex >= parent.Children.Count)
        {
            return;
        }

        (parent.Children[index], parent.Children[newIndex]) = (parent.Children[newIndex], parent.Children[index]);
        var parentViewModel = FindParentViewModel(RootNodes[0], SelectedNode);
        if (parentViewModel is null)
        {
            return;
        }

        var currentVmIndex = parentViewModel.Children.IndexOf(SelectedNode);
        if (currentVmIndex < 0)
        {
            return;
        }

        parentViewModel.Children.Move(currentVmIndex, newIndex);
        MarkDocumentChanged();
    }

    private void RebuildTree(Guid? selectedNodeId = null)
    {
        RootNodes.Clear();
        RootNodes.Add(CreateTree(_document.Root));
        SelectedNode = selectedNodeId.HasValue
            ? FindNodeViewModel(RootNodes[0], selectedNodeId.Value) ?? RootNodes[0]
            : RootNodes[0];
    }

    private static UiNode? FindParent(UiNode current, UiNode target)
    {
        foreach (var child in current.Children)
        {
            if (child.Id == target.Id)
            {
                return current;
            }

            var nested = FindParent(child, target);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static bool IsDescendantOf(UiNode candidate, UiNode ancestor)
    {
        foreach (var child in ancestor.Children)
        {
            if (child.Id == candidate.Id || IsDescendantOf(candidate, child))
            {
                return true;
            }
        }

        return false;
    }

    private static UiNode CloneNode(UiNode source)
    {
        var clone = new UiNode(source.ElementName);
        foreach (var attribute in source.Attributes)
        {
            clone.Attributes[attribute.Key] = attribute.Value;
        }

        foreach (var child in source.Children)
        {
            clone.Children.Add(CloneNode(child));
        }

        return clone;
    }

    private static NodeViewModel? FindNodeViewModel(NodeViewModel root, Guid id)
    {
        if (root.Node.Id == id)
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var nested = FindNodeViewModel(child, id);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static NodeViewModel? FindParentViewModel(NodeViewModel current, NodeViewModel target)
    {
        foreach (var child in current.Children)
        {
            if (child.Node.Id == target.Node.Id)
            {
                return current;
            }

            var nested = FindParentViewModel(child, target);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private NodeViewModel CreateTree(UiNode node)
    {
        var definition = ResolveLvglElement(node.ElementName);
        var nodeViewModel = new NodeViewModel(
            node,
            definition?.DisplayName ?? (_registry.TryGet(node.ElementName, out var legacyDefinition) ? legacyDefinition?.DisplayName : null),
            true,
            BuildElementHint(node.ElementName));
        nodeViewModel.Children.Clear();

        foreach (var child in node.Children)
        {
            nodeViewModel.Children.Add(CreateTree(child));
        }

        return nodeViewModel;
    }

    private static bool AreAllExpanded(NodeViewModel node)
    {
        if (!node.IsExpanded)
        {
            return false;
        }

        return node.Children.All(AreAllExpanded);
    }

    private void BuildToolboxItems()
    {
        var toolboxElements = _lvglMetaRegistry.Elements
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (string.Equals(ToolboxSortMode, "Alphabetisch", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var entry in toolboxElements
                         .Select(BuildToolboxEntry)
                         .OrderBy(x => x.DisplayLabel, StringComparer.OrdinalIgnoreCase))
            {
                ToolboxItems.Add(new ToolboxItemViewModel(
                    $"{entry.DisplayLabel} ({entry.DocumentElementName})",
                    entry.LvglElementName,
                    false,
                    isRuntimeSupported: entry.IsSupported,
                    availabilityHint: BuildElementHint(entry.LvglElementName)));
            }

            return;
        }

        foreach (var category in toolboxElements
                     .Select(BuildToolboxEntry)
                     .GroupBy(x => x.Category)
                     .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var isExpanded = !_toolboxGroupState.TryGetValue(category.Key, out var stored) || stored;
            ToolboxItems.Add(new ToolboxItemViewModel(category.Key, null, true, category.Key, isExpanded));

            if (!isExpanded)
            {
                continue;
            }

            foreach (var entry in category.OrderBy(x => x.DisplayLabel, StringComparer.OrdinalIgnoreCase))
            {
                ToolboxItems.Add(new ToolboxItemViewModel(
                    $"  {entry.DisplayLabel} ({entry.DocumentElementName})",
                    entry.LvglElementName,
                    false,
                    category.Key,
                    isRuntimeSupported: entry.IsSupported,
                    availabilityHint: BuildElementHint(entry.LvglElementName)));
            }
        }
    }

    private static ToolboxEntry BuildToolboxEntry(LvglElementDefinition element)
    {
        return new ToolboxEntry(
            element.DisplayName,
            element.Name,
            element.Targets.TryGetValue("lvgl", out var target) ? target.Type : element.Name,
            element.Category,
            element.Supported);
    }

    private string? BuildElementHint(string elementName)
    {
        var lookupName = elementName?.Trim();
        var parts = new List<string>();
        if (string.IsNullOrWhiteSpace(lookupName))
        {
            parts.Add("LVGL-Element: <leer>");
        }
        else
        {
            var description = _localizationCatalog.GetElementDescription(lookupName);
            if (!string.IsNullOrWhiteSpace(description))
            {
                parts.Add(description);
            }
            else
            {
                var fallbackLabel = _registry.TryGet(lookupName, out var definition) && definition is not null
                    ? definition.DisplayName
                    : lookupName;
                parts.Add($"LVGL-Element: {fallbackLabel} ({lookupName})");
            }
        }

        return parts.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private sealed record ToolboxEntry(
        string DisplayLabel,
        string DocumentElementName,
        string LvglElementName,
        string Category,
        bool IsSupported);

    private string BuildAttributeHint(string attributeName, string lvglAttributeName, string typeName, bool isRequired)
    {
        var parts = new List<string>();
        var description = _localizationCatalog.GetAttributeDescription(lvglAttributeName)
                          ?? _localizationCatalog.GetAttributeDescription(attributeName);
        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description);
        }

        parts.Add($"Typ: {typeName}");
        parts.Add(isRequired ? "Pflichtfeld" : "Optionales Feld");
        return string.Join(Environment.NewLine, parts);
    }

    private IReadOnlyDictionary<string, string> BuildAllowedValueLabels(string typeName)
    {
        if (!string.Equals(typeName, "color", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0x000000"] = Ui("color.black"),
            ["0xffffff"] = Ui("color.white"),
            ["0xf6f7fb"] = Ui("color.surface"),
            ["0xd8dde6"] = Ui("color.border"),
            ["0xcccccc"] = Ui("color.light_gray"),
            ["0x1f2937"] = Ui("color.slate_800"),
            ["0x4b5563"] = Ui("color.slate_600"),
            ["0x007acc"] = Ui("color.primary_blue"),
            ["0xff0000"] = Ui("color.red"),
            ["0x00ff00"] = Ui("color.green"),
            ["0x0000ff"] = Ui("color.blue")
        };
    }

    private void BuildPropertyGridItems()
    {
        var visibleProperties = GetVisibleProperties().ToList();

        if (IsEventsModeSelected)
        {
            string? currentCategory = null;

            foreach (var editor in visibleProperties)
            {
                if (!string.Equals(currentCategory, editor.Category, StringComparison.OrdinalIgnoreCase))
                {
                    currentCategory = editor.Category;
                    var isExpanded = !_propertyGroupState.TryGetValue(currentCategory, out var stored) || stored;
                    PropertyGridItems.Add(new PropertyGridItemViewModel(currentCategory, true, null, currentCategory, isExpanded));

                    if (!isExpanded)
                    {
                        continue;
                    }
                }

                var categoryExpanded = !_propertyGroupState.TryGetValue(editor.Category, out var categoryStored) || categoryStored;
                if (categoryExpanded)
                {
                    PropertyGridItems.Add(new PropertyGridItemViewModel(editor.DisplayName, false, editor, editor.Category));
                }
            }

            return;
        }

        if (string.Equals(PropertySortMode, "Alphabetisch", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var editor in visibleProperties.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                PropertyGridItems.Add(new PropertyGridItemViewModel(editor.DisplayName, false, editor));
            }

            return;
        }

        foreach (var category in visibleProperties
                     .GroupBy(x => x.Category)
                     .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var isExpanded = !_propertyGroupState.TryGetValue(category.Key, out var stored) || stored;
            PropertyGridItems.Add(new PropertyGridItemViewModel(category.Key, true, null, category.Key, isExpanded));

            if (!isExpanded)
            {
                continue;
            }

            foreach (var editor in category.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                PropertyGridItems.Add(new PropertyGridItemViewModel(editor.DisplayName, false, editor, category.Key));
            }
        }
    }

    private IEnumerable<AttributeEditorViewModel> GetVisibleProperties()
    {
        var showEvents = IsEventsModeSelected;
        var visible = Properties.Where(editor => !ShouldHidePropertyForCurrentTemplate(editor));

        return showEvents
            ? visible.Where(ShouldShowOnEventsTab)
            : visible.Where(x => !ShouldShowOnEventsTab(x));
    }

    private bool ShouldHidePropertyForCurrentTemplate(AttributeEditorViewModel editor) =>
        IsRtosMessagesTemplateActive() &&
        string.Equals(editor.Category, "MCU-Integration", StringComparison.OrdinalIgnoreCase);

    private bool ShouldShowOnEventsTab(AttributeEditorViewModel editor)
    {
        if (editor.Name.StartsWith("__event__", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsRtosMessagesTemplateActive() &&
               string.Equals(editor.Category, "Data", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(editor.Name, "__rtos_event_id", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPropertyCategory(string attributeName, string lvglAttributeName, string typeName)
    {
        if (attributeName is "eventGroup" or "eventType" or "useMessages" ||
            lvglAttributeName is "eventGroup" or "eventType" or "useMessages")
        {
            return "MCU-Integration";
        }

        if (attributeName.StartsWith("on_", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("on_", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "event", StringComparison.OrdinalIgnoreCase))
        {
            return "Events";
        }

        if (attributeName is "x" or "y" or "align" ||
            lvglAttributeName is "x" or "y" or "align" ||
            attributeName.StartsWith("flex", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("flex_", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("grid", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("grid_", StringComparison.OrdinalIgnoreCase))
        {
            return "Layout";
        }

        if (attributeName.Contains("width", StringComparison.OrdinalIgnoreCase) ||
            attributeName.Contains("height", StringComparison.OrdinalIgnoreCase) ||
            attributeName.Contains("layout", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "size", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "coordinate", StringComparison.OrdinalIgnoreCase))
        {
            return "Layout";
        }

        if (attributeName.StartsWith("padding", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("pad_", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("background", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("bg_", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("border", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("border_", StringComparison.OrdinalIgnoreCase) ||
            attributeName.Contains("radius", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "opa", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "color", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "opa", StringComparison.OrdinalIgnoreCase))
        {
            return "Style";
        }

        if (attributeName.Contains("text", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "color", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "font", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "base_dir", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "text_align", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lvglAttributeName, "text_align", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "font", StringComparison.OrdinalIgnoreCase))
        {
            return "Text";
        }

        if (string.Equals(attributeName, "id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "name", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "src", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "source", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "map", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "options", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "useUpdate", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("bind_", StringComparison.OrdinalIgnoreCase) ||
            lvglAttributeName.StartsWith("bind_", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "accepted_chars", StringComparison.OrdinalIgnoreCase))
        {
            return "Data";
        }

        if (string.Equals(typeName, "boolean", StringComparison.OrdinalIgnoreCase) ||
            attributeName.Contains("hidden", StringComparison.OrdinalIgnoreCase) ||
            attributeName.Contains("clickable", StringComparison.OrdinalIgnoreCase) ||
            attributeName.Contains("scrollable", StringComparison.OrdinalIgnoreCase))
        {
            return "Behavior";
        }

        return "Behavior";
    }

    private LvglElementDefinition? ResolveLvglElement(string elementName)
    {
        if (_lvglMetaRegistry.TryGet(elementName, out var directDefinition))
        {
            return directDefinition;
        }

        if (_lvglMetaRegistry.TryGetByLvglType(elementName, out var lvglDefinition))
        {
            return lvglDefinition;
        }

        return null;
    }

    private bool IsRtosMessagesTemplateActive() =>
        string.Equals(_projectSettings.ProjectTemplate, "RTOS-Messages", StringComparison.OrdinalIgnoreCase);

    private bool SelectedNodeSupportsEvents()
    {
        if (SelectedNode is null)
        {
            return false;
        }

        var definition = ResolveLvglElement(SelectedNode.Node.ElementName);
        return definition?.Events.Count > 0;
    }

    private static bool ShouldSkipAttributeEditor(LvglElementAttributeDefinition attribute)
    {
        if (string.Equals(attribute.TypeName, "event", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return attribute.Name is "eventGroup" or "eventType" or "useMessages";
    }

    private bool ShouldSkipEventAttributeEditor(LvglElementAttributeDefinition attribute)
    {
        if (!IsRtosMessagesTemplateActive())
        {
            return false;
        }

        return attribute.Name is "eventGroup" or "eventType" or "useMessages";
    }

    private bool CanParentAcceptTool(string parentElementName, string toolName)
    {
        var parentDefinition = ResolveLvglElement(parentElementName);
        var toolDefinition = ResolveLvglElement(toolName);

        if (parentDefinition is null || toolDefinition is null)
        {
            return false;
        }

        if (parentDefinition.Children.Allowed.Count == 0)
        {
            return false;
        }

        return parentDefinition.Children.Allowed.Any(allowed =>
            string.Equals(allowed, toolDefinition.Name, StringComparison.OrdinalIgnoreCase) ||
            (toolDefinition.Targets.TryGetValue("lvgl", out var target) &&
             string.Equals(allowed, target.Type, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(allowed, toolName, StringComparison.OrdinalIgnoreCase));
    }
}
