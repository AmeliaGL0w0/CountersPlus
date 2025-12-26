using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using CountersPlus.ConfigModels;
using CountersPlus.Custom;
using CountersPlus.UI.FlowCoordinators;
using CountersPlus.UI.ViewControllers.Editing;
using CountersPlus.Utils;
using HMUI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    class CountersPlusHUDListViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CountersPlus.UI.BSML.HUDs.HUDList.bsml";

        public bool IsDeleting = false;
        public int SelectedCanvas { get; private set; } = -1;

        [UIComponent("list")] private CustomListTableData data;
        [UIComponent("new-canvas-name")] private ModalKeyboard newCanvasKeyboard;
        [UIComponent("delete-canvas")] private ModalView deleteCanvas;
        [UIComponent("canvas-error")] private ModalView canvasError;
        [UIParams] private BSMLParserParams parserParams;

        [Inject] private HUDConfigModel hudConfig;
        [Inject] private MainConfigModel mainConfig;
        [Inject] private CanvasUtility canvasUtility;
        [Inject] private LazyInject<CountersPlusSettingsFlowCoordinator> flowCoordinator;
        [Inject] private LazyInject<CountersPlusHUDEditViewController> hudEdit;
        [Inject] private LazyInject<CountersPlusCounterEditViewController> counterEdit;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            RefreshData();
            ClearSelection();
            IsDeleting = false;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemEnabling)
        {
            DeactivateModals();
        }

        public void RefreshData()
        {
            data.Data.Clear();
            for (int i = -1; i < hudConfig.OtherCanvasSettings.Count; i++)
            {
                HUDCanvas settings = canvasUtility.GetCanvasSettingsFromID(i);
                int countersUsingCanvas = flowCoordinator.Value.AllConfigModels.Count(x => x.CanvasID == i);
                var info = new CustomListTableData.CustomCellInfo(
                    settings?.Name ?? "Unknown",
                    $"{countersUsingCanvas} counter(s) use this Canvas.", Utilities.LoadSpriteFromTexture(Texture2D.blackTexture));
                data.Data.Add(info);
            }
            data.TableView.ReloadData();
        }

        public void CreateNewCanvasDialog()
        {
            flowCoordinator.Value.SetRightViewController(null);
            DeactivateModals();
            newCanvasKeyboard.ModalView.Show(true);
        }

        public void DeactivateModals() => parserParams.EmitEvent("on-deactivate");

        public void ClearSelection() => data.TableView.ClearSelection();

        [UIAction("cell-selected")]
        private void CellSelected (TableView view, int idx)
        {
            SelectedCanvas = idx - 1;
            if (IsDeleting)
            {
                flowCoordinator.Value.SetRightViewController(null);
                DeactivateModals();
                if (idx == 0)
                {
                    canvasError.Show(true);
                }
                else
                {
                    deleteCanvas.Show(true);
                }
            }
            else
            {
                flowCoordinator.Value.SetRightViewController(hudEdit.Value);
                hudEdit.Value.ApplyCanvasForEditing(SelectedCanvas);
            }
        }

        [UIAction("create-new-canvas")]
        private void CreateNewCanvas(string name)
        {
            HUDCanvas settings = new HUDCanvas();
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) name = "New Canvas";
            settings.Name = name;
            canvasUtility.RegisterNewCanvas(settings, hudConfig.OtherCanvasSettings.Count);
            hudConfig.OtherCanvasSettings.Add(settings);
            mainConfig.HUDConfig = hudConfig;
            counterEdit.Value.ClearCachedSettings();
            RefreshData();
        }

        [UIAction("delete-selected-canvas")]
        private void DeleteSelectedCanvas()
        {
            DeactivateModals();
            if (SelectedCanvas == -1) return;
            
            // Update all counters using this canvas to use the main canvas instead
            IEnumerable<ConfigModel> needToUpdate = flowCoordinator.Value.AllConfigModels.Where(x => x.CanvasID == SelectedCanvas);
            foreach (var config in needToUpdate)
            {
                config.CanvasID = -1;
            }
            
            // Update all counters using canvases after this one (their IDs will shift down by 1)
            IEnumerable<ConfigModel> needToShift = flowCoordinator.Value.AllConfigModels.Where(x => x.CanvasID > SelectedCanvas);
            foreach (var config in needToShift)
            {
                config.CanvasID--;
            }
            
            // Save all modified custom counter configs back to the main config
            foreach (var config in flowCoordinator.Value.AllConfigModels)
            {
                if (config is CustomConfigModel customConfig && customConfig.AttachedCustomCounter != null)
                {
                    mainConfig.CustomCounters[customConfig.AttachedCustomCounter.Name] = customConfig;
                }
            }
            
            // Unregister and destroy the deleted canvas
            canvasUtility.UnregisterCanvas(SelectedCanvas);
            
            // Remove from settings list
            hudConfig.OtherCanvasSettings.RemoveAt(SelectedCanvas);
            
            // Now rebuild ALL canvas mappings from scratch to ensure consistency
            // Clear all existing mappings first (except -1 which is the main canvas)
            for (int i = 0; i < hudConfig.OtherCanvasSettings.Count + 1; i++) // +1 because we just removed one
            {
                try
                {
                    canvasUtility.UnregisterCanvas(i);
                }
                catch
                {
                    // Canvas might not exist, that's okay
                }
            }
            
            // Re-register all canvases with their new indices
            for (int i = 0; i < hudConfig.OtherCanvasSettings.Count; i++)
            {
                canvasUtility.RegisterNewCanvas(hudConfig.OtherCanvasSettings[i], i);
            }
            
            mainConfig.HUDConfig = hudConfig;
            counterEdit.Value.ClearCachedSettings();
            SelectedCanvas--;
            RefreshData();
            flowCoordinator.Value.RefreshAllMockCounters();
        }

        [UIAction("cancel-deletion")]
        private void CancelCanvasDeletion()
        {
            DeactivateModals();
        }
    }
}
