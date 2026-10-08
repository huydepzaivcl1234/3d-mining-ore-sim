# NPC buy button and shop icon padding

- Existing HUD button retained, with `Group 3.svg`, existing miner icon, a coin icon and live price. Apply using `Mining Simulator/UI/Apply NPC Buy SVG`. Save the scene yourself.
- Configure `Assets/GameData/NPC/NpcData.asset`: **Purchase Cost** (base price) and **Purchase Cost Increase Percent** (default 10%). Price is `ceil(base * (1 + percent / 100)^successfulPurchases)`, capped at int.MaxValue. Zero percent keeps prices fixed.
- Living miner count still controls capacity. A separate saved lifetime purchase counter controls price; miner deaths do not reduce it. Old saves use their living count as initial purchase tier. Rebirth/reset follows the existing ResetAllNpcs workflow and resets both counts. Failed spawns refund exactly the price charged and never increase the tier.
- Shop icon frame remains 68x68; icon is 52x52 with preserved aspect ratio. Reapplying the shop style does not shrink either repeatedly. Runtime cloned cards inherit this layout.
- Button hover/punch/click sound and MiningHud purchase handling are unchanged. No new purchase listener is added.
