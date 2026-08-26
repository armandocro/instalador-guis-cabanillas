# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [1.3.0] - 2026-08-24

### Added
- PULL&BEAR brand configuration (24 GUIs)
- ZARA HOME brand configuration (17 GUIs)
- Update checking and auto-update flow
- Free registration flow for custom JNLP URLs
- Decision-tree chatbot for user assistance
- Local telemetry/metrics system
- Custom dialog system (AppDialogHost)

### Changed
- Migrated to .NET 8 / WPF
- Improved error handling with 3-level global exception handlers

### Security
- Process execution without shell (`ArgumentList`) to prevent command injection
- URL validation with allowlist regex and sanitization
- BinaryFormatter serialization disabled
