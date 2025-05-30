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
using TheForest;
using TheForest.Buildings.Creation;

[assembly: MelonInfo(typeof(MyModMenu), "The Forest Mod Menu", "1.0.4", "Pr0gnoxCode")]
[assembly: MelonGame("SKS", "TheForest")]
public class MyModMenu : MelonMod
{
    // Hauptmenü-Felder
    private bool showMenu = false;
    private Rect mainWindowRect = new Rect(100, 100, 250, 300);

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
    private bool noclipEnabled = false; // Noclip Toggle
    private bool _noclipEnabledHasRun = false; // Check if Noclip runs already or not

    // Noclip-bezogene Felder
    private float noclipSpeed = 20f;
    private readonly List<Collider> playerColliders = new List<Collider>();
    private Rigidbody playerRigidbody;
    private float noClipSpeedchange = 20f;

    // Geschwindigkeitswerte speichern
    private static float originalWalkSpeed = 0f;
    private static float originalRunSpeed = 0f;
    private static float originalCrouchSpeed = 0f;
    private static float originalSwimmingSpeed = 0f;
    private static float fastWalkSpeedchange = 3f;

    // several other features
    private static bool buildhack = false;
    private static bool cancelAllGhosts = false;

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
        // Toggle Hauptmenü mit Insert
        if (UnityEngine.Input.GetKeyDown(KeyCode.Insert))
            showMenu = !showMenu;

        //Toggle Inventory Window with  F10
        if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            showInventorySubmenu = !showInventorySubmenu;

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

        // Toggle Noclip (F5)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
        {
            noclipEnabled = !noclipEnabled;
            ToggleNoclip(noclipEnabled);
            MelonLogger.Msg("Noclip: " + (noclipEnabled ? "enabled" : "disabled"));
        }

        // Toggle Build Hack (F4)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F4))
        {
            buildhack = !buildhack;
            MelonLogger.Msg("Build Hack: " + (buildhack ? "enabled" : "disabled"));
            Cheats.Creative = buildhack;
        }

        // Toggle Cancel all Ghosts (F3)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
        {
            cancelAllGhosts = !cancelAllGhosts;
            MelonLogger.Msg("Cancel all Ghosts: " + (cancelAllGhosts ? "enabled" : "disabled"));
            if (cancelAllGhosts)
            {
                Craft_Structure[] array = UnityEngine.Object.FindObjectsOfType<Craft_Structure>();
                if (array != null && array.Length > 0)
                {
                    foreach (Craft_Structure craft_Structure in array)
                    {
                        craft_Structure.CancelBlueprint();
                    }
                }
            }
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

                fpc.walkSpeed = fastWalkSpeedchange;
                fpc.runSpeed = fastWalkSpeedchange;
                fpc.crouchSpeed = fastWalkSpeedchange;
                fpc.swimmingSpeed = fastWalkSpeedchange;
                fpc.maxSwimVelocity = fastWalkSpeedchange;
                fpc.maxDiveVelocity = originalSwimmingSpeed * fastWalkSpeedchange;
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
                    fpc.maxSwimVelocity = originalSwimmingSpeed;
                    fpc.maxDiveVelocity = 7f;
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

        // Noclip-Bewegung: Wenn Noclip aktiviert, erlaube freie Bewegung
        if (noclipEnabled && fpc != null)
        {
            if (noClipSpeedchange <= 500f && noClipSpeedchange >= 20f)
            {
                noclipSpeed = noClipSpeedchange;
            }

            Vector3 horizontal = Vector3.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W)) { horizontal += fpc.transform.forward; }
            if (UnityEngine.Input.GetKey(KeyCode.S)) { horizontal -= fpc.transform.forward; }
            if (UnityEngine.Input.GetKey(KeyCode.A)) { horizontal -= fpc.transform.right; }
            if (UnityEngine.Input.GetKey(KeyCode.D)) { horizontal += fpc.transform.right; }

