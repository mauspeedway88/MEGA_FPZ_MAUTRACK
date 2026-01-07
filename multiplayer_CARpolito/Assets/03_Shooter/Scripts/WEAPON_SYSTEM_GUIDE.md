# Weapon System Guide

This guide explains how to use the new weapon variation system in the 03_Shooter scene.

## Overview

The weapon system supports two types of weapons:
1. **Hitscan Weapons** - Instant raycast weapons (like the original system)
2. **Projectile Weapons** - Physical projectiles that travel through space

## Setup Instructions

### Step 1: Create Weapon Data Assets

1. Right-click in Project window → Create → Shooter → Weapon Data
2. Configure the weapon properties:
   - **Weapon Name**: Display name
   - **Weapon Type**: Hitscan or Projectile
   - **Damage**: Damage per shot
   - **Range**: Max range (hitscan) or travel distance (projectile)
   - **Fire Rate**: Shots per second
   - **Spread**: Accuracy spread in degrees (0 = perfect)
   - **Pellet Count**: Number of projectiles (for shotguns)

### Step 2: Setup Player Prefab

1. Add `WeaponSystem` component to Player prefab
2. Assign references:
   - **Fire Point**: Transform where projectiles spawn (usually CameraHandle)
   - **Hit Mask**: Layers that can be hit
   - **Audio Source**: For weapon sounds
3. Add weapon data assets to **Available Weapons** array

### Step 3: For Projectile Weapons

1. Create a projectile prefab:
   - Add `NetworkObject` component
   - Add `Projectile` script
   - Add `Rigidbody` component
   - Add `Collider` component
   - Add visual mesh/model
   - Optionally add particle effects for trail
2. Assign the prefab to WeaponData's **Projectile Prefab** field

## Example Weapon Configurations

### Assault Rifle (Hitscan)
- Type: Hitscan
- Damage: 1
- Range: 200
- Fire Rate: 10
- Spread: 1 degree
- Pellet Count: 1

### Shotgun (Hitscan)
- Type: Hitscan
- Damage: 2
- Range: 50
- Fire Rate: 1.5
- Spread: 8 degrees
- Pellet Count: 8

### Rocket Launcher (Projectile)
- Type: Projectile
- Damage: 10
- Range: 100
- Fire Rate: 1
- Projectile Speed: 30
- Projectile Gravity: 9.8
- Projectile Lifetime: 5

### Sniper Rifle (Hitscan)
- Type: Hitscan
- Damage: 5
- Range: 500
- Fire Rate: 1
- Spread: 0 degrees
- Pellet Count: 1

## Controls

- **Fire**: Left Mouse Button / Touch (bottom 20% of screen)
- **Reload**: R key
- **Switch Weapon**: Mouse Scroll Wheel or Number Keys (1-9)

## Backward Compatibility

The system maintains backward compatibility:
- If `WeaponSystem` is not assigned, Player uses the legacy fire system
- Old weapon setup (ImpactPrefab, MuzzleParticle, etc.) still works

## Network Considerations

- Weapon switching is networked via `CurrentWeaponIndex`
- Fire count is synchronized for visual effects
- Projectiles are spawned as NetworkObjects
- All weapon logic runs on State Authority

## Ammo System (Optional)

To enable ammo:
1. Set **Max Ammo** > 0 in WeaponData
2. Set **Clip Size** (ammo per magazine)
3. Set **Reload Time**
4. Players can reload with R key

## Tips

- Use hitscan for fast, responsive weapons
- Use projectiles for slower, visible projectiles (rockets, grenades)
- Adjust spread for weapon balance
- Use pellet count > 1 for shotguns
- Projectile gravity creates arc trajectories

