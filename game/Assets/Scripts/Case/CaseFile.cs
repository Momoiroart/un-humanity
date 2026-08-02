// UN-HUMANITY case core — the investigation's domain model. Pure C#.
// Loop steps 2-3 (UH-001): collect evidence on the street, and once four
// pieces are in the file, the game asks the question the player is not
// ready to answer: what is this thing? Wrong calls have consequences —
// the fork is honest on the evidence, and the file records the verdict.

using System.Collections.Generic;
using System.Linq;

namespace UnHumanity.Case
{
    public enum ClueId
    {
        TheStop,     // the seated absence at the stop that isn't there
        Witnesses,   // two people, two descriptions, nothing described
        Bench,       // new city bench, weathered fifty years
        Sediment,    // newspaper strata; bottom layer Oct 1974
        Archive,     // Route 9 night service discontinued 10/1974
        Victim,      // the commuter, still waiting, politely
    }

    public enum Verdict { Unclassified, UnHumanity, Remnant }

    public sealed class Clue
    {
        public readonly ClueId Id;
        public readonly string Title;
        public readonly string NormalcyText;   // null = reads as scenery in Normalcy
        public readonly string SightText;

        public Clue(ClueId id, string title, string normalcyText, string sightText)
        {
            Id = id;
            Title = title;
            NormalcyText = normalcyText;
            SightText = sightText;
        }

        public bool RespondsInNormalcy => NormalcyText != null;
    }

    public sealed class CollectedClue
    {
        public readonly Clue Clue;
        public readonly bool UnderSight;
        public CollectedClue(Clue clue, bool underSight) { Clue = clue; UnderSight = underSight; }
        public string Text => UnderSight ? Clue.SightText : Clue.NormalcyText;
    }

    public sealed class CaseFile
    {
        public const int CluesToClassify = 4;

        readonly Dictionary<ClueId, Clue> catalog;
        readonly Dictionary<ClueId, CollectedClue> found = new();
        public readonly List<string> Log = new();

        public Verdict Verdict { get; private set; } = Verdict.Unclassified;

        public CaseFile(IEnumerable<Clue> clues) =>
            catalog = clues.ToDictionary(c => c.Id, c => c);

        public IReadOnlyDictionary<ClueId, Clue> Catalog => catalog;
        public int FoundCount => found.Count;
        public bool Has(ClueId id) => found.ContainsKey(id);
        public CollectedClue Get(ClueId id) => found.TryGetValue(id, out var c) ? c : null;
        public bool CanClassify => FoundCount >= CluesToClassify && Verdict == Verdict.Unclassified;

        /// Collect a clue. In Normalcy, scenery-only nodes refuse — the
        /// street holds its secrets until Sight. Upgrades a Normalcy read
        /// to its Sight read if re-examined under Sight.
        public bool Collect(ClueId id, bool underSight)
        {
            if (!catalog.TryGetValue(id, out var clue)) return false;
            if (!underSight && !clue.RespondsInNormalcy)
            {
                Log.Add($"{clue.Title}: reads as scenery. Nothing responds.");
                return false;
            }
            if (found.TryGetValue(id, out var existing))
            {
                if (existing.UnderSight || !underSight) return false;   // nothing new
                found[id] = new CollectedClue(clue, true);
                Log.Add($"{clue.Title}: the Sight reading replaces what you thought you saw.");
                return true;
            }
            found[id] = new CollectedClue(clue, underSight);
            Log.Add($"EVIDENCE {FoundCount}/{catalog.Count} — {clue.Title}");
            return true;
        }

        /// One verdict per case. The fork is permanent — that is the weight.
        public bool Classify(Verdict verdict)
        {
            if (!CanClassify || verdict == Verdict.Unclassified) return false;
            Verdict = verdict;
            Log.Add(verdict == Verdict.UnHumanity
                ? "CLASSIFIED: UN-HUMANITY. Hostile. Response: destroy."
                : "CLASSIFIED: REMNANT. Non-malicious. Response: end the wait.");
            return true;
        }
    }

    /// FILE UH-001 — the six clue nodes of the street block, blueprint-true.
    /// Evidence is deliberately ambiguous between the two readings.
    public static class CaseUH001
    {
        /// Taxonomy tag (GDD v0.4): flavor classification of the
        /// phenomenon, independent of the player's hostile/remnant call.
        public const string CaseType = "TEMPORAL RESIDUAL";

        public static CaseFile NewCaseFile() => new CaseFile(new[]
        {
            new Clue(ClueId.TheStop, "The stop",
                null,
                "There is no man. A seated absence — the bench holds the shape of someone waiting."),
            new Clue(ClueId.Witnesses, "Witness statements",
                "Two people saw him. Tall, they say. Short. Old. Young. They are both certain.",
                "The descriptions never match because nothing is being described."),
            new Clue(ClueId.Bench, "The bench",
                null,
                "Installed eight months ago; weathered like fifty years. Four replacement work orders on file — every bench here \"ages overnight.\""),
            new Clue(ClueId.Sediment, "Sediment",
                null,
                "Newspaper strata under the bench, pressed like geology. The bottom layer is dated October 1974."),
            new Clue(ClueId.Archive, "Transit archive",
                null,
                "Route 9 night service — discontinued October 1974. The complaints go back decades. They are all the same complaint."),
            new Clue(ClueId.Victim, "The commuter",
                null,
                "She sits beside the absence, patient, drained. Asked how long she's waited: \"twenty minutes.\" Her phone has been here nine days."),
        });
    }
}
