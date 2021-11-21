﻿/*
?  Based upon KerbalRecruitment from the 'CivilianPopulation' mod for Kerbal Space Program
    https://github.com/linuxgurugamer/CivilianPopulation

    LinuxGuruGamer
    CC BY-NC 4.0 (Attribution-NonCommercial 4.0 International) (https://creativecommons.org/licenses/by-nc/4.0/)
    specifically: https://github.com/linuxgurugamer/CivilianPopulation

    This file has been modified extensively.

*/

using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace MoarKerbals
{
    /// <summary>PartModule: KerbalRecruitment</summary>
    /// <seealso cref="PartModule" />
    public class KerbalRecruitment : MoarKerbalsBase // PartModule
    {
        /// <summary>KerbalJob Enum - kerbal professions</summary>
        enum KerbalJob { Pilot, Engineer, Scientist }

        /// <summary>KerbalRecuitmentEnabled - is the module enabled (default = false)</summary>
        [KSPField(guiName = "#MOAR-Academy-00",
                            groupName = "MoarKerbals",
                            guiActive = true,
                            guiActiveEditor = true,
                            isPersistant = true),
                            UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool KerbalRecruitmentEnabled = false;

        /// <summary>The graduation sound</summary>
        protected AudioSource graduation0;

        /// <summary>The graduation sound</summary>
        protected AudioSource graduation1;

        /// <summary>The graduation sound</summary>
        protected AudioSource dropOut0;

        /// <summary>The graduation sound</summary>
        protected AudioSource dropOut1;

        /// <summary>onStart</summary>
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            // Logging.DLog("KerbalAcademy.OnStart");

            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings3>().coloredPAW)
                Fields["KerbalRecruitmentEnabled"].group.displayName = System.String.Format("<color=#BADA55>" + groupName + "</color>");
            else
                Fields["KerbalRecruitmentEnabled"].group.displayName = groupName;

            graduation0 = gameObject.AddComponent<AudioSource>();
            graduation0.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/positive");
            graduation0.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
            graduation0.panStereo = 0;
            graduation0.rolloffMode = AudioRolloffMode.Linear;
            graduation0.Stop();

            graduation1 = gameObject.AddComponent<AudioSource>();
            graduation1.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/Rise05");
            graduation1.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
            graduation1.panStereo = 0;
            graduation1.rolloffMode = AudioRolloffMode.Linear;
            graduation1.Stop();

            dropOut0 = gameObject.AddComponent<AudioSource>();
            dropOut0.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/negative");
            dropOut0.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
            dropOut0.panStereo = 0;
            dropOut0.rolloffMode = AudioRolloffMode.Linear;
            dropOut0.Stop();

            dropOut1 = gameObject.AddComponent<AudioSource>();
            dropOut1.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/misc_sound");
            dropOut1.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
            dropOut1.panStereo = 0;
            dropOut1.rolloffMode = AudioRolloffMode.Linear;
            dropOut1.Stop();

            Events["RecruitKerbal"].guiName = Localizer.Format("#MOAR-Academy-01"); //initiateAction;
        }

        private protected void OnFixedUpdate()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings3>().coloredPAW)
                Fields["KerbalRecruitmentEnabled"].group.displayName = System.String.Format("<color=#BADA55>" + groupName + "</color>");
            else
                Fields["KerbalRecruitmentEnabled"].group.displayName = groupName;
        }

        /// <summary>Recruits the kerbal.</summary>
        [KSPEvent(guiName = "#MOAR-Academy-01",
                  groupName = "MoarKerbals",
                  active = true,
                  guiActive = true)]
        void RecruitKerbal()
        {
            Logging.DLog(logMsg: $"Academy: RecruitKerbal");
            if (KerbalRecruitmentEnabled)
            {
                Logging.DLog(logMsg: "Academy: Recruitment Button pressed!");

                List<ProtoCrewMember> vesselCrew;

                // need to be able to only affect one part
                if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().entireVesselAcademy)
                    vesselCrew = vessel.GetVesselCrew();
                else
                    vesselCrew = part.protoModuleCrew;

                int count = 0;
                bool onlyOne = HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().recruitOnlyOne;
                bool changedTrait = false;
                foreach (ProtoCrewMember crewMember in vesselCrew)
                {
                    Logging.DLog(logMsg: Localizer.Format("#MOAR-Academy-06", crewMember.name, crewMember.trait, crewMember.type));
                    if (crewMember.trait == Localizer.Format("#MOAR-004"))  // && changedTrait == false) // "Civilian"
                    {
                        if (GatherResources(part) && GatherCurrencies())
                        {
                            DebitCurrencies();
                            crewMember.trait = getRandomTrait();
                            Logging.DLog(logMsg: $"{crewMember.displayName} is now a {crewMember.trait}");

                            // just in case
                            if (crewMember.type != ProtoCrewMember.KerbalType.Crew) crewMember.type = ProtoCrewMember.KerbalType.Crew;
                            // update the roster
                            if (crewMember.trait == Localizer.Format("#MOAR-004"))
                            {
                                BadOutcome(crewMember); // negative outcome
                            }
                            else
                            { // positive outcome
                                Logging.Msg(s: Localizer.Format("#MOAR-Academy-07", crewMember.name, crewMember.trait), true); // crewMember.name + " is now a " + crewMember.trait + "!"
                                KerbalRoster.SetExperienceTrait(crewMember, crewMember.trait);
                                changedTrait = true;
                                count++;
                            }
                        }
                        if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().recruitOnlyOne) changedTrait = true; // tried break;
                    }
                    if (onlyOne && changedTrait) break;
                }
                if (changedTrait || (count > 0))
                {
                    Logging.DLog(logMsg: $"Academy: Count {count} out of {vesselCrew.Count}");
                    GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);

                }
                else Logging.Msg(s: "No civilians available to recruit", true);
            }
        }

        /// <summary>
        /// BadOutCome - if accident rate is greater than 0 and random is less than or equal to
        /// </summary>
        /// <param name="crewMember"></param>
        private void BadOutcome(ProtoCrewMember crewMember)
        {
            var rnd = new System.Random();
            double localDouble = rnd.Next(0, 101);
            Logging.DLog(logMsg: $"Academy: BadOutcome roll: {localDouble:F0}");
            switch (localDouble)
            {
                case < 05:
                    Logging.Msg(Localizer.Format("#MOAR-Academy-08", crewMember.displayName) + ".");
                    break;
                case < 30:
                    Logging.Msg(Localizer.Format("#MOAR-Academy-09", crewMember.displayName) + ".");
                    break;
                case < 60:
                    Logging.Msg(Localizer.Format("#MOAR-Academy-10", crewMember.displayName) + ".");
                    break;
                case <= 80:
                    Logging.Msg(Localizer.Format("#MOAR-Academy-11", crewMember.displayName) + ".");
                    break;
                case <= 90:
                    Logging.Msg(Localizer.Format("#MOAR-Academy-12", crewMember.displayName) + ".");
                    break;
                case <= 100:
                    Logging.Msg(Localizer.Format("#MOAR-Academy-13", crewMember.displayName) + ".");
                    break;
                default:
                    Logging.DLogWarning(logMsg: "Academy: BadOutcome out of bounds.");
                    break;
            }
        }

        /// <summary>
        /// getRandomTrait - returns random trait (Pilot, Engineer, Scientist - with a chance of remaining a Civilian
        /// </summary>
        /// <returns>kerbalTrait</returns>
        private protected string getRandomTrait()
        {
            Logging.DLog(logMsg: $"Academy: getRandomTrait");

            int numberOfClasses = 3;
            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().dropOut) numberOfClasses = 4;

            string kerbalTrait = "";
            System.Random newRand = new System.Random();

            switch (newRand.Next() % numberOfClasses)
            {
                case 0:
                    kerbalTrait = Localizer.Format("#autoLOC_8005006"); //  "Pilot";
                    if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().soundOn) SuccessSound();
                    break;
                case 1:
                    kerbalTrait = Localizer.Format("#autoLOC_8005007"); // "Engineer"; 
                    if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().soundOn) SuccessSound();
                    break;
                case 2:
                    kerbalTrait = Localizer.Format("#autoLOC_8005008"); // "Scientist"; 
                    if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().soundOn) SuccessSound();
                    break;
                case 3:
                    kerbalTrait = Localizer.Format("#MOAR-004"); // "Civilian"; 
                    if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().soundOn) FailureSound();
                    Logging.Msg(s: String.Format("Created trait:  {0}", kerbalTrait));
                    break;
                default:
                    break;
            }
            Logging.DLog(logMsg: String.Format("Created trait:  {0}", kerbalTrait));
            return kerbalTrait;
        }

        /// <summary>Play sound upon failure</summary>
        private protected void FailureSound()
        {
            Logging.DLog(logMsg: $"Academy: Failure Sound");
            int _soundSelection = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundClipC0;
            if (_soundSelection == 0)
            {
                System.Random newRand = new System.Random();
                _soundSelection = newRand.Next(1, 2);
            }
            switch (_soundSelection)
            {
                case 1:
                    dropOut0.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    dropOut0.Play();
                    return;
                case 2:
                    dropOut1.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    dropOut1.Play();
                    return;
                default:
                    return;
            }
        }

        /// <summary>Play sound upon success</summary>
        private protected void SuccessSound()
        {
            Logging.DLog(logMsg: $"Academy: Success Sound");
            int _soundSelection = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundClipC1;
            if (_soundSelection == 0)
            {
                System.Random newRand = new System.Random();
                _soundSelection = newRand.Next(1, 2);
            }
            switch (_soundSelection)
            {
                case 1:
                    graduation0.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    graduation0.Play();
                    return;
                case 2:
                    graduation1.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    graduation1.Play();
                    return;
                default:
                    return;
            }
        }

        /// <summary>Module information shown in editors</summary>
        private string info = string.Empty;

        /// <summary>What shows up in editor for the part</summary>
        /// <returns>string: display</returns>
        public override string GetInfo()
        {
            base.OnStart(StartState.None);

            /* :::This is what it should look like with default settings:::
             * Kerbal Recruitment
             * MoarKerbals
             * Kerbthulhu Kinetics Program
             * v 1.3.0.0
             * 
             * Input: (color)
             * One or more living Civilians.
             * 
             * Required Resources: (color)
             *   ElectricCharge: 8000
             *   Oxidizer: 100
             *   Ore: 500
             * 
             * Required Currency: (color)
             *   Funds 1000
             *   Science: 1
             *   Reputation: 2
             * 
             * Output:
             * Anything from one Kerbal to a deep dish pizza.
             * 
             */
            info += Localizer.Format("#MOAR-Manu") + "\r\n"; // Kerbthulhu Kinetics Program
            info += Localizer.Format("#MOAR-003", Version.SText) + "\r\n"; // MoarKerbals v Version Number text


            info += $"\r\n<color=#FFFF19>{Localizer.Format("#MOAR-005")}:</color>\r\n"; // Input

            // "Input: One Civilian Kerbal";
            info = $"{Localizer.Format("#MOAR-Academy-03")} {Localizer.Format("#MOAR-004")} {Localizer.Format("#MOAR-Academy-04")}.\r\n";

            // resource section header
            info += String.Format("\r\n<color=#00CC00>" + Localizer.Format("#MOAR-008") + ":</color>\r\n"); // Resources: <color=#E600E6>

            // create string with all resourceRequired.name and resourceRequired.amount
            foreach (ResourceRequired resources in resourceRequired)
                info += String.Format("- {0}: {1:n0}\r\n", resources.resource, resources.amount);

            // currency section header
            if ((costFunds != 0) || (costScience != 0) || (costReputation != 0))
            {
                info += String.Format("\r\n<color=#00CC00>" + Localizer.Format("#MOAR-009") + ":</color>"); // Currency:
                if (costFunds != 0) info += $"\r\n- {Localizer.Format("#autoLOC_7001031")}: {costFunds:n0}";
                if (costScience != 0) info += $"\r\n- {Localizer.Format("#autoLOC_7001032")}: {costScience:n0}";
                if (costReputation != 0) info += $"\r\n- {Localizer.Format("#autoLOC_7001033")}: {costReputation:n0}";
                info += "\r\n"; // line return
            }

            info += $"\r\n<color=#FFFF19>{Localizer.Format("#MOAR-006")}:</color>"; // Output:
            info += $"\r\n- {Localizer.Format("#MOAR-Academy-05")}.\r\n"; // "-  Pilot, Engineer, Scientist Kerbal (random)\reating a MinmusMint ice cream cone."

            return info;
        }
    }
}
