using System;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;
using Duckov.UI;

namespace DogEat
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public Harmony harmony;
        private static Inventory playerInventory;
        private static Inventory petInventory;
        private static bool hasInventoryOpened = false;

        // 定时器相关变量
        private static float nextCheckTime = 0f;
        private static System.Random random = new System.Random();
        private static int foodConsumptionFlag = 0; // 0:未消耗, 1:部分消耗, 2:全部消耗
        private static int totalConsumptionAmount = 0; // 累加的食物消耗总数

        // Unity生命周期事件函数
        private void OnEnable()
        {
            HarmonyLoad.HarmonyLoad.Load0Harmony();
        }

        private void Start()
        {
            // 初始化下次检查时间
            nextCheckTime = Time.time + random.Next(180, 301); // 3-5分钟 (180-300秒)

            harmony = new Harmony("DogEat");
            harmony.PatchAll();
        }

        private void Update()
        {
            // 定时检查并消耗938号物品
            if (Time.time >= nextCheckTime)
            {
                ConsumeFoodItems();
                // 设置下次检查时间（3-5分钟后）
                nextCheckTime = Time.time + random.Next(180, 301); // 3-5分钟 (180-300秒)
            }
        }

        private void OnDisable()
        {
            harmony.UnpatchAll("DogEat");
        }

        // Harmony补丁事件函数
        // 使用Harmony补丁在玩家角色启用时获取背包引用
        [HarmonyPatch(typeof(CharacterMainControl), "OnEnable")]
        public class CharacterMainControlOnEnablePatch
        {
            static void Postfix(CharacterMainControl __instance)
            {
                // 只处理玩家角色
                if (__instance == CharacterMainControl.Main)
                {
                    // 查找玩家的背包组件
                    playerInventory = __instance.GetComponentInChildren<Inventory>();

                    // 获取宠物背包
                    petInventory = PetProxy.PetInventory;
                }
            }
        }

        // 监听背包界面打开事件
        [HarmonyPatch(typeof(LootView), "OnOpen")]
        public class ViewOnOpenPatch
        {
            static void Postfix(View __instance)
            {
                // ShowBubbleText("__instance.GetType()：" + __instance.GetType());

                // 设置标记表示背包已打开
                hasInventoryOpened = true;
            }
        }

        // 监听背包界面关闭事件
        [HarmonyPatch(typeof(LootView), "OnClose")]
        public class ViewOnClosePatch
        {
            static void Postfix(View __instance)
            {
                // ShowBubbleText("===背包已关闭=== 测试成功");

                // 只有在背包已打开的情况下才显示提示
                if (hasInventoryOpened)
                {
                    hasInventoryOpened = false;

                    // 根据食物消耗状态显示相应提示
                    if (foodConsumptionFlag == 1)
                    {
                        ShowBubbleText("狗子，你又偷吃！？");
                    }
                    else if (foodConsumptionFlag == 2)
                    {
                        ShowBubbleText("你吃完了我吃什么[･Д･ ]");
                    }

                    // 重置食物消耗状态标志
                    foodConsumptionFlag = 0;
                }
            }
        }

        // 主要功能函数（按照调用顺序排列）
        // 消耗938号物品的方法
        public static void ConsumeFoodItems()
        {
            // 这里需要获取当前背包中的938号物品数量
            int foodTypeID = 938;
            int petFoodCount = 0;
            int playerFoodCount = 0;

            // 统计宠物背包中的938号物品
            if (petInventory != null)
            {
                for (int i = 0; i < petInventory.Content.Count; i++)
                {
                    Item item = petInventory.Content[i];
                    if (item != null && item.TypeID == foodTypeID)
                    {
                        petFoodCount += item.StackCount;
                    }
                }
            }

            // 统计玩家背包中的938号物品
            if (playerInventory != null)
            {
                for (int i = 0; i < playerInventory.Content.Count; i++)
                {
                    Item item = playerInventory.Content[i];
                    if (item != null && item.TypeID == foodTypeID)
                    {
                        playerFoodCount += item.StackCount;
                    }
                }
            }

            // 计算总数量
            int totalFoodCount = petFoodCount + playerFoodCount;

            // 如果没有938号物品，直接返回
            if (totalFoodCount <= 0)
            {
                return;
            }

            // 获取938号物品的最大堆叠数
            int maxStackCount = 99; // 默认值，如果可能的话应该从实际物品数据中获取

            // 如果背包中有物品，尝试获取实际的最大堆叠数
            Item sampleItem = null;
            if (playerInventory != null)
            {
                for (int i = 0; i < playerInventory.Content.Count; i++)
                {
                    Item item = playerInventory.Content[i];
                    if (item != null && item.TypeID == foodTypeID)
                    {
                        sampleItem = item;
                        break;
                    }
                }
            }

            if (sampleItem == null && petInventory != null)
            {
                for (int i = 0; i < petInventory.Content.Count; i++)
                {
                    Item item = petInventory.Content[i];
                    if (item != null && item.TypeID == foodTypeID)
                    {
                        sampleItem = item;
                        break;
                    }
                }
            }

            // 如果找到了物品样本，使用其最大堆叠数
            if (sampleItem != null)
            {
                maxStackCount = sampleItem.MaxStackCount;
            }

            // 确保最大堆叠数至少为1
            maxStackCount = Math.Max(1, maxStackCount);

            // 计算要消耗的数量（1到最大堆叠数）
            int consumptionAmount = random.Next(1, maxStackCount + 1);

            // 确保消耗数量不超过总数量
            consumptionAmount = Math.Min(consumptionAmount, totalFoodCount);

            // 累加消耗数量到全局变量
            totalConsumptionAmount += consumptionAmount;

            // 消耗物品
            int remainingConsumption = consumptionAmount;
            bool playerItemsConsumed = false; // 标记玩家背包物品是否被消耗

            // 优先消耗宠物背包中的物品
            if (petFoodCount > 0 && remainingConsumption > 0)
            {
                int petConsumption = System.Math.Min(petFoodCount, remainingConsumption);
                RemoveItemsFromInventory(petInventory, foodTypeID, petConsumption);
                remainingConsumption -= petConsumption;
            }

            // 如果还有剩余消耗量，消耗玩家背包中的物品
            if (playerFoodCount > 0 && remainingConsumption > 0)
            {
                int playerConsumption = System.Math.Min(playerFoodCount, remainingConsumption);
                RemoveItemsFromInventory(playerInventory, foodTypeID, playerConsumption);
                playerItemsConsumed = true; // 标记玩家背包物品被消耗
            }

            // 更新flag状态
            // 计算消耗后的剩余总数量
            int newTotalFoodCount = totalFoodCount - consumptionAmount;
            
            // 设置flag: 1表示部分消耗, 2表示全部消耗
            if (newTotalFoodCount <= 0)
            {
                foodConsumptionFlag = 2; // 全部消耗
            }
            else
            {
                foodConsumptionFlag = 1; // 部分消耗
            }

            // 宠物显示冒泡框
            if (totalConsumptionAmount > 20)
            {
                // 累计消耗超过20个时，显示特殊提示
                ShowBubbleText("不行了，吃不下了！！！");
                // 重置累计消耗数量
                totalConsumptionAmount = 0;
            }

            // 玩家显示冒泡框
            if (playerItemsConsumed)
            {
                // 玩家背包物品被消耗时，只显示"感觉轻松了"和"脚步变轻盈了"
                string[] playerMessages = { "感觉轻松了", "脚步变轻盈了" };
                int randomIndex = random.Next(playerMessages.Length);
                ShowBubbleText(playerMessages[randomIndex]);
            }
            else
            {
                // 只消耗了宠物背包物品时，显示"是不是少了什么东西"
                ShowBubbleText("是不是少了什么东西");
            }
        }

        // 从指定背包中移除指定数量的物品
        private static void RemoveItemsFromInventory(Inventory inventory, int itemTypeID, int count)
        {
            if (inventory == null || count <= 0)
            {
                return;
            }

            // 检查Content是否为空
            if (inventory.Content == null)
            {
                return;
            }

            int remainingCount = count;

            // 遍历背包内容，移除指定类型的物品
            for (int i = 0; i < inventory.Content.Count && remainingCount > 0; i++)
            {
                Item item = inventory.Content[i];
                if (item != null && item.TypeID == itemTypeID)
                {
                    // 检查当前物品堆叠数量
                    int stackCount = item.StackCount;

                    if (stackCount <= remainingCount)
                    {
                        // 如果当前物品的堆叠数量小于等于剩余需要消耗的数量，则销毁整个物品
                        item.DestroyTree();
                        inventory.Content[i] = null;
                        remainingCount -= stackCount;
                    }
                    else
                    {
                        // 如果当前物品的堆叠数量大于剩余需要消耗的数量
                        // 直接修改StackCount属性（根据新发现的代码，该属性有setter）
                        item.StackCount = stackCount - remainingCount;
                        remainingCount = 0;
                    }
                }
            }
        }

        // 显示冒泡框的通用方法
        public static void ShowBubbleText(string text)
        {
            CharacterMainControl mainCharacter = CharacterMainControl.Main;
            if (mainCharacter != null)
            {
                mainCharacter.PopText(text);
            }
        }
    }
}