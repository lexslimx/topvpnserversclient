# OpenVPN Installation Prerequisites

This directory contains prerequisites for the TopVPN installer package.

## OpenVPN MSI

For the installer to work correctly, you need to download the OpenVPN MSI installer and place it in this directory.

### Instructions:

1. Download OpenVPN-2.6.14-I001-amd64.msi from the official OpenVPN website:
   - Visit: https://openvpn.net/community-downloads/
   - Find version 2.6.14 (or the version specified in the Prerequisites.wxs file)
   - Download the Windows MSI Installer (64-bit)

2. Place the downloaded MSI file in this directory, ensuring it has the exact name:
   - OpenVPN-2.6.14-I001-amd64.msi

3. Build the installer package using the WiX Toolset.

**Note**: The installer package will include OpenVPN as a prerequisite and install it before installing the main application.