using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SailwindVirtualCrew
{
    public class SquareTrimRequest
    {
        public DualSheetSail Sail { get; }
        public WorkRequestStatus Status { get; set; }
        public Crewman AssignedCrewman  { get; set; }
        public Crewman AssignedCrewman2 { get; set; }
        public string CommandName => "Square Auto-Trim";

        public float PositioningTimeTotal { get; private set; }
        private float positioningStartTime;

        private enum SquareTrimPhase { ScanIn, ScanOut, Optimize }
        private SquareTrimPhase phase;

        private GPButtonRopeWinch portWinch;
        private GPButtonRopeWinch starbWinch;
        private WinchTarget portWT;
        private WinchTarget starbWT;
        private float sharedMaxPower;

        private float portStartPos,  portScanInPos,  portScanOutPos;
        private float starbStartPos, starbScanInPos, starbScanOutPos;

        private struct Sample { public float PortPos; public float StarbPos; public float Efficiency; }
        private readonly List<Sample> samples = new List<Sample>();

        private static readonly FieldInfo unamplifiedForwardInfo =
            AccessTools.Field(typeof(Sail), "unamplifiedForwardForce");
        private static readonly FieldInfo unamplifiedSideInfo =
            AccessTools.Field(typeof(Sail), "unamplifiedSidewayForce");
        private static readonly FieldInfo totalWindForceInfo =
            AccessTools.Field(typeof(Sail), "totalWindForce");

        public SquareTrimRequest(DualSheetSail sail)
        {
            Sail   = sail;
            Status = WorkRequestStatus.Open;
        }

        // Called after both crewmen are assigned.
        public void BeginPositioning()
        {
            PositioningTimeTotal = Mathf.Max(
                7 - AssignedCrewman.Dexterity,
                7 - AssignedCrewman2.Dexterity);
            positioningStartTime = Time.time;
            Status = WorkRequestStatus.Positioning;
        }

        public bool IsPositioningComplete() =>
            Time.time >= positioningStartTime + PositioningTimeTotal;

        public float GetPositioningProgress() =>
            PositioningTimeTotal <= 0f ? 0f
                : Mathf.Clamp01(1f - (Time.time - positioningStartTime) / PositioningTimeTotal) * 100f;

        public void Begin()
        {
            portWinch  = Sail.getPortSheetWinch();
            starbWinch = Sail.getStarboardSheetWinch();

            // Shared power is the bottleneck of the two crew strengths.
            sharedMaxPower = Mathf.Min(AssignedCrewman.Strength, AssignedCrewman2.Strength) * 5f;

            portStartPos  = portWinch.rope.currentLength;
            starbStartPos = starbWinch.rope.currentLength;

            // ScanIn moves the sail to port: port tightens (decreases), starboard eases (increases).
            portScanInPos  = Mathf.Clamp01(portStartPos  - 0.25f);
            starbScanInPos = Mathf.Clamp01(starbStartPos + 0.25f);

            // ScanOut moves the sail to starboard: port eases (increases), starboard tightens (decreases).
            portScanOutPos  = Mathf.Clamp01(portStartPos  + 0.25f);
            starbScanOutPos = Mathf.Clamp01(starbStartPos - 0.25f);

            samples.Clear();
            SetupTargets(portScanInPos, starbScanInPos);
            phase  = SquareTrimPhase.ScanIn;
            Status = WorkRequestStatus.InProgress;
        }

        public bool IsComplete() =>
            phase == SquareTrimPhase.Optimize && portWT.IsAtTarget() && starbWT.IsAtTarget();

        // 0–100: ScanIn 0–33, ScanOut 33–66, Optimize 66–100.
        public float GetProgress()
        {
            float avg = (portWT.GetProgress() + starbWT.GetProgress()) / 2f;
            switch (phase)
            {
                case SquareTrimPhase.ScanIn:    return avg * 0.33f;
                case SquareTrimPhase.ScanOut:   return 33f + avg * 0.33f;
                case SquareTrimPhase.Optimize:  return 66f + avg * 0.34f;
                default:                        return 100f;
            }
        }

        // Called every frame while InProgress.
        public void UpdateFrame()
        {
            switch (phase)
            {
                case SquareTrimPhase.ScanIn:
                    samples.Add(new Sample
                    {
                        PortPos    = portWinch.rope.currentLength,
                        StarbPos   = starbWinch.rope.currentLength,
                        Efficiency = ScoredEfficiency()
                    });
                    if (portWT.IsAtTarget() && starbWT.IsAtTarget())
                    {
                        SetupTargets(portScanOutPos, starbScanOutPos);
                        phase = SquareTrimPhase.ScanOut;
                    }
                    break;

                case SquareTrimPhase.ScanOut:
                    samples.Add(new Sample
                    {
                        PortPos    = portWinch.rope.currentLength,
                        StarbPos   = starbWinch.rope.currentLength,
                        Efficiency = ScoredEfficiency()
                    });
                    if (portWT.IsAtTarget() && starbWT.IsAtTarget())
                        BeginOptimize();
                    break;

                // Optimize: P controller drives both to best positions; IsComplete() polls.
            }
        }

        private void SetupTargets(float portTgt, float starbTgt)
        {
            portWT = new WinchTarget(portWinch, portTgt);
            portWT.MaxPower = sharedMaxPower;
            portWT.RecordStart();

            starbWT = new WinchTarget(starbWinch, starbTgt);
            starbWT.MaxPower = sharedMaxPower;
            starbWT.RecordStart();

            var mgr = VirtualCrewManager.Instance;
            mgr.crewWinchInstructions[portWinch]  = portWT;
            mgr.crewWinchInstructions[starbWinch] = starbWT;
        }

        private void BeginOptimize()
        {
            float bestEff  = float.MinValue;
            float bestPort = portStartPos;
            float bestStarb = starbStartPos;
            foreach (var s in samples)
            {
                if (s.Efficiency > bestEff)
                {
                    bestEff   = s.Efficiency;
                    bestPort  = s.PortPos;
                    bestStarb = s.StarbPos;
                }
            }

            System.Console.WriteLine(
                $"Square optimize — port: {bestPort:F3}  starb: {bestStarb:F3}" +
                $"  eff: {bestEff:F1}  samples: {samples.Count}");

            portWT.TargetLength  = bestPort;
            portWT.RecordStart();
            starbWT.TargetLength = bestStarb;
            starbWT.RecordStart();
            phase = SquareTrimPhase.Optimize;
        }

        private float ScoredEfficiency()
        {
            float raw = CombinedEfficiencyRaw();
            return raw <= 0f ? -500f : raw;
        }

        private float CombinedEfficiencyRaw()
        {
            var sail  = Sail.getRealSail();
            float total = GetTotalForce(sail);
            if (total == 0f) return 0f;
            float eff = SailEfficiency(sail, total);
            if (eff <= 0f) return eff;
            float ineff = 100 - SailInefficiency(sail, total);
            return Mathf.Round((eff + ineff) / 2f);
        }

        private static float SailEfficiency(Sail sail, float totalForce) =>
            Mathf.Round((float)unamplifiedForwardInfo.GetValue(sail) / totalForce * 100f);

        private static float SailInefficiency(Sail sail, float totalForce) =>
            Mathf.Abs(Mathf.Round((float)unamplifiedSideInfo.GetValue(sail) / totalForce * 100f));

        private float GetTotalForce(Sail sail)
        {
            float applied = sail.appliedWindForce;
            if (applied == 0f)
                return (float)totalWindForceInfo.GetValue(sail);
            return applied / sail.GetCapturedForceFraction();
        }
    }
}
