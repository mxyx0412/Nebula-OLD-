﻿using AmongUs.GameOptions;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules.ScriptComponents;

public class ModAbilityButton : INebulaScriptComponent
{
    public enum LabelType
    {
        Standard,
        Impostor,
        Utility,
        Crewmate,
    }

    public ActionButton VanillaButton { get; private set; }

    public Timer? CoolDownTimer;
    public Timer? EffectTimer;
    public Timer? CurrentTimer => EffectTimer?.IsInProcess ?? false ? EffectTimer : CoolDownTimer;
    public bool EffectActive = false;

    public Action<ModAbilityButton>? OnEffectStart { get; set; } = null;
    public Action<ModAbilityButton>? OnEffectEnd { get; set; } = null;
    public Action<ModAbilityButton>? OnClick { get; set; } = null;
    public Action<ModAbilityButton>? OnMeetingStart { get; set; } = null;
    public Predicate<ModAbilityButton>? Availability { get; set; } = null;
    public Predicate<ModAbilityButton>? Visibility { get; set; } = null;
    private KeyCode? keyCode { get; set; } = null;

    internal ModAbilityButton(bool isLeftSideButton = false, bool isArrangedAsKillButton = false)
    {

        VanillaButton = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton, HudManager.Instance.KillButton.transform.parent);
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));

        VanillaButton.buttonLabelText.GetComponent<TextTranslatorTMP>().enabled = false;
        var passiveButton = VanillaButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener(() => DoClick());

        var gridContent = VanillaButton.gameObject.GetComponent<HudContent>();
        gridContent.MarkAsKillButtonContent(isArrangedAsKillButton);
        NebulaGameManager.Instance?.HudGrid.RegisterContent(gridContent, isLeftSideButton);

    }

    public override void OnReleased()
    {
        if (VanillaButton) UnityEngine.Object.Destroy(VanillaButton.gameObject);
    }

    public override void Update()
    {
        //表示・非表示切替
        VanillaButton.gameObject.SetActive(Visibility?.Invoke(this) ?? true);
        //使用可能性切替
        if (Availability?.Invoke(this) ?? true)
            VanillaButton.SetEnabled();
        else
            VanillaButton.SetDisabled();

        if (EffectActive && (EffectTimer == null || !EffectTimer.IsInProcess)) InactivateEffect();

        VanillaButton.SetCooldownFill(CurrentTimer?.Percentage ?? 0f);

        string timerText = "";
        if (CurrentTimer?.IsInProcess ?? false) timerText = Mathf.CeilToInt(CurrentTimer.CurrentTime).ToString();
        VanillaButton.cooldownTimerText.text = timerText;

        if (keyCode.HasValue && NebulaInput.GetKey(keyCode.Value)) DoClick();
    }

    public ModAbilityButton InactivateEffect()
    {
        if (EffectActive) return this;
        EffectActive = false;
        OnEffectEnd?.Invoke(this);
        return this;
    }

    public ModAbilityButton ActivateEffect()
    {
        if (!EffectActive) return this;
        EffectActive = true;
        OnEffectStart?.Invoke(this);
        return this;
    }

    public ModAbilityButton StartCoolDown()
    {
        CoolDownTimer?.Start();
        return this;
    }

    public ModAbilityButton DoClick()
    {
        //効果中でなく、クールダウン中ならばなにもしない
        if (!EffectActive && (CoolDownTimer?.IsInProcess ?? false)) return this;
        //使用可能でないかを判定 (ボタン発火のタイミングと可視性更新のタイミングにずれが生じうるためここで再計算)
        if (!(Visibility?.Invoke(this) ?? true) || !(Availability?.Invoke(this) ?? true)) return this;

        OnClick?.Invoke(this);
        return this;
    }

    public ModAbilityButton SetSprite(Sprite sprite)
    {
        VanillaButton.graphic.sprite = sprite;
        VanillaButton.graphic.SetCooldownNormalizedUvs();
        return this;
    }

    public ModAbilityButton SetLabel(string translationKey)
    {
        VanillaButton.buttonLabelText.text = Language.Translate(translationKey);
        VanillaButton.graphic.SetCooldownNormalizedUvs();
        return this;
    }

    public ModAbilityButton SetLabelType(LabelType labelType)
    {
        Material? material = null;
        switch (labelType)
        {
            case LabelType.Standard:
                material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial;
                break;
            case LabelType.Utility:
                material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.PolusAdminButton].FontMaterial;
                break;
            case LabelType.Impostor:
                material = RoleManager.Instance.GetRole(RoleTypes.Shapeshifter).Ability.FontMaterial;
                break;
            case LabelType.Crewmate:
                material = RoleManager.Instance.GetRole(RoleTypes.Engineer).Ability.FontMaterial;
                break;

        }
        if (material != null) VanillaButton.buttonLabelText.SetSharedMaterial(material);
        return this;
    }

    public ModAbilityButton KeyBind(KeyCode keyCode)
    {
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));

        this.keyCode= keyCode;
        ButtonEffect.SetKeyGuide(VanillaButton.gameObject, keyCode);
        return this;
    }
}

