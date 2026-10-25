# Notes for Claude

## New subclasses: check the name before shipping a build

Subclass names have come out wrong in game more than once: the Arcana Domain and the Undead patron both showed raw keys like `Subclass/&PatronUndeadTitle` instead of their names. Both times the build had embedded an old copy of the translations. Before you package any build that adds or renames a subclass:

1. Give the subclass a clean title in `SolastaUnfinishedBusiness/Translations/en/SubClasses/<Name>-en.txt`:
   - `Subclass/&<Name>Title=` and `Subclass/&<Name>Description=`, where `<Name>` is exactly the name passed to `CharacterSubclassDefinitionBuilder.Create(...)`.
   - Use the name the user asked for, matching the class's existing style. Warlock patrons are "The <Patron>" (The Celestial, The Undead). Cleric domains use the bare domain name (Grave, Arcana). For other classes, copy the style of that class's existing titles.
   - Never use the internal code name (`PatronUndead`) as the title.
2. Build with the `Release Workflow` configuration. That configuration rezips `Translations/` into `Resources/Translations.zip`, and the csproj then deletes the cached `.resources` file so the new zip is always embedded.
3. Run the check on the built DLL. It reads the text embedded in the DLL, not the source files:

   ```
   python3 Scripts/checkSubclassNames.py SolastaFiles/Mods/SolastaUnfinishedBusiness/SolastaUnfinishedBusiness.dll
   ```

   Don't package the build unless it prints `OK`.
4. When you report the build, tell the user the exact in-game title. It is shown on both the subclass selection screen and the mod's settings toggle.

## New items: show the copyright marker in the mod manager

Every new feat, subclass, spell, invocation, race, subrace or similar item added in this fork must show the © symbol after its name in the mod manager, unless the user says otherwise for that item. The mod manager adds the symbol to every definition whose name is in `ModUi.TabletopDefinitionNames` (`SolastaUnfinishedBusiness/Displays/_ModUi.cs`), so add the item's definition name there, in alphabetical order. Never put © in the translation title or description, because those show in game.

Use the name of the definition the mod manager lists, which is not always the item itself:
- Feats with ability or damage choices are listed by their group: `FeatGroup<Name>`, not `Feat<Name>Int`.
- Subclasses use the name passed to `CharacterSubclassDefinitionBuilder.Create(...)`. For Theurgy that is the settings toggle `WizardTheurgy`.
- Changes to vanilla races or spells (Dragonborn, High Elf, Spare the Dying) have no mod manager entry, so they need nothing.
