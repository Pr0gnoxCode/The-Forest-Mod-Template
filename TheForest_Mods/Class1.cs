using HarmonyLib;
using MelonLoader;
using UnityEngine;
using TheForest.Items.Inventory; 
using TheForest.Items;          
using TheForest.Items.Utils;    
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[assembly: MelonInfo(typeof(MyModMenu), "The Forest Mod Menu", "1.0.3", "Pr0gnoxCode")]
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
    private bool infiniteHealth = false;
    private bool infiniteAmmo = false;
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
        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.example.myfastwalkmod");
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
        foreach (Item item in ItemDatabase.Items)
        {
            ids.Add(item._id);
            // Verwende den originalen Namen, ohne Veränderung:
            itemIdToName[item._id] = item._name;
        }
        // Sortiere die IDs anhand des originalen Namens alphabetisch
        ids.Sort((a, b) => itemIdToName[a].CompareTo(itemIdToName[b]));
        inventoryItemIDs = ids.ToArray();
        MelonLogger.Msg("Loaded " + inventoryItemIDs.Length + " items from ItemDatabase.");
    }

    public override void OnUpdate()
    {
        // Toggle Hauptmenü mit F10
        if (Input.GetKeyDown(KeyCode.F10))
            showMenu = !showMenu;

        // Toggle Fast Walk (F9)
        if (Input.GetKeyDown(KeyCode.F9))
        {
            fastWalk = !fastWalk;
            MelonLogger.Msg("Fast Walk: " + (fastWalk ? "enabled" : "disabled"));
        }

        // Toggle Infinite Energy (F8)
        if (Input.GetKeyDown(KeyCode.F8))
        {
            infiniteEnergy = !infiniteEnergy;
            MelonLogger.Msg("Infinite Energy: " + (infiniteEnergy ? "enabled" : "disabled"));
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

            if (infiniteEnergy)
            {
                fpc.staminaCostPerSec = 0.0f;
            }
            else
            {
                fpc.staminaCostPerSec = 4.0f;
            }
        }
    }

    public override void OnGUI()
    {
        // Zeichne das Hauptmod-Menü
        if (showMenu)
        {
            // Wenn die Maus über dem Hauptfenster ist, fokussiere es
            if (mainWindowRect.Contains(Event.current.mousePosition))
            {
                GUI.FocusWindow(0);
            }
            mainWindowRect = GUI.Window(0, mainWindowRect, DrawMainWindow, "The Forest Mod Menu");
        }

        // Zeichne das Inventarfenster separat, wenn es aktiv ist
        if (showInventorySubmenu)
        {
            // Feste Position rechts neben dem Hauptfenster
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
        infiniteHealth = GUILayout.Toggle(infiniteHealth, "Infinite Health");
        infiniteAmmo = GUILayout.Toggle(infiniteAmmo, "Infinite Ammo");
        godMode = GUILayout.Toggle(godMode, "God Mode");
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
        inventoryScrollPos = GUILayout.BeginScrollView(inventoryScrollPos, GUILayout.Height(300));
        if (inventoryItemIDs != null)
        {
            foreach (int itemID in inventoryItemIDs)
            {
                string displayName = "Unknown";
                Item item = ItemDatabase.ItemById(itemID);
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

    // Nutzt die offizielle AddItem-Methode der PlayerInventory, um ein Item per ID hinzuzufügen.
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

    // Verwendet die offizielle SpawnItem-Methode aus ItemUtils, um ein Item zu spawnen.
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
}
