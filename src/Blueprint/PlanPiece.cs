using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public class PlanPiece
{
    public readonly string PrefabId = string.Empty;
    private readonly Vector3 Position = Vector3.zero;
    private readonly Quaternion Rotation = Quaternion.identity;
    private readonly string Category = "";
    private readonly Vector3 Scale = Vector3.one;
    private readonly string ZDO = "";
    private readonly string AttachItemStand = "";
    private readonly int State;

    public PlanPiece(Piece piece)
    {
        PrefabId = Utils.GetPrefabName(piece.name);
        Category = piece.m_category.ToString();
        Position = piece.transform.localPosition;
        Rotation = piece.transform.localRotation;
        Scale = piece.transform.localScale;
        if (piece.TryGetComponent(out Plan tempZDO))
        {
            ZDO = tempZDO.zdo;
        }
    }

    public PlanPiece(string line, FileType type)
    {
        string[] parts;
        switch (type)
        {
            case FileType.Blueprint:
                parts = line.Split(';');
                PrefabId = parts.GetString(0);
                Category = parts.GetString(1);
                Position = parts.GetVector(2);
                Rotation = parts.GetQuaternion(5);
                ZDO = parts.GetString(9);
                Scale = parts.GetVector(10);
                if (string.IsNullOrEmpty(ZDO)) ZDO = parts.GetString(13);
                if (ZDO.Contains(":"))
                {
                    string attachItem = ZDO.StripCitations();
                    AttachItemStand = attachItem;
                    ZDO = string.Empty;
                }
                else if (int.TryParse(ZDO.StripCitations(), out int state))
                {
                    ZDO = string.Empty;
                    State = state;
                }
                else if (ZDO.Length < 4)
                {
                    ZDO = string.Empty;
                }
                break;
            case FileType.VBuild:
                if (line.IndexOf(',') > -1)
                {
                    line = line.Replace(',', '.');
                }
                parts = line.Split(' ');
                PrefabId = parts.GetString(0);
                Rotation = parts.GetQuaternion(1).normalized;
                Position = parts.GetVector(5);
                break;

        }
    }

    public GameObject Create(Transform parent, int index)
    {
        GameObject source = PrefabManager.GetPrefab(PrefabId);
        if (source == null) return null;
        
        ZNetView.m_forceDisableInit = true;
        GameObject instance = Object.Instantiate(source, parent);
        ZNetView.m_forceDisableInit = false;
        instance.SetActive(false);
        instance.name = source.name;
        instance.transform.localPosition = Position;
        instance.transform.localRotation = Rotation;
        instance.transform.localScale = Scale;
        int height = 8;
        int width = 5;
        if (source.TryGetComponent(out Container container))
        {
            width = container.m_width;
            height = container.m_height;
        }
        instance.RemoveAllComponents();
        Plan temp = instance.AddComponent<Plan>();
        temp.zdo = ZDO;
        temp.attach = AttachItemStand;
        temp.state = State;
        temp.width = width;
        temp.height = height;
        instance.SetActive(true);
        return instance;
    }
    
    public override string ToString() => $"{PrefabId};{Category};{Position.ToCustomString(';')};{Rotation.ToCustomString(';')};{ZDO};{Scale.ToCustomString(';')}";
}