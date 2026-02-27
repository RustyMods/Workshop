using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public static class GhostVars
{
    public static readonly int IsGhost = "GhostPiece".GetStableHashCode();
    public static readonly int ZDO = "GhostPiece.ZDO".GetStableHashCode();
    public static readonly int ItemStand = "GhostPiece.ItemStand".GetStableHashCode();
    public static readonly int State = "GhostPiece.State".GetStableHashCode();
}
public class GhostPiece : MonoBehaviour
{
    public static readonly List<GhostPiece> m_instances = new();
    
    private readonly Dictionary<Collider, int> m_colliderLayers = new();
    public ZNetView m_nview;
    public Piece m_piece;
    public string zdo = string.Empty;
    public ItemStandItemData itemStand;
    public int state;
    public Inventory inventory = new Inventory("GhostInventory", null, 8, 5);
    
    public TerrainModifier.PaintType m_type;
    public bool m_isSquare;
    public float m_radius;
    public float m_smoothRadius;
    public bool m_level;

    public void Awake()
    {
        m_instances.Add(this);
        m_nview = GetComponent<ZNetView>();
        m_piece = GetComponent<Piece>();

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; ++i)
        {
            Collider collider = colliders[i];
            m_colliderLayers[collider] = collider.gameObject.layer;
        }

