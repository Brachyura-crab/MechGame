using System;
using System.Collections.Generic;
using UnityEngine;

// สั่งเสบียงด้วยแต้ม ของมาถึงหลังรอ deliveryTime วินาที วางบนผู้เล่น
// ภายหลังจะเปลี่ยนเป็นของตกจริง แก้ที่ Deliver() ที่เดียว UI ไม่ต้องแก้
[RequireComponent(typeof(ResourceInventory), typeof(ScorePoints))]
public class SupplyService : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public ResourceType type;
        public int cost = 20;      // แต้มต่อ 1 ชุด
        public int amount = 30;    // จำนวนที่ได้ต่อ 1 ชุด
    }

    [SerializeField] List<Item> items = new List<Item>();
    [SerializeField] float deliveryTime = 10f;

    ResourceInventory inventory;
    ScorePoints points;
    int[] pending;

    public IReadOnlyList<Item> Items => items;
    public ScorePoints Points => points;
    public ResourceInventory Inventory => inventory;
    public bool Pending { get; private set; }
    public float Remaining { get; private set; }

    public event Action Changed;     // เริ่มสั่ง / ของมา
    public event Action Delivered;   // ของมาถึง (ไว้ต่อเสียง/ข้อความทีหลัง)

    void Awake()
    {
        inventory = GetComponent<ResourceInventory>();
        points = GetComponent<ScorePoints>();
    }

    // จำนวนชุดสูงสุดที่ยังใส่ได้ (ไม่เกินเพดาน)
    public int MaxPacks(int i)
    {
        Item it = items[i];
        if (it.type == null || it.amount <= 0) return 0;
        int room = it.type.maxAmount - inventory.Get(it.type);
        return room <= 0 ? 0 : Mathf.CeilToInt(room / (float)it.amount);
    }

    public int TotalCost(int[] picks)
    {
        int sum = 0;
        for (int i = 0; i < items.Count && i < picks.Length; i++) sum += picks[i] * items[i].cost;
        return sum;
    }

    public bool CanRequest(int[] picks) =>
        !Pending && TotalCost(picks) > 0 && points.Points >= TotalCost(picks);

    public bool Request(int[] picks)
    {
        if (!CanRequest(picks)) return false;
        if (!points.TrySpend(TotalCost(picks))) return false;
        pending = (int[])picks.Clone();
        Pending = true;
        Remaining = deliveryTime;
        Changed?.Invoke();
        return true;
    }

    void Update()
    {
        if (!Pending) return;
        Remaining -= Time.deltaTime;
        if (Remaining <= 0f) Deliver();
    }

    void Deliver()
    {
        for (int i = 0; i < items.Count && i < pending.Length; i++)
            if (pending[i] > 0 && items[i].type != null)
                inventory.Add(items[i].type, pending[i] * items[i].amount);   // เกินเพดานจะถูกตัด
        Pending = false;
        Remaining = 0f;
        Changed?.Invoke();
        Delivered?.Invoke();
    }
}