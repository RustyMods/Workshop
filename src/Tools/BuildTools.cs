
using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public static class BuildTools
{
    public static readonly Sprite BlueprintIcon = AssetBundleManager.LoadAsset<Sprite>("buildtoolbundle", "blueprint_icon");
    private const string FlagMarkerSharedName = "$piece_flag_marker";
    private static bool toolsLoaded;
    public static GameObject TerrainFlag;

    public static GhostHammer ghostHammer;
    public static BlueprintHammer planHammer;
    
    public static void OnFejdStartup()
    {
        if (toolsLoaded) return;
        
        GameObject blueprintTable = BlueprintTable.Setup(out CraftingStation blueprintStation);
        BlueprintTable.CRAFTING_STATION = blueprintStation;
        
        GameObject terrainFlag = SetupFlagMarker();
        
        ghostHammer = new GhostHammer("$item_plan_hammer", blueprintStation);
        planHammer = new BlueprintHammer("$item_blueprint_build_hammer", blueprintStation);

        planHammer.InsertPiece(blueprintTable, 1);
        planHammer.InsertPiece(terrainFlag, 1);

        _ = new SelectByBounds("piece_select_many", "$piece_select_many");
        _ = new SelectByArea("piece_select_area", "$piece_select_area");
        _ = new Select("piece_select", "$piece_select");
        _ = new Move("piece_move", "$piece_move");
        _ = new RemoveByArea("piece_remove_area", "$piece_remove_area");
        _ = new RemoveByBounds("piece_remove_many", "$piece_remove_many");
        
        _ = new Lava("piece_paint_lava", "$piece_lava");
        _ = new AshlandsGrass("piece_paint_ashlands_grass", "$piece_ashlands_grass");
        _ = new PavedGrass("piece_paint_paved_grass", "$piece_paved_grass");
        _ = new PavedDark("piece_paint_paved_cultivated", "$piece_paved_dark");
        _ = new MistlandsGrass("piece_paint_mistlands", "$piece_mistlands_grass");
        _ = new MistlandsDirt("piece_paint_mistlands_dirt", "$piece_mistlands_dirt");
        _ = new BlackForestGrass("piece_paint_blackforest_grass", "$piece_blackforest_grass");
        _ = new PlainsGrass("piece_paint_plains_grass", "$piece_plains_grass");
        _ = new Snow("piece_paint_snow", "$piece_snow");
        _ = new SwampGrass("piece_paint_swamp_grass", "$piece_swamp_grass");
        _ = new MeadowsGrass("piece_paint_meadows_grass", "$piece_meadows_grass");
        _ = new Reset("piece_paint_reset", "$piece_reset_all");
        
        toolsLoaded = true;
    }

    public static GameObject SetupFlagMarker()
    {
        GameObject asset = AssetBundleManager.LoadAsset<GameObject>("buildtoolbundle", "FlagMarker_RS");
        Piece piece = asset.GetComponent<Piece>();
        piece.m_name = FlagMarkerSharedName;
        piece.m_category = Piece.PieceCategory.Misc;
        asset.AddComponent<TerrainFlag>();
        
        GameObject guardStone = PrefabManager.GetPrefab("dverger_guardstone");
        PrivateArea area = guardStone.GetComponent<PrivateArea>();
        GameObject marker = area.m_areaMarker.m_prefab;
        
        CustomProjector projector = asset.AddComponent<CustomProjector>();
        projector.m_prefab = marker;
        
        projector.enabled = false;
        
        PrefabManager.RegisterPrefab(asset);

        TerrainFlag = asset;
        return asset;
    }
    
    public static bool IsConstructionWard(this Piece piece) => piece != null && piece.m_name == ConstructionWard.SHARED_NAME;

    public static bool IsGhostHammer(this ItemDrop.ItemData item) =>
        item != null && item.m_shared.m_name.Equals(ghostHammer.sharedName);

    public static bool IsPlanHammer(this ItemDrop.ItemData item) =>
        item != null && item.m_shared.m_name.Equals(planHammer.sharedName);
    
    public static bool IsFlagMarker(this Piece piece) => piece != null && piece.m_name.Equals(FlagMarkerSharedName);

    public static void Place(
        Player player, 
        GameObject prefab,
        Vector3 pos, 
        Quaternion rot, 
        GhostPiece ghost,
        ConstructionWard ward = null)
    {
        TerrainModifier.SetTriggerOnPlaced(true);
        GameObject instance = Object.Instantiate(prefab, pos, rot);
        TerrainModifier.SetTriggerOnPlaced(false);

        if (ward != null && instance.TryGetComponent(out ZNetView znv))
        {
            ZDO ZDO = znv.GetZDO();
            if (ZDO != null)
            {
                ZDOHelper.TryDeserialize(ZDO, ghost.zdo, pos, rot);
                ZDOHelper.LoadItemStand(player, ZDO, ward, instance, ghost.itemStand);
                ZDOHelper.LoadDoorState(ZDO, instance, ghost.state);
                
                if (instance.TryGetComponent(out TerrainFlag marker))
                {
                    marker.SetPaintType(ghost.m_type);
                    marker.SetIsSquare(ghost.m_isSquare);
                    marker.SetRadius(ghost.m_radius);
                    marker.SetSmoothRadius(ghost.m_smoothRadius);
                    marker.SetLevel(ghost.m_level);
                    marker.Poke(true);
                    marker.interactable = false;
                }
            }
        }
        
        if (instance.TryGetComponent(out Piece piece))
        {
            piece.SetCreator(player.GetPlayerID());
            if (ConfigManager.UsePlaceEffects)
            {
                piece.m_placeEffect.Create(pos, rot, instance.transform);
            }
        }

        if (instance.TryGetComponent(out PrivateArea privateArea))
        {
            privateArea.Setup(Game.instance.GetPlayerProfile().GetName());
        }

        if (instance.TryGetComponent(out WearNTear wear))
        {
            wear.OnPlaced();
        }
        
        Player.m_placed.Clear();
        instance.GetComponents(Player.m_placed);
        for (int i = 0; i < Player.m_placed.Count; ++i)
        {
            IPlaced iPlace = Player.m_placed[i];
            iPlace.OnPlaced();
        }

        player.AddNoise(50f);

        Workshop.LogDebug($"Placed {prefab.name}");
    }
}