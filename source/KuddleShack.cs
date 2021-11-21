using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MoarKerbals.Logging;
using KSP.Localization;

namespace MoarKerbals
{
    /// <summary>KuddleShack module</summary>
    [KSPModule("KuddleShack")]
    public class KuddleShack : MoarKerbalsBase
    {
        [KSPField(guiName = "#MOAR-Kuddle-00",
                  groupName = "MoarKerbals",
                  groupDisplayName = "#MOAR-001",
                  groupStartCollapsed = true,
                  guiActive = true,
                  guiActiveEditor = true,
                  isPersistant = true),
                  UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool KuddleShackEnabled = false;


        [KSPField(guiName = "#MOAR-Kuddle-12",
                  groupName = "MoarKerbals",
                  guiActive = true,
                  guiActiveEditor = true,
                  isPersistant = false)]
        public string timeRemaining = Localizer.Format("#MOAR-Kuddle-13");

        #region SoundVars

        /// <summary>kuddling success sound</summary>
        protected AudioSource kuddling_success0;

        /// <summary>kuddling success sound</summary>
        protected AudioSource kuddling_success1;

        /// <summary>kuddling success sound</summary>
        protected AudioSource kuddling_failure0;

        /// <summary>kuddling success sound</summary>
        protected AudioSource kuddling_failure1;
        #endregion

        /// <summary>
        /// Unity OnStart()
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(PartModule.StartState state)
        {
            //Logging.DLog("KuddleShack.OnStart");
            base.OnStart(state);

            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings3>().coloredPAW)
                Fields["KuddleShackEnabled"].group.displayName = System.String.Format("<color=#BADA55>" + groupName + "</color>");
            else
                Fields["KuddleShackEnabled"].group.displayName = groupName;

            kuddling_success0 = gameObject.AddComponent<AudioSource>();
            kuddling_success0.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/kuddleshack");
            kuddling_success0.volume = 0.9f;
            kuddling_success0.panStereo = 0;
            kuddling_success0.rolloffMode = AudioRolloffMode.Linear;
            kuddling_success0.Stop();

            kuddling_success1 = gameObject.AddComponent<AudioSource>();
            kuddling_success1.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/positive");
            kuddling_success1.volume = 0.9f;
            kuddling_success1.panStereo = 0;
            kuddling_success1.rolloffMode = AudioRolloffMode.Linear;
            kuddling_success1.Stop();

            kuddling_failure0 = gameObject.AddComponent<AudioSource>();
            kuddling_failure0.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/misc_sound");
            kuddling_failure0.volume = 0.9f;
            kuddling_failure0.panStereo = 0;
            kuddling_failure0.rolloffMode = AudioRolloffMode.Linear;
            kuddling_failure0.Stop();

            kuddling_failure1 = gameObject.AddComponent<AudioSource>();
            kuddling_failure1.clip = GameDatabase.Instance.GetAudioClip("KerbthulhuKineticsProgram/MoarKerbals/Sounds/negative");
            kuddling_failure1.volume = 0.9f;
            kuddling_failure1.panStereo = 0;
            kuddling_failure1.rolloffMode = AudioRolloffMode.Linear;
            kuddling_failure1.Stop();

            Events["ActivateKuddling"].guiName = "#MOAR-Kuddle-00"; // initiateAction;
            Events["ActivateKuddling"].guiActive = false;
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            GameEvents.onCrewTransferred.Add(onCrewTransferred);
            GameEvents.onCrewOnEva.Add(onCrewOnEva);

            GetLightingModules();
            GetMatingStatus();
            StartCoroutine(SlowUpdate());
        }