            Vector3 vertical = Vector3.zero;
            if (UnityEngine.Input.GetKey(KeyCode.Space)) { vertical = Vector3.up; }
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl)) { vertical = Vector3.down; }

            Vector3 newPos = fpc.transform.position + (horizontal + vertical) * noclipSpeed * Time.deltaTime;
            fpc.transform.position = newPos;
        }
                

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

        // Cancel all Ghosts Toogle
        cancelAllGhosts = GUILayout.Toggle(cancelAllGhosts, "Cancel all Ghosts (F3)");

        // Build Hack Toggle
        buildhack = GUILayout.Toggle(buildhack, "Build Hack (F4)");
        
        // Noclip Toggle and speed
        noclipEnabled = GUILayout.Toggle(noclipEnabled, "Noclip (F5)");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Noclip Speed:");
        noClipSpeedchange = GUILayout.HorizontalSlider(noClipSpeedchange, 50f, 500f);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Feature-Toggles
        infiniteFlaireAmmo = GUILayout.Toggle(infiniteFlaireAmmo, "Infinite Flaire Ammo (F6)");

        // God Mode Toggle
        bool newGodMode = GUILayout.Toggle(godMode, "God Mode (F7)");
        if (newGodMode != godMode)
        {
            godMode = newGodMode;
            if (godMode)
                EnableGodMode();
            else
                DisableGodMode();
        }

        infiniteEnergy = GUILayout.Toggle(infiniteEnergy, "Infinite Energy (F8)");

        fastWalk = GUILayout.Toggle(fastWalk, "Fast Walk (F9)");
        fastWalkSpeedchange = GUILayout.HorizontalSlider(fastWalkSpeedchange, 3f, 100f);
        GUILayout.Space(20);

        // Button zum Umschalten des Inventarfensters
        if (GUILayout.Button("Toggle Inventory Window (F10)"))
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

        GUI.contentColor = Color.green;
        Color currentcontentColor = GUI.contentColor;
        Color previousColor = GUI.color;

        GUI.contentColor = Color.red;
        GUI.color = Color.red;
        if (GUILayout.Button("Add Max. for all Item to Inventory"))
        {
            AddMaxItemsToInventory();
        }
        GUI.color = previousColor;
        GUI.contentColor = currentcontentColor;

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


    private void AddMaxItemsToInventory()
    {
        PlayerInventory inventory = GameObject.FindObjectOfType<PlayerInventory>();
        if (inventory != null)
        {
            foreach (int itemID in inventoryItemIDs)
            {
                TheForest.Items.Item item = ItemDatabase.ItemById(itemID);
                if (item != null)
                {
                    int maxCount = 0;
                    try
                    {
                        maxCount = item._maxAmount;
                    }
                    catch (Exception)
                    {
                        maxCount = 999999;
                    }

                    // Falls der Wert aus irgendeinem Grund nicht sinnvoll ist, einen Fallback nutzen
                    if (maxCount <= 0)
                        maxCount = 999999;

                    bool added = inventory.AddItem(itemID, maxCount, false, false, null);
                    if (added)
                        MelonLogger.Msg("Added max count (" + maxCount + ") for item " + itemID);
                    else
                        MelonLogger.Msg("Failed to add max count for item " + itemID);
                }
            }
        }
        else
        {
            MelonLogger.Msg("PlayerInventory not found.");
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

    private IEnumerator DisableGodModeDelayed()
    {
        yield return new WaitForSeconds(30f);
        DisableGodMode();
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

    // Noclip-Funktion: Deaktiviert bzw. reaktiviert die Kollisionskomponenten und Schwerkraft des Spielers
    private void ToggleNoclip(bool enabled)
    {
        FirstPersonCharacter fpc = GameObject.FindObjectOfType<FirstPersonCharacter>();
        if (fpc != null)
        {
            // Beim ersten Aufruf alle relevanten Collider sammeln
            if (playerColliders.Count == 0)
            {
                playerColliders.AddRange(fpc.GetComponentsInChildren<Collider>());
            }

            if (enabled)
            {
                // Aktiviert GodMode, falls nötig
                if (!godMode && !_noclipEnabledHasRun)
                {
                    EnableGodMode();
                    godMode = !godMode;
                    _noclipEnabledHasRun = true;
                }

                // Deaktiviere nur physikalische Collider (die nicht als Trigger fungieren)
                foreach (var col in playerColliders)
                {
                    if (!col.isTrigger)
                    {
                        col.enabled = false;
                    }
                    // Trigger-Collider bleiben aktiv, damit Interaktionen funktionieren
                }
                // Deaktiviere den CharacterController (falls vorhanden)
                CharacterController cc = fpc.GetComponent<CharacterController>();
                if (cc != null)
                    cc.enabled = false;

                // Setze den Rigidbody auf kinematisch, um physikalische Einflüsse auszuschalten
                playerRigidbody = fpc.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                {
                    playerRigidbody.isKinematic = true;
                    playerRigidbody.velocity = Vector3.zero;
                }
            }
            else
            {
                // Reaktiviere alle Collider (Trigger und physikalisch)
                foreach (var col in playerColliders)
                {
                    col.enabled = true;
                }
                // Reaktiviere den CharacterController (falls vorhanden)
                CharacterController cc = fpc.GetComponent<CharacterController>();
                if (cc != null)
                    cc.enabled = true;

                // Setze den Rigidbody wieder auf nicht-kinematisch
                if (playerRigidbody != null)
                {
                    playerRigidbody.isKinematic = false;
                }

                // Deaktiviere GodMode (sofern nicht dauerhaft aktiv)
                if (!godMode)
                {
                    MelonCoroutines.Start(DisableGodModeDelayed());
                    _noclipEnabledHasRun = false;
                }
            }
        }
    }
}