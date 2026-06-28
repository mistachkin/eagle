# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in Eagle, please report it
responsibly.  **Do not file a public GitHub issue.**

Instead, please use one of the following methods:

- **GitHub private vulnerability reporting:**
  Use the [Security Advisories](https://github.com/mistachkin/eagle/security/advisories)
  tab to report a vulnerability privately.

- **Direct contact:**
  Reach out to the project maintainer via the contact information
  available at [https://www.mistachkin.com/](https://www.mistachkin.com/).

## What to Include

When reporting, please include:

- A description of the vulnerability and its potential impact.
- Steps to reproduce the issue or a proof-of-concept.
- The version(s) of Eagle affected, if known.
- Any relevant configuration details (e.g., interpreter flags,
  execution policy, safe interpreter context).

## Response

You can expect an initial acknowledgment within a reasonable timeframe.
Fixes for confirmed vulnerabilities will be prioritized based on
severity and impact.

## Scope

This policy covers the Eagle core library, shell, and all first-party
plugins and scripts distributed in this repository.  Third-party
plugins, extensions, or scripts are outside the scope of this policy.

## Security Model & Documentation

For background on Eagle's security architecture -- safe interpreters,
execution policies, command/option safety flags, and the trust model --
see the canonical [documentation repository](https://urn.to/r/docs)
(this will redirect), in particular:

- [`safe.md`](https://github.com/mistachkin/docs/blob/trunk/safe.md) -- safe interpreters and the security model.
- [`whitepaper.md`](https://github.com/mistachkin/docs/blob/trunk/whitepaper.md) -- the comprehensive design and
  security rationale.
- [`why_eagle.md`](https://github.com/mistachkin/docs/blob/trunk/why_eagle.md) -- includes an overview of the
  security model and threat considerations.

The project's broader design principles and coding conventions -- including
how security-sensitive and `safe`-interpreter code must be written and
reviewed -- are catalogued in [`architecture_patterns.md`](https://github.com/mistachkin/docs/blob/trunk/architecture_patterns.md).