        /// <summary>
        /// Unity OnFixedUpdate ()
        /// </summary>
        private protected void OnFixedUpdate()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings3>().coloredPAW)
                Fields["KuddleShackEnabled"].group.displayName = System.String.Format("<color=#BADA55>" + groupName + "</color>");
            else
                Fields["KuddleShackEnabled"].group.displayName = groupName;
        }

        private List<PartModule> modulesLight = new List<PartModule>();

        void GetLightingModules()
        {
            Logging.DLog(logMsg: $"Kuddle: GetLightingModules");
            // Check for lightable modules
            if (part.Modules.Contains<ModuleColorChanger>())
            {
                ModuleColorChanger partM = part.Modules.GetModule<ModuleColorChanger>();
                modulesLight.Add(partM);
            }
            if (part.Modules.Contains<ModuleLight>())
            {
                foreach (ModuleLight partM in part.Modules.GetModules<ModuleLight>())
                    modulesLight.Add(partM);
            }
            if (part.Modules.Contains<ModuleAnimateGeneric>())
            {
                foreach (ModuleAnimateGeneric partM in part.Modules.GetModules<ModuleAnimateGeneric>())
                    modulesLight.Add(partM);
            }
            if (part.Modules.Contains("WBILight"))
            {
                foreach (PartModule partM in part.Modules)
                {
                    if (partM.ClassName == "WBILight")
                        modulesLight.Add(partM);
                }
            }
            if (part.Modules.Contains("ModuleKELight"))
            {
                foreach (PartModule partM in part.Modules)
                {
                    if (partM.ClassName == "ModuleKELight")
                        modulesLight.Add(partM);
                }
            }
        }


        bool readyToKuddle = false;
        const int SECS_IN_DAY = 21600;
        const int SECS_IN_HOUR = 3600;
        const int SECS_IN_MINUTE = 60;
        IEnumerator SlowUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds((float)SettingsInterface.slowUpdateTime());
                if (!readyToKuddle)
                    Events["ActivateKuddling"].guiActive = false;
                if (hasMatingPair && KuddleShackEnabled)
                {
                    if (GatherResources(part, (float)SettingsInterface.slowUpdateTime() / SettingsInterface.kuddleTimeNeeded()))
                    {
                        int t = (int)(Planetarium.GetUniversalTime() - startMatingTimer);
                        if (t > 1f)
                        {
                            int days = t / SECS_IN_DAY;
                            int hours = (t - days * SECS_IN_DAY) / SECS_IN_HOUR;
                            int minutes = (t - days * SECS_IN_DAY - hours * SECS_IN_HOUR) / SECS_IN_MINUTE;
                            int seconds = t - days * SECS_IN_DAY - hours * SECS_IN_HOUR - minutes * SECS_IN_MINUTE;

                            string time = "";
                            if (t >= SECS_IN_DAY)
                                time += days + " days";
                            if (t >= SECS_IN_HOUR)
                            {
                                if (time.Length > 0) time += ", ";
                                time += hours + " hours";
                            }
                            if (t >= SECS_IN_MINUTE)
                            {
                                if (time.Length > 0) time += ", ";
                                time += minutes + " minutes";
                            }
                            if (time.Length > 0) time += ", ";
                            time += seconds + " seconds";
                            Logging.Msg(Localizer.Format("#MOAR-Kuddle-01", time.ToString()));

                            timeRemaining = $" {time}";
                        }

                        if (Planetarium.GetUniversalTime() - startMatingTimer >= SettingsInterface.kuddleTimeNeeded())
                        {
                            readyToKuddle = true;
                            Events["ActivateKuddling"].guiActive = true;
                            Logging.Msg(s: Localizer.Format("#MOAR-Kuddle-02")); // "Minimum Kuddle Time Reached"
                        }
                    }
                    else
                        readyToKuddle = false;
                }
            }
        }

        /// <summary>has a male kerbal</summary>
        [KSPField]
        bool hasMale = false; /// <summary>has x kerbal</summary>

        /// <summary>has a femalmale kerbal</summary>
        [KSPField]
        bool hasFemale = false;  /// <summary>has y kerbal</summary>

        /// <summary>has a male and female kerbal</summary>
        [KSPField]
        bool hasMatingPair = false; /// <summary>has mating pair of kerbals</summary>

        /// <summary>the mating timer (how long has there been a mating pair)</summary>
        [KSPField]
        double startMatingTimer = 0;  /// <summary>start mating timer</summary>


        /// <summary>GetMatingStatus</summary>
        void GetMatingStatus()
        {
            Logging.DLog(logMsg: $"Kuddle: GetMatingStatus");
            hasMale = false;
            hasFemale = false;

            foreach (var k in part.protoModuleCrew)
            {
                if (k.gender == ProtoCrewMember.Gender.Male)
                    hasMale = true;
                if (k.gender == ProtoCrewMember.Gender.Female)
                    hasFemale = true;
            }

            bool newHasMatingPair = hasMale && hasFemale;
            if (!hasMatingPair && newHasMatingPair)
            {
                startMatingTimer = Planetarium.GetUniversalTime();
                hasMatingPair = true;
                SwitchLight.On(modulesLight);
            }
            else
            {
                if (!newHasMatingPair)
                {
                    hasMatingPair = false;
                    SwitchLight.Off(modulesLight);
                }
            }
        }

        /// <summary> GameEvent: crew boards vessel.</summary>
        /// <param name="fta">The FromToAction.</param>
        void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> fta)
        {
            Logging.DLog(logMsg: $"Kuddle: onCrewBoardVessel");
            GetMatingStatus();
        }

        /// <summary> GameEvent: crew transfers.</summary>
        /// <param name="hfta">The HostedFromToAction.</param>
        void onCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> hfta)
        {
            Logging.DLog(logMsg: $"Kuddle: HostedFromToAction");
            GetMatingStatus();
        }

        /// <summary> GameEvent: crew on EVA.</summary>
        /// /// <param name="hfta">The hfta.</param>
        /// <param name="fta">The FromToAction.</param>
        void onCrewOnEva(GameEvents.FromToAction<Part, Part> fta)
        {
            Logging.DLog(logMsg: $"Kuddle: FromToAction");
            GetMatingStatus();
        }

        /// <summary>KSPEvent: Complete Kuddling</summary>
        [KSPEvent(guiName = "#MOAR-Kuddle-03",
                  groupName = "MoarKerbals",
                  active = true,
                  guiActiveUncommand = true,
                  guiActiveUnfocused = true,
                  guiActive = true)]
        public void ActivateKuddling()
        {
            Logging.DLog(logMsg: $"Kuddle: ActivateKuddling");
            //{
            //Logging.DLog(logMsg: $"Kuddle: KuddleShackEnabled");

            //if (hasMatingPair && Planetarium.GetUniversalTime() - startMatingTimer >= SettingsInterface.kuddleTimeNeeded())
            if (hasMatingPair)
            {
                if (PartHasRoom(part) && GatherResources(part))
                {
                    KuddleKerbal();
                    //startMatingTimer = 0d;
                    GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
                }
            }
            else
            {
                if (hasMatingPair)
                    Logging.Msg(s: Localizer.Format("#MOAR-Kuddle-04")); // "Insufficient time for reproduction"
                else
                {
                    if (part.protoModuleCrew.Count == 2) Logging.Msg(Localizer.Format("#MOAR-Kuddle-05")); // "One kerbal of each gender is needed for kuddling"
                    else Logging.Msg(s: Localizer.Format("#MOAR-Kuddle-06")); // Two kerbals are needed for kuddling
                }
            }
            //}
        }

        /// <summary>
        /// Where kerbal meets kerbal
        /// </summary>
        private protected void KuddleKerbal()
        {
            Logging.DLog(logMsg: "Kuddle: KuddleKerbal");
            DebitCurrencies();
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
            kerbal.suit = (ProtoCrewMember.KerbalSuit)HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().birthdaySuit;
            kerbal.trait = Localizer.Format("#MOAR-004");

            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().kloneCivilians) KerbalRoster.SetExperienceTrait(kerbal, Localizer.Format("#MOAR-004")); ;
            part.AddCrewmember(kerbal);

            if (kerbal.seat != null)
                kerbal.seat.SpawnCrew();

            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;

            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().kuddleCivilians) KerbalRoster.SetExperienceTrait(kerbal, Localizer.Format("#MOAR-004"));

            // "Kuddling Success!  " + kerbal.name + "(Lv " + kerbal.experienceLevel.ToString() + " " + kerbal.experienceTrait.Title + ") has joined your space program");
            Logging.Msg(s: Localizer.Format("#MOAR-Kuddle-11", kerbal.name, kerbal.experienceLevel.ToString(), kerbal.experienceTrait.Title), true);

            if (HighLogic.CurrentGame.Parameters.CustomParams<Settings2>().soundOn) SuccessSound();
        }

        //Checks to make sure there are two kerbals and that there is room to store the new kerbal
        private bool PartHasRoom(Part part)
        {
            Logging.DLog(logMsg: $"Kuddle: PartHasRoom");
            if ((part.protoModuleCrew.Count < part.CrewCapacity) && ((part.protoModuleCrew.Count > 0)))
            {
                bool male = false, female = false;
                foreach (var k in part.protoModuleCrew)
                {
                    if (!male)
                        male = (k.gender == ProtoCrewMember.Gender.Male);
                    Logging.DLog(logMsg: "Found a male");
                    if (!female)
                        female = (k.gender == ProtoCrewMember.Gender.Female);
                    Logging.DLog(logMsg: "Found a female");
                }
                if (male && female)
                {
                    Logging.DLog(logMsg: "Found a mating pair");
                    return true;
                }
                else Logging.Msg(Localizer.Format("#MOAR-Kuddle-08"));  // "Kuddling requires both a male and female kerbal"
            }
            else
            {
                if (part.protoModuleCrew.Count == 0) Logging.Msg(Localizer.Format("#MOAR-Kuddle-06")); // Two kerbals are needed for reproduction
                else if (part.protoModuleCrew.Count == part.CrewCapacity) Logging.Msg(Localizer.Format("#MOAR-Kuddle-09")); // "No room left to kuddle"
            }
            return false;
        }

        /// <summary>Play sound upon failure</summary>
        private void FailureSound()
        {
            Logging.DLog(logMsg: $"Kuddle: Failure Sound");
            int _soundSelection = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundClipB0;
            if (_soundSelection == 0)
            {
                System.Random newRand = new System.Random();
                _soundSelection = newRand.Next(1, 2);
            }
            switch (_soundSelection)
            {
                case 1:
                    kuddling_failure0.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    kuddling_failure0.Play();
                    return;
                case 2:
                    kuddling_failure1.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    kuddling_failure1.Play();
                    return;
                default:
                    return;
            }
        }

        /// <summary>Play sound upon success</summary>
        private void SuccessSound()
        {
            Logging.DLog(logMsg: $"Kuddle: Success Sound");
            int _soundSelection = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundClipB1;
            if (_soundSelection == 0)
            {
                System.Random newRand = new System.Random();
                _soundSelection = newRand.Next(1, 2);
            }
            switch (_soundSelection)
            {
                case 1:
                    kuddling_success0.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    kuddling_success0.Play();
                    return;
                case 2:
                    kuddling_success1.volume = HighLogic.CurrentGame.Parameters.CustomParams<Settings>().soundVolume;
                    kuddling_success1.Play();
                    return;
                default:
                    return;
            }
        }

        /// <summary>Module information shown in editors</summary>
        private string info = string.Empty;

        /// <summary>What shows up in editor for the part</summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            base.OnStart(StartState.None);

            /* :::This is what it should look like with default settings:::
             * 
             * MoarKerbals
             * Kerbthulhu Kinetics Program
             * v 1.3.0.0
             * 
             * Input: (color)
             * One kerbal of each gender is needed for kuddling
             * 
             * Required Resources: (color)
             * - ElectricCharge: 8000
             * - Oxidizer: 100
             * - Ore: 500
             * 
             * Required Currency: (color)
             * - Funds 1000
             * - Science: 1
             * - Reputation: 2
             * 
             * Output:
             * A brand new Kerbal.
             * 
             */
            info += Localizer.Format("#MOAR-Manu") + "\r\n"; // Kerbthulhu Kinetics Program
            info += Localizer.Format("#MOAR-003", Version.SText) + "\r\n"; // MoarKerbals v Version Number text

            info += $"\r\n<color=#FFFF19>{Localizer.Format("#MOAR-005")}:</color>\r\n"; // Input
            info += $"{Localizer.Format("#MOAR-Kuddle-05")}\r\n"; // One kerbal of each gender is needed for kuddling

            info += $"\r\n\t{Localizer.Format("#MOAR-Kuddle-13")}\r\n"; // "what, no disco music?"

            // info += $"Global Cost Multiplier of {gblMult:P2} (is not included)\r\n";

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

            info += $"\r\n<color=#FFFF19>{Localizer.Format("#MOAR-006")}:</color>\r\n"; // Output:
            info += $"{Localizer.Format("#MOAR-Kuddle-10")}."; // A brand new Kerbal.

            return info;
        }

    }
}