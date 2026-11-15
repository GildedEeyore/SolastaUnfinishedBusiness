using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Classes;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Validators;
using Color = UnityEngine.Color;
using static RuleDefinitions;
using static FeatureDefinitionSavingThrowAffinity;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;
using static SolastaUnfinishedBusiness.Models.ItemPropertyDescriptionsContext;

namespace SolastaUnfinishedBusiness.Models;

// Arcana Unleashed and DMG magic items added in this fork
internal static class MagicItemsContext
{
    private const int StaffOfCharmingMaxCharges = 10;
    private const int StaffOfFrostMaxCharges = 10;
    private const int VoyagerStaffMaxCharges = 10;
    private const int DissuaderSaveDC = 15;
    private const int StaffOfThunderAndLightningSaveDC = 17;
    private const int StaffOfPowerMaxCharges = 20;
    private const int StaffOfTheMagiMaxCharges = 50;
    private const int RetributiveStrikeRange = 6; // 30 ft
    private const int RetributiveStrikeDC = 17;
    private const int RetributiveStrikeSelfMultiplier = 16;

    internal static ItemDefinition OrbOfSorcery;
    internal static ItemDefinition RobeOfTheArchmagi;
    internal static ItemDefinition SpellguardShield;
    internal static ItemDefinition StaffOfFrost;
    internal static ItemDefinition StaffOfPower;
    internal static ItemDefinition StaffOfTheMagi;
    internal static ItemDefinition SunBlade;
    internal static ItemDefinition AnstruthHarp;
    internal static ItemDefinition CanaithMandolin;
    internal static ItemDefinition CliLyre;
    internal static ItemDefinition DossLute;
    internal static ItemDefinition FochlucanBandore;
    internal static ItemDefinition MacFuirmidhCittern;
    internal static ItemDefinition OllamhHarp;
    internal static ItemDefinition SpellDuelistsTrophy;
    internal static ItemDefinition StaffOfThunderAndLightning;
    internal static ItemDefinition Dissuader;
    internal static ItemDefinition VoyagerStaff;
    internal static ItemDefinition StaffOfCharming;

    private static FeatureDefinition _featureSpellDuelistsTrophyHelpingHand;

    private static FeatureDefinitionPower _powerStaffOfTheMagiRetributiveStrike;

    // charges to use on a Retributive Strike triggered by Spell Absorption going over the staff's maximum
    private static readonly Dictionary<ulong, int> ChargesOverride = [];

    internal static void Load()
    {
        OrbOfSorcery = BuildOrbOfSorcery();
        RobeOfTheArchmagi = BuildRobeOfTheArchmagi();
        SpellguardShield = BuildSpellguardShield();
        StaffOfFrost = BuildStaffOfFrost();
        StaffOfPower = BuildStaffOfPower();
        StaffOfTheMagi = BuildStaffOfTheMagi();
        SunBlade = BuildSunBlade();
        BuildInstrumentsOfTheBards();
        SpellDuelistsTrophy = BuildSpellDuelistsTrophy();
        StaffOfThunderAndLightning = BuildStaffOfThunderAndLightning();
        Dissuader = BuildDissuader();
        VoyagerStaff = BuildVoyagerStaff();
        StaffOfCharming = BuildStaffOfCharming();

        foreach (var item in new[]
                 {
                     OrbOfSorcery, RobeOfTheArchmagi, SpellguardShield, StaffOfFrost, StaffOfPower, StaffOfTheMagi,
                     SunBlade,
                     AnstruthHarp, CanaithMandolin, CliLyre, DossLute, FochlucanBandore, MacFuirmidhCittern,
                     OllamhHarp, SpellDuelistsTrophy,
                     StaffOfThunderAndLightning, Dissuader, VoyagerStaff, StaffOfCharming
                 })
        {
            item.inDungeonEditor = Main.Settings.AddNewWeaponsAndRecipesToEditor;
        }

        MerchantContext.AddItem(OrbOfSorcery, ShopItemType.MagicItemUncommon);
        MerchantContext.AddItem(SunBlade, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(SpellguardShield, ShopItemType.MagicItemVeryRare);
        MerchantContext.AddItem(StaffOfFrost, ShopItemType.MagicItemVeryRare);
        MerchantContext.AddItem(StaffOfPower, ShopItemType.MagicItemVeryRare);
        MerchantContext.AddItem(RobeOfTheArchmagi, ShopItemType.MagicItemLegendary);
        MerchantContext.AddItem(StaffOfTheMagi, ShopItemType.MagicItemLegendary);
        MerchantContext.AddItem(DossLute, ShopItemType.MagicItemUncommon);
        MerchantContext.AddItem(FochlucanBandore, ShopItemType.MagicItemUncommon);
        MerchantContext.AddItem(MacFuirmidhCittern, ShopItemType.MagicItemUncommon);
        MerchantContext.AddItem(CanaithMandolin, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(CliLyre, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(SpellDuelistsTrophy, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(AnstruthHarp, ShopItemType.MagicItemVeryRare);
        MerchantContext.AddItem(OllamhHarp, ShopItemType.MagicItemLegendary);
        MerchantContext.AddItem(Dissuader, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(StaffOfCharming, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(StaffOfThunderAndLightning, ShopItemType.MagicItemVeryRare);
        MerchantContext.AddItem(VoyagerStaff, ShopItemType.MagicItemVeryRare);
    }

    #region Orb of Sorcery

    private static ItemDefinition BuildOrbOfSorcery()
    {
        const string NAME = "OrbOfSorcery";

        var effectFormSorcerousReservoir = EffectFormBuilder
            .Create()
            .SetSpellForm(9)
            .Build();

        effectFormSorcerousReservoir.SpellSlotsForm.type = SpellSlotsForm.EffectType.GainSorceryPoints;
        effectFormSorcerousReservoir.SpellSlotsForm.sorceryPointsGain = 2;

        var powerSorcerousReservoir = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}SorcerousReservoir")
            .SetGuiPresentation(Category.Feature, FeatureDefinitionPowers.PowerSorcererCreateSorceryPoints)
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(effectFormSorcerousReservoir)
                    .SetParticleEffectParameters(FeatureDefinitionPowers.PowerSorcererCreateSorceryPoints)
                    .Build())
            .AddToDB();

        var item = ItemDefinitionBuilder
            .Create(ItemDefinitions.ArcaneFocusOrb, NAME)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(NAME, Resources.OrbOfSorcery, 128))
            .SetMerchantCategory(MerchantCategoryDefinitions.MagicDevice)
            .SetItemRarity(ItemRarity.Uncommon)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement(Sorcerer)
            .SetSlotTypes(
                SlotTypeDefinitions.MainHandSlot, SlotTypeDefinitions.OffHandSlot, SlotTypeDefinitions.ContainerSlot)
            .SetSlotsWhereActive(SlotTypeDefinitions.MainHandSlot, SlotTypeDefinitions.OffHandSlot)
            .SetGold(400)
            .SetUsableDeviceDescription(
                new UsableDeviceDescriptionBuilder()
                    .AddFunctions(
                        new DeviceFunctionDescriptionBuilder()
                            .SetUsage(useAffinity: DeviceFunctionDescription.FunctionUseAffinity.IterationPerRecharge)
                            .SetPower(powerSorcerousReservoir)
                            .Build())
                    .Build())
            .AddToDB();

        return item;
    }

    #endregion

    #region Robe of the Archmagi

    private static ItemDefinition BuildRobeOfTheArchmagi()
    {
        const string NAME = "RobeOfTheArchmagi";

        var magicAffinityWarMage = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{NAME}WarMage")
            .SetGuiPresentation(Category.Feature)
            .SetCastingModifiers(2, SpellParamsModifierType.FlatValue, 2)
            .AddToDB();

        var item = ItemDefinitionBuilder
            .Create(ItemDefinitions.WizardClothes_Alternate, NAME)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(NAME, Resources.RobeOfTheArchmagi, 128))
            .SetMerchantCategory(MerchantCategoryDefinitions.MagicDevice)
            .SetItemRarity(ItemRarity.Legendary)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement(Sorcerer, Warlock, Wizard)
            .SetGold(200000)
            .SetStaticProperties(
                BuildFrom(FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinitySpellResistance, false),
                BuildFrom(magicAffinityWarMage, false))
            .AddToDB();

        // the robe takes the armor slot, so it can only be worn without armor: base AC 15 + Dexterity
        item.armorDefinition = new ArmorDescription(item.ArmorDescription)
        {
            armorClassValue = 15, isBaseArmorClass = true, maxDexterityBonus = -1
        };

        return item;
    }

    #endregion

    #region Spellguard Shield

    private static ItemDefinition BuildSpellguardShield()
    {
        const string NAME = "SpellguardShield";

        var featureSpellAttackDisadvantage = FeatureDefinitionBuilder
            .Create($"Feature{NAME}")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureSpellAttackDisadvantage.AddCustomSubFeatures(
            new MagicEffectAttackInitiatedOnMeSpellguard(featureSpellAttackDisadvantage));

        var item = ItemDefinitionBuilder
            .Create(ItemDefinitions.ShieldPlus2, NAME)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(NAME, Resources.SpellguardShield, 128))
            .SetItemRarity(ItemRarity.VeryRare)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement()
            .SetGold(40000)
            .SetStaticProperties(
                BuildFrom(FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinitySpellResistance, false),
                BuildFrom(featureSpellAttackDisadvantage, false))
            .AddToDB();

        // a Spellguard Shield is a regular shield, not a +2 one
        item.armorDefinition = new ArmorDescription(item.ArmorDescription)
        {
            armorClassValue = ItemDefinitions.Shield.ArmorDescription.ArmorClassValue
        };

        return item;
    }

    private sealed class MagicEffectAttackInitiatedOnMeSpellguard(FeatureDefinition feature)
        : IMagicEffectAttackInitiatedOnMe
    {
        public IEnumerator OnMagicEffectAttackInitiatedOnMe(
            CharacterActionMagicEffect action,
            RulesetEffect activeEffect,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier attackModifier,
            bool firstTarget,
            bool checkMagicalAttackDamage)
        {
            if (activeEffect is RulesetEffectSpell)
            {
                attackModifier.AttackAdvantageTrends.Add(
                    new TrendInfo(-1, FeatureSourceType.Equipment, feature.Name, feature));
            }

            yield break;
        }
    }

    #endregion

    #region Sun Blade

    private static ItemDefinition BuildSunBlade()
    {
        const string NAME = "SunBlade";

        var baseItem = ItemDefinitions.LongswordPlus2;
        var weaponDescription = new WeaponDescription(baseItem.WeaponDescription);

        weaponDescription.WeaponTags.TryAdd(TagsDefinitions.WeaponTagFinesse);
        weaponDescription.EffectDescription
            .GetFirstFormOfType(EffectForm.EffectFormType.Damage).DamageForm.damageType = DamageTypeRadiant;

        var additionalDamageUndead = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag(NAME)
            .SetDamageDice(DieType.D8, 1)
            .SetSpecificDamageType(DamageTypeRadiant)
            .SetTriggerCondition(AdditionalDamageTriggerCondition.SpecificCharacterFamily)
            .SetRequiredCharacterFamily(CharacterFamilyDefinitions.Undead)
            .AddToDB();

        var item = ItemDefinitionBuilder
            .Create(baseItem, NAME)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(NAME, Resources.SunBlade, 128))
            .SetItemRarity(ItemRarity.Rare)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement()
            .SetGold(4000)
            .SetWeaponDescription(weaponDescription)
            .SetStaticProperties(WeaponPlus2, BuildFrom(additionalDamageUndead))
            .AddToDB();

        // the blade sheds sunlight: bright light in a 15-foot radius and dim light for another 15 feet
        var lightSource = new LightSourceItemDescription(ItemDefinitions.DivineBladeWeapon.LightSourceItemDescription)
        {
            lightSourceType = LightSourceType.Sun,
            brightRange = 3,
            dimAdditionalRange = 3,
            color = new Color(1f, 0.92f, 0.7f),
            durationType = DurationType.Permanent,
            durationValue = 0
        };

        item.isLightSourceItem = true;
        item.lightSourceItemDefinition = lightSource;

        return item;
    }

