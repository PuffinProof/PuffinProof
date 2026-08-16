# Security

PuffinProof is local-only spellcheck. It does not send your typing to a server.

## What it does on your PC

- Refuses to run elevated.
- Skips password-like fields before reading text.
- Skips password managers by default.
- Replaces only a matching word in the live field, not the whole string.
- Caps word length and only writes letter/hyphen/apostrophe replacements.
- Loads Hunspell files only from the bundled `Dictionaries` folder, with a constrained language id.
- The stub installer only downloads `https` GitHub release assets.

## What it cannot do

- It cannot see text in Administrator windows unless you also elevate it. Do not elevate it.
- Some apps expose no UI Automation text. Those fields are skipped.

## Report a vulnerability

Open a [private security advisory](https://github.com/PuffinProof/PuffinProof/security/advisories/new) on this repo, or email the address on the org profile. Please do not file a public issue for a live exploit.

## Signing

Unsigned MSIX will warn on SmartScreen until a release is signed. That is expected for a first OSS drop.
