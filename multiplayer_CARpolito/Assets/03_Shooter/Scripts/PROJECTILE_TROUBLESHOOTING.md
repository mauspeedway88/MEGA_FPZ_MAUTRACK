# Projectile Troubleshooting Guide

## Issue: Projectiles Not Being Created

If projectiles are not spawning when firing a projectile weapon, check the following:

## Checklist

### 1. Weapon Data Configuration

- [ ] **Projectile Prefab is assigned** in WeaponData asset
- [ ] **Weapon Type** is set to `Projectile` (not `Hitscan`)
- [ ] **Projectile Speed** > 0
- [ ] **Projectile Lifetime** > 0

### 2. Projectile Prefab Setup

The projectile prefab **MUST** have:

- [ ] **NetworkObject component** (required for Runner.Spawn)
- [ ] **Projectile script** component
- [ ] **Rigidbody component** (for physics movement)
- [ ] **Collider component** (for collision detection)
- [ ] **Visual mesh/model** (to see the projectile)

### 3. WeaponSystem Setup

- [ ] **WeaponSystem component** is on the Player
- [ ] **FirePoint** is assigned (usually CameraHandle transform)
- [ ] **HitMask** includes the layers you want to hit
- [ ] **AudioSource** is assigned (for sounds)
- [ ] **AvailableWeapons** array contains your weapon data
- [ ] **Current weapon** is set to a projectile weapon

### 4. Network Setup

- [ ] **NetworkRunner** is running
- [ ] **HasStateAuthority** is true (only state authority can spawn)
- [ ] Projectile prefab is in the **Network Prefabs** list

## Common Issues and Solutions

### Issue: "Projectile prefab not set for weapon"

**Solution:**
1. Open your WeaponData asset
2. Assign the Projectile Prefab in the "Projectile Settings" section

### Issue: "Projectile prefab must have a NetworkObject component"

**Solution:**
1. Open your projectile prefab
2. Add a **NetworkObject** component
3. Make sure it's enabled

### Issue: "Runner.Spawn returned null"

**Possible causes:**
- Projectile prefab not in Network Prefabs list
- NetworkRunner not running
- Not running on State Authority

**Solution:**
1. Add projectile prefab to NetworkRunner's Prefab List
2. Ensure NetworkRunner is started
3. Check that you're on State Authority (host/server)

### Issue: Projectile spawns but doesn't move

**Possible causes:**
- Rigidbody not assigned
- Speed is 0
- Gravity too high (pulls down immediately)

**Solution:**
1. Check Projectile script's Rigidbody reference is assigned
2. Set Projectile Speed > 0 in WeaponData
3. Adjust Projectile Gravity (0 = no gravity)

### Issue: Projectile spawns but disappears immediately

**Possible causes:**
- Lifetime too short
- Max distance too short
- Collision detection too sensitive

**Solution:**
1. Increase Projectile Lifetime in WeaponData
2. Increase Range/Max Distance
3. Check collision detection settings

## Debug Steps

1. **Check Console for Errors**
   - Look for `[WeaponSystem]` debug messages
   - Check for any red error messages

2. **Verify Weapon is Projectile Type**
   ```csharp
   // In WeaponSystem, check:
   CurrentWeapon.Type == WeaponData.WeaponType.Projectile
   ```

3. **Test with Debug Logs**
   - The code now logs when projectiles are spawned
   - Check console for: `[WeaponSystem] Spawned projectile at...`

4. **Check Network Prefabs**
   - Open NetworkRunner prefab
   - Verify projectile prefab is in the Prefab List
   - Make sure it's enabled

5. **Test in Single Player Mode**
   - Use single player mode to test (easier debugging)
   - Enable "Force Single Player" in UIGameMenu

## Example Projectile Prefab Setup

```
ProjectilePrefab (GameObject)
├── NetworkObject (Component) ✓
├── Projectile (Script) ✓
├── Rigidbody (Component) ✓
│   └── IsKinematic: false
│   └── UseGravity: false (we handle gravity in script)
├── SphereCollider (Component) ✓
│   └── IsTrigger: false (or true, both work)
└── Visual (MeshRenderer + MeshFilter or Model)
```

## Quick Test

1. Create a simple projectile prefab:
   - Add NetworkObject
   - Add Projectile script
   - Add Rigidbody
   - Add SphereCollider
   - Add a simple sphere mesh

2. Create WeaponData:
   - Type: Projectile
   - Assign the prefab
   - Set speed: 50
   - Set lifetime: 5

3. Assign to WeaponSystem:
   - Add to AvailableWeapons array
   - Switch to it (number key or scroll)

4. Fire and check console for debug messages

## Still Not Working?

If projectiles still don't spawn after checking all of the above:

1. **Check Unity Console** for specific error messages
2. **Verify NetworkRunner** is actually running
3. **Test with a simple hitscan weapon** first to ensure WeaponSystem works
4. **Check that FireProjectile is being called** (add breakpoint or debug log)

The code now includes extensive error checking and debug logging to help identify the issue.

