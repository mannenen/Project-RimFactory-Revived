﻿using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProjectRimFactory.Industry
{
    public class Building_FuelingMachine : Building
    {
        public IntVec3 FuelableCell => Rotation.FacingCell + Position;
        public override void Tick()
        { // in case you *really* want to use Tick
            base.Tick();
            if (this.IsHashIntervalTick(10)) Refuel();
        }
        public override void TickRare() { // prefer to use TickRare
            base.TickRare();
            Refuel();
        }
        public void Refuel()
        {
            if (GetComp<CompPowerTrader>().PowerOn)
            {
                // Get what we are supposed to refuel:
                //   (only refuel one thing - if you need to adjust this to fuel more
                //    than one thing, make the loop here and put some breaking logic
                //    instead of all the "return;"s below)
                CompRefuelable refuelableComp=null;
                foreach (Thing tmpThing in Map.thingGrid.ThingsListAt(FuelableCell)) {
                    if (tmpThing is Building) refuelableComp=(tmpThing as Building).GetComp<CompRefuelable>();
                    if (refuelableComp != null) break;
                }
                if (refuelableComp != null) {
                    // Check if there is already enough fuel:
                    //  (because Fuel is a float, we check the current fuel + .99, so it
                    //   only refuels when Fuel drops at least one full unit below taret level)
                    if (refuelableComp.Fuel + 0.9999f >= refuelableComp.TargetFuelLevel) return; // fully fueled
                    foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this))
                    {
                        List<Thing> l = Map.thingGrid.ThingsListAt(cell);
                        for (int i=l.Count-1; i>=0; i--) { // count down because items may be destroyed
                            Thing item=l[i];
                            // Without this check, if there is something that is fueled by
                            //     minified Power Conduits (weird, but ...possible?), then
                            //     our FuelingMachine will happily rip conduits out of the
                            //     ground to fuel it.  I'm okay with this behavior.
                            //     Feature.  Not a bug.
                            // But if it ever causes a problem, uncomment this check:
                            // if (item.def.category != ThingCategory.Item) continue;
                            if (refuelableComp.Props.fuelFilter.Allows(item))
                            {
                                // round down to not waste fuel:
                                int num = Mathf.Min(item.stackCount, Mathf.FloorToInt(refuelableComp.TargetFuelLevel - refuelableComp.Fuel));
                                if (num > 0) {
                                    refuelableComp.Refuel(num);
                                    item.SplitOff(num).Destroy();
                                } else { // It's not quite 1 below TargetFuelLevel
                                    // but we KNOW we are at least .9999f below TargetFuelLevel (see test above)
                                    // So we call it close enough to 1:
                                    refuelableComp.Refuel(1);
                                    item.SplitOff(1).Destroy();
                                }
                                // check fuel as float (as above)
                                if (refuelableComp.Fuel + 0.9999f >= refuelableComp.TargetFuelLevel) return; // fully fueled
                            }
                        }
                    }
                }
            }
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3>(GenAdj.CellsAdjacent8Way(this)));
            GenDraw.DrawFieldEdges(new List<IntVec3>() { FuelableCell }, Color.yellow);
        }
    }
}