        if (m_piece)
        {
            m_piece.m_primaryTarget = false;
            m_piece.m_targetNonPlayerBuilt = false;
        }
    }

    public void Build(Player player, ConstructionWard ward)
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(m_nview.GetZDO().m_prefab);
        if (prefab == null) return;
        m_nview.ClaimOwnership();
        BuildTools.Place(player, prefab, transform.position, transform.rotation, this, ward);
        m_nview.Destroy();
    }

    public void Start()
    {
        ConstructionWard.OnGhostPiecesChanged();
        GhostMan.Register(gameObject);
        DisableOrRemoveComponents();
        if (m_nview && m_nview.GetZDO() != null)
        {
            SetOrSaveZDOData();
            m_nview.GetZDO().Set(GhostVars.IsGhost, true);
        }
    }

    public bool TryGetCraftingStation(out CraftingStation station)
    {
        station = m_piece ? m_piece.m_craftingStation : null;
        return station != null;
    }

    public void SetOrSaveZDOData()
    {
        if (string.IsNullOrEmpty(zdo))
        {
            zdo = m_nview.GetZDO().GetString(GhostVars.ZDO);
        }
        else
        {
            m_nview.GetZDO().Set(GhostVars.ZDO, zdo);
        }

        if (itemStand != null && itemStand.isValid)
        {
            m_nview.GetZDO().Set(GhostVars.ItemStand, itemStand.ToString());
        }
        else
        {
            itemStand = new ItemStandItemData(m_nview.GetZDO().GetString(GhostVars.ItemStand));
        }

        if (state != 0)
        {
            m_nview.GetZDO().Set(GhostVars.State, state);
        }
        else
        {
            state = m_nview.GetZDO().GetInt(GhostVars.State);
        }

        if (inventory.NrOfItems() > 0)
        {
            ZPackage pkg = new ZPackage();
            inventory.Save(pkg);
            string base64 = pkg.GetBase64();
            m_nview.GetZDO().Set(ZDOVars.s_items, base64);
        }
        else
        {
            string base64 = m_nview.GetZDO().GetString(ZDOVars.s_items);
            if (!string.IsNullOrEmpty(base64))
            {
                ZPackage pkg = new ZPackage(base64);
                inventory.Load(pkg);
                Workshop.LogDebug($"[ Ghost Piece ] {name} loaded inventory: {inventory.NrOfItems()}");
            }
        }
    }

    public void DisableOrRemoveComponents()
    {
        RemoveComponent<Rigidbody>();
        RemoveComponent<Humanoid>();
        RemoveComponent<MonsterAI>();
        RemoveComponent<BaseAI>();

        EnableColliders(false);
        EnableParticles(false);
        EnableComponent<ParticleSystemForceField>(false);
        EnableComponent<Demister>(false);
        EnableComponent<TerrainModifier>(false);
        EnableComponent<GuidePoint>(false);
        EnableComponent<LightLod>(false);
        EnableComponent<LightFlicker>(false);
        EnableComponent<Light>(false);
        EnableComponent<AudioSource>(false);
        EnableComponent<ZSFX>(false);
        EnableComponent<WispSpawner>(false);
        EnableComponent<Windmill>(false);
        EnableComponent<Aoe>(false);
        EnableComponent<SmokeSpawner>(false);
        EnableComponent<EffectArea>(false);
        EnableComponent<EffectFade>(false);
        EnableComponent<CookingStation>(false);
        EnableComponent<Smelter>(false);
        EnableComponent<Fermenter>(false);
        EnableFirePlace(false);
        
        RemoveComponent<ParticleSystemForceField>();
        RemoveComponent<Demister>();
        RemoveComponent<TerrainModifier>();
        RemoveComponent<GuidePoint>();
        RemoveComponent<LightLod>();
        RemoveComponent<Light>();
        RemoveComponent<LightFlicker>();
        RemoveComponent<AudioSource>();
        RemoveComponent<ZSFX>();
        RemoveComponent<WispSpawner>();
        RemoveComponent<Windmill>();
        RemoveComponent<Aoe>();
        RemoveComponent<SmokeSpawner>();
        RemoveComponent<EffectArea>();
        RemoveComponent<EffectFade>();
        RemoveComponent<Fireplace>();
        RemoveComponent<CookingStation>();
        RemoveComponent<Fermenter>();
        RemoveComponent<Smelter>();
    }
    
    public void EnableColliders(bool enable)
    {
        foreach (KeyValuePair<Collider, int> kvp in m_colliderLayers)
        {
            if (enable)
            {
                kvp.Key.gameObject.layer = kvp.Value;
            }
            else
            {
                if (kvp.Key.isTrigger) continue;
                
                kvp.Key.gameObject.layer = LayerMask.NameToLayer("piece_nonsolid");
            }
        }
    }

    public void EnableParticles(bool enable)
    {
        foreach (ParticleSystem component in GetComponentsInChildren<ParticleSystem>(true))
        {
            component.Stop();
        }

    }
    
    public void EnableFirePlace(bool enable)
    {
        if (!TryGetComponent(out Fireplace component)) return;
        if (!enable) component.SetFuel(0f);
    }

    public void EnableComponent<T>(bool enable) where T : Behaviour
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            component.enabled = enable;
        }
    }

    public void RemoveComponent<T>() where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            Destroy(component);
        }
    }

    public void OnDestroy()
    {
        m_instances.Remove(this);
        GhostMan.UnRegister(gameObject);
        ConstructionWard.OnGhostPiecesChanged();
    }

    public static void PlacePiece(Player player, GameObject prefab, Vector3 pos, Quaternion rot, bool doAttack, Plan plan = null)
    {
        if (prefab == null) return;
        
        GameObject instance = Instantiate(prefab, pos, rot);
        instance.GetComponent<WearNTear>()?.OnPlaced();
        
        if (doAttack)
        {
            ItemDrop.ItemData rightItem = player.GetRightItem();
            if (rightItem != null)
            {
                player.FaceLookDirection();
                player.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
            }
        }

        instance.GetComponent<ItemDrop>()?.MakePiece(true);

        player.AddNoise(50f);

        GhostPiece ghost = instance.AddComponent<GhostPiece>();
        if (plan != null)
        {
            ghost.zdo = plan.zdo;
            ghost.itemStand = plan.itemStand;
            ghost.state = plan.state;
            ghost.inventory = plan.inventory;
            ghost.m_type = plan.m_type;
            ghost.m_isSquare = plan.m_isSquare;
            ghost.m_radius = plan.m_radius;
            ghost.m_smoothRadius = plan.m_smoothRadius;
            ghost.m_level = plan.m_level;
        }
    }
}