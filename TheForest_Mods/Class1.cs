﻿using MelonLoader;
using UnityEngine;
using TheForest.Items.Inventory;
using TheForest.Items;
using TheForest.Items.Utils;
using TheForest.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using static CoopPlayerUpgrades;

[assembly: MelonInfo(typeof(MyModMenu), "The Forest Mod Menu", "1.0.4", "Pr0gnoxCode")]
[assembly: MelonGame("SKS", "TheForest")]

public class MyModMenu : MelonMod
{
    // Hauptmenü-Felder
    private bool showMenu = false;
    private Rect mainWindowRect = new Rect(100, 100, 350, 400);

    // Inventarfenster (separat)
    private bool showInventorySubmenu = false;
    private Rect inventoryWindowRect = new Rect(480, 100, 350, 800);
    private Vector2 inventoryScrollPos;
    private int[] inventoryItemIDs;
    private Dictionary<int, string> itemIdToName;

    // Feature-Toggles
    //private bool infiniteHealth = false;  //nicht implementiert
    private bool infiniteFlaireAmmo = false;
    private bool godMode = false;
    private bool infiniteEnergy = false;
    public static bool fastWalk = false;

    // Geschwindigkeitswerte speichern
    private static float originalWalkSpeed = 0f;
    private static float originalRunSpeed = 0f;
    private static float originalCrouchSpeed = 0f;
    private static float originalSwimmingSpeed = 0f;

    public override void OnApplicationStart()
    {
        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.example.mytheforestmod");
        harmony.PatchAll();
        MelonLogger.Msg("My Mod Menu loaded onApplicationStart!");

        // Starte eine Coroutine, die auf die ItemDatabase wartet
        MelonCoroutines.Start(WaitForItemDatabase());
    }

    private IEnumerator WaitForItemDatabase()
    {
        // Warte, bis ItemDatabase.Items verfügbar und nicht leer sind
        while (ItemDatabase.Items == null || ItemDatabase.Items.Length == 0)
        {
            yield return null;
        }

        List<int> ids = new List<int>();
        itemIdToName = new Dictionary<int, string>();
        foreach (TheForest.Items.Item item in ItemDatabase.Items)
        {
            ids.Add(item._id);
            // Verwende den originalen Namen, unverändert:
            itemIdToName[item._id] = item._name;
        }
        // Sortiere die IDs alphabetisch anhand des originalen Namens
        ids.Sort((a, b) => itemIdToName[a].CompareTo(itemIdToName[b]));
        inventoryItemIDs = ids.ToArray();
        MelonLogger.Msg("Loaded " + inventoryItemIDs.Length + " items from ItemDatabase.");
    }

