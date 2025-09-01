# BallisticsManager Hit Marker Fix Plan

## Steps to Complete:

1. [x] Update BallisticsManager.cs to handle three marker types:
   - Hit markers (direct hits on target)
   - Near miss markers (shots close to target)
   - Miss markers (shots completely off target)

2. [x] Implement near miss detection logic:
   - Calculate distance from hit point to target center
   - Define near miss threshold based on target size
   - Handle different marker types in PlotOnHitBoard

3. [x] Add proper marker type parameter to PlotOnHitBoard method
4. [x] Enhance debug logging for better troubleshooting
5. [x] Fix direct target hits spawning proper UI markers instead of particle effects
6. [x] Fix NullReferenceException in PlotOnHitBoard when hit has no collider
7. [x] Implement PlotTargetHitAtCenter method for direct target hit plotting
8. [x] Test the updated hit detection system

## Current Status:
- BallisticsManager.cs has been updated with three-marker system
- Near miss detection implemented with distance-based threshold
- Enhanced debug logging for troubleshooting
- Fixed direct target hits to spawn UI markers at center of HitBoard
- Added PlotTargetHitAtCenter method for direct target hit plotting
- Fixed NullReferenceException by adding collider null check in PlotOnHitBoard
- Fixed UI positioning issues with proper anchor and pivot settings
- Successfully tested with target hits spawning markers at center

## Notes:
- Added MarkerType enum for hit, near miss, and miss markers
- Implemented DetermineMarkerType method for proximity-based detection
- Updated PlotOnHitBoard to accept marker type parameter
- Added targetTransform reference for near miss calculations
- Enhanced debug logs show marker type and positioning details
- Near miss threshold is configurable via inspector (default: 0.3f)
- Fixed UI positioning with proper anchorMin/Max and pivot settings
