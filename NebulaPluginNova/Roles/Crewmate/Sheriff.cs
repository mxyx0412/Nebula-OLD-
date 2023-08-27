﻿using Epic.OnlineServices.Stats;
using Nebula.Configuration;
using System;

namespace Nebula.Roles.Crewmate;

public class Sheriff : ConfigurableStandardRole
{
    static public Sheriff MyRole = new Sheriff();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "sheriff";
    public override Color RoleColor => new Color(240f / 255f, 191f / 255f, 0f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerControl player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration CanKillMadmateOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        CanKillMadmateOption = new(RoleConfig, "canKillMadmate", null, false, false);
    }

    public class Instance : Crewmate.Instance
    {
        private ModAbilityButton? killButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SheriffKillButton.png", 100f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerControl player) : base(player)
        {
        }

        private bool CanKill(PlayerControl target)
        {
            var info = target.GetModInfo();
            if (info.Role.Role == Madmate.MyRole) return Sheriff.MyRole.CanKillMadmateOption.GetBool()!.Value;
            if (info.Role.Role.RoleCategory == RoleCategory.CrewmateRole) return false;
            return true;
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                var killTracker = Bind(ObjectTrackers.ForPlayer(1.2f, player, (p) => p.PlayerId != player.PlayerId && !p.Data.IsDead));
                killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(KeyCode.F);
                killButton.SetSprite(buttonSprite.GetSprite());
                killButton.Availability = (button) => killTracker.CurrentTarget != null && player.CanMove;
                killButton.Visibility = (button) => !player.Data.IsDead;
                killButton.OnClick = (button) => {
                    if (CanKill(killTracker.CurrentTarget!)) 
                        player.ModKill(killTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill); 
                    else
                    {
                        player.ModKill(player, PlayerState.Misfired, null); 
                        GameStatistics.RpcRecord.Invoke(new(GameStatistics.EventVariation.Kill, EventDetail.Misfire, player, killTracker.CurrentTarget!));
                    }
                    button.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(0f, 25f));
                killButton.SetLabelType(ModAbilityButton.LabelType.Standard);
            }
        }

        public override void OnGameStart()
        {
            killButton?.StartCoolDown();
        }

        public override void OnGameReenabled()
        {
            killButton?.StartCoolDown();
        }

    }
}
