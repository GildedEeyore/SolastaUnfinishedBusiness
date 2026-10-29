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
    private const int StaffOfPowerMaxCharges = 20;
    private const int StaffOfTheMagiMaxCharges = 50;
    private const int RetributiveStrikeRange = 6; // 30 ft
    private const int RetributiveStrikeDC = 17;
    private const int RetributiveStrikeSelfMultiplier = 16;

    internal static ItemDefinition OrbOfSorcery;
    internal static ItemDefinition RobeOfTheArchmagi;
    internal static ItemDefinition SpellguardShield;
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

    private static FeatureDefinition _featureSpellDuelistsTrophyHelpingHand;

    private static FeatureDefinitionPower _powerStaffOfTheMagiRetributiveStrike;

    // charges to use on a Retributive Strike triggered by Spell Absorption going over the staff's maximum
    private static readonly Dictionary<ulong, int> ChargesOverride = [];

    internal static void Load()
    {
        OrbOfSorcery = BuildOrbOfSorcery();
        RobeOfTheArchmagi = BuildRobeOfTheArchmagi();
        SpellguardShield = BuildSpellguardShield();
        StaffOfPower = BuildStaffOfPower();
        StaffOfTheMagi = BuildStaffOfTheMagi();
        SunBlade = BuildSunBlade();
        BuildInstrumentsOfTheBards();
        SpellDuelistsTrophy = BuildSpellDuelistsTrophy();

        foreach (var item in new[]
                 {
                     OrbOfSorcery, RobeOfTheArchmagi, SpellguardShield, StaffOfPower, StaffOfTheMagi, SunBlade,
                     AnstruthHarp, CanaithMandolin, CliLyre, DossLute, FochlucanBandore, MacFuirmidhCittern,
                     OllamhHarp, SpellDuelistsTrophy
                 })
        {
            item.inDungeonEditor = Main.Settings.AddNewWeaponsAndRecipesToEditor;
        }

        MerchantContext.AddItem(OrbOfSorcery, ShopItemType.MagicItemUncommon);
        MerchantContext.AddItem(SunBlade, ShopItemType.MagicItemRare);
        MerchantContext.AddItem(SpellguardShield, ShopItemType.MagicItemVeryRare);
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
            .SetGuiPresentation(NAME, Category.Item)
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

    private static ItemDefinition BuildStaffOfPower()
    {
        const string NAME = "StaffOfPower";

        var savingThrowAffinity = FeatureDefinitionSavingThrowAffinityBuilder
            .Create($"SavingThrowAffinity{NAME}")
            .SetGuiPresentation(NAME, Category.Item)
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
            .SetGuiPresentation(NAME, Category.Item)
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
            .SetGuiPresentation(NAME, Category.Item)
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
        params ItemPropertyDescription[] properties)
    {
        var baseItem = ItemDefinitions.StaffOfFire;

        return ItemDefinitionBuilder
            .Create(baseItem, name)
            .SetGuiPresentation(Category.Item, Sprites.GetSprite(name, icon, 128))
            .SetItemRarity(rarity)
            .MakeMagical()
            .SetRequiresIdentification(false)
            .RequireAttunement(Sorcerer, Warlock, Wizard)
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

        if ((itemDefinition != StaffOfPower && itemDefinition != StaffOfTheMagi) ||
            function.DeviceFunctionDescription.UseAffinity !=
            DeviceFunctionDescription.FunctionUseAffinity.ChargeCost ||
            usableDevice.RemainingCharges > 0)
        {
            return;
        }

        // expending the last charge: roll a d20
        var roll = RollDie(DieType.D20, AdvantageType.None, out _, out _);

        hero.ShowDieRoll(DieType.D20, roll, title: itemDefinition.GuiPresentation.Title);

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
            .SetGuiPresentation(Category.Item, ItemDefinitions.AmuletOfHealth)
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