    public override void OnUpdate()
    {
        // Toggle Hauptmenü mit F10
        if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            showMenu = !showMenu;

        // Toggle Fast Walk (F9)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
        {
            fastWalk = !fastWalk;
            MelonLogger.Msg("Fast Walk: " + (fastWalk ? "enabled" : "disabled"));
        }

        // Toggle Infinite Energy (F8)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
        {
            infiniteEnergy = !infiniteEnergy;
            MelonLogger.Msg("Infinite Energy: " + (infiniteEnergy ? "enabled" : "disabled"));
        }

        // Toggle God Mode (F7)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F7))
        {
            godMode = !godMode;
            MelonLogger.Msg("God Mode: " + (godMode ? "enabled" : "disabled"));
            if (godMode)
                EnableGodMode();
            else
                DisableGodMode();
        }

        // Toggle Infinite Flaire Ammo (F6)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F6))
        {
            infiniteFlaireAmmo = !infiniteFlaireAmmo;
            MelonLogger.Msg("Infinite Flaire Ammo: " + (infiniteFlaireAmmo ? "enabled" : "disabled"));
        }
                
        // Geschwindigkeitseinstellungen (Fast Walk)
        FirstPersonCharacter fpc = GameObject.FindObjectOfType<FirstPersonCharacter>();
        if (fpc != null)
        {
            if (fastWalk)
            {
                if (originalWalkSpeed == 0f && originalRunSpeed == 0f && originalCrouchSpeed == 0f && originalSwimmingSpeed == 0f)
                {
                    originalWalkSpeed = fpc.walkSpeed;
                    originalRunSpeed = fpc.runSpeed;
                    originalCrouchSpeed = fpc.crouchSpeed;
                    originalSwimmingSpeed = fpc.swimmingSpeed;
                }
                fpc.walkSpeed = originalWalkSpeed * 4.0f;
                fpc.runSpeed = originalRunSpeed * 4.0f;
                fpc.crouchSpeed = originalCrouchSpeed * 4.0f;
                fpc.swimmingSpeed = originalSwimmingSpeed * 4.0f;
                fpc.staminaCostPerSec = 0.0f;
            }
            else
            {
                if (originalWalkSpeed != 0f && originalRunSpeed != 0f && originalCrouchSpeed != 0f && originalSwimmingSpeed != 0f)
                {
                    fpc.walkSpeed = originalWalkSpeed;
                    fpc.runSpeed = originalRunSpeed;
                    fpc.crouchSpeed = originalCrouchSpeed;
                    fpc.swimmingSpeed = originalSwimmingSpeed;
                    fpc.staminaCostPerSec = 4.0f;
                }
            }

            // Infinite Energy: Setze Stamina-Kosten auf 0
            if (infiniteEnergy)
            {
                fpc.staminaCostPerSec = 0.0f;
            }
        }

        UpdateInfiniteFlaireAmmoSetting();
    }

    public override void OnGUI()
    {
        // Zeichne das Hauptmod-Menü
        if (showMenu)
        {
            if (mainWindowRect.Contains(Event.current.mousePosition))
            {
                GUI.FocusWindow(0);
            }
            mainWindowRect = GUI.Window(0, mainWindowRect, DrawMainWindow, "The Forest Mod Menu");
        }

        // Zeichne das Inventarfenster separat, wenn es aktiv ist
        if (showInventorySubmenu)
        {
            if (inventoryWindowRect.Contains(Event.current.mousePosition))
            {
                GUI.FocusWindow(1);
            }
            inventoryWindowRect = GUI.Window(1, inventoryWindowRect, DrawInventoryWindow, "Inventory");
        }
    }

