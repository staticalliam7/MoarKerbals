﻿/*
?  Based upon KerbalRecruitment from the 'CivilianPopulation' mod for Kerbal Space Program
    https://github.com/linuxgurugamer/CivilianPopulation

    LinuxGuruGamer
    CC BY-NC 4.0 (Attribution-NonCommercial 4.0 International) (https://creativecommons.org/licenses/by-nc/4.0/)
    specifically: https://github.com/linuxgurugamer/CivilianPopulation

    This file has been modified extensively and is released under the same license.

*/
using System;
using System.Collections;
using System.Collections.Generic;
using static MoarKerbals.Init;

namespace MoarKerbals
{
    /// <summary>
    /// 
    /// </summary>
    public class KerbalRecruitment : MoarKerbalBase
    {
        enum KerbalJob
        {
            Pilot,
            Engineer,
            Scientist
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            Events["RecruitKerbal"].guiName = initiateAction;
        }

        [KSPEvent(guiName = "Recruit Kerbal", active = true, guiActive = true)]
        void RecruitKerbal()
        {
            //Debug.Log(debuggingClass.modName + "Kerbal Recruitment Button pressed!");
            ScreenMessages.PostScreenMessage("Kerbal Recruitment Button pressed!", 5f, ScreenMessageStyle.UPPER_CENTER);
            bool changedTrait = false;
            List<ProtoCrewMember> vesselCrew = vessel.GetVesselCrew();
            foreach (ProtoCrewMember crewMember in vesselCrew)
            {
                Log.Info(crewMember.name + " : " + crewMember.trait + ": " + crewMember.type);
                //if (crewMember.trait == debuggingClass.civilianTrait && changedTrait == false)
                if (crewMember.trait == "Civilian" && changedTrait == false)
                {
                    if (GatherResources(part))
                    {
                        crewMember.trait = getRandomTrait();
                        //crewMember.Save(.this);
                        crewMember.type = ProtoCrewMember.KerbalType.Crew;
                        //crewMember.trait = KerbalJob.Engineer;
                        changedTrait = true;
                        //HighLogic.CurrentGame.CrewRoster.Save(HighLogic.CurrentGame());
                        Utilities.msg(crewMember.name + " is now a " + crewMember.trait + "!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        //      Debug.Log(debuggingClass.modName + crewMember.name + " is now a " + crewMember.trait + "!");
                    }
                }
            }
            if (changedTrait) GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
        }

        private string getRandomTrait()
        {
            int numberOfClasses = 3;
            string kerbalTrait = "";
            int randNum;
            System.Random newRand = new System.Random();
            randNum = newRand.Next() % numberOfClasses;
            if (randNum == 0)
                kerbalTrait = "Pilot";
            if (randNum == 1)
                kerbalTrait = "Engineer";
            if (randNum == 2)
                kerbalTrait = "Scientist";
            ScreenMessages.PostScreenMessage("Created trait:  " + kerbalTrait, 5f, ScreenMessageStyle.UPPER_CENTER);
            // Debug.Log(debuggingClass.modName + "Created trait:  " + kerbalTrait);
            return kerbalTrait;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            base.OnStart(StartState.None);
            string display = "\r\nInput:\r\n One Civilian Kerbal";

            for (int i = 0; i < resourceRequired.Count; i++)
                display += String.Format("\r\n{0:0,0}", resourceRequired[i].amount) + " " + resourceRequired[i].resource + "\r\n";

            display += "\r\nOutput:\r\n Pilot, Engineer, Scientest Kerbal (random) eating a MinmusMint icecream cone.";
            return display;
        }
    }
}
