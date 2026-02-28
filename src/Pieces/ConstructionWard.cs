using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public static class WardVars
{
    public static readonly int isBuilding = "Workshop.ConstructionWard.IsBuilding".GetStableHashCode();
}
public class ConstructionWard : MonoBehaviour
{
    private static Piece PIECE;
    public const string SHARED_NAME = "$piece_construction_ward";
    private static readonly List<ConstructionWard> instances = new();
    private static readonly int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    public const string BUILD_KEY = "JoyLTrigger";
    private static float lastGhostPieceChangeTime;

    public Piece m_piece;
    public ZNetView m_nview;
    public WearNTear m_wearNTear;
    public Container m_container;

    public List<GhostPiece> m_ghostPieces = new();
    private readonly List<PieceData> m_disabledPieceData = new();
    private readonly Dictionary<string, PieceData> m_pieces = new();
    private readonly List<Piece.Requirement> m_totalRequirements = new();
    public bool reloadGhosts = true;
    public bool isSearching;
    public float ghostsCount;
    public float ghostProcessed;
    public Task LoadingGhostTask;

    public Vector3 m_connectionOffset = new Vector3(0.0f, 1.0f, 0.0f);
    private readonly Dictionary<string, StationData> m_connections = new();
    public bool connectionEnabled = true;

    public GameObject m_areaMarker;
    public CircleProjector m_areaMarkerCircle;

    public GameObject m_inRangeEffect;
    public GameObject m_connectEffect;
    public MeshRenderer m_model;
    public GameObject m_enabledEffect;
    public List<Material> m_materials = new();

    public EffectList m_flashEffect;
    public EffectList m_activateEffect;
    public EffectList m_addPermittedEffect;
    public EffectList m_removedPermittedEffect;

    public List<CraftingStation> stationsInRange = new();
    private bool isBuilding;
    private CancellationTokenSource cancelToken;
    public float m_constructionTimer;
    public float m_constructionLength;

    public static implicit operator bool(ConstructionWard ward) => ward != null;
    public void Awake()
    {
        instances.Add(this);

        m_piece = GetComponent<Piece>();
        m_nview = GetComponent<ZNetView>();
        m_wearNTear = GetComponent<WearNTear>();
        m_container = GetComponent<Container>();
        
        if (m_nview.IsValid())
        {
            InvokeRepeating(nameof(UpdateConnection), 1f, 4f);
            InvokeRepeating(nameof(UpdateAreaMarker), 0f, 2f);
        }

        connectionEnabled = ConfigManager.ConnectionEnabled;
        Invoke(nameof(UpdateGhostPieces), 5f);
    }

    public List<CraftingStation> GetConnectedStations() => stationsInRange;
    
    public void UpdateAreaMarker()
    {
        if (!Player.m_localPlayer) return;
        float distance = Vector3.Distance(transform.position, Player.m_localPlayer.transform.position);
        ToggleAreaMarker(distance < ConfigManager.BuildRange);
    }

    public void ToggleAreaMarker(bool enable)
    {
        if (!m_areaMarker) return;
        m_areaMarker.SetActive(enable);
        m_areaMarkerCircle.m_radius = ConfigManager.BuildRange;
    }
    public void OnDestroy()
    {
        instances.Remove(this);
    }

    public void UpdateConnection()
    {
        stationsInRange = FindStationsInRange();
        StopConnectionEffect();
        if (stationsInRange.Count <= 0) return;
        StartConnectionEffect(stationsInRange);
    }
    
    private List<CraftingStation> FindStationsInRange() 
    {
        Dictionary<string, CraftingStation> closestStations = new Dictionary<string, CraftingStation>();
    
        for (int i = 0; i < CraftingStation.m_allStations.Count; i++) 
        {
            CraftingStation station = CraftingStation.m_allStations[i];
            float distance = Vector3.Distance(transform.position, station.transform.position);
        
            if (distance > ConfigManager.MaxCraftingStationRange || station.GetComponent<GhostPiece>()) continue;
        
            if (!closestStations.TryGetValue(station.m_name, out CraftingStation existing) ||
                Vector3.Distance(existing.transform.position, transform.position) > distance)
            {
                closestStations[station.m_name] = station;
            }
        }
    
        return new List<CraftingStation>(closestStations.Values);
    }

    private Vector3 GetConnectionPoint() => transform.TransformPoint(m_connectionOffset);
    
