using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ai.McuUiStudio.App.Services.Project;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class MainWindow : Window
{
    private const string ToolboxDragFormat = "mcu-ui-studio.toolbox-item";
    private const string StructureNodeDragFormat = "mcu-ui-studio.structure-node";
    private const string StructureNodeTextPrefix = "structure-node:";
    private const double SiblingDropZoneRatio = 0.18;
    private const double MinSiblingDropZoneHeight = 4;
    private const double MaxSiblingDropZoneHeight = 6;
    private static readonly SolidColorBrush DragHoverHighlightBrush = new(Color.Parse("#9962CCED"));
    private static readonly SolidColorBrush DragHighlightBorderBrush = new(Color.Parse("#CC96BE25"));
    private MainWindowViewModel? _trackedMainWindowViewModel;
    private EditorStateViewModel? _trackedEditorState;
    private Point? _toolboxDragStartPoint;
    private Point? _structureDragStartPoint;
    private PointerPressedEventArgs? _toolboxDragStartEvent;
    private PointerPressedEventArgs? _structureDragStartEvent;
    private ToolboxItemViewModel? _pendingToolboxDragItem;
    private NodeViewModel? _pendingStructureDragNode;
    private AttributeEditorViewModel? _activePropertyEditor;
    private bool _startupProjectDialogShown;
    private bool _startupEditorRevealPending = true;
    private double _documentationExpandedWidth = 320;
    private double _documentationMinimumWidth = 320;
    private double _editorExpandedWidth;
    private double _windowWidthWithDocumentationCollapsed;
    private double _windowWidthWithDocumentationExpanded;
    private double _editorMinimumWidth;
    private bool _editorMinimumWidthInitialized;
    private bool _documentationMinimumWidthInitialized;
    private bool _documentationContentInitialized;

    public MainWindow()
    {
        InitializeComponent();
        Opacity = 0;
        ShowInTaskbar = false;
        DataContextChanged += HandleDataContextChanged;
        Opened += HandleOpened;
        LayoutUpdated += HandleLayoutUpdated;
    }

    private void EnsureDocumentationContent()
    {
        if (_documentationContentInitialized)
        {
            return;
        }

        var host = this.FindControl<ContentControl>("DocumentationContentHost");
        if (host is null)
        {
            return;
        }

        _documentationContentInitialized = true;

        try
        {
            host.Content = CreateDocumentationContent();
        }
        catch (Exception ex)
        {
            host.Content = CreateDocumentationPlaceholder(ex.GetType().Name + ": " + ex.Message);
        }
    }

    private Control CreateDocumentationContent()
    {
        if (DataContext is MainWindowViewModel vm &&
            Uri.TryCreate(vm.DocumentationUrl, UriKind.Absolute, out var documentationUri))
        {
            if (OperatingSystem.IsWindows())
            {
                return CreateDocumentationBrowserFallback(vm, documentationUri);
            }

            return new NativeWebView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Source = documentationUri
            };
        }

        return CreateDocumentationPlaceholder(null);
    }

    private Control CreateDocumentationBrowserFallback(MainWindowViewModel vm, Uri documentationUri)
    {
        var panel = new StackPanel
        {
            Spacing = 10
        };

        panel.Children.Add(new TextBlock
        {
            Text = vm.DocumentationTitle,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Der eingebettete Handbuchbereich ist unter Windows derzeit deaktiviert. Das Handbuch kann stattdessen im Standardbrowser geoeffnet werden.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8
        });

        panel.Children.Add(new TextBlock
        {
            Text = documentationUri.ToString(),
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7
        });

        var openButton = new Button
        {
            Content = "Handbuch im Browser oeffnen",
            HorizontalAlignment = HorizontalAlignment.Left
        };

        openButton.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = documentationUri.ToString(),
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore browser launch errors and keep the app stable.
            }
        };

        panel.Children.Add(openButton);

        return new Border
        {
            Padding = new Thickness(16),
            Child = panel
        };
    }

    private Control CreateDocumentationPlaceholder(string? errorText)
    {
        var vm = DataContext as MainWindowViewModel;
        var panel = new StackPanel
        {
            Spacing = 10
        };

        panel.Children.Add(new TextBlock
        {
            Text = vm?.DocumentationPlaceholderTitle ?? "Handbuchbereich",
            FontSize = 15,
            FontWeight = FontWeight.SemiBold
        });

        panel.Children.Add(new TextBlock
        {
            Text = vm?.DocumentationPlaceholderText ?? "Diese Spalte ist fuer kontextbezogene Hilfe und das spaetere Handbuch vorgesehen.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8
        });

        if (!string.IsNullOrWhiteSpace(errorText))
        {
            panel.Children.Add(new TextBlock
            {
                Text = errorText,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7
            });
        }

        return new Border
        {
            Padding = new Thickness(16),
            Child = panel
        };
    }

    private void AddElementClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.AddSelectedToolAsChild();
        }
    }

    private void DeleteElementClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.DeleteSelectedNode();
        }
    }

    private void DuplicateElementClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.DuplicateSelectedNode();
        }
    }

    private void MoveUpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.MoveSelectedNodeUp();
        }
    }

    private void MoveDownClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.MoveSelectedNodeDown();
        }
    }

    private async void PreviewZoomOutClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.DecreasePreviewZoom();
            UpdateNativePreviewWindowStartPosition();
            await vm.RenderPreviewNowAsync(forceFullReload: false);
        }
    }

    private async void PreviewZoomInClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.IncreasePreviewZoom();
            UpdateNativePreviewWindowStartPosition();
            await vm.RenderPreviewNowAsync(forceFullReload: false);
        }
    }

    private void PreviewResetSizeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ResetPreviewZoomToOriginalSize();
        }
    }

    private void RenderNowClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.RenderPreviewNow();
        }
    }

    private void GenerateMcuCodeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        CommitPendingPropertyEdits(vm);

        if (vm.TryGenerateMcuDisplaySources(out var statusMessage))
        {
            vm.EditorState.SetPreviewStatus(statusMessage ?? "MCU-Quelldateien wurden generiert.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            vm.EditorState.SetPreviewStatus(statusMessage);
        }
    }

    private async void OpenProjectDialogClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ShowProjectDialogAsync(closeApplicationOnCancel: false);
    }

    private void ExitApplicationClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void NewProjectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || StorageProvider is null)
        {
            return;
        }

        var screensDirectory = GetScreensDirectory(vm);
        if (string.IsNullOrWhiteSpace(screensDirectory))
        {
            return;
        }

        Directory.CreateDirectory(screensDirectory);

        var targetPath = await PickNewScreenPathAsync(vm, screensDirectory);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }

        if (!vm.TryCreateAndLoadNewScreen(targetPath, out var errorMessage))
        {
            vm.EditorState.SetPreviewStatus(string.Format(vm.ErrorFileLoadFailedFormat, errorMessage));
        }
    }

    private async void HandleOpened(object? sender, EventArgs e)
    {
        EnsureDocumentationContent();

        if (_startupProjectDialogShown)
        {
            return;
        }

        _startupProjectDialogShown = true;
        var keepOpen = await ShowProjectDialogAsync(closeApplicationOnCancel: true);
        if (!keepOpen)
        {
            Close();
            return;
        }

        RevealEditorAfterStartupDialog();
    }

    private async Task<bool> ShowProjectDialogAsync(bool closeApplicationOnCancel)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return false;
        }

        while (true)
        {
            var previousSettings = vm.SnapshotProjectSettings();
            var previousProjectFilePath = vm.CurrentProjectFilePath;
            var previousProjectLoaded = vm.IsProjectLoaded;
            var dialog = new ProjectDialog
            {
                DataContext = new ProjectDialogViewModel(
                    vm.GetLocalizationCatalog(),
                    vm.SnapshotProjectSettings(),
                    vm.CurrentProjectFilePath)
            };

            var result = await dialog.ShowDialog<bool?>(this);
            var accepted = result == true || dialog.WasAccepted;
            if (!accepted || dialog.DataContext is not ProjectDialogViewModel projectVm)
            {
                return !closeApplicationOnCancel && vm.IsProjectLoaded;
            }

            vm.ApplyProjectSettings(projectVm.Settings, dialog.SavedProjectFilePath ?? projectVm.ProjectFilePath);
            UpdateNativePreviewWindowStartPosition();
            vm.SetProjectLoaded(true);

            if (vm.TryLoadProjectMainDocument(out var errorMessage))
            {
                return true;
            }

            if (closeApplicationOnCancel)
            {
                vm.SetProjectLoaded(false);
            }
            else
            {
                vm.ApplyProjectSettings(previousSettings, previousProjectFilePath);
                vm.SetProjectLoaded(previousProjectLoaded);
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                await ShowProjectLoadErrorAsync(vm, errorMessage);
            }

            if (!closeApplicationOnCancel)
            {
                return false;
            }
        }
    }

    private void RevealEditorAfterStartupDialog()
    {
        if (!_startupEditorRevealPending)
        {
            return;
        }

        _startupEditorRevealPending = false;
        ShowInTaskbar = true;
        Opacity = 1;
        Activate();
    }

    private async Task ShowProjectLoadErrorAsync(MainWindowViewModel vm, string errorMessage)
    {
        var dialog = new InfoDialog
        {
            DataContext = new InfoDialogViewModel(
                vm.ProjectDialogTitle,
                string.Format(vm.ErrorFileLoadFailedFormat, errorMessage),
                vm.DialogCloseLabel)
        };

        await dialog.ShowDialog(this);
    }

    private void UpdateNativePreviewWindowStartPosition()
    {
        try
        {
            var editorX = Position.X;
            var editorY = Position.Y;
            var previewX = editorX + (int)Math.Ceiling(Bounds.Width) + 24;
            var previewY = editorY;

            Environment.SetEnvironmentVariable("MCU_UI_PREVIEW_START_X", previewX.ToString());
            Environment.SetEnvironmentVariable("MCU_UI_PREVIEW_START_Y", previewY.ToString());
        }
        catch
        {
            // Best effort only. The preview can still open with default placement.
        }
    }

    private async void OpenThemeDialogClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        try
        {
            var service = new ThemeFileService();
            var document = service.Load(vm.SnapshotProjectSettings().ThemeFile);
            var dialog = new ThemeDialog
            {
                DataContext = new ThemeDialogViewModel(
                    vm.GetLocalizationCatalog(),
                    vm.ThemeDialogTitle,
                    vm.ThemeDialogFileLabel,
                    vm.ThemeDialogPropertyHeader,
                    vm.ThemeDialogValueHeader,
                    vm.ThemeDialogDescriptionHeader,
                    vm.ThemeDialogSaveLabel,
                    vm.DialogCloseLabel,
                    vm.ThemeDialogSaveSuccessFormat,
                    vm.ThemeDialogSaveFailedFormat,
                    document,
                    service)
            };

            var saved = await dialog.ShowDialog<bool?>(this);
            if (saved == true)
            {
                vm.EditorState.SetPreviewStatus(string.Format(vm.ThemeDialogSaveSuccessFormat, Path.GetFileName(document.FilePath)));
            }
        }
        catch (Exception ex)
        {
            var dialog = new InfoDialog
            {
                DataContext = new InfoDialogViewModel(
                    vm.ThemeDialogTitle,
                    string.Format(vm.ThemeDialogLoadFailedFormat, ex.Message),
                    vm.DialogCloseLabel)
            };

            await dialog.ShowDialog(this);
        }
    }

    private async void OpenLvConfDialogClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        try
        {
            var service = new LvConfFileService();
            var document = service.Load(vm.SnapshotProjectSettings().LvConfFile);
            var dialog = new LvConfDialog
            {
                DataContext = new LvConfDialogViewModel(
                    vm.LvConfDialogTitle,
                    vm.LvConfDialogFileLabel,
                    vm.LvConfDialogPropertyHeader,
                    vm.LvConfDialogValueHeader,
                    vm.LvConfDialogDescriptionHeader,
                    vm.LvConfDialogSaveLabel,
                    vm.DialogCloseLabel,
                    vm.LvConfDialogSaveSuccessFormat,
                    vm.LvConfDialogSaveFailedFormat,
                    document,
                    service)
            };

            var saved = await dialog.ShowDialog<bool?>(this);
            if (saved == true)
            {
                vm.EditorState.SetPreviewStatus(string.Format(vm.LvConfDialogSaveSuccessFormat, Path.GetFileName(document.FilePath)));
            }
        }
        catch (Exception ex)
        {
            var dialog = new InfoDialog
            {
                DataContext = new InfoDialogViewModel(
                    vm.LvConfDialogTitle,
                    string.Format(vm.LvConfDialogLoadFailedFormat, ex.Message),
                    vm.DialogCloseLabel)
            };

            await dialog.ShowDialog(this);
        }
    }

    private async void OpenDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || StorageProvider is null)
        {
            return;
        }

        var screensDirectory = GetScreensDirectory(vm);
        if (string.IsNullOrWhiteSpace(screensDirectory) || !Directory.Exists(screensDirectory))
        {
            return;
        }

        var selectedPath = await PickExistingScreenPathAsync(vm, screensDirectory);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(selectedPath);

        if (!vm.TryLoadDocumentJson(json, selectedPath, out var errorMessage))
        {
            vm.EditorState.SetPreviewStatus(string.Format(
                vm.ErrorFileLoadFailedFormat,
                errorMessage));
        }
    }

    private async void SaveDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || StorageProvider is null)
        {
            return;
        }

        CommitPendingPropertyEdits(vm);

        string? targetPath = vm.CurrentDocumentPath;
        IStorageFile? targetFile = null;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = vm.SaveDialogTitle,
                SuggestedFileName = "ui_start.json",
                FileTypeChoices =
                [
                    new FilePickerFileType(vm.ScreenJsonFileTypeLabel)
                    {
                        Patterns = ["*.json"]
                    }
                ]
            });

            if (targetFile is null)
            {
                return;
            }

            targetPath = targetFile.TryGetLocalPath();
        }

        if (targetFile is null)
        {
            targetFile = await StorageProvider.TryGetFileFromPathAsync(targetPath!);
        }

        if (targetFile is null)
        {
            vm.EditorState.SetPreviewStatus(vm.ErrorSaveTargetUnavailableText);
            return;
        }

        await using var stream = await targetFile.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(vm.ExportDocumentJson());
        await writer.FlushAsync();

        vm.MarkDocumentSaved(targetPath);
    }

    private async Task<string?> PickNewScreenPathAsync(MainWindowViewModel vm, string screensDirectory)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var startLocation = await StorageProvider.TryGetFolderFromPathAsync(screensDirectory);
        var targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = vm.NewScreenDialogTitle,
            SuggestedStartLocation = startLocation,
            SuggestedFileName = "ui_start",
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType(vm.ScreenJsonFileTypeLabel)
                {
                    Patterns = ["*.json"]
                }
            ]
        });

        return targetFile?.TryGetLocalPath();
    }

    private async Task<string?> PickExistingScreenPathAsync(MainWindowViewModel vm, string screensDirectory)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var startLocation = await StorageProvider.TryGetFolderFromPathAsync(screensDirectory);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = vm.OpenDialogTitle,
            AllowMultiple = false,
            SuggestedStartLocation = startLocation,
            FileTypeFilter =
            [
                new FilePickerFileType(vm.ScreenJsonFileTypeLabel)
                {
                    Patterns = ["*.json"]
                }
            ]
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private void ToggleToolboxGroupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm &&
            sender is Control control &&
            control.DataContext is ToolboxItemViewModel item)
        {
            vm.ToggleToolboxGroup(item.GroupKey, item.IsExpanded);
        }
    }

    private void TogglePropertyGroupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm &&
            sender is Control control &&
            control.DataContext is PropertyGridItemViewModel item)
        {
            vm.TogglePropertyGroup(item.GroupKey, item.IsExpanded);
        }
    }

    private static string? GetScreensDirectory(MainWindowViewModel vm)
    {
        var projectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(vm.SnapshotProjectSettings().ProjectDirectory);
        return string.IsNullOrWhiteSpace(projectDirectory)
            ? null
            : Path.Combine(projectDirectory, "screens");
    }

    private void PropertyEditorGotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: PropertyGridItemViewModel { Editor: { } editor } })
        {
            return;
        }

        _activePropertyEditor = editor;
    }

    private void PropertyEditorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: PropertyGridItemViewModel { Editor: { } editor } })
        {
            return;
        }

        editor.CommitEditValue();

        if (ReferenceEquals(_activePropertyEditor, editor))
        {
            _activePropertyEditor = null;
        }
    }

    private void CommitPendingPropertyEdits(MainWindowViewModel vm)
    {
        _activePropertyEditor?.CommitEditValue();
        _activePropertyEditor = null;
        vm.CommitPendingPropertyEdits();
    }

    private void ShowPropertiesPanelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowPropertiesPanel();
        }
    }

    private void ShowEventsPanelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowEventsPanel();
        }
    }

    private void ClearPreviewLogClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.EditorState.ClearPreviewLog();
        }
    }

    private void TogglePropertySortModeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.TogglePropertySortMode();
        }
    }

    private void ToggleToolboxSortModeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ToggleToolboxSortMode();
        }
    }

    private void ToggleDocumentationPanelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            AdjustWindowWidthForDocumentationToggle(vm);
            vm.ToggleDocumentationPanel();
            ApplyDocumentationPanelVisibility(vm);
        }
    }

    private void ToolboxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ToolboxListBox?.SelectedItem is not null)
        {
            ToolboxListBox.ScrollIntoView(ToolboxListBox.SelectedItem);
        }
    }

    private void ToggleAllTreeNodesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ToggleAllTreeNodes();
        }
    }

    private void ShowValidationTabClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectValidationTab();
        }
    }

    private void ShowTechnicalLogTabClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectTechnicalLogTab();
        }
    }

    private void ShowEventCallbacksTabClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectEventCallbacksTab();
        }
    }

    private void ShowJsonPreviewTabClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectJsonPreviewTab();
        }
    }

    private void BindAvailabilityTooltip(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        var hint = control.DataContext switch
        {
            ToolboxItemViewModel toolboxItem => toolboxItem.AvailabilityHint,
            NodeViewModel nodeItem => nodeItem.AvailabilityHint,
            _ => null
        };

        ToolTip.SetShowDelay(control, 200);
        ToolTip.SetTip(control, string.IsNullOrWhiteSpace(hint) ? null : hint);
    }

    private void ToolboxEntryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not ToolboxItemViewModel item ||
            !item.IsSelectable)
        {
            _toolboxDragStartPoint = null;
            _toolboxDragStartEvent = null;
            _pendingToolboxDragItem = null;
            return;
        }

        _toolboxDragStartPoint = e.GetPosition(this);
        _toolboxDragStartEvent = e;
        _pendingToolboxDragItem = item;
    }

    private async void ToolboxEntryPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingToolboxDragItem is null || _toolboxDragStartPoint is null)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        var delta = currentPoint - _toolboxDragStartPoint.Value;
        var properties = e.GetCurrentPoint(this).Properties;

        if (!properties.IsLeftButtonPressed ||
            (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6))
        {
            return;
        }

        var draggedItem = _pendingToolboxDragItem;
        _pendingToolboxDragItem = null;
        _toolboxDragStartPoint = null;
        var dragStartEvent = _toolboxDragStartEvent;
        _toolboxDragStartEvent = null;

        var item = new DataTransferItem();
        item.SetText(draggedItem.ToolName ?? string.Empty);
        item.Set(ToolboxDragDataFormat, draggedItem.ToolName ?? string.Empty);

        var data = new DataTransfer();
        data.Add(item);

        try
        {
            if (dragStartEvent is not null)
            {
                await DragDrop.DoDragDropAsync(dragStartEvent, data, DragDropEffects.Copy);
            }
        }
        finally
        {
        }
    }

    private void ToolboxEntryPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _toolboxDragStartPoint = null;
        _toolboxDragStartEvent = null;
        _pendingToolboxDragItem = null;
    }

    private void StructureEntryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not NodeViewModel node)
        {
            _structureDragStartPoint = null;
            _structureDragStartEvent = null;
            _pendingStructureDragNode = null;
            return;
        }

        _structureDragStartPoint = e.GetPosition(this);
        _structureDragStartEvent = e;
        _pendingStructureDragNode = node;
    }

    private async void StructureEntryPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingStructureDragNode is null || _structureDragStartPoint is null)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        var delta = currentPoint - _structureDragStartPoint.Value;
        var properties = e.GetCurrentPoint(this).Properties;

        if (!properties.IsLeftButtonPressed ||
            (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6))
        {
            return;
        }

        var draggedNode = _pendingStructureDragNode;
        _pendingStructureDragNode = null;
        _structureDragStartPoint = null;
        var dragStartEvent = _structureDragStartEvent;
        _structureDragStartEvent = null;

        var item = new DataTransferItem();
        item.SetText($"{StructureNodeTextPrefix}{draggedNode.Node.Id:D}");
        item.Set(StructureNodeDragDataFormat, draggedNode.Node.Id.ToString("D"));

        var data = new DataTransfer();
        data.Add(item);

        if (dragStartEvent is not null)
        {
            await DragDrop.DoDragDropAsync(dragStartEvent, data, DragDropEffects.Move);
        }
    }

    private void StructureEntryPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _structureDragStartPoint = null;
        _structureDragStartEvent = null;
        _pendingStructureDragNode = null;
    }

    private void StructureTreeDragOver(object? sender, DragEventArgs e)
    {
        if (IsOverStructureNode(e.Source))
        {
            return;
        }

        if (DataContext is MainWindowViewModel vm &&
            TryGetDraggedStructureNodeId(e.DataTransfer, out var draggedNodeId))
        {
            var draggedNode = vm.FindNodeById(draggedNodeId);
            e.DragEffects = draggedNode is not null && vm.CanMoveNodeAsChild(draggedNode, vm.RootNodes.FirstOrDefault())
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }
        else
        {
            e.DragEffects = HasToolboxDragData(e.DataTransfer) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void StructureNodeDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm ||
            sender is not Border border ||
            border.DataContext is not NodeViewModel targetNode)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var dropMode = GetDropMode(border, e);
        bool canDrop;
        DragDropEffects effect;

        if (TryGetDraggedStructureNodeId(e.DataTransfer, out var draggedNodeId) &&
            vm.FindNodeById(draggedNodeId) is { } draggedNode)
        {
            canDrop = dropMode switch
            {
                StructureDropMode.Child => vm.CanMoveNodeAsChild(draggedNode, targetNode),
                StructureDropMode.Before => vm.CanMoveNodeAsSibling(draggedNode, targetNode),
                StructureDropMode.After => vm.CanMoveNodeAsSibling(draggedNode, targetNode),
                _ => false
            };
            effect = canDrop ? DragDropEffects.Move : DragDropEffects.None;
        }
        else if (TryGetDraggedToolName(e.DataTransfer, out var toolName))
        {
            canDrop = dropMode switch
            {
                StructureDropMode.Child => vm.CanAddToolAsChild(toolName, targetNode),
                StructureDropMode.Before => vm.CanAddToolBefore(toolName, targetNode),
                StructureDropMode.After => vm.CanAddToolAfter(toolName, targetNode),
                _ => false
            };
            effect = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            canDrop = false;
            effect = DragDropEffects.None;
        }

        ApplyDropVisual(border, dropMode, canDrop);
        e.DragEffects = effect;
        e.Handled = true;
    }

    private void StructureNodeDragLeave(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border)
        {
            ResetDropVisual(border);
        }
    }

    private void StructureTreeDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (TryGetDraggedStructureNodeId(e.DataTransfer, out var draggedNodeId) &&
            vm.FindNodeById(draggedNodeId) is { } draggedNode)
        {
            vm.MoveNodeAsChild(draggedNode, vm.RootNodes.FirstOrDefault());
        }
        else if (TryGetDraggedToolName(e.DataTransfer, out var toolName))
        {
            vm.AddToolAsChild(toolName, vm.RootNodes.FirstOrDefault());
        }

        e.Handled = true;
    }

    private void StructureNodeDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm ||
            sender is not Border border ||
            border.DataContext is not NodeViewModel targetNode)
        {
            return;
        }

        var dropMode = GetDropMode(border, e);

        if (TryGetDraggedStructureNodeId(e.DataTransfer, out var draggedNodeId) &&
            vm.FindNodeById(draggedNodeId) is { } draggedNode)
        {
            switch (dropMode)
            {
                case StructureDropMode.Child when vm.CanMoveNodeAsChild(draggedNode, targetNode):
                    vm.MoveNodeAsChild(draggedNode, targetNode);
                    break;
                case StructureDropMode.Before when vm.CanMoveNodeAsSibling(draggedNode, targetNode):
                    vm.MoveNodeBefore(draggedNode, targetNode);
                    break;
                case StructureDropMode.After when vm.CanMoveNodeAsSibling(draggedNode, targetNode):
                    vm.MoveNodeAfter(draggedNode, targetNode);
                    break;
                default:
                    e.DragEffects = DragDropEffects.None;
                    break;
            }
        }
        else if (TryGetDraggedToolName(e.DataTransfer, out var toolName))
        {
            switch (dropMode)
            {
                case StructureDropMode.Child when vm.CanAddToolAsChild(toolName, targetNode):
                    vm.AddToolAsChild(toolName, targetNode);
                    break;
                case StructureDropMode.Before when vm.CanAddToolBefore(toolName, targetNode):
                    vm.AddToolBefore(toolName, targetNode);
                    break;
                case StructureDropMode.After when vm.CanAddToolAfter(toolName, targetNode):
                    vm.AddToolAfter(toolName, targetNode);
                    break;
                default:
                    e.DragEffects = DragDropEffects.None;
                    break;
            }
        }

        ResetDropVisual(border);
        e.Handled = true;
    }

    private static readonly DataFormat<string> ToolboxDragDataFormat =
        DataFormat.CreateStringApplicationFormat(ToolboxDragFormat);
    private static readonly DataFormat<string> StructureNodeDragDataFormat =
        DataFormat.CreateStringApplicationFormat(StructureNodeDragFormat);

    private static bool HasToolboxDragData(IDataTransfer data) =>
        data.Contains(ToolboxDragDataFormat) || data.Contains(DataFormat.Text);

    private static bool TryGetDraggedToolName(IDataTransfer data, out string toolName)
    {
        toolName = string.Empty;

        var customTool = data.TryGetValue(ToolboxDragDataFormat);
        if (!string.IsNullOrWhiteSpace(customTool))
        {
            toolName = customTool;
            return true;
        }

        var text = data.TryGetText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            toolName = text;
            return true;
        }

        return false;
    }

    private static bool TryGetDraggedStructureNodeId(IDataTransfer data, out Guid nodeId)
    {
        nodeId = Guid.Empty;
        var rawNodeId = data.TryGetValue(StructureNodeDragDataFormat);
        if (!string.IsNullOrWhiteSpace(rawNodeId) && Guid.TryParse(rawNodeId, out nodeId))
        {
            return true;
        }

        var text = data.TryGetText();
        if (!string.IsNullOrWhiteSpace(text) &&
            text.StartsWith(StructureNodeTextPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(text[StructureNodeTextPrefix.Length..], out nodeId);
        }

        return false;
    }

    private static StructureDropMode GetDropMode(Border border, DragEventArgs e)
    {
        var point = e.GetPosition(border);
        var height = Math.Max(border.Bounds.Height, 1);
        var siblingZone = Math.Clamp(
            height * SiblingDropZoneRatio,
            MinSiblingDropZoneHeight,
            MaxSiblingDropZoneHeight);
        if (point.Y <= siblingZone)
        {
            return StructureDropMode.Before;
        }

        if (point.Y >= height - siblingZone)
        {
            return StructureDropMode.After;
        }

        return StructureDropMode.Child;
    }

    private static bool IsOverStructureNode(object? source)
    {
        if (source is not Visual visual)
        {
            return false;
        }

        var current = visual;
        while (current is not null)
        {
            if (current is Border { DataContext: NodeViewModel })
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
    }

    private static void ApplyDropVisual(Border border, StructureDropMode mode, bool isAllowed)
    {
        if (!isAllowed)
        {
            border.Background = Brushes.Transparent;
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0);
            return;
        }

        switch (mode)
        {
            case StructureDropMode.Child:
                border.Background = DragHoverHighlightBrush;
                border.BorderBrush = DragHighlightBorderBrush;
                border.BorderThickness = new Thickness(1);
                break;
            case StructureDropMode.Before:
                border.Background = DragHoverHighlightBrush;
                border.BorderBrush = DragHighlightBorderBrush;
                border.BorderThickness = new Thickness(0, 3, 0, 0);
                break;
            case StructureDropMode.After:
                border.Background = DragHoverHighlightBrush;
                border.BorderBrush = DragHighlightBorderBrush;
                border.BorderThickness = new Thickness(0, 0, 0, 3);
                break;
        }
    }

    private static void ResetDropVisual(Border border)
    {
        border.Background = Brushes.Transparent;
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
    }

    private enum StructureDropMode
    {
        Before,
        Child,
        After
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (_trackedMainWindowViewModel is not null)
        {
            _trackedMainWindowViewModel.PropertyChanged -= HandleMainWindowViewModelPropertyChanged;
        }

        if (_trackedEditorState is not null)
        {
            _trackedEditorState.PropertyChanged -= HandleEditorStatePropertyChanged;
        }

        _trackedMainWindowViewModel = DataContext as MainWindowViewModel;
        _trackedEditorState = _trackedMainWindowViewModel?.EditorState;

        if (_trackedMainWindowViewModel is not null)
        {
            _trackedMainWindowViewModel.PropertyChanged += HandleMainWindowViewModelPropertyChanged;
        }

        if (_trackedEditorState is not null)
        {
            _trackedEditorState.PropertyChanged += HandleEditorStatePropertyChanged;
        }

        if (_trackedMainWindowViewModel is not null)
        {
            ApplyDocumentationPanelVisibility(_trackedMainWindowViewModel);
        }
    }

    private void HandleMainWindowViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(MainWindowViewModel.DocumentationUrl), StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.UIThread.Post(UpdateDocumentationContentSource, DispatcherPriority.Background);
    }

    private void HandleEditorStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(EditorStateViewModel.TechnicalLog), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(EditorStateViewModel.EventCallbackLog), StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.UIThread.Post(ScrollPreviewLogToEnd, DispatcherPriority.Background);
    }

    private void ScrollPreviewLogToEnd()
    {
        ScrollViewerToEnd(TechnicalLogScrollViewer);
        ScrollViewerToEnd(EventLogScrollViewer);
    }

    private static void ScrollViewerToEnd(ScrollViewer? scrollViewer)
    {
        if (scrollViewer is null)
        {
            return;
        }

        scrollViewer.ScrollToEnd();
    }

    private void UpdateDocumentationContentSource()
    {
        var host = this.FindControl<ContentControl>("DocumentationContentHost");
        if (host is null)
        {
            return;
        }

        try
        {
            host.Content = CreateDocumentationContent();
        }
        catch (Exception ex)
        {
            host.Content = CreateDocumentationPlaceholder(ex.GetType().Name + ": " + ex.Message);
        }
    }

    private void ApplyDocumentationPanelVisibility(MainWindowViewModel vm)
    {
        var rootLayoutGrid = this.FindControl<Grid>("RootLayoutGrid");
        if (rootLayoutGrid is null || rootLayoutGrid.ColumnDefinitions.Count < 3)
        {
            return;
        }

        var visible = vm.IsDocumentationPanelVisible;
        if (visible)
        {
            var currentEditorWidth = MainEditorLayout?.Bounds.Width ?? 0;
            if (currentEditorWidth > 0)
            {
                _editorExpandedWidth = Math.Max(_editorMinimumWidth, Math.Ceiling(currentEditorWidth));
            }

            var currentPanelWidth = DocumentationPanel?.Bounds.Width ?? 0;
            if (currentPanelWidth > 0 && !_documentationMinimumWidthInitialized)
            {
                var width = Math.Ceiling(currentPanelWidth);
                _documentationExpandedWidth = Math.Max(320, width);
                _documentationMinimumWidth = Math.Max(320, width);
                _documentationMinimumWidthInitialized = true;
            }
        }

        rootLayoutGrid.ColumnDefinitions[0].MinWidth = _editorMinimumWidth > 0
            ? _editorMinimumWidth
            : 0;
        rootLayoutGrid.ColumnDefinitions[0].Width = visible
            ? new GridLength(Math.Max(_editorMinimumWidth, _editorExpandedWidth > 0 ? _editorExpandedWidth : _editorMinimumWidth))
            : new GridLength(1, GridUnitType.Star);
        rootLayoutGrid.ColumnDefinitions[1].Width = visible ? new GridLength(16) : new GridLength(20);
        rootLayoutGrid.ColumnDefinitions[2].MinWidth = visible
            ? _documentationMinimumWidth
            : 0;
        rootLayoutGrid.ColumnDefinitions[2].Width = visible
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);

        var documentationPanelSplitter = this.FindControl<GridSplitter>("DocumentationPanelSplitter");
        if (documentationPanelSplitter is not null)
        {
            documentationPanelSplitter.IsVisible = visible;
        }

        var documentationPanel = this.FindControl<Border>("DocumentationPanel");
        if (documentationPanel is not null)
        {
            documentationPanel.IsVisible = visible;
        }

        var documentationPanelHandle = this.FindControl<Button>("DocumentationPanelHandle");
        if (documentationPanelHandle is not null)
        {
            documentationPanelHandle.IsVisible = !visible;
        }

        ApplyWindowMinimumWidth(visible);
    }

    private void AdjustWindowWidthForDocumentationToggle(MainWindowViewModel vm)
    {
        if (vm.IsDocumentationPanelVisible)
        {
            var currentEditorWidth = MainEditorLayout?.Bounds.Width ?? 0;
            if (currentEditorWidth > 0)
            {
                _editorExpandedWidth = Math.Max(_editorMinimumWidth, Math.Ceiling(currentEditorWidth));
            }

            var currentPanelWidth = DocumentationPanel?.Bounds.Width ?? 0;
            if (currentPanelWidth > 0)
            {
                var width = Math.Ceiling(currentPanelWidth);
                _documentationExpandedWidth = Math.Max(320, width);

                if (!_documentationMinimumWidthInitialized)
                {
                    _documentationMinimumWidth = Math.Max(320, width);
                    _documentationMinimumWidthInitialized = true;
                }
            }

            _windowWidthWithDocumentationExpanded = Width;

            var collapsedMinimumWidth = GetRequiredWindowMinimumWidth(false);
            var restoredCollapsedWidth = _windowWidthWithDocumentationCollapsed > 0
                ? _windowWidthWithDocumentationCollapsed
                : collapsedMinimumWidth;

            Width = Math.Max(collapsedMinimumWidth, restoredCollapsedWidth);
            return;
        }

        _windowWidthWithDocumentationCollapsed = Width;

        var expandedMinimumWidth = GetRequiredWindowMinimumWidth(true);
        var restoredExpandedWidth = _windowWidthWithDocumentationExpanded > 0
            ? _windowWidthWithDocumentationExpanded
            : expandedMinimumWidth;

        Width = Math.Max(expandedMinimumWidth, restoredExpandedWidth);
    }

    private void ApplyWindowMinimumWidth(bool documentationVisible)
    {
        if (!_editorMinimumWidthInitialized || _editorMinimumWidth <= 0)
        {
            return;
        }

        var requiredMinWidth = GetRequiredWindowMinimumWidth(documentationVisible);
        if (requiredMinWidth > 0)
        {
            MinWidth = Math.Max(200, requiredMinWidth);
        }
    }

    private double GetRequiredWindowMinimumWidth(bool documentationVisible)
    {
        if (!_editorMinimumWidthInitialized || _editorMinimumWidth <= 0)
        {
            return 0;
        }

        var editorWidth = _editorMinimumWidth;
        var documentationWidth = documentationVisible
            ? Math.Max(_documentationMinimumWidth, 320)
            : 0;
        var splitterWidth = documentationVisible ? 16 : 20;
        var outerMargin = 32; // RootLayoutGrid margin left/right
        var documentationGap = documentationVisible ? 8 : 0;

        return editorWidth + documentationWidth + splitterWidth + documentationGap + outerMargin;
    }

    private void HandleLayoutUpdated(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var mainEditorLayout = this.FindControl<Grid>("MainEditorLayout");
        var currentEditorWidth = mainEditorLayout?.Bounds.Width ?? 0;
        if (currentEditorWidth <= 0)
        {
            return;
        }

        if (!_editorMinimumWidthInitialized)
        {
            _editorMinimumWidth = Math.Ceiling(currentEditorWidth);
            _editorMinimumWidthInitialized = true;
            ApplyDocumentationPanelVisibility(vm);
            return;
        }

        var documentationPanel = this.FindControl<Border>("DocumentationPanel");
        var currentPanelWidth = documentationPanel?.Bounds.Width ?? 0;
        if (currentPanelWidth > 0 && !_documentationMinimumWidthInitialized)
        {
            var width = Math.Ceiling(currentPanelWidth);
            _documentationExpandedWidth = Math.Max(320, width);
            _documentationMinimumWidth = Math.Max(320, width);
            _documentationMinimumWidthInitialized = true;
        }

        ApplyWindowMinimumWidth(vm.IsDocumentationPanelVisible);
    }
}
