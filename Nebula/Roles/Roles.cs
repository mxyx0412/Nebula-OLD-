﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Roles
{
    static public class Roles
    {

        public static CrewmateRoles.Crewmate Crewmate = new CrewmateRoles.Crewmate();
        public static CrewmateRoles.Agent Agent = new CrewmateRoles.Agent();
        public static CrewmateRoles.Alien Alien = new CrewmateRoles.Alien();
        public static CrewmateRoles.Bait Bait = new CrewmateRoles.Bait();
        public static CrewmateRoles.Booster Booster = new CrewmateRoles.Booster();
        public static CrewmateRoles.DamnedCrew DamnedCrew = new CrewmateRoles.DamnedCrew();
        public static CrewmateRoles.Engineer Engineer = new CrewmateRoles.Engineer();
        public static CrewmateRoles.Mayor Mayor = new CrewmateRoles.Mayor();
        public static CrewmateRoles.Necromancer Necromancer = new CrewmateRoles.Necromancer();
        public static ComplexRoles.Guesser NiceGuesser = new ComplexRoles.Guesser("NiceGuesser", "niceGuesser", false);
        public static ComplexRoles.Trapper NiceTrapper = new ComplexRoles.Trapper("NiceTrapper", "niceTrapper", false);
        public static CrewmateRoles.Provocateur Provocateur = new CrewmateRoles.Provocateur();
        public static CrewmateRoles.Psychic Psychic = new CrewmateRoles.Psychic();
        public static CrewmateRoles.SecurityGuard SecurityGuard = new CrewmateRoles.SecurityGuard();
        public static CrewmateRoles.Sheriff Sheriff = new CrewmateRoles.Sheriff();
        public static CrewmateRoles.Spy Spy = new CrewmateRoles.Spy();

        public static CrewmateRoles.Madmate Madmate = new CrewmateRoles.Madmate();

        public static ImpostorRoles.Impostor Impostor = new ImpostorRoles.Impostor();
        public static ImpostorRoles.Camouflager Camouflager = new ImpostorRoles.Camouflager();
        public static ImpostorRoles.Cleaner Cleaner = new ImpostorRoles.Cleaner();
        public static ImpostorRoles.Damned Damned = new ImpostorRoles.Damned();
        public static ImpostorRoles.Eraser Eraser = new ImpostorRoles.Eraser();
        public static ImpostorRoles.EvilAce EvilAce = new ImpostorRoles.EvilAce();
        public static ComplexRoles.Guesser EvilGuesser = new ComplexRoles.Guesser("EvilGuesser", "evilGuesser", true);
        public static ComplexRoles.Trapper EvilTrapper = new ComplexRoles.Trapper("EvilTrapper", "evilTrapper", true);
        public static ImpostorRoles.Morphing Morphing = new ImpostorRoles.Morphing();
        public static ImpostorRoles.Reaper Reaper = new ImpostorRoles.Reaper();
        public static ImpostorRoles.Sniper Sniper = new ImpostorRoles.Sniper();

        public static NeutralRoles.Arsonist Arsonist = new NeutralRoles.Arsonist();
        public static NeutralRoles.Empiric Empiric = new NeutralRoles.Empiric();
        public static NeutralRoles.Jackal Jackal = new NeutralRoles.Jackal();
        public static NeutralRoles.Jester Jester = new NeutralRoles.Jester();
        public static NeutralRoles.Opportunist Opportunist = new NeutralRoles.Opportunist();
        public static NeutralRoles.Vulture Vulture = new NeutralRoles.Vulture();

        public static ComplexRoles.FCrewmate F_Crewmate = new ComplexRoles.FCrewmate();
        public static ComplexRoles.FGuesser F_Guesser = new ComplexRoles.FGuesser();
        public static ComplexRoles.FTrapper F_Trapper = new ComplexRoles.FTrapper();

        public static ExtraRoles.Lover Lover = new ExtraRoles.Lover();
        public static ExtraRoles.Trilemma Trilemma = new ExtraRoles.Trilemma();


        //全てのロールはこの中に含まれている必要があります
        public static List<Role> AllRoles = new List<Role>()
        {
            Impostor,Camouflager,Cleaner,Damned,Eraser,EvilAce,EvilGuesser,EvilTrapper,Morphing,Reaper,Sniper,
            F_Guesser,F_Trapper,
            Arsonist,Empiric,Jackal,Jester,Opportunist,Vulture,
            F_Crewmate,
            Crewmate,Agent,Alien,Bait,Booster,DamnedCrew,Engineer,Mayor,Necromancer,NiceGuesser,NiceTrapper,Provocateur,Psychic,SecurityGuard,Sheriff,Spy,
            Madmate,
        };

        public static List<ExtraRole> AllExtraRoles = new List<ExtraRole>()
        {
            Lover,Trilemma
        };

        public static void ResetWinTrigger()
        {
            foreach(Role role in AllRoles)
            {
                if(role is Template.HasWinTrigger)
                {
                    ((Template.HasWinTrigger)role).WinTrigger = false;
                }
            }

            foreach (ExtraRole role in AllExtraRoles)
            {
                if (role is Template.HasWinTrigger)
                {
                    ((Template.HasWinTrigger)role).WinTrigger = false;
                }
            }
        }

        public static void StaticInitialize()
        {
            foreach (Role role in AllRoles)
            {
                role.StaticInitialize();
            }

            foreach (ExtraRole role in AllExtraRoles)
            {
                role.StaticInitialize();
            }
        }
    }
}