    public void StartConnectionEffect(List<CraftingStation> stations)
    {
        for (int i = 0; i < stations.Count; ++i)
        {
            CraftingStation station = stations[i];
            StartConnectionEffect(station);
        }
    }

    public bool HasCraftingStation(CraftingStation station)
    {
        if (station == null) return true;
        return m_connections.ContainsKey(station.m_name);
    }

    public void StartConnectionEffect(CraftingStation station)
    {
        Vector3 targetPos = station.GetConnectionEffectPoint();
        Vector3 connectionPoint = GetConnectionPoint();

        if (!m_connections.TryGetValue(station.m_name, out StationData connection))
        {
            connection = new StationData(station, m_connectEffect, connectionPoint, transform);
            m_connections[station.m_name] = connection;
        }
        
        Vector3 distance = targetPos - connectionPoint;
        Quaternion quaternion = Quaternion.LookRotation(distance.normalized);
        connection.effectPrefab.transform.position = connectionPoint;
        connection.effectPrefab.transform.rotation = quaternion;
        connection.effectPrefab.transform.localScale = new Vector3(1f, 1f, distance.magnitude);
    }

    public void StopConnectionEffect()
    {
        if (m_connections.Count <= 0) return;
        List<string> connectionsToRemove = new();
        foreach (KeyValuePair<string, StationData> kvp in m_connections)
        {
            if (!stationsInRange.Contains(kvp.Value.station))
            {
                Destroy(kvp.Value.effectPrefab);
                connectionsToRemove.Add(kvp.Key);
            }
        }
        foreach (string key in connectionsToRemove)
        {
            m_connections.Remove(key);
        }
    }
    
    public void UpdateGhostPieces()
    {
        if (isBuilding || isSearching || !reloadGhosts) return;
        TimeSpan delay = TimeSpan.FromMilliseconds(5f);
        LoadingGhostTask = LoadGhostPieces(GhostPiece.m_instances, delay);
    }

    private async Task LoadGhostPieces(List<GhostPiece> ghosts, TimeSpan delay)
    {
        isSearching = true;
        ghostsCount = ghosts.Count + 1;
        ghostProcessed = 0.0f;
        m_ghostPieces.Clear();
        m_pieces.Clear();
        m_totalRequirements.Clear();
        for (int i = 0; i < ghosts.Count; ++i)
        {
            await Task.Delay(delay);
            ++ghostProcessed;
            GhostPiece ghost = ghosts[i];
            float distance = Utils.DistanceXZ(ghost.transform.position, transform.position);
            if (distance > ConfigManager.BuildRange) continue;
            m_ghostPieces.Add(ghost);
            if (ghost.m_piece != null)
            {
                if (m_pieces.TryGetValue(ghost.m_piece.m_name, out PieceData data))
                {
                    data.ghosts.Add(ghost);
                    ++data.count;
                }
                else
                {
                    PieceData pieceData = new PieceData
                    {
                        piece = ghost.m_piece,
                        count = 1
                    };
                    pieceData.ghosts.Add(ghost);

                    m_pieces[ghost.m_piece.m_name] = pieceData;
                }
            }
        }

        foreach (KeyValuePair<string, PieceData> kvp in m_pieces)
        {
            kvp.Value.GetRequirements();
        }
        
        Dictionary<string, Piece.Requirement> requirements = new();

        List<PieceData> pieces = m_pieces.Values.ToList();
        for (int i = 0; i < pieces.Count; ++i)
        {
            PieceData data = pieces[i];
            if (data.IsDisabled(this)) continue;
            List<Piece.Requirement> reqs = data.GetRequirements();
            for (int j = 0; j < reqs.Count; ++j)
            {
                Piece.Requirement requirement = reqs[j];
                string item = requirement.m_resItem.m_itemData.m_shared.m_name;
                if (requirements.TryGetValue(item, out Piece.Requirement req))
                {
                    req.m_amount += requirement.m_amount;
                }
                else
                {
                    Piece.Requirement res = new Piece.Requirement
                    {
                        m_resItem = requirement.m_resItem,
                        m_amount = requirement.m_amount
                    };
                    requirements.Add(item, res);
                }
            }
        }
        
        m_totalRequirements.AddRange(requirements.Values);

        isSearching = false;
        ghostsCount = 1;
        ghostProcessed = 0.0f;
        LoadingGhostTask = null;
        reloadGhosts = false;
    }

