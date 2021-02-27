# valheim-pickup-filter-plugin
A BepInEx plugin for Valheim to selectively ignore items on the ground

# Requirements
- BepInEx: https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/

# Installation
Just drop PickupFilterPlugin.dll into the `BepInEx/plugins` folder

# How it works

When you encounter an item that you don't want to automatically pick up from the ground, open your inventory and ALT + Left Click on the item.

To stop ignoring an item on the ground, ALT + Left Click it again from your inventory. You can still manually pick up items off the ground with the Use key.

# Commands

## /pickup

Displays the PickupFilter help menu

## /pickup list

Returns the list of items that will be ignored when walking hear them on the ground

## /pickup clear

Clears the ignored item list 

