using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop;

public class BlueprintSettings
{
    public string filename = string.Empty;
    public string Name = string.Empty;
    public string Creator = string.Empty;
    public string Description = string.Empty;
    public string Center = string.Empty;
    public string Icon = string.Empty;
    public Vector3 Coordinates;
    public Vector3 Rotation;
    public string Requirements = string.Empty;
    
    public readonly List<PlanSnapPoint> SnapPoints = new();
    public readonly List<PlanPiece> Pieces = new();
    public readonly List<PlanTerrain> Terrains = new();
    public readonly List<Requirement> requirements = new();

    public void Parse(Blueprint plan)
    {
        switch (plan.type)
        {
            case FileType.Blueprint:
                ParseBlueprint(plan);
                break;
            case FileType.VBuild:
                ParseVBuild(plan);
                break;
            default:
                Workshop.LogWarning("Unknown file type: " + plan.type);
                break;
        }

        if (string.IsNullOrEmpty(Name)) Name = plan.filename.Split('.').First();
        filename = plan.filename;
    }
    private void ParseBlueprint(Blueprint plan)
    {
        bool isSnapPoints = false;
        bool isPiece = false;
        bool isTerrain = false;
        foreach (string line in plan.lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("#Name")) Name = line.Substring(line.IndexOf(':') + 1).StripCitations();
            else if (line.StartsWith("#Creator")) Creator = line.Substring(line.IndexOf(':') + 1).StripCitations();
            else if (line.StartsWith("#Description")) Description = line.Substring(line.IndexOf(':') + 1).StripCitations();                    
            else if (line.StartsWith("#Center")) Center = line.Substring(line.IndexOf(':') + 1).StripCitations();
            else if (line.StartsWith("#Coordinates"))
            {
                string[] parts = line.Split(':').Last().Split(',');
                Coordinates = parts.GetVector(0);
            }
            else if (line.StartsWith("#Rotation"))
            {
                string[] parts = line.Split(':').Last().Split(',');
                Rotation = parts.GetVector(0);
            }
            else if (line.StartsWith("#Requirements"))
            {
                Requirements = line.Split(':').Last();
            }
            else if (line.StartsWith("#Icon"))
            {
                Icon = line.Split(':').Last();
            }
            else if (line.StartsWith("#SnapPoints"))
            {
                isSnapPoints = true;
                isPiece = false;
                isTerrain = false;
                continue;
            }
            else if (line.StartsWith("#Pieces"))
            {
                isPiece = true;
                isSnapPoints = false;
                isTerrain = false;
                continue;
            }
            else if (line.StartsWith("#Terrain"))
            {
                isTerrain = true;
                isSnapPoints = false;
                isPiece = false;
                continue;
            }
            if (isSnapPoints)
            {
                PlanSnapPoint planSnapPoint = new PlanSnapPoint(line);
                SnapPoints.Add(planSnapPoint);
            }
            else if (isPiece)
            {
                PlanPiece planPiece = new PlanPiece(line, FileType.Blueprint);
                Pieces.Add(planPiece);
            }
            else if (isTerrain)
            {
                PlanTerrain planTerrain = new PlanTerrain(line);
                Terrains.Add(planTerrain);
            }
        }
    }
    private void ParseVBuild(Blueprint plan)
    {
        foreach (string line in plan.lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            PlanPiece planPiece = new PlanPiece(line, FileType.VBuild);
            Pieces.Add(planPiece);
        }
    }
    public void Parse(Piece container, List<Piece> pieces)
    {
        Name = container.name;
        Description = string.IsNullOrEmpty(container.m_description)
            ? DateTime.UtcNow.ToString("F")
            : container.m_description;
        if (string.IsNullOrEmpty(Creator))
        {
            Creator = Game.instance.m_playerProfile.m_playerName;
        }
        pieces.Remove(container);
        Pieces.Clear();
        for (int i = 0; i < pieces.Count; ++i)
        {
            Pieces.Add(new PlanPiece(pieces[i]));
        }
    }
}