    #endregion

    #region Staffs

    private static bool IsChargedStaff(ItemDefinition itemDefinition)
    {
        return itemDefinition && (itemDefinition == StaffOfCharming ||
                                  itemDefinition == StaffOfFrost ||
                                  itemDefinition == StaffOfPower ||
                                  itemDefinition == StaffOfTheMagi ||
                                  itemDefinition == VoyagerStaff);
    }

    private static ItemDefinition BuildStaffOfFrost()
    {
        const string NAME = "StaffOfFrost";

        var item = BuildStaff(NAME, ItemRarity.VeryRare, 40000,
            new UsableDeviceDescriptionBuilder()
                .SetUsage(EquipmentDefinitions.ItemUsage.Charges)
                .SetChargesCapitalNumber(StaffOfFrostMaxCharges)
                .SetRecharge(RechargeRate.Dawn, 1, DieType.D6, 4)
                .SetOutOfChargesConsequence(EquipmentDefinitions.ItemOutOfCharges.Persist)
                .SetSaveDc(EffectHelpers.BasedOnUser)
                .SetMagicAttackBonus(EffectHelpers.BasedOnUser)
                .AddFunctions(
                    SpellFunction(SpellDefinitions.ConeOfCold, 5),
                    SpellFunction(SpellDefinitions.FogCloud, 1),
                    SpellFunction(SpellDefinitions.IceStorm, 4),
                    SpellFunction(SpellsContext.WallOfIce, 4))
                .Build(),
            new DeviceSpellLevels(),
            Resources.StaffOfFrost,
            [Druid, Sorcerer, Warlock, Wizard],
            BuildFrom(FeatureDefinitionDamageAffinitys.DamageAffinityColdResistance, false));

        return item;
    }

    private static ItemDefinition BuildStaffOfPower()
    {
        const string NAME = "StaffOfPower";

        var savingThrowAffinity = FeatureDefinitionSavingThrowAffinityBuilder
            .Create($"SavingThrowAffinity{NAME}")
            .SetGuiPresentation(Category.Feature)
            .SetModifiers(ModifierType.AddDice, DieType.D1, 2, false,
                AttributeDefinitions.Strength,
                AttributeDefinitions.Dexterity,
                AttributeDefinitions.Constitution,
                AttributeDefinitions.Intelligence,
                AttributeDefinitions.Wisdom,
                AttributeDefinitions.Charisma)
            .AddToDB();

        var magicAffinity = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{NAME}")
            .SetGuiPresentation(Category.Feature)
            .SetCastingModifiers(2)
            .AddToDB();

        var powerRetributiveStrike = BuildRetributiveStrike(NAME, 4, () => StaffOfPower);

        var item = BuildStaff(NAME, ItemRarity.VeryRare, 40000,
            new UsableDeviceDescriptionBuilder()
                .SetUsage(EquipmentDefinitions.ItemUsage.Charges)
                .SetChargesCapitalNumber(StaffOfPowerMaxCharges)
                .SetRecharge(RechargeRate.Dawn, 2, DieType.D8, 4)
                .SetOutOfChargesConsequence(EquipmentDefinitions.ItemOutOfCharges.Persist)
                .SetSaveDc(EffectHelpers.BasedOnUser)
                .SetMagicAttackBonus(EffectHelpers.BasedOnUser)
                .AddFunctions(
                    SpellFunction(SpellDefinitions.ConeOfCold, 5),
                    SpellFunction(SpellDefinitions.Fireball, 5),
                    SpellFunction(SpellDefinitions.GlobeOfInvulnerability, 6),
                    SpellFunction(SpellDefinitions.HoldMonster, 5),
                    SpellFunction(SpellDefinitions.Levitate, 2),
                    SpellFunction(SpellDefinitions.LightningBolt, 5),
                    SpellFunction(SpellDefinitions.MagicMissile, 1),
                    SpellFunction(SpellDefinitions.RayOfEnfeeblement, 1),
                    SpellFunction(SpellDefinitions.WallOfForce, 5),
                    PowerFunction(powerRetributiveStrike))
                .Build(),
            new DeviceSpellLevels(
                (SpellDefinitions.Fireball, 5),
                (SpellDefinitions.LightningBolt, 5)),
            Resources.StaffOfPower,
            [Sorcerer, Warlock, Wizard],
            WeaponPlus2,
            BuildFrom(FeatureDefinitionAttributeModifiers.AttributeModifierArmorPlus2, false),
            BuildFrom(savingThrowAffinity, false),
            BuildFrom(magicAffinity, false));

        return item;
    }