    public List<GhostPiece> GetGhostPieces() => m_ghostPieces;
    public List<PieceData> GetPieces() => m_pieces.Values.ToList();

    public List<Piece.Requirement> GetTotalBuildRequirements()
    {
        return m_totalRequirements;
    }

    public HashSet<CraftingStation> GetRequiredCraftingStations()
    {
        HashSet<CraftingStation> stations = new();
        for (int i = 0; i < m_ghostPieces.Count; ++i)
        {
            GhostPiece ghost = m_ghostPieces[i];
            if (ghost == null) continue;
            if (ghost.TryGetCraftingStation(out CraftingStation station))
            {
                stations.Add(station);
            }
        }
        return stations;
    }

    public List<CraftingStation> GetMissingCraftingStations()
    {
        HashSet<CraftingStation> stations = GetRequiredCraftingStations();
        List<CraftingStation> missing = new();
        foreach (CraftingStation station in stations)
        {
            if (!HasCraftingStation(station))
            {
                missing.Add(station);
            }
        }
        return missing;
    }

    public void SetIsBuilding(bool value)
    {
        m_nview.GetZDO().Set(WardVars.isBuilding, value);
    }

    public bool IsBuilding() => m_nview.IsValid() ? m_nview.GetZDO().GetBool(WardVars.isBuilding) : isBuilding;
    public void Build(Player player)
    {
        if (AreBuilding())
        {
            Workshop.LogDebug("Cannot build, already processing");
            return;
        }
        if (m_ghostPieces.Count <= 0)
        {
            UpdateGhostPieces();
            return;
        }
        m_activateEffect.Create(transform.position, transform.rotation);
        List<GhostPiece> pieces = new(m_ghostPieces);
        List<GhostPiece> disabledPieces = m_disabledPieceData.SelectMany(p => p.ghosts).ToList();
        pieces.RemoveAll(p => disabledPieces.Contains(p) || p == null);
        List<GhostPiece> order = pieces.OrderBy(p => p.transform.position.y).ToList();

        cancelToken = new CancellationTokenSource();
        m_constructionLength = order.Count * ConfigManager.BuildInterval;
        m_constructionTimer = 0f;
        SetIsBuilding(true);
        _ = Process(player, order, cancelToken.Token);
    }

    public void Cancel()
    {
        if (cancelToken == null) return;
        
        cancelToken.Cancel();
        cancelToken.Dispose();
        cancelToken = null;
        SetIsBuilding(false);
    }

    private async Task Process(Player player, List<GhostPiece> pieces, CancellationToken cancellationToken = default)
    {
        isBuilding = true;
        m_enabledEffect.SetActive(true);
        int builtCount = 0;
        try
        {
            for (int i = 0; i < pieces.Count; ++i)
            {
                GhostPiece piece = pieces[i];
                if (!player.NoCostCheat() && !HaveRequirements(piece)) continue;
                if (!player.NoCostCheat()) ConsumeResources(piece);
                piece.Build(player, this);
                ++builtCount;
                m_constructionTimer += ConfigManager.BuildInterval;
                if (ConfigManager.BuildInterval > 0f)
                {
                    await Task.Delay(TimeSpan.FromSeconds(ConfigManager.BuildInterval), cancellationToken);
                }
            }
            player.Message(MessageHud.MessageType.Center, "$msg_finished_construction");
        }
        catch (OperationCanceledException)
        {
            int unfinishedCount = pieces.Count - builtCount;
            player.Message(MessageHud.MessageType.Center, $"$msg_cancelled_construction ($msg_construction_leftover: {unfinishedCount})");
        }
        finally
        {
            m_ghostPieces.Clear();
            m_enabledEffect.SetActive(false);
            m_constructionTimer = 0f;
            reloadGhosts = true;
            isBuilding = false;
            cancelToken.Dispose();
            cancelToken = null;
            SetIsBuilding(false);
            if (Tab.currentWard) InventoryGui.instance.UpdateCraftingPanel();
        }
    }
    