public static class ButtonEffect
{
    [NebulaPreLoad]
    public class KeyCodeInfo
    {
        static public Dictionary<KeyCode, KeyCodeInfo> AllKeyInfo = new();
        public KeyCode keyCode { get; private set; }
        public DividedSpriteLoader textureHolder { get; private set; }
        public int num { get; private set; }
        public string TranslationKey { get; private set; }
        public KeyCodeInfo(KeyCode keyCode, string translationKey, DividedSpriteLoader spriteLoader, int num)
        {
            this.keyCode = keyCode;
            this.TranslationKey = translationKey;
            this.textureHolder = spriteLoader;
            this.num = num;

            AllKeyInfo.Add(keyCode, this);
        }

        public Sprite Sprite => textureHolder.GetSprite(num);
        public static void Load()
        {
            DividedSpriteLoader spriteLoader;
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters0.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.Tab, "Tab", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.Space, "Space", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.Comma, "<", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.Period, ">", spriteLoader, 3);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters1.png", 100f, 18, 19, true);
            for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
                new KeyCodeInfo(key, ((char)('A' + key - KeyCode.A)).ToString(), spriteLoader, key - KeyCode.A);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters2.png", 100f, 18, 19, true);
            for (int i = 0; i < 15; i++)
                new KeyCodeInfo(KeyCode.F1 + i, "F" + (i + 1), spriteLoader, i);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters3.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.RightShift, "RShift", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.LeftShift, "LShift", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.RightControl, "RControl", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.LeftControl, "LControl", spriteLoader, 3);
            new KeyCodeInfo(KeyCode.RightAlt, "RAlt", spriteLoader, 4);
            new KeyCodeInfo(KeyCode.LeftAlt, "LAlt", spriteLoader, 5);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters4.png", 100f, 18, 19, true);
            for (int i = 0; i < 6; i++)
                new KeyCodeInfo(KeyCode.Mouse1 + i, "Mouse " + (i == 0 ? "Right" : i == 1 ? "Middle" : (i + 1).ToString()), spriteLoader, i);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters5.png", 100f, 18, 19, true);
            for (int i = 0; i < 10; i++)
                new KeyCodeInfo(KeyCode.Alpha0 + i, "0" + (i + 1), spriteLoader, i);
        }
    }

    private static DividedSpriteLoader textureUsesIconsSprite = DividedSpriteLoader.FromResource("Nebula.Resources.UsesIcon.png", 100f, 10, 1);
    static public GameObject ShowUsesIcon(this ActionButton button)
    {
        Transform template = HudManager.Instance.AbilityButton.transform.GetChild(2);
        var usesObject = GameObject.Instantiate(template.gameObject);
        usesObject.transform.SetParent(button.gameObject.transform);
        usesObject.transform.localScale = template.localScale;
        usesObject.transform.localPosition = template.localPosition * 1.2f;
        return usesObject;
    }

    static public GameObject ShowUsesIcon(this ActionButton button, int iconVariation, out TMPro.TextMeshPro text)
    {
        GameObject result = ShowUsesIcon(button);
        var renderer = result.GetComponent<SpriteRenderer>();
        renderer.sprite = textureUsesIconsSprite.GetSprite(iconVariation);
        text = result.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
        return result;
    }

    static public SpriteRenderer AddOverlay(this ActionButton button, Sprite sprite, float order)
    {
        GameObject obj = new GameObject("Overlay");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(button.gameObject.transform);
        obj.transform.localScale = new Vector3(1, 1, 1);
        obj.transform.localPosition = new Vector3(0, 0, -1f - order);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }

    static ISpriteLoader keyBindBackgroundSprite = SpriteLoader.FromResource("Nebula.Resources.KeyBindBackground.png", 100f);
    static public GameObject? AddKeyGuide(GameObject button, KeyCode key, Vector2 pos)
    {
        Sprite? numSprite = null;
        if (KeyCodeInfo.AllKeyInfo.ContainsKey(key)) numSprite = KeyCodeInfo.AllKeyInfo[key].Sprite;
        if (numSprite == null) return null;

        GameObject obj = new GameObject();
        obj.name = "HotKeyGuide";
        obj.transform.SetParent(button.transform);
        obj.layer = button.layer;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = (Vector3)pos + new Vector3(0f, 0f, -10f);
        renderer.sprite = keyBindBackgroundSprite.GetSprite();

        GameObject numObj = new GameObject();
        numObj.name = "HotKeyText";
        numObj.transform.SetParent(obj.transform);
        numObj.layer = button.layer;
        renderer = numObj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = new Vector3(0, 0, -1f);
        renderer.sprite = numSprite;

        return obj;
    }
    static public GameObject? SetKeyGuide(GameObject button, KeyCode key)
    {
        return AddKeyGuide(button, key, new Vector2(0.48f, 0.48f));
    }

    static public GameObject? SetKeyGuideOnSmallButton(GameObject button, KeyCode key)
    {
        return AddKeyGuide(button, key, new Vector2(0.28f, 0.28f));
    }
}