    private static ItemDefinition BuildStaffOfTheMagi()
    {
        const string NAME = "StaffOfTheMagi";

        var magicAffinity = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{NAME}")
            .SetGuiPresentation(Category.Feature)
            .SetCastingModifiers(2)
            .AddToDB();

        _powerStaffOfTheMagiRetributiveStrike = BuildRetributiveStrike(NAME, 6, () => StaffOfTheMagi);

        var featureSpellAbsorption = FeatureDefinitionBuilder
            .Create($"Feature{NAME}SpellAbsorption")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureSpellAbsorption.AddCustomSubFeatures(new OnSpellCastedSpellAbsorption(featureSpellAbsorption));

        // only spells that exist in the game or this mod are listed (no Arcane Lock, Enlarge/Reduce, Mage Hand,
        // Passwall or Plane Shift)
        var item = BuildStaff(NAME, ItemRarity.Legendary, 200000,
            new UsableDeviceDescriptionBuilder()
                .SetUsage(EquipmentDefinitions.ItemUsage.Charges)
                .SetChargesCapitalNumber(StaffOfTheMagiMaxCharges)
                .SetRecharge(RechargeRate.Dawn, 4, DieType.D6, 2)
                .SetOutOfChargesConsequence(EquipmentDefinitions.ItemOutOfCharges.Persist)
                .SetSaveDc(EffectHelpers.BasedOnUser)
                .SetMagicAttackBonus(EffectHelpers.BasedOnUser)
                .AddFunctions(
                    SpellFunction(SpellDefinitions.ConjureElemental, 7),
                    SpellFunction(SpellDefinitions.DetectMagic, 0),
                    SpellFunction(SpellDefinitions.DispelMagic, 3),
                    SpellFunction(SpellDefinitions.Fireball, 7),
                    SpellFunction(SpellDefinitions.FlamingSphere, 2),
                    SpellFunction(SpellDefinitions.IceStorm, 4),
                    SpellFunction(SpellDefinitions.Invisibility, 2),
                    SpellFunction(SpellDefinitions.Knock, 2),
                    SpellFunction(SpellDefinitions.Light, 0),
                    SpellFunction(SpellDefinitions.LightningBolt, 7),
                    SpellFunction(SpellDefinitions.ProtectionFromEvilGood, 0),
                    SpellFunction(SpellsContext.Telekinesis, 5),
                    SpellFunction(SpellDefinitions.WallOfFire, 4),
                    SpellFunction(SpellsContext.Web, 2),
                    PowerFunction(_powerStaffOfTheMagiRetributiveStrike))
                .Build(),
            new DeviceSpellLevels(
                (SpellDefinitions.Fireball, 7),
                (SpellDefinitions.LightningBolt, 7)),
            Resources.StaffOfTheMagi,
            [Sorcerer, Warlock, Wizard],
            WeaponPlus2,
            BuildFrom(FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinitySpellResistance, false),
            BuildFrom(magicAffinity, false),
            BuildFrom(featureSpellAbsorption, false));

        return item;
    }