    [Obsolete]
    public void LoadGhosts()
    {
        m_ghostPieces.Clear();
        m_pieces.Clear();
        for (int i = 0; i < GhostPiece.m_instances.Count; ++i)
        {
            GhostPiece ghost = GhostPiece.m_instances[i];
            float distance = Utils.DistanceXZ(ghost.transform.position, transform.position);
            if (distance > ConfigManager.BuildRange) continue;
            m_ghostPieces.Add(ghost);
            if (ghost.m_piece != null)
            {
                if (m_pieces.TryGetValue(ghost.m_piece.m_name, out PieceData data))
                {
                    data.ghosts.Add(ghost);
                    ++data.count;
                }
                else
                {
                    PieceData pieceData = new PieceData
                    {
                        piece = ghost.m_piece,
                        count = 1
                    };
                    pieceData.ghosts.Add(ghost);
        
                    m_pieces[ghost.m_piece.m_name] = pieceData;
                }
            }
        }
    }

    [Obsolete]
    private IEnumerator ProcessConstruction(Player player, List<GhostPiece> pieces)
    {
        isBuilding = true;
        m_enabledEffect.SetActive(true);
        for (int i = 0; i < pieces.Count; ++i)
        {
            GhostPiece piece = pieces[i];
            if (!player.NoCostCheat() && !HaveRequirements(piece)) continue;
            piece.m_nview.ClaimOwnership();
            if (!player.NoCostCheat()) ConsumeResources(piece);
            piece.Build(player, this);
            yield return new WaitForSeconds(ConfigManager.BuildInterval);
        }
        m_ghostPieces.Clear();
        m_enabledEffect.SetActive(false);
        isBuilding = false;
        player.Message(MessageHud.MessageType.Center, "$msg_finished_construction");
    }
    
    private bool HaveBuildStation(GhostPiece ghost)
    {
        if (!ghost.m_piece) return true;
        return m_connections.ContainsKey(ghost.m_piece.m_craftingStation.m_name);
    }

    private bool HaveRequirements(GhostPiece ghost)
    {
        if (!ghost.m_piece) return true;
        if (!HaveBuildStation(ghost)) return false;
        if (ghost.m_piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(ghost.m_piece.m_dlc)) return false;

        if (ZoneSystem.instance.GetGlobalKey(ghost.m_piece.FreeBuildKey())) return true;

        for (int i = 0; i < ghost.m_piece.m_resources.Length; ++i)
        {
            Piece.Requirement res = ghost.m_piece.m_resources[i];
            int inventoryCount = m_container.GetInventory().CountItems(res.m_resItem.m_itemData.m_shared.m_name);
            if (inventoryCount < res.m_amount) return false;
        }

        if (ghost.inventory.NrOfItems() > 0)
        {
            List<ItemDrop.ItemData> items = ghost.inventory.GetAllItems();
            for (int i = 0; i < items.Count; ++i)
            {
                ItemDrop.ItemData item = items[i];
                int inventoryCount = m_container.GetInventory().CountItems(item.m_shared.m_name);
                if (inventoryCount < item.m_stack) return false;
            }
        }

        if (ghost.itemStand != null &&
            ghost.itemStand.TryGetPieceRequirement(out Piece.Requirement itemStandRequirement) && 
            !m_container.GetInventory().HaveItem(itemStandRequirement.m_resItem.m_itemData.m_shared.m_name))
        {
            return false;
        }
        return true;
    }
    
    public void ConsumeResources(GhostPiece ghost)
    {
        if (!ghost.m_piece || ZoneSystem.instance.GetGlobalKey(ghost.m_piece.FreeBuildKey())) return;
        for (int i = 0; i < ghost.m_piece.m_resources.Length; ++i)
        {
            Piece.Requirement res = ghost.m_piece.m_resources[i];
            m_container.GetInventory().RemoveItem(res.m_resItem.m_itemData.m_shared.m_name, res.m_amount);
        }

        if (ghost.inventory.NrOfItems() > 0)
        {
            List<ItemDrop.ItemData> items = ghost.inventory.GetAllItems();
            for (int i = 0; i < items.Count; ++i)
            {
                ItemDrop.ItemData item = items[i];
                m_container.GetInventory().RemoveItem(item.m_shared.m_name, item.m_stack);
            }
        }

        if (ghost.itemStand != null &&
            ghost.itemStand.TryGetPieceRequirement(out Piece.Requirement itemStandRequirement))
        {
            m_container.GetInventory().RemoveItem(itemStandRequirement.m_resItem.m_itemData.m_shared.m_name, itemStandRequirement.m_amount);
        }
    }
    
    public void SetEmission(Color color) => m_materials[0].SetColor(_EmissionColor, color );

