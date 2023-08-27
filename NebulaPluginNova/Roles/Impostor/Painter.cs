﻿using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class Painter : ConfigurableStandardRole
{
    static public Painter MyRole = new Painter();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "painter";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerControl player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration SampleCoolDownOption;
    private NebulaConfiguration PaintCoolDownOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        SampleCoolDownOption = new NebulaConfiguration(RoleConfig, "sampleCoolDown", null, 5f, 60f, 2.5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
        PaintCoolDownOption = new NebulaConfiguration(RoleConfig, "paintCoolDown", null, 5f, 60f, 5f, 30f, 30f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? sampleButton = null;
        private ModAbilityButton? paintButton = null;

        static private ISpriteLoader sampleButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SampleButton.png", 115f);
        static private ISpriteLoader paintButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.MorphButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerControl player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                GameData.PlayerOutfit? sample = null;
                PoolablePlayer? sampleIcon = null;
                var sampleTracker = Bind(ObjectTrackers.ForPlayer(1.2f, player, (p) => p.PlayerId != player.PlayerId && !p.Data.IsDead));

                sampleButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                sampleButton.SetSprite(sampleButtonSprite.GetSprite());
                sampleButton.Availability = (button) => sampleTracker.CurrentTarget != null && player.CanMove;
                sampleButton.Visibility = (button) => !player.Data.IsDead && sample == null;
                sampleButton.OnClick = (button) => {
                    paintButton.CoolDownTimer.SetTime(5f).Resume();
                    sample = sampleTracker.CurrentTarget!.GetModInfo().GetOutfit(75);

                    sampleIcon = AmongUsUtil.GetPlayerIcon(sample, paintButton.VanillaButton.transform, new Vector3(-0.4f, 0.35f, -0.5f), new(0.3f, 0.3f)).SetAlpha(0.5f);
                };
                sampleButton.CoolDownTimer = Bind(new Timer(0f, MyRole.SampleCoolDownOption.GetFloat()!.Value));
                sampleButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                sampleButton.SetLabel("sample");

                paintButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                paintButton.SetSprite(paintButtonSprite.GetSprite());
                paintButton.Availability = (button) => sampleTracker.CurrentTarget != null && player.CanMove;
                paintButton.Visibility = (button) => !player.Data.IsDead && sample != null;
                paintButton.OnClick = (button) => {
                    PlayerModInfo.RpcAddOutfit.Invoke(new(sampleTracker.CurrentTarget!.PlayerId, new("Paint", 40, false, sample)));
                };
                paintButton.OnMeeting = (button) =>
                {
                    if (sampleIcon != null)
                    {
                        GameObject.Destroy(sampleIcon.gameObject);
                        sampleIcon = null;
                    }
                    sample = null;
                };
                paintButton.CoolDownTimer = Bind(new Timer(0f, MyRole.PaintCoolDownOption.GetFloat()!.Value));
                paintButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                paintButton.SetLabel("paint");
            }
        }

        public override void OnGameStart()
        {
            sampleButton?.StartCoolDown();
            paintButton?.StartCoolDown();
        }

        public override void OnGameReenabled()
        {
            sampleButton?.StartCoolDown();
            paintButton?.StartCoolDown();
        }
    }
}
