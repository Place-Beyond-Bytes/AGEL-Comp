using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public InventorySlot[] itemSlots;
    public UseItem useItem;
    public int gold;
    public TMP_Text goldText;
    public GameObject lootPrefab;
    public Transform player;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gold = 0; // Force gold to 0 at the start
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        foreach (var slot in itemSlots)
        {
            slot.UpdateUI();
        }
    }

    private void Update()
    {
        // Keyboard shortcuts for consuming items
        if (Input.GetKeyDown(KeyCode.Alpha7) && itemSlots.Length > 0)
        {
            UseItemFromSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8) && itemSlots.Length > 1)
        {
            UseItemFromSlot(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9) && itemSlots.Length > 2)
        {
            UseItemFromSlot(2);
        }
    }

    private void UseItemFromSlot(int slotIndex)
    {
        if (slotIndex < itemSlots.Length && itemSlots[slotIndex].quantity > 0)
        {
            UseItem(itemSlots[slotIndex]);
            Debug.Log("Used item from slot " + (slotIndex + 1) + " using key " + (slotIndex + 7));
        }
    }

    private void OnEnable()
    {
        Loot.OnItemLooted += AddItem;
    }

    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }

    public void AddItem(ItemSO itemSO, int quantity)
    {
        quantity = 1; // Always pick up only 1 item
        if (itemSO.isGold)
        {
            gold += quantity;
            goldText.text = gold.ToString();
            return;
        }

        foreach (var slot in itemSlots)     //It it is the SAME item AND there is ROOM left
        {
            if(slot.itemSO == itemSO && slot.quantity < itemSO.stackSize)
            {
                int availableSpace = itemSO.stackSize - slot.quantity;
                int amountToAdd = Mathf.Min(availableSpace, quantity);

                slot.quantity += amountToAdd;
                quantity -= amountToAdd;

                slot.UpdateUI();

                if (quantity <= 0)
                    return;
            }
        }

        foreach (var slot in itemSlots)     //If items remain we will now look at the empty slots
        {
            if (slot.itemSO == null)
            {
                int amountToAdd = Mathf.Min(itemSO.stackSize, quantity);
                slot.itemSO = itemSO;
                slot.quantity = quantity;
                slot.UpdateUI();
                return;
            }
        }

        if (quantity > 0)
            DropLoot(itemSO, quantity);
    }

    public void DropItem(InventorySlot slot)
    {
        DropLoot(slot.itemSO, 1);
        slot.quantity--;
        if(slot.quantity <= 0)
        {
            slot.itemSO = null;
        }
        slot.UpdateUI();
    }

    private void DropLoot(ItemSO itemSO, int quantity)
    {
        Loot loot = Instantiate(lootPrefab, player.position, Quaternion.identity).GetComponent<Loot>();
        loot.Initialize(itemSO, quantity);
    }

    public void UseItem(InventorySlot slot)
    {
        if (slot.itemSO != null && slot.quantity >= 0)
        {
            useItem.ApplyItemEffects(slot.itemSO);

            slot.quantity--;
            if(slot.quantity <= 0)
            {
                slot.itemSO = null;
            }
            slot.UpdateUI();
        }
    }

    public bool HasItem(ItemSO itemSO)
    {
        foreach (var slot in itemSlots)
        {
            if (slot.itemSO == itemSO && slot.quantity > 0)
                return true;
        }
        return false;
    }
}