    private void DrawMainWindow(int windowID)
    {
        GUILayout.BeginVertical();

        // Feature-Toggles
        //infiniteHealth = GUILayout.Toggle(infiniteHealth, "Infinite Health");   //Not impelemented
        infiniteFlaireAmmo = GUILayout.Toggle(infiniteFlaireAmmo, "Infinite Flaire Ammo");

        //godMode wird hier zusätzlich als Toggle dargestellt:
        bool newGodMode = GUILayout.Toggle(godMode, "God Mode");
        if (newGodMode != godMode)
        {
            godMode = newGodMode;
            if (godMode)
                EnableGodMode();
            else
                DisableGodMode();
        }
        infiniteEnergy = GUILayout.Toggle(infiniteEnergy, "Infinite Energy");
        fastWalk = GUILayout.Toggle(fastWalk, "Fast Walk");

        GUILayout.Space(10);
        // Button zum Umschalten des Inventarfensters
        if (GUILayout.Button("Toggle Inventory Window"))
            showInventorySubmenu = !showInventorySubmenu;

        GUILayout.Space(10);
        if (GUILayout.Button("Close"))
            showMenu = false;

        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    private void DrawInventoryWindow(int windowID)
    {
        GUILayout.BeginVertical();
        GUILayout.Label("Inventory Items:");
        inventoryScrollPos = GUILayout.BeginScrollView(inventoryScrollPos, GUILayout.Height(750));
        if (inventoryItemIDs != null)
        {
            foreach (int itemID in inventoryItemIDs)
            {
                string displayName = "Unknown";
                TheForest.Items.Item item = ItemDatabase.ItemById(itemID);
                if (item != null)
                    displayName = item._name;

                GUILayout.BeginHorizontal();
                GUILayout.Label("ID: " + itemID + " - " + displayName);
                if (GUILayout.Button("Add"))
                    AddInventoryItem(itemID);
                if (GUILayout.Button("Spawn"))
                    SpawnInventoryItem(itemID);
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("No inventory items available.");
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Close Inventory"))
            showInventorySubmenu = false;

        GUILayout.EndVertical();
        GUI.DragWindow();
    }


    private void AddInventoryItem(int itemID)
    {
        PlayerInventory inventory = GameObject.FindObjectOfType<PlayerInventory>();
        if (inventory != null)
        {
            bool added = inventory.AddItem(itemID, 1, false, false, null);
            if (added)
                MelonLogger.Msg("Added item with ID: " + itemID);
            else
                MelonLogger.Msg("Failed to add item with ID: " + itemID);
        }
        else
        {
            MelonLogger.Msg("PlayerInventory not found.");
        }
    }

    
    private void SpawnInventoryItem(int itemID)
    {
        FirstPersonCharacter fpc = GameObject.FindObjectOfType<FirstPersonCharacter>();
        if (fpc != null)
        {
            Vector3 spawnPos = fpc.transform.position + fpc.transform.forward * 2f + Vector3.up * 1f;
            GameObject spawned = ItemUtils.SpawnItem(itemID, spawnPos, Quaternion.identity, false);
            if (spawned != null)
            {
                MelonLogger.Msg("Spawned item with ID: " + itemID + " at " + spawnPos);
            }
            else
            {
                MelonLogger.Msg("Failed to spawn item with ID: " + itemID);
            }
        }
        else
        {
            MelonLogger.Msg("Player not found.");
        }
    }

   
    // Setzt die Spieler-Stats auf voll, schaltet Überlebensfeatures aus und aktiviert unendliche Energie.
    private void EnableGodMode()
    {
        if (LocalPlayer.Stats != null)
        {
            // Setze volle Werte
            LocalPlayer.Stats.Health = 100f;
            LocalPlayer.Stats.Energy = 100f;
            LocalPlayer.Stats.Stamina = 100f;
            LocalPlayer.Stats.Fullness = 100f;
            LocalPlayer.Stats.Thirst = 0f;
        }
        // Deaktiviere Überlebensfeatures und aktiviere Infinite Energy
        Cheats.NoSurvival = true;
        Cheats.InfiniteEnergy = true;
        Cheats.GodMode = true;
        MelonLogger.Msg("God Mode enabled");
    }
    
    // Deaktiviert den GodMode und stellt Überlebensfeatures wieder her.
    private void DisableGodMode()
    {
        Cheats.NoSurvival = false;
        Cheats.InfiniteEnergy = false;
        Cheats.GodMode = false;
        MelonLogger.Msg("God Mode disabled");
    }


    // Prüft in jedem Update, ob infiniteFlaireAmmo aktiviert ist und fügt ggf. so lange Flaire Ammo hinzu,
    // dass der Bestand im Inventar 999999 erreicht.
    private void UpdateInfiniteFlaireAmmoSetting()
    {
        int desiredAmmo = 999999;
        bool added = false;

        // Hole das PlayerInventory
        PlayerInventory inventory = GameObject.FindObjectOfType<PlayerInventory>();
        
        if (inventory == null)
        {
            //MelonLogger.Msg("PlayerInventory not found in UpdateInfiniteFlaireAmmoSetting.");
            return;
        }

        // Ermittle die aktuell vorhandene Anzahl dieses Items.
        int currentAmmo = inventory.AmountOf(107, false);

        if (infiniteFlaireAmmo)
        {
            if (currentAmmo < desiredAmmo)
            {   
                int toAdd = desiredAmmo - currentAmmo;
                added = inventory.AddItem(107, UnityEngine.Random.Range(999990, 999999), false, false, null);
                
                if (added)
                {
                    MelonLogger.Msg("Infinite Ammo enabled: Added Flaire Ammo, total now " + inventory.AmountOf(107, false));
                }
                else
                {
                    MelonLogger.Msg("Failed to add Flaire Ammo for Infinite Ammo.");
                }
            }
        }
    }
}
