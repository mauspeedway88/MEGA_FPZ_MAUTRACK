# Projectile Collision Detection - How Chickens Get Detected

## Overview

The projectile system uses **multiple collision detection methods** to reliably detect chickens and other targets. This ensures that fast-moving projectiles don't miss targets, especially those with trigger colliders.

## Collision Detection Methods

### 1. Primary: Physics.SphereCast (Line 117)

**How it works:**
- Casts a sphere along the projectile's movement path
- Checks for collisions between current position and next position
- **Key Fix**: Uses `QueryTriggerInteraction.Collide` to hit BOTH trigger and non-trigger colliders

```csharp
Physics.SphereCast(currentPos, 0.2f, direction, out var hit, distance, _hitMask, QueryTriggerInteraction.Collide)
```

**Why this detects chickens:**
- Chickens have TWO colliders:
  - **Trigger collider** (radius 0.75) - for chicken's own collision detection
  - **Non-trigger collider** (radius 0.5) - for projectile hits
- `QueryTriggerInteraction.Collide` ensures both types are detected
- Sphere radius of 0.2f provides good detection area

### 2. Backup: Physics.OverlapSphere (Line 130)

**How it works:**
- Checks for any colliders overlapping at the projectile's current position
- Catches cases where the projectile is already inside a collider
- Finds the closest collider and creates a synthetic hit

**Why this is needed:**
- If a projectile spawns inside or very close to a chicken, SphereCast might miss it
- OverlapSphere catches these edge cases

### 3. Fallback: OnTriggerEnter (Line 165)

**How it works:**
- Unity's built-in trigger callback
- Only works if the projectile's collider is set as a trigger
- Creates a synthetic hit from the trigger collision

**When it activates:**
- If the projectile prefab has a trigger collider
- Provides backup detection if SphereCast/OverlapSphere miss

## Chicken Collision Setup

Chickens have the following collider setup (from Chicken.prefab):

1. **Trigger Collider** (SphereCollider)
   - Radius: 0.75
   - IsTrigger: true
   - Used for: Chicken's own collision detection (walls, etc.)

2. **Non-Trigger Collider** (SphereCollider)
   - Radius: 0.5
   - IsTrigger: false
   - Used for: Projectile hits, raycast detection

## Detection Flow

```
Projectile Movement
    ↓
CheckCollisions() called every FixedUpdateNetwork
    ↓
1. Physics.SphereCast (with QueryTriggerInteraction.Collide)
   ├─ Hits trigger collider? → OnHit()
   ├─ Hits non-trigger collider? → OnHit()
   └─ No hit? → Continue to step 2
    ↓
2. Physics.OverlapSphere (backup check)
   ├─ Finds overlapping colliders
   ├─ Selects closest one
   └─ Creates synthetic hit → OnHit()
    ↓
3. OnTriggerEnter (if projectile has trigger collider)
   ├─ Unity callback when entering trigger
   └─ Creates synthetic hit → OnHit()
```

## Key Improvements Made

1. **QueryTriggerInteraction.Collide**: Now explicitly checks trigger colliders
2. **Larger sphere radius**: Increased from 0.1f to 0.2f for better detection
3. **OverlapSphere backup**: Catches edge cases where projectile is inside collider
4. **Improved OnTriggerEnter**: Better handling of trigger collisions

## Important Notes

### For Projectile Prefab Setup:

**Option A: Non-Trigger Collider (Recommended)**
- Set projectile collider as **non-trigger**
- Relies on SphereCast and OverlapSphere
- More predictable physics behavior

**Option B: Trigger Collider**
- Set projectile collider as **trigger**
- Uses OnTriggerEnter as additional detection
- May have slight performance impact

### Hit Mask Configuration:

Make sure the WeaponSystem's `HitMask` includes the layer that chickens are on:
- Check chicken prefab's layer
- Ensure it's included in the HitMask
- Default layer (0) should work if not changed

## Troubleshooting

**If chickens aren't being hit:**

1. **Check HitMask**: Ensure chicken's layer is included
2. **Check Collider Setup**: Chicken should have at least one collider (trigger or non-trigger)
3. **Check Projectile Speed**: Very fast projectiles might need larger detection radius
4. **Check Network Authority**: Only State Authority processes collisions
5. **Check Collider Size**: Increase sphere cast radius if projectiles are small

## Performance Considerations

- SphereCast: Fast, efficient for most cases
- OverlapSphere: Slightly more expensive, only runs if SphereCast misses
- OnTriggerEnter: Unity callback, minimal overhead

The system is optimized to use the fastest method (SphereCast) first, with backups only when needed.

