using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;
using WDE.SqlQueryGenerator;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Common.Database;

namespace WDE.MapSpawns.Rendering;

public class SpawnContextMenu
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IMessageBoxService messageBoxService;

    private ICommand CopyGuidCommand { get; }
    private ICommand CopyPositionCommand { get; }
    private ICommand CopyOrientationCommand { get; }
    private ICommand UpdateValuesCommand { get; }

    public SpawnContextMenu(
        ISpawnSelectionService spawnSelectionService,
        IClipboardService clipboardService,
        ITableEditorPickerService tableEditorPickerService,
        IMySqlExecutor mySqlExecutor,
        ITableDefinitionProvider definitionProvider,
        IMessageBoxService messageBoxService)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.mySqlExecutor = mySqlExecutor;
        this.definitionProvider = definitionProvider;
        this.messageBoxService = messageBoxService;

        CopyGuidCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Guid.ToString()));
        CopyPositionCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText($"X: {inst.WorldObject!.Position.X} Y: {inst.WorldObject!.Position.Y} Z: {inst.WorldObject!.Position.Z}"));
        CopyOrientationCommand = new DelegateCommand<SpawnInstance>(inst =>
        {
            CreatureSpawnInstance? creature = inst as CreatureSpawnInstance;
            GameObjectSpawnInstance? go = inst as GameObjectSpawnInstance;

            if (creature != null) 
            { 
                clipboardService.SetText(creature.Creature!.Orientation.ToString()); 
            }
            else if (go != null) 
            {
                clipboardService.SetText(go.GameObject!.Orientation.ToString());
            }
        });
        UpdateValuesCommand = new DelegateCommand<SpawnInstance>(async (inst) =>
        {
            var transaction = Queries.BeginTransaction();

            CreatureSpawnInstance? creature = inst as CreatureSpawnInstance;
            GameObjectSpawnInstance? go = inst as GameObjectSpawnInstance;

            var typeString = creature != null ? "creature" : "gameobject";

            var update = transaction.Table(typeString)
                .Where(row => row.Column<uint>("Guid") == inst.Guid)
                .ToUpdateQuery();

            update = update.Set("position_x", inst.WorldObject!.Position.X);
            update = update.Set("position_y", inst.WorldObject!.Position.Y);
            update = update.Set("position_z", inst.WorldObject!.Position.Z);

            var orientation = creature != null ? creature.Creature!.Orientation : go!.GameObject!.Orientation;

            update = update.Set("orientation", orientation);
            
            if(go != null)
            {
                update = update.Set("rotation0", go.GameObject!.Rotation.X);
                update = update.Set("rotation1", go.GameObject!.Rotation.Y);
                update = update.Set("rotation2", go.GameObject!.Rotation.Z);
                update = update.Set("rotation3", go.GameObject!.Rotation.W);
            }

            update.Update();
            var query = transaction.Close();

            var definition = definitionProvider.GetDefinition(typeString);

            try
            {
                await mySqlExecutor.ExecuteSql(query);
            }
            catch (Exception e)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error")
                    .SetMainInstruction("Error while saving")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }
        });
    }
    
    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        var spawn = spawnSelectionService.SelectedSpawn.Value;
        if (spawn == null || !spawn.IsSpawned)
            yield break;

        yield return ("Copy guid", CopyGuidCommand, spawn);
        yield return ("Copy position", CopyPositionCommand, spawn);
        yield return ("Copy orientation", CopyOrientationCommand, spawn);
        yield return ("Update values", UpdateValuesCommand, spawn);
    }
}