    private static ItemDefinition BuildStaff(
        string name,
        ItemRarity rarity,
        int gold,
        UsableDeviceDescription usableDeviceDescription,
        DeviceSpellLevels deviceSpellLevels,
        byte[] icon,
        CharacterClassDefinition[] attunementClasses,
        params ItemPropertyDescription[] properties)
    {
        var baseItem = ItemDefinitions.StaffOfFire;

        return ItemDefinitionBuilder
            .Create(baseItem, name)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(name, icon, 128))
            .SetItemRarity(rarity)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement(attunementClasses)
            .SetGold(gold)
            .SetUsableDeviceDescription(usableDeviceDescription)
            .SetStaticProperties(properties)
            .AddCustomSubFeatures(deviceSpellLevels)
            .AddToDB();
    }

    private static DeviceFunctionDescription SpellFunction(SpellDefinition spell, int charges)
    {
        return new DeviceFunctionDescriptionBuilder()
            .SetUsage(
                EquipmentDefinitions.ItemUsage.Charges,
                charges == 0
                    ? DeviceFunctionDescription.FunctionUseAffinity.AtWill
                    : DeviceFunctionDescription.FunctionUseAffinity.ChargeCost,
                charges)
            .SetSpell(spell)
            .Build();
    }

    private static DeviceFunctionDescription PowerFunction(FeatureDefinitionPower power)
    {
        return new DeviceFunctionDescriptionBuilder()
            .SetUsage(
                EquipmentDefinitions.ItemUsage.Charges,
                DeviceFunctionDescription.FunctionUseAffinity.AtWill,
                0)
            .SetPower(power)
            .Build();
    }

    // Staff of Power casts Fireball and Lightning Bolt at 5th level, Staff of the Magi at 7th level
    internal sealed class DeviceSpellLevels(params (SpellDefinition spell, int level)[] spellLevels)
    {
        internal bool TryGetLevel(SpellDefinition spell, out int level)
        {
            foreach (var (spellDefinition, spellLevel) in spellLevels)
            {
                if (spellDefinition != spell)
                {
                    continue;
                }

                level = spellLevel;

                return true;
            }

            level = 0;

            return false;
        }
    }

    // called from the ItemDefinition.FillTags patch
    // the staffs cast a dozen spells each and their effect tags made the tooltip run off screen
    internal static void RemoveStaffSpellTags(
        ItemDefinition itemDefinition, Dictionary<string, TagsDefinitions.Criticity> tagsMap)
    {
        if (!IsChargedStaff(itemDefinition))
        {
            return;
        }

        var spellTags = new Dictionary<string, TagsDefinitions.Criticity>();
        var weaponTags = new Dictionary<string, TagsDefinitions.Criticity>();

        itemDefinition.UsableDeviceDescription.FillTags(spellTags);
        itemDefinition.WeaponDescription.FillTags(weaponTags);

        foreach (var tag in spellTags.Keys.Where(tag => !weaponTags.ContainsKey(tag)))
        {
            tagsMap.Remove(tag);
        }
    }

    // called from the TooltipFeatureDeviceFunctionsEnumerator.Bind patch
    // lists the staff spells on one wrapped line instead of one line each so the tooltip fits on screen
    internal static void CompactStaffFunctions(TooltipFeatureDeviceFunctionsEnumerator enumerator, ITooltip tooltip)
    {
        if (tooltip.DataProvider is not IItemDefinitionProvider { ItemDefinition: var itemDefinition } ||
            !IsChargedStaff(itemDefinition) ||
            !enumerator.gameObject.activeSelf ||
            !enumerator.functionsTable.gameObject.activeSelf)
        {
            return;
        }

        var functions = itemDefinition.UsableDeviceDescription.DeviceFunctions;
        var spells = functions
            .Where(x => x.Type == DeviceFunctionDescription.FunctionType.Spell)
            .Select(x => Gui.Format("Tooltip/&StaffSpellFormat", x.SpellDefinition.FormatTitle(),
                x.UseAmount.ToString()));
        var lines = new List<string> { Gui.Format("Tooltip/&StaffSpellsFormat", string.Join(", ", spells)) };

        lines.AddRange(functions
            .Where(x => x.Type == DeviceFunctionDescription.FunctionType.Power)
            .Select(x => x.FeatureDefinitionPower.FormatTitle()));

        var table = enumerator.functionsTable;

        for (var i = 0; i < table.childCount; i++)
        {
            var child = table.GetChild(i);

            child.gameObject.SetActive(i < lines.Count);

            if (i < lines.Count)
            {
                child.GetComponent<GuiLabel>().Text = lines[i];
            }
        }
    }

    // called from the RulesetEffectSpell device constructor patch
    internal static void ApplyDeviceSpellLevel(RulesetEffectSpell rulesetEffectSpell)
    {
        var deviceSpellLevels = rulesetEffectSpell.OriginItem?.ItemDefinition
            .GetFirstSubFeatureOfType<DeviceSpellLevels>();

        if (deviceSpellLevels != null &&
            deviceSpellLevels.TryGetLevel(rulesetEffectSpell.SpellDefinition, out var level))
        {
            rulesetEffectSpell.slotLevel = level;
        }
    }

    // called from the RulesetCharacterHero.UseDeviceFunction patch
    internal static void OnDeviceFunctionUsed(
        RulesetCharacterHero hero,
        RulesetItemDevice usableDevice,
        RulesetDeviceFunction function)
    {
        var itemDefinition = usableDevice.ItemDefinition;

        if (!IsChargedStaff(itemDefinition) ||
            function.DeviceFunctionDescription.UseAffinity !=
            DeviceFunctionDescription.FunctionUseAffinity.ChargeCost ||
            usableDevice.RemainingCharges > 0)
        {
            return;
        }

        RollOnLastCharge(hero, usableDevice);
    }

    private static void RollOnLastCharge(RulesetCharacterHero hero, RulesetItemDevice usableDevice)
    {
        var itemDefinition = usableDevice.ItemDefinition;

        // expending the last charge: roll a d20
        var roll = RollDie(DieType.D20, AdvantageType.None, out _, out _);

        hero.ShowDieRoll(DieType.D20, roll, title: itemDefinition.GuiPresentation.Title);

        // on a 1 the Staff of Frost turns to water, the Voyager Staff vanishes forever and the Staff of Charming
        // crumbles to dust
        if (itemDefinition == StaffOfFrost || itemDefinition == VoyagerStaff || itemDefinition == StaffOfCharming)
        {
            if (roll == 1)
            {
                hero.LogCharacterActivatesAbility(itemDefinition.GuiPresentation.Title,
                    $"Feedback/&{itemDefinition.Name}Destroyed");
                hero.CharacterInventory.DestroyItem(usableDevice);
            }

            return;
        }

        if (roll == 20)
        {
            var regained = itemDefinition == StaffOfPower
                ? RollDie(DieType.D8, AdvantageType.None, out _, out _) + 2
                : RollDie(DieType.D12, AdvantageType.None, out _, out _) + 1;

            usableDevice.SpendCharges(-regained);
            hero.LogCharacterActivatesAbility(itemDefinition.GuiPresentation.Title,
                "Feedback/&MagicStaffRegainCharges",
                extra: [(ConsoleStyleDuplet.ParameterType.Positive, regained.ToString())]);

            return;
        }

        if (roll != 1 || itemDefinition != StaffOfPower)
        {
            return;
        }

        // on a 1 the Staff of Power keeps its +2 bonus to attack and damage rolls and loses all other properties
        hero.LogCharacterActivatesAbility(itemDefinition.GuiPresentation.Title, "Feedback/&StaffOfPowerLosesPower");
        hero.CharacterInventory.DestroyItem(usableDevice);
        hero.GrantItem(ItemDefinitions.QuarterstaffPlus2, true);
    }

    private static RulesetItemDevice GetWieldedStaff(RulesetCharacter character, ItemDefinition staff)
    {
        if (character.GetOriginalHero() is not { } hero)
        {
            return null;
        }

        return new[] { EquipmentDefinitions.SlotTypeMainHand, EquipmentDefinitions.SlotTypeOffHand }
            .Select(slot => hero.CharacterInventory.InventorySlotsByName[slot].EquipedItem)
            .OfType<RulesetItemDevice>()
            .FirstOrDefault(item => item.ItemDefinition == staff);
    }

    private static int GetCharges(RulesetItemDevice staff)
    {
        return ChargesOverride.TryGetValue(staff.Guid, out var charges) ? charges : staff.RemainingCharges;
    }

    #endregion

    #region Staff of Thunder and Lightning

    private static ItemDefinition BuildStaffOfThunderAndLightning()
    {
        const string NAME = "StaffOfThunderAndLightning";

        // the on hit properties are tracked by hidden powers that recharge at dawn
        var powerLightning = BuildDailyTracker($"Power{NAME}Lightning");
        var powerThunder = BuildDailyTracker($"Power{NAME}Thunder");
        var powerThunderAndLightning = BuildDailyTracker($"Power{NAME}ThunderAndLightning");

        var powerThunderEffect = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}ThunderEffect")
            .SetGuiPresentation($"Power{NAME}Thunder", Category.Feature, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.IndividualsUnique)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Constitution,
                        false,
                        EffectDifficultyClassComputation.FixedValue,
                        AttributeDefinitions.Constitution,
                        StaffOfThunderAndLightningSaveDC)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(ConditionDefinitions.ConditionStunned,
                                ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetImpactEffectParameters(SpellDefinitions.Thunderwave)
                    .Build())
            .AddCustomSubFeatures(ModifyPowerVisibility.Hidden)
            .AddToDB();

        var featureOnHit = FeatureDefinitionBuilder
            .Create($"Feature{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddToDB();

        featureOnHit.AddCustomSubFeatures(new CustomBehaviorStaffOfThunderAndLightning(
            powerLightning, powerThunder, powerThunderAndLightning, powerThunderEffect));

        var powerLightningStrike = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}LightningStrike")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.LightningBolt)
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Line, 24)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Dexterity,
                        false,
                        EffectDifficultyClassComputation.FixedValue,
                        AttributeDefinitions.Dexterity,
                        StaffOfThunderAndLightningSaveDC)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeLightning, 9, DieType.D6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build())
                    .SetParticleEffectParameters(SpellDefinitions.LightningBolt)
                    .Build())
            .AddToDB();

        var powerThunderclap = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}Thunderclap")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Thunderwave)
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Sphere, 12)
                    .ExcludeCaster()
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Constitution,
                        false,
                        EffectDifficultyClassComputation.FixedValue,
                        AttributeDefinitions.Constitution,
                        StaffOfThunderAndLightningSaveDC)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeThunder, 2, DieType.D6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(ConditionDefinitions.ConditionDeafened,
                                ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetParticleEffectParameters(SpellDefinitions.Thunderwave)
                    .Build())
            .AddToDB();

        var item = BuildStaff(NAME, ItemRarity.VeryRare, 40000,
            new UsableDeviceDescriptionBuilder()
                .SetSaveDc(StaffOfThunderAndLightningSaveDC)
                .AddFunctions(
                    DailyPowerFunction(powerLightningStrike),
                    DailyPowerFunction(powerThunderclap))
                .Build(),
            new DeviceSpellLevels(),
            Resources.StaffOfThunderAndLightning,
            [],
            WeaponPlus2,
            BuildFrom(featureOnHit, false),
            BuildFrom(powerLightning, false),
            BuildFrom(powerThunder, false),
            BuildFrom(powerThunderAndLightning, false));

        return item;
    }

    // a hidden power with one use that comes back at dawn, used to track a once per day item property
    private static FeatureDefinitionPower BuildDailyTracker(string name)
    {
        return FeatureDefinitionPowerBuilder
            .Create(name)
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.Dawn)
            .SetShowCasting(false)
            .AddCustomSubFeatures(ModifyPowerVisibility.Hidden)
            .AddToDB();
    }

    private static DeviceFunctionDescription DailyPowerFunction(FeatureDefinitionPower power)
    {
        return new DeviceFunctionDescriptionBuilder()
            .SetUsage(useAffinity: DeviceFunctionDescription.FunctionUseAffinity.IterationPerRecharge)
            .SetPower(power)
            .Build();
    }

    private static DeviceFunctionDescription DailySpellFunction(SpellDefinition spell)
    {
        return new DeviceFunctionDescriptionBuilder()
            .SetUsage(useAffinity: DeviceFunctionDescription.FunctionUseAffinity.IterationPerRecharge)
            .SetSpell(spell)
            .Build();
    }

    private sealed class CustomBehaviorStaffOfThunderAndLightning(
        FeatureDefinitionPower powerLightning,
        FeatureDefinitionPower powerThunder,
        FeatureDefinitionPower powerThunderAndLightning,
        FeatureDefinitionPower powerThunderEffect)
        : IPhysicalAttackBeforeHitConfirmedOnEnemy, IPhysicalAttackFinishedByMe
    {
        private const string StaffOfThunderAndLightningName = "StaffOfThunderAndLightning";

        // targets that must save against the Thunder property once the attack finishes
        private readonly HashSet<GameLocationCharacter> _thunderTargets = [];

        public IEnumerator OnPhysicalAttackBeforeHitConfirmedOnEnemy(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            if (rangedAttack ||
                attackMode is not { thrown: false } ||
                attackMode.SourceDefinition != StaffOfThunderAndLightning ||
                !ValidatorsWeapon.IsMelee(attackMode))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var usableLightning = PowerProvider.Get(powerLightning, rulesetAttacker);
            var usableThunder = PowerProvider.Get(powerThunder, rulesetAttacker);
            var usableBoth = PowerProvider.Get(powerThunderAndLightning, rulesetAttacker);
            var hasLightning = rulesetAttacker.GetRemainingUsesOfPower(usableLightning) > 0;
            var hasThunder = rulesetAttacker.GetRemainingUsesOfPower(usableThunder) > 0;
            var hasBoth = rulesetAttacker.GetRemainingUsesOfPower(usableBoth) > 0 &&
                          (Gui.Battle == null ||
                           attacker.GetActionTypeStatus(ActionDefinitions.ActionType.Bonus) ==
                           ActionDefinitions.ActionStatus.Available);

            if (!hasLightning && !hasThunder && !hasBoth)
            {
                yield break;
            }

            var selected = -1;

            yield return attacker.MyReactToSelectOption(
                attacker,
                StaffOfThunderAndLightningName,
                $"CustomReaction{StaffOfThunderAndLightningName}Description".Formatted(
                    Category.Reaction, defender.Name),
                [
                    (Gui.Localize($"Reaction/&CustomReaction{StaffOfThunderAndLightningName}Lightning"),
                        hasLightning),
                    (Gui.Localize($"Reaction/&CustomReaction{StaffOfThunderAndLightningName}Thunder"), hasThunder),
                    (Gui.Localize($"Reaction/&CustomReaction{StaffOfThunderAndLightningName}Both"), hasBoth)
                ],
                option => selected = option,
                hasLightning ? 0 : hasThunder ? 1 : 2,
                battleManager);

            switch (selected)
            {
                case 0:
                    rulesetAttacker.UsePower(usableLightning);
                    rulesetAttacker.LogCharacterUsedPower(powerLightning);
                    AddLightningDamage(actualEffectForms);
                    break;
                case 1:
                    rulesetAttacker.UsePower(usableThunder);
                    rulesetAttacker.LogCharacterUsedPower(powerThunder);
                    _thunderTargets.Add(defender);
                    break;
                case 2:
                    // uses the bonus action and only the daily use of Thunder and Lightning
                    rulesetAttacker.UsePower(usableBoth);
                    rulesetAttacker.LogCharacterUsedPower(powerThunderAndLightning);

                    if (Gui.Battle != null)
                    {
                        attacker.SpendActionType(ActionDefinitions.ActionType.Bonus);
                    }

                    AddLightningDamage(actualEffectForms);
                    _thunderTargets.Add(defender);
                    break;
            }
        }

        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (!_thunderTargets.Remove(defender) ||
                defender.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            var usablePower = PowerProvider.Get(powerThunderEffect, attacker.RulesetCharacter);

            attacker.MyExecuteActionSpendPower(usablePower, defender);
        }

        private static void AddLightningDamage(List<EffectForm> actualEffectForms)
        {
            // part of the hit, so a critical hit doubles these dice too
            actualEffectForms.Add(EffectFormBuilder.DamageForm(DamageTypeLightning, 2, DieType.D6));
        }
    }

    #endregion

    #region Dissuader

    private static ItemDefinition BuildDissuader()
    {
        const string NAME = "Dissuader";

        var conditionFrightened = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionFrightened, $"Condition{NAME}Frightened")
            .SetParentCondition(ConditionDefinitions.ConditionFrightened)
            .SetSpecialDuration(DurationType.Round, 0, TurnOccurenceType.StartOfTurn)
            .AddToDB();

        // shows the emanation around the holder, the same way Spirit Guardians does
        var conditionAura = ConditionDefinitionBuilder
            .Create($"Condition{NAME}DissuadingAura")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .CopyParticleReferences(ConditionDefinitions.ConditionSpiritGuardiansSelf)
            .AddToDB();

        var powerDissuadingAura = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}DissuadingAura")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Fear)
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    // RAW you can force creatures you can see to save; here every enemy in the emanation saves
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 6)
                    .ExcludeCaster()
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Wisdom,
                        false,
                        EffectDifficultyClassComputation.FixedValue,
                        AttributeDefinitions.Wisdom,
                        DissuaderSaveDC)
                    .SetRecurrentEffect(RecurrentEffect.OnEnter | RecurrentEffect.OnTurnEnd)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionFrightened, ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetParticleEffectParameters(SpellDefinitions.SpiritGuardians)
                    .SetImpactEffectParameters(SpellDefinitions.Fear)
                    .Build())
            .AddCustomSubFeatures(
                ApplyRecurrentEffectWhenSourceMoves.Mark,
                new PowerOrSpellFinishedByMeShowAura(conditionAura))
            .AddToDB();

        _powerDissuaderRepel = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}Repel")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Thunderwave, hidden: true)
            .SetUsesFixed(ActivationTime.Reaction, RechargeRate.Dawn)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    // RAW each creature of your choice; here every enemy in the emanation
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 6)
                    .ExcludeCaster()
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Strength,
                        false,
                        EffectDifficultyClassComputation.FixedValue,
                        AttributeDefinitions.Strength,
                        DissuaderSaveDC)
                    .SetEffectForms(
                        // RAW up to 30 feet; here always the full 30 feet
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.PushFromOrigin, 6)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetParticleEffectParameters(SpellDefinitions.Thunderwave)
                    .Build())
            .AddCustomSubFeatures(ModifyPowerVisibility.Hidden)
            .AddToDB();

        var item = BuildStaff(NAME, ItemRarity.Rare, 4000,
            new UsableDeviceDescriptionBuilder()
                .SetSaveDc(DissuaderSaveDC)
                .AddFunctions(
                    DailySpellFunction(SpellDefinitions.CalmEmotions),
                    DailyPowerFunction(powerDissuadingAura))
                .Build(),
            new DeviceSpellLevels(),
            Resources.Dissuader,
            [],
            WeaponPlus1,
            BuildFrom(_powerDissuaderRepel, false));

        return item;
    }

    private static FeatureDefinitionPower _powerDissuaderRepel;

    // called from the CharacterActionMoveStepWalk patch after each step of a move
    internal static IEnumerator HandleDissuaderRepel(GameLocationCharacter mover)
    {
        if (Gui.Battle == null ||
            !_powerDissuaderRepel ||
            mover.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false } ||
            !MovementTracker.TryGetMovement(mover.Guid, out var movement))
        {
            yield break;
        }

        var (from, to) = movement;
        var battleManager = ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;

        foreach (var holder in Gui.Battle.AllContenders
                     .Where(x =>
                         x != mover &&
                         x.IsOppositeSide(mover.Side) &&
                         x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                         GetWieldedStaff(x.RulesetCharacter, Dissuader) != null)
                     .ToArray())
        {
            // the mover must enter a space within 5 feet of the holder
            if (CellDistance(holder.LocationPosition, from) <= 1 ||
                CellDistance(holder.LocationPosition, to) > 1 ||
                !holder.CanReact())
            {
                continue;
            }

            var rulesetHolder = holder.RulesetCharacter;
            var usablePower = PowerProvider.Get(_powerDissuaderRepel, rulesetHolder);

            if (rulesetHolder.GetRemainingUsesOfPower(usablePower) == 0)
            {
                continue;
            }

            var targets = Gui.Battle.GetContenders(holder, withinRange: 6);

            yield return holder.MyReactToUsePower(
                ActionDefinitions.Id.PowerReaction,
                usablePower,
                targets,
                mover,
                "DissuaderRepel",
                "UsePowerDissuaderRepelDescription".Formatted(Category.Reaction, mover.Name),
                battleManager: battleManager);
        }
    }

    private static int CellDistance(TA.int3 a, TA.int3 b)
    {
        return System.Math.Max(System.Math.Abs(a.x - b.x),
            System.Math.Max(System.Math.Abs(a.y - b.y), System.Math.Abs(a.z - b.z)));
    }

    // the power has no effect on activation, so add a condition on the holder to show the emanation
    private sealed class PowerOrSpellFinishedByMeShowAura(ConditionDefinition conditionAura)
        : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action.Countered || action.ExecutionFailed)
            {
                yield break;
            }

            var rulesetCharacter = action.ActingCharacter.RulesetCharacter;
            var rulesetEffect = action.ActionParams.RulesetEffect;
            var activeCondition = rulesetCharacter.InflictCondition(
                conditionAura.Name,
                DurationType.Round,
                rulesetEffect.RemainingRounds,
                TurnOccurenceType.EndOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetCharacter.guid,
                rulesetCharacter.CurrentFaction.Name,
                1,
                conditionAura.Name,
                0,
                0,
                0);

            // ends the visual together with the aura
            rulesetEffect.TrackCondition(
                rulesetCharacter, rulesetCharacter.guid, rulesetCharacter, rulesetCharacter.guid,
                activeCondition, AttributeDefinitions.TagEffect);
        }
    }

    #endregion

    #region Voyager Staff

    private static ItemDefinition BuildVoyagerStaff()
    {
        const string NAME = "VoyagerStaff";

        var magicAffinity = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{NAME}")
            .SetGuiPresentation(Category.Feature)
            .SetCastingModifiers(1)
            .AddToDB();

        // Blink, Passwall and Teleport aren't in the game or this mod, so the staff casts Banishment and Misty Step
        var item = BuildStaff(NAME, ItemRarity.VeryRare, 40000,
            new UsableDeviceDescriptionBuilder()
                .SetUsage(EquipmentDefinitions.ItemUsage.Charges)
                .SetChargesCapitalNumber(VoyagerStaffMaxCharges)
                .SetRecharge(RechargeRate.Dawn, 1, DieType.D6, 4)
                .SetOutOfChargesConsequence(EquipmentDefinitions.ItemOutOfCharges.Persist)
                .SetSaveDc(EffectHelpers.BasedOnUser)
                .SetMagicAttackBonus(EffectHelpers.BasedOnUser)
                .AddFunctions(
                    SpellFunction(SpellDefinitions.Banishment, 4),
                    SpellFunction(SpellDefinitions.MistyStep, 2))
                .Build(),
            new DeviceSpellLevels(),
            Resources.VoyagerStaff,
            SpellcasterClasses,
            WeaponPlus1,
            BuildFrom(magicAffinity, false));

        return item;
    }

    #endregion

    #region Staff of Charming

    private static ItemDefinition BuildStaffOfCharming()
    {
        const string NAME = "StaffOfCharming";

        var powerResistEnchantment = BuildDailyTracker($"Power{NAME}ResistEnchantment");

        var featureEnchantmentWard = FeatureDefinitionBuilder
            .Create($"Feature{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddToDB();

        featureEnchantmentWard.AddCustomSubFeatures(new TryAlterOutcomeSavingThrowStaffOfCharming(
            powerResistEnchantment));

        var item = BuildStaff(NAME, ItemRarity.Rare, 4000,
            new UsableDeviceDescriptionBuilder()
                .SetUsage(EquipmentDefinitions.ItemUsage.Charges)
                .SetChargesCapitalNumber(StaffOfCharmingMaxCharges)
                .SetRecharge(RechargeRate.Dawn, 1, DieType.D8, 2)
                .SetOutOfChargesConsequence(EquipmentDefinitions.ItemOutOfCharges.Persist)
                .SetSaveDc(EffectHelpers.BasedOnUser)
                .SetMagicAttackBonus(EffectHelpers.BasedOnUser)
                .AddFunctions(
                    SpellFunction(SpellDefinitions.CharmPerson, 1),
                    SpellFunction(SpellsContext.Command, 1),
                    SpellFunction(SpellDefinitions.ComprehendLanguages, 1))
                .Build(),
            new DeviceSpellLevels(),
            Resources.StaffOfCharming,
            [Bard, Cleric, Druid, Sorcerer, Warlock, Wizard],
            BuildFrom(featureEnchantmentWard, false),
            BuildFrom(powerResistEnchantment, false));

        return item;
    }

    private sealed class TryAlterOutcomeSavingThrowStaffOfCharming(FeatureDefinitionPower powerResistEnchantment)
        : ITryAlterOutcomeSavingThrow
    {
        private const string ResistEnchantment = "ResistEnchantment";
        private const string ReflectEnchantment = "ReflectEnchantment";

        public IEnumerator OnTryAlterOutcomeSavingThrow(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            SavingThrowData savingThrowData,
            bool hasHitVisual)
        {
            if (helper != defender ||
                attacker == null ||
                attacker == defender ||
                savingThrowData.SourceDefinition is not SpellDefinition spell ||
                spell.SchoolOfMagic != SchoolOfMagicDefinitions.SchoolEnchantment.Name)
            {
                yield break;
            }

            // the spell must target only the staff holder
            var targets = savingThrowData.Action?.ActionParams.TargetCharacters;

            if (targets == null ||
                targets.Count == 0 ||
                targets.Any(x => x != defender))
            {
                yield break;
            }

            var rulesetDefender = defender.RulesetCharacter;
            var staff = GetWieldedStaff(rulesetDefender, StaffOfCharming);

            if (staff == null)
            {
                yield break;
            }

            // Resist Enchantment: turn a failed save into a success, once per dawn
            if (savingThrowData.SaveOutcome == RollOutcome.Failure)
            {
                var usablePower = PowerProvider.Get(powerResistEnchantment, rulesetDefender);

                if (rulesetDefender.GetRemainingUsesOfPower(usablePower) == 0)
                {
                    yield break;
                }

                yield return defender.MyReactToDoNothing(
                    ExtraActionId.DoNothingFree,
                    defender,
                    ResistEnchantment,
                    $"CustomReaction{ResistEnchantment}Description".Formatted(
                        Category.Reaction, attacker.Name, spell.FormatTitle()),
                    ReactionValidated,
                    battleManager: battleManager);

                yield break;

                void ReactionValidated()
                {
                    rulesetDefender.UsePower(usablePower);
                    rulesetDefender.LogCharacterUsedPower(powerResistEnchantment);
                    savingThrowData.SaveOutcome = RollOutcome.Success;
                    savingThrowData.SaveOutcomeDelta = 0;
                }
            }

            // Reflect Enchantment: after a successful save, use a reaction and 1 charge to turn the spell back
            if (savingThrowData.SaveOutcome != RollOutcome.Success ||
                staff.RemainingCharges <= 0 ||
                !defender.CanReact() ||
                attacker.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            // casts the spell as the holder, with the holder's spellcasting
            var repertoire = rulesetDefender.SpellRepertoires
                                 .FirstOrDefault(x =>
                                     x.SpellCastingFeature.SpellCastingOrigin is
                                         FeatureDefinitionCastSpell.CastingOrigin.Class or
                                         FeatureDefinitionCastSpell.CastingOrigin.Subclass) ??
                             rulesetDefender.SpellRepertoires.FirstOrDefault();

            if (repertoire == null)
            {
                yield break;
            }

            var slotLevel = savingThrowData.Action?.ActionParams.RulesetEffect is RulesetEffectSpell effectSpell
                ? effectSpell.EffectLevel
                : spell.SpellLevel;

            yield return defender.MyReactToDoNothing(
                ExtraActionId.DoNothingReaction,
                defender,
                ReflectEnchantment,
                $"CustomReaction{ReflectEnchantment}Description".Formatted(
                    Category.Reaction, attacker.Name, spell.FormatTitle()),
                ReflectValidated,
                battleManager: battleManager);

            yield break;

            void ReflectValidated()
            {
                defender.SpendActionType(ActionDefinitions.ActionType.Reaction);
                staff.SpendCharges(1);
                rulesetDefender.LogCharacterActivatesAbility(
                    StaffOfCharming.GuiPresentation.Title, "Feedback/&StaffOfCharmingReflectEnchantment",
                    extra: [(ConsoleStyleDuplet.ParameterType.AbilityInfo, spell.FormatTitle())]);

                var actionParams = new CharacterActionParams(defender, ActionDefinitions.Id.CastNoCost)
                {
                    ActionModifiers = { new ActionModifier() }, TargetCharacters = { attacker }
                };

                defender.MyExecuteActionCastNoCost(spell, slotLevel, actionParams, repertoire);

                if (staff.RemainingCharges <= 0 && rulesetDefender.GetOriginalHero() is { } hero)
                {
                    RollOnLastCharge(hero, staff);
                }
            }
        }
    }

    #endregion

    #region Retributive Strike

    private static FeatureDefinitionPower BuildRetributiveStrike(
        string name, int damageMultiplier, System.Func<ItemDefinition> staffGetter)
    {
        var power = FeatureDefinitionPowerBuilder
            .Create($"Power{name}RetributiveStrike")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Shatter)
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Sphere, RetributiveStrikeRange)
                    .ExcludeCaster()
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Dexterity,
                        false,
                        EffectDifficultyClassComputation.FixedValue,
                        AttributeDefinitions.Dexterity,
                        RetributiveStrikeDC)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypeForce)
                            .Build())
                    .SetParticleEffectParameters(SpellDefinitions.Shatter)
                    .Build())
            .AddToDB();

        power.AddCustomSubFeatures(new CustomBehaviorRetributiveStrike(power, damageMultiplier, staffGetter));

        return power;
    }

    private sealed class CustomBehaviorRetributiveStrike(
        FeatureDefinitionPower powerRetributiveStrike,
        int damageMultiplier,
        System.Func<ItemDefinition> staffGetter)
        : IModifyEffectDescription, IPowerOrSpellInitiatedByMe, IPowerOrSpellFinishedByMe
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition == powerRetributiveStrike;
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            var staff = rulesetEffect?.OriginItem ?? GetWieldedStaff(character, staffGetter());
            var charges = staff == null ? 0 : GetCharges(staff);

            effectDescription.FindFirstDamageForm().BonusDamage = damageMultiplier * charges;

            return effectDescription;
        }

        public IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            // the damage depends on the charges in the staff right now, so don't use a cached effect
            PowerBundle.ClearSpellEffectCache(action.ActingCharacter.RulesetCharacter);

            yield break;
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var actingCharacter = action.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;
            var staff = action.ActionParams.RulesetEffect?.OriginItem ??
                        GetWieldedStaff(rulesetCharacter, staffGetter());

            if (staff == null)
            {
                yield break;
            }

            var charges = GetCharges(staff);

            ChargesOverride.Remove(staff.Guid);
            PowerBundle.ClearSpellEffectCache(rulesetCharacter);

            // 50 percent chance to travel to another plane and avoid the explosion
            var roll = RollDie(DieType.D10, AdvantageType.None, out _, out _);

            if (roll <= 5)
            {
                rulesetCharacter.LogCharacterUsedPower(powerRetributiveStrike, "Feedback/&RetributiveStrikeAvoided");
            }
            else
            {
                var damage = RetributiveStrikeSelfMultiplier * charges;
                var damageForm = new DamageForm
                {
                    DamageType = DamageTypeForce, DieType = DieType.D1, DiceNumber = 0, BonusDamage = damage
                };
                var applyFormsParams = new RulesetImplementationDefinitions.ApplyFormsParams
                {
                    sourceCharacter = rulesetCharacter,
                    targetCharacter = rulesetCharacter,
                    position = actingCharacter.LocationPosition
                };

                rulesetCharacter.LogCharacterUsedPower(powerRetributiveStrike, "Feedback/&RetributiveStrikeCaught");
                RulesetActor.InflictDamage(
                    damage,
                    damageForm,
                    damageForm.DamageType,
                    applyFormsParams,
                    rulesetCharacter,
                    false,
                    rulesetCharacter.Guid,
                    false,
                    [],
                    new RollInfo(DieType.D1, [], damage),
                    false,
                    out _);
            }

            // the staff is destroyed
            if (rulesetCharacter.GetOriginalHero() is { } hero)
            {
                hero.CharacterInventory.DestroyItem(staff);
            }
        }
    }

    #endregion

    #region Instruments of the Bards

    // the game has no harp, lyre, mandolin, bandore or cittern, so the lute-family instruments use the Lute and the
    // harps and the lyre use the Dulcimer, the only other string instrument
    private static void BuildInstrumentsOfTheBards()
    {
        var sl = SpellDefinitions.ProtectionFromEnergyLightning;
        var sf = SpellDefinitions.ProtectionFromEnergyFire;

        // Stone Shape is left out of the Cli Lyre, Speak with Animals of the Fochlucan Bandore and Control Weather of
        // the Ollamh Harp as none of these spells is in the game or this mod
        AnstruthHarp = BuildInstrument("AnstruthHarp", ItemDefinitions.Dulcimer, ItemRarity.VeryRare, 40000,
            new DeviceSpellLevels((SpellDefinitions.CureWounds, 5)),
            SpellDefinitions.CureWounds, SpellDefinitions.IceStorm, SpellDefinitions.WallOfThorns);
        CanaithMandolin = BuildInstrument("CanaithMandolin", ItemDefinitions.Lute, ItemRarity.Rare, 4000,
            new DeviceSpellLevels((SpellDefinitions.CureWounds, 3)),
            SpellDefinitions.CureWounds, SpellDefinitions.DispelMagic, sl);
        CliLyre = BuildInstrument("CliLyre", ItemDefinitions.Dulcimer, ItemRarity.Rare, 4000, null,
            SpellDefinitions.WallOfFire, SpellDefinitions.WindWall);
        DossLute = BuildInstrument("DossLute", ItemDefinitions.Lute, ItemRarity.Uncommon, 400, null,
            SpellDefinitions.AnimalFriendship, sf, SpellDefinitions.ProtectionFromPoison);
        FochlucanBandore = BuildInstrument("FochlucanBandore", ItemDefinitions.Lute, ItemRarity.Uncommon, 400, null,
            SpellDefinitions.Entangle, SpellDefinitions.FaerieFire, SpellDefinitions.Shillelagh);
        MacFuirmidhCittern = BuildInstrument("MacFuirmidhCittern", ItemDefinitions.Lute, ItemRarity.Uncommon, 400,
            null,
            SpellDefinitions.Barkskin, SpellDefinitions.CureWounds, SpellDefinitions.FogCloud);
        OllamhHarp = BuildInstrument("OllamhHarp", ItemDefinitions.Dulcimer, ItemRarity.Legendary, 200000, null,
            SpellDefinitions.Confusion, SpellDefinitions.FireStorm);
    }

    private static ItemDefinition BuildInstrument(
        string name,
        ItemDefinition baseItem,
        ItemRarity rarity,
        int gold,
        DeviceSpellLevels deviceSpellLevels,
        params SpellDefinition[] spells)
    {
        // each spell can be cast once from the instrument, and comes back at the next dawn
        var functions = new[]
            {
                SpellDefinitions.Fly, SpellDefinitions.Invisibility, SpellDefinitions.Levitate,
                SpellDefinitions.ProtectionFromEvilGood
            }
            .Concat(spells)
            .Select(spell => new DeviceFunctionDescriptionBuilder()
                .SetUsage(useAffinity: DeviceFunctionDescription.FunctionUseAffinity.IterationPerRecharge)
                .SetSpell(spell)
                .Build())
            .ToArray();

        var builder = ItemDefinitionBuilder
            .Create(baseItem, name)
            .SetGuiPresentation(Category.Item, baseItem)
            .SetMerchantCategory(MerchantCategoryDefinitions.MagicDevice)
            .SetItemRarity(rarity)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement(Bard)
            .SetGold(gold)
            .SetUsableDeviceDescription(
                new UsableDeviceDescriptionBuilder()
                    .SetSaveDc(EffectHelpers.BasedOnUser)
                    .SetMagicAttackBonus(EffectHelpers.BasedOnUser)
                    .AddFunctions(functions)
                    .Build());

        if (deviceSpellLevels != null)
        {
            builder.AddCustomSubFeatures(deviceSpellLevels);
        }

        return builder.AddToDB();
    }

    #endregion

    #region Attunement by a Spellcaster

    // items whose attunement list holds every vanilla spellcasting class require attunement by a spellcaster, so
    // anyone with spell slots can attune to them, including Eldritch Knights and Arcane Tricksters
    private static readonly CharacterClassDefinition[] SpellcasterClasses =
        [Bard, Cleric, Druid, Paladin, Ranger, Sorcerer, Warlock, Wizard];

    internal static bool RequiresAttunementBySpellcaster(ItemDefinition itemDefinition)
    {
        return itemDefinition.RequiresAttunement &&
               SpellcasterClasses.All(itemDefinition.RequiredAttunementClasses.Contains);
    }

    private static bool HasSpellSlots(RulesetCharacterHero hero)
    {
        return hero.SpellRepertoires.Any(x =>
            x.SpellCastingFeature.SpellCastingOrigin is
                FeatureDefinitionCastSpell.CastingOrigin.Class or FeatureDefinitionCastSpell.CastingOrigin.Subclass &&
            SharedSpellsContext.MaxSpellLevelOfSpellCastingLevel(x) > 0);
    }

    // called from the RulesetCharacterHero.EnumerateAttunableItems patch
    internal static void AddItemsAttunableBySpellcaster(
        RulesetCharacterHero hero,
        List<RulesetCharacterHero> heroes,
        List<RulesetItem> attunableItems,
        bool considerMyAttunedItems)
    {
        if (!HasSpellSlots(hero))
        {
            return;
        }

        var items = new List<RulesetItem>();

        foreach (var owner in heroes)
        {
            items.Clear();
            owner.CharacterInventory.EnumerateAllItems(items);

            foreach (var item in items)
            {
                var itemDefinition = item.ItemDefinition;

                if (attunableItems.Contains(item) ||
                    !itemDefinition.Magical ||
                    !RequiresAttunementBySpellcaster(itemDefinition) ||
                    (itemDefinition.RequiresIdentification && !item.Identified) ||
                    (item.AttunedToCharacter == hero.Name && !considerMyAttunedItems))
                {
                    continue;
                }

                attunableItems.Add(item);
            }
        }
    }

    // called from the GuiItemDefinition.AttunementInfo patch
    internal static void FormatAttunementBySpellcaster(ItemDefinition itemDefinition, ref string attunementInfo)
    {
        if (!RequiresAttunementBySpellcaster(itemDefinition))
        {
            return;
        }

        attunementInfo = Gui.Colorize(
            Gui.Localize("Equipment/&DeviceRequiresAttunementTitle") +
            Gui.Format(" ({0})", Gui.Localize("Tooltip/&AttunementBySpellcaster")),
            "EA7171");
    }

    #endregion

    #region Spell Duelist's Trophy

    private static ItemDefinition BuildSpellDuelistsTrophy()
    {
        const string NAME = "SpellDuelistsTrophy";

        var powerDispellingTouch = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}DispellingTouch")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.DispelMagic)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.Dawn)
            .SetShowCasting(false)
            .AddToDB();

        powerDispellingTouch.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new MagicEffectFinishedByMeDispellingTouch(powerDispellingTouch));

        _featureSpellDuelistsTrophyHelpingHand = FeatureDefinitionBuilder
            .Create($"Feature{NAME}HelpingHand")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        var attunementClasses = new[]
            {
                Bard, Cleric, Druid, Paladin, Ranger, Sorcerer, Warlock, Wizard, InventorClass.Class
            }
            .Where(x => x)
            .ToArray();

        var item = ItemDefinitionBuilder
            .Create(ItemDefinitions.AmuletOfHealth, NAME)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(NAME, Resources.SpellDuelistsTrophy, 128))
            .SetMerchantCategory(MerchantCategoryDefinitions.MagicDevice)
            .SetItemRarity(ItemRarity.Rare)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement(attunementClasses)
            .SetGold(4000)
            .SetStaticProperties(
                BuildFrom(powerDispellingTouch, false),
                BuildFrom(_featureSpellDuelistsTrophyHelpingHand, false))
            .AddToDB();

        return item;
    }

    // called on RulesetCharacter.IsComponentSomaticValid
    internal static void ValidateHelpingHand(
        RulesetCharacter caster, SpellDefinition spell, ref bool result, ref string failure)
    {
        // cantrips don't use a spell slot
        if (result ||
            !_featureSpellDuelistsTrophyHelpingHand ||
            spell.SpellLevel == 0 ||
            !caster.HasAnyFeature(_featureSpellDuelistsTrophyHelpingHand))
        {
            return;
        }

        result = true;
        failure = string.Empty;
    }

    private sealed class MagicEffectFinishedByMeDispellingTouch(FeatureDefinitionPower powerDispellingTouch)
        : IMagicEffectFinishedByMe
    {
        private const string DispellingTouch = "DispellingTouch";
        private const int DispelMagicSlotLevel = 3;

        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            // only a hit with a melee spell attack
            if (action is not CharacterActionCastSpell { Countered: false, ExecutionFailed: false } actionCastSpell ||
                actionCastSpell.ActiveSpell.EffectDescription.RangeType != RangeType.MeleeHit ||
                actionCastSpell.AttackRollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerDispellingTouch, rulesetAttacker);

            if (rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false } ||
                rulesetAttacker.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            var target = targets.FirstOrDefault(x => x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false });

            if (target == null)
            {
                yield break;
            }

            yield return attacker.MyReactToDoNothing(
                ExtraActionId.DoNothingFree,
                attacker,
                DispellingTouch,
                $"CustomReaction{DispellingTouch}Description".Formatted(Category.Reaction, target.Name),
                ReactionValidated);

            yield break;

            void ReactionValidated()
            {
                rulesetAttacker.UsePower(usablePower);
                rulesetAttacker.LogCharacterUsedPower(powerDispellingTouch);

                var actionParams = new CharacterActionParams(attacker, ActionDefinitions.Id.CastNoCost)
                {
                    ActionModifiers = { new ActionModifier() }, TargetCharacters = { target }
                };

                attacker.MyExecuteActionCastNoCost(
                    SpellDefinitions.DispelMagic,
                    DispelMagicSlotLevel,
                    actionParams,
                    actionCastSpell.ActiveSpell.SpellRepertoire);
            }
        }
    }

    #endregion

    #region Spell Absorption

    private sealed class OnSpellCastedSpellAbsorption(FeatureDefinition featureSpellAbsorption) : IOnSpellCasted
    {
        public int Priority => 0;

        public IEnumerator OnSpellCasted(
            RulesetCharacter featureOwner,
            GameLocationCharacter caster,
            CharacterActionCastSpell castAction,
            RulesetEffectSpell selectEffectSpell,
            RulesetSpellRepertoire selectedRepertoire,
            SpellDefinition selectedSpellDefinition)
        {
            if (Gui.Battle == null ||
                castAction.Countered ||
                caster.RulesetCharacter == featureOwner)
            {
                yield break;
            }

            var owner = GameLocationCharacter.GetFromActor(featureOwner);

            if (owner == null || !owner.CanReact())
            {
                yield break;
            }

            // the spell must target only the staff holder
            var targets = castAction.ActionParams.TargetCharacters;

            if (selectEffectSpell.EffectDescription.TargetType is not
                    (TargetType.Individuals or TargetType.IndividualsUnique) ||
                targets.Count == 0 ||
                targets.Any(x => x != owner))
            {
                yield break;
            }

            var staff = GetWieldedStaff(featureOwner, StaffOfTheMagi);

            if (staff == null)
            {
                yield break;
            }

            yield return owner.MyReactToDoNothing(
                ExtraActionId.DoNothingReaction,
                caster,
                "SpellAbsorption",
                "CustomReactionSpellAbsorptionDescription".Formatted(
                    Category.Reaction, caster.Name, selectedSpellDefinition.FormatTitle()),
                ReactionValidated);

            yield break;

            void ReactionValidated()
            {
                castAction.Countered = true;

                var level = selectEffectSpell.EffectLevel;
                var charges = staff.RemainingCharges + level;

                featureOwner.LogCharacterUsedFeature(featureSpellAbsorption,
                    "Feedback/&StaffOfTheMagiSpellAbsorptionGain",
                    extra: [(ConsoleStyleDuplet.ParameterType.Positive, level.ToString())]);

                if (charges <= StaffOfTheMagiMaxCharges)
                {
                    staff.SpendCharges(-level);

                    return;
                }

                // going above the maximum makes the staff explode as if its Retributive Strike was activated
                ChargesOverride[staff.Guid] = charges;

                var usablePower = PowerProvider.Get(_powerStaffOfTheMagiRetributiveStrike, featureOwner);
                var explosionTargets = Gui.Battle.AllContenders
                    .Where(x =>
                        x != owner &&
                        x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                        owner.IsWithinRange(x, RetributiveStrikeRange))
                    .ToArray();

                owner.MyExecuteActionSpendPower(usablePower, explosionTargets);
            }
        }
    }

    #endregion
}
