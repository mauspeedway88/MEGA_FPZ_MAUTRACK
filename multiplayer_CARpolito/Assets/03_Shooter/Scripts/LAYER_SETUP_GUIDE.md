# Layer Setup Guide - Projectiles vs Players

## Overview

This guide explains how to set up layers so that projectiles only hit chickens and other enemies, but **never hit players**.

## Current Setup

Based on the code analysis:
- **Player**: Layer 0 (Default)
- **Chickens**: Layer 0 (Default)
- **KCC (Player Controller)**: Layer 6

## Recommended Layer Setup

### Option 1: Use Component-Based Filtering (Current Implementation)

The code now automatically filters out players using component detection:
- ✅ **No layer changes needed** - works with current setup
- ✅ Projectiles check for `Player` component and skip it
- ✅ Works regardless of what layer players are on

### Option 2: Create Dedicated Layers (Recommended for Clean Setup)

For a cleaner, more maintainable setup:

1. **Create New Layers** (Edit → Project Settings → Tags and Layers):
   ```
   Layer 8: "Player"
   Layer 9: "Enemy" (or "Chicken")
   Layer 10: "Projectile"
   Layer 11: "Environment"
   ```

2. **Assign Layers**:
   - **Player Prefab**: Set to "Player" layer (8)
   - **Chicken Prefab**: Set to "Enemy" layer (9)
   - **Projectile Prefab**: Set to "Projectile" layer (10)
   - **Environment/Walls**: Set to "Environment" layer (11)

3. **Configure HitMask in WeaponSystem**:
   - Include: "Enemy" (9) and "Environment" (11)
   - Exclude: "Player" (8) and "Projectile" (10)

## Code Implementation

The projectile system now has **automatic player filtering**:

### 1. Component-Based Detection
```csharp
private bool IsPlayer(Collider collider)
{
    // Checks if collider has Player component
    var player = collider.GetComponentInParent<Player>();
    return player != null;
}
```

### 2. Layer Mask Filtering
```csharp
private LayerMask GetFilteredHitMask(LayerMask originalMask)
{
    // Automatically removes player layer from hit mask
    // Works even if player is on Default layer
}
```

## How It Works

### Collision Detection Flow

1. **SphereCast** checks for collisions
   - Uses filtered mask (player layer excluded)
   - Double-checks with `IsPlayer()` component check

2. **OverlapSphere** backup check
   - Filters out any player colliders found
   - Only processes non-player collisions

3. **OnTriggerEnter** fallback
   - Skips players immediately
   - Only processes enemy/environment collisions

## Testing

### Verify Projectiles Don't Hit Players

1. **Fire projectile at yourself** (look down and shoot)
   - Projectile should pass through player
   - No damage should be dealt
   - Projectile should continue flying

2. **Fire projectile at chicken**
   - Projectile should hit chicken
   - Chicken should take damage
   - Projectile should be destroyed

3. **Check Console**
   - No errors about hitting players
   - Projectiles spawn and move correctly

## Current Behavior

With the current implementation:
- ✅ **Projectiles automatically ignore players** (component-based check)
- ✅ **Works with any layer setup** (no changes needed)
- ✅ **Only hits chickens and environment**
- ✅ **Double-checked** (both layer mask and component check)

## Manual Layer Setup (Optional)

If you want to use layers explicitly:

### Step 1: Create Layers
1. Go to **Edit → Project Settings → Tags and Layers**
2. Add new layers:
   - Layer 8: `Player`
   - Layer 9: `Enemy`
   - Layer 10: `Projectile`

### Step 2: Assign to Prefabs
1. **Player Prefab**:
   - Select Player prefab
   - Set Layer to "Player" (8)

2. **Chicken Prefab**:
   - Select Chicken prefab
   - Set Layer to "Enemy" (9)

3. **Projectile Prefab**:
   - Select Projectile prefab
   - Set Layer to "Projectile" (10)

### Step 3: Update WeaponSystem HitMask
1. Select Player prefab
2. Find **WeaponSystem** component
3. Set **HitMask** to include:
   - ✅ Enemy (layer 9)
   - ✅ Default (layer 0) - for environment
   - ❌ Player (layer 8) - **UNCHECKED**
   - ❌ Projectile (layer 10) - **UNCHECKED**

## Troubleshooting

### Projectiles Still Hit Players

1. **Check Component Detection**:
   - Ensure Player prefab has `Player` component
   - Check that collider is a child of Player GameObject

2. **Check Layer Setup**:
   - Verify player layer is excluded from HitMask
   - Check that projectile layer is not in HitMask

3. **Check Collision Delay**:
   - Projectiles have 0.1s delay before collision detection
   - This prevents immediate self-collision

### Projectiles Don't Hit Chickens

1. **Check HitMask**:
   - Ensure chicken layer is included
   - Check that chickens are on the correct layer

2. **Check Collider Setup**:
   - Chickens need colliders (trigger or non-trigger)
   - Verify colliders are enabled

## Summary

**Current Implementation**: ✅ **No changes needed!**
- Code automatically filters out players
- Works with existing layer setup
- Component-based detection is most reliable

**Optional**: Create dedicated layers for cleaner organization, but it's not required.

The projectile system is now **player-safe** and will only hit enemies and environment! 🎯

