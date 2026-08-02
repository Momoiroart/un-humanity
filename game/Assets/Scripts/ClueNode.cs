// UN-HUMANITY — a clue node on the street. Placement carries the position;
// what the node SAYS lives in the case domain (CaseUH001). Nodes whose
// clue reads as scenery in Normalcy show no prompt until Sight is held —
// the street doesn't advertise its secrets.

using System.Collections.Generic;
using UnityEngine;
using UnHumanity.Case;

public class ClueNode : MonoBehaviour
{
    public static readonly List<ClueNode> All = new();

    public ClueId clueId;
    public float radius = 2.2f;
    [HideInInspector] public bool respondsInNormalcy;   // synced from the domain catalog

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);
}
