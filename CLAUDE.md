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