    public string GetHoverText()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(m_container.m_name);
        if (m_container.m_inventory.NrOfItems() == 0)
        {
            sb.Append(" ( $piece_container_empty )");
        }
        sb.Append($"\n$hover_connected_pieces ( {m_ghostPieces.Count} )");
        sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open $msg_stackall_hover");
        sb.AppendFormat("\n[<color=yellow><b>{0}</b></color>] {1}", 
            "$button_lalt + $KEY_Use", 
            isBuilding ? "$ward_cancel" : "$ward_build");
        
        return Localization.instance.Localize(sb.ToString());
    }

    public void ToggleConnectionEffect()
    {
        connectionEnabled = !connectionEnabled;
        foreach (StationData data in m_connections.Values)
        {
            if (connectionEnabled) data.connection.Play();
            else data.connection.Stop();
        }
    }

    public class PieceData
    {
        public readonly List<GhostPiece> ghosts = new List<GhostPiece>();
        public Piece piece;
        public int count;
        public Image icon;
        public TMP_Text name;
        public List<Piece.Requirement> requirements = new List<Piece.Requirement>();

        public bool IsDisabled(ConstructionWard ward) => ward.m_disabledPieceData.Contains(this);
        
        public void OnCraft(InventoryGui gui, ConstructionWard ward, string label)
        {
            bool isRemoved = IsDisabled(ward);

            if (isRemoved)
            {
                ward.m_disabledPieceData.Remove(this);
                icon.color = Color.white;
                name.fontStyle = FontStyles.Normal;
            }
            else
            {
                ward.m_disabledPieceData.Add(this);
                icon.color = Color.black;
                name.fontStyle = FontStyles.Strikethrough;
            }

            Tab.craftButtonLabel.text = Localization.instance.Localize(label);
            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
        }

        public bool HasRequirements(ConstructionWard ward)
        {
            for (int i = 0; i < requirements.Count; ++i)
            {
                Piece.Requirement requirement = requirements[i];
                string item = requirement.m_resItem.m_itemData.m_shared.m_name;
                int amount = requirement.m_amount;
                int inventoryCount = ward.m_container.GetInventory().CountItems(item);
                if (amount > inventoryCount) return false;
            }

            return true;
        }

        public List<Piece.Requirement> GetRequirements()
        {
            requirements.Clear();
            for (int i = 0; i < piece.m_resources.Length; ++i)
            {
                Piece.Requirement req = piece.m_resources[i];
                requirements.Add(new  Piece.Requirement
                {
                    m_resItem = req.m_resItem,
                    m_amount = req.m_amount * count
                });
            }

            for (int i = 0; i < ghosts.Count; ++i)
            {
                GhostPiece ghost = ghosts[i];
                if (ghost.itemStand != null &&
                    ghost.itemStand.TryGetPieceRequirement(out Piece.Requirement itemStandRequirement))
                {
                    requirements.Add(itemStandRequirement);
                }
                if (ghost.inventory.NrOfItems() <= 0) continue;
                AppendInventoryItems(ref requirements, ghost.inventory);
            }
            return requirements;
        }

        private static void AppendInventoryItems(ref List<Piece.Requirement> requirements, Inventory inventory)
        {
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            for (int i = 0; i < items.Count; ++i)
            {
                ItemDrop.ItemData item = items[i];
                if (!PrefabManager.TryGetItemDrop(item.m_shared.m_name, out ItemDrop itemDrop)) continue;
                requirements.Add(new Piece.Requirement
                {
                    m_resItem = itemDrop,
                    m_amount = item.m_stack
                });
            }
        }
    }

    private class StationData
    {
        public readonly CraftingStation station;
        public readonly GameObject effectPrefab;
        public readonly StationConnection connection;

        public StationData(CraftingStation station, GameObject prefab, Vector3 connectionPoint, Transform parent)
        {
            this.station = station;
            effectPrefab = Instantiate(prefab, connectionPoint, Quaternion.identity, parent);
            connection = effectPrefab.GetComponent<StationConnection>();
        }
    }

    public static void OnEmissionColorChange(object sender, EventArgs e)
    {
        for (int i = 0; i < instances.Count; ++i)
        {
            ConstructionWard instance = instances[i];
            instance.SetEmission(ConfigManager.WardEmissionColor * ConfigManager.WardEmissionIntensity);
        }
    }

    public static List<ConstructionWard> GetConstructionWards() => instances;

    public static List<ConstructionWard> GetConstructionWardsInRange(Vector3 point, float range)
    {
        List<ConstructionWard> wards = new();
        for (int i = 0; i < instances.Count; ++i)
        {
            ConstructionWard ward = instances[i];
            float distance = Vector3.Distance(point, ward.transform.position);
            if (distance > range) continue;
            wards.Add(ward);
        }

        return wards;
    }


    public static void OnGhostPiecesChanged()
    {
        if (Time.time - lastGhostPieceChangeTime < 1f) return;
        lastGhostPieceChangeTime = Time.time;
        for (int i = 0; i < instances.Count; ++i)
        {
            ConstructionWard instance = instances[i];
            instance.reloadGhosts = true;
        }
    }

    public static bool AreBuilding() => instances.Any(x => x.IsBuilding());

    public static void OnRecipeChange(object sender, EventArgs e)
    {
        Piece.Requirement[] recipe = ConfigManager.WardRecipe;
        if (PIECE != null)
        {
            PIECE.m_resources = recipe;
        }
        for (int i = 0; i < instances.Count; ++i)
        {
            ConstructionWard ward = instances[i];
            ward.m_piece.m_resources = recipe;
        }
    }
    public static void Setup()
    {
        Mock constructionWard = new Mock("guard_stone", "ConstructionWard");
        constructionWard.OnCreated += prefab =>
        {
            PrivateArea privateArea = prefab.GetComponent<PrivateArea>();
            ConstructionWard ward = prefab.AddComponent<ConstructionWard>();
            Container container = prefab.AddComponent<Container>();
            container.m_name = SHARED_NAME;
            container.m_width = 8;
            container.m_height = 15;
            if (prefab.TryGetComponent(out Piece piece))
            {
                piece.m_name = SHARED_NAME;
                piece.m_description = SHARED_NAME + "_desc";
                piece.m_resources = ConfigManager.WardRecipe;
                PIECE = piece;
            }
            
            ward.m_areaMarkerCircle = privateArea.m_areaMarker;
            ward.m_areaMarker = privateArea.m_areaMarker.gameObject;
            ward.m_areaMarker.SetActive(false);

            GameObject effect = Instantiate(privateArea.m_connectEffect, MockManager.transform);
            effect.name = "vfx_constructionward_connection";
            StationConnection connection = effect.AddComponent<StationConnection>();
            connection.system = effect.transform.Find("Particle System").GetComponent<ParticleSystem>();
            connection.glow = effect.transform.Find("glow").GetComponent<ParticleSystem>();
            connection.poff = effect.transform.Find("poff").GetComponent<ParticleSystem>();
            connection.backward = effect.transform.Find("Backward").GetComponent<ParticleSystem>();

            ward.m_connectEffect = effect;
            ward.m_inRangeEffect = privateArea.m_inRangeEffect;
            ward.m_model = privateArea.m_model;
            ward.m_enabledEffect = privateArea.m_enabledEffect;
            ward.m_flashEffect = privateArea.m_flashEffect;
            ward.m_activateEffect = privateArea.m_activateEffect;
            ward.m_addPermittedEffect = privateArea.m_addPermittedEffect;
            ward.m_removedPermittedEffect = privateArea.m_removedPermittedEffect;

            List<Material> materials = new List<Material>();
            for (int i = 0; i < ward.m_model.sharedMaterials.Length; ++i)
            {
                Material material = ward.m_model.sharedMaterials[i];
                materials.Add(new Material(material));
            }
            ward.m_model.materials = materials.ToArray();
            ward.m_model.sharedMaterials = materials.ToArray();
            ward.m_materials = materials;
            ward.SetEmission(ConfigManager.WardEmissionColor * ConfigManager.WardEmissionIntensity);
            
            prefab.Remove<PrivateArea>();

            GuidePoint guidePoint = prefab.GetComponentInChildren<GuidePoint>(true);
            guidePoint.m_text.m_key = "ConstructionWard";
            guidePoint.m_text.m_topic = "$tutorial_construction_ward_topic";
            guidePoint.m_text.m_label = "$tutorial_construction_ward_label";
            guidePoint.m_text.m_text = "$tutorial_construction_ward_text";
            guidePoint.m_text.m_munin = true;

            guidePoint.gameObject.SetActive(true);
            
            BuildTools.planHammer.InsertPiece(prefab);
        };
    }
}