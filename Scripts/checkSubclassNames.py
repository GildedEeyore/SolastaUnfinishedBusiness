#
# Checks that every subclass shows a clean name in game.
#
# Run it on the built DLL before packaging a build:
#   python3 Scripts/checkSubclassNames.py SolastaFiles/Mods/SolastaUnfinishedBusiness/SolastaUnfinishedBusiness.dll
#
# It reads the translations embedded in the DLL (not the source files), so it
# catches a build that shipped stale text and shows raw keys like
# "Subclass/&PatronUndeadTitle" on the selection screen and settings toggle.
#

import io
import os
import re
import struct
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "SolastaUnfinishedBusiness", "Translations", "en")
KEY = re.compile(r"^(Subclass/&(\w+)(Title|Description))=(.*)$")


def read_terms(lines):
    terms = {}
    for line in lines:
        match = KEY.match(line.rstrip("\r\n"))
        if match:
            terms[match.group(1)] = match.group(4)
    return terms


def source_terms():
    terms = {}
    for root, _, files in os.walk(SOURCE):
        for file in files:
            if file.endswith("-en.txt"):
                with open(os.path.join(root, file), encoding="utf-8-sig") as f:
                    terms.update(read_terms(f))
    return terms


def dll_terms(dll):
    data = open(dll, "rb").read()
    start = data.find(b"PK\x03\x04")
    while start != -1:
        # embedded byte[] resources are prefixed with their length
        length = struct.unpack("<i", data[start - 4:start])[0]
        try:
            if not 0 < length <= len(data) - start:
                raise zipfile.BadZipFile
            archive = zipfile.ZipFile(io.BytesIO(data[start:start + length]))
            names = [n for n in archive.namelist() if n.startswith("en/") and n.endswith("-en.txt")]
            if names:
                terms = {}
                for name in names:
                    terms.update(read_terms(archive.read(name).decode("utf-8-sig").splitlines()))
                return terms
        except zipfile.BadZipFile:
            pass
        start = data.find(b"PK\x03\x04", start + 1)
    sys.exit(f"no embedded translations found in {dll}")


def name_problems(key, text):
    problems = []
    if text != text.strip():
        problems.append("leading or trailing spaces")
    if "&" in text or "/" in text:
        problems.append("looks like a raw key")
    if key.endswith("Title"):
        name = key[len("Subclass/&"):-len("Title")]
        if not text:
            problems.append("empty title")
        elif re.fullmatch(r"[A-Z][a-z]+(?:[A-Z][a-z]+)+", text) or text == name:
            problems.append("looks like the internal code name")
        if name.startswith("Patron") and not text.startswith("The "):
            problems.append('warlock patrons are named "The <Patron>" like "The Celestial"')
    return problems


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__ or "usage: checkSubclassNames.py <SolastaUnfinishedBusiness.dll>")

    source = source_terms()
    built = dll_terms(sys.argv[1])
    errors = []

    for key, text in sorted(source.items()):
        if key not in built:
            errors.append(f"{key}: missing from the DLL, the game will show the raw key")
        elif built[key] != text:
            errors.append(f"{key}: DLL has stale text {built[key]!r}, source has {text!r}")
        for problem in name_problems(key, text):
            errors.append(f"{key}={text!r}: {problem}")

    for key in sorted(source):
        if key.endswith("Title") and key[:-len("Title")] + "Description" not in source:
            errors.append(f"{key}: has no matching Description")

    if errors:
        print("\n".join(errors))
        sys.exit(1)

    print(f"OK: {len(source)} subclass titles and descriptions are in the DLL and look clean")


if __name__ == "__main__":
    